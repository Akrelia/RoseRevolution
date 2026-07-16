using UnityEngine;
using UnityEditor;
using UnityRose.Formats;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using System.Collections;
using UnityRose;
using System.IO;
using System;
using System.Linq;
using System.Collections.Generic;
using static AddressableIndex;

/// <summary>
/// Rose Import Window.
/// </summary>
public class ROSEImportWindow : EditorWindow
{
    private const string DataPathKey = "ROSE_DataPath";

    private bool wasEditing = false;
    private int indexNPC = 0;
    private string dataPath = "";
    private Vector2 mapListScrollPosition;
    private bool mapListShowUnnamed = false;
    private ImportationSettings settings;

    /// <summary>
    /// Initialize.
    /// </summary>
    [MenuItem("ROSE Online/Data Importer")]
    static void Init()
    {
        var window = GetWindow<ROSEImportWindow>(true, "ROSE Data Import");
        window.Show();
    }

    private List<EquipmentItemData> BuildEquipmentItems(STB stb, STL stl, IDDatabase itemTypes, ItemType itemType, string path, int nullCheckColumnIndex, int iconColumnIndex, int categoryColumnIndex, int stringIDIndex)
    {
        AssetDatabase.StartAssetEditing();

        var items = new List<EquipmentItemData>();

        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        for (int i = 0; i < stb.Cells.Count; i++)
        {
            var cell = stb.Cells[i];

            if (string.IsNullOrEmpty(cell[nullCheckColumnIndex]))
                continue; // skip empty entries

            var category = string.IsNullOrEmpty(cell[categoryColumnIndex]) ? (short)0 : Convert.ToInt16(cell[categoryColumnIndex]);

            if (itemTypes.ids.ContainsKey(category) && string.IsNullOrEmpty(itemTypes.ids[category].meta))
            {
                itemTypes.ids[category].meta = itemType.ToString();
                EditorUtility.SetDirty(itemTypes);
                AssetDatabase.SaveAssets();
            }

            var data = CreateInstance<EquipmentItemData>();
            string name = stl.GetText(cell[stringIDIndex], STL.Language.English);
            string description = stl.GetComment(cell[stringIDIndex], STL.Language.English);

            data.itemName = name;
            data.description = description;
            data.id = i;
            data.iconID = string.IsNullOrEmpty(cell[iconColumnIndex]) ? (short)0 : Convert.ToInt16(cell[iconColumnIndex]);
            data.category = category;
            data.type = itemType;
            data.Labels.Add(itemType.ToString());
            data.Labels.Add(itemTypes.ids.ContainsKey(category) ? itemTypes.ids[category].dataName : "No category");

            items.Add(data);

            string safeName = string.IsNullOrEmpty(name) ? "Unnamed" : name;

            if (!string.IsNullOrEmpty(name))
            {
                AssetDatabase.CreateAsset(data, Path.Combine(path, $"({i}){safeName}.asset"));
            }
        }

        AssetDatabase.StopAssetEditing();
        AssetDatabase.SaveAssets();


        return items;
    }

    void BuildAllFromSettings(ImportationSettings settings)
    {
        var itemTypeSTL = new STL(Path.Combine(GetDataPath(), "3DDATA/STB/STR_ITEMTYPE.STL"));
        var itemTypes = BuildItemTypeDatabase(itemTypeSTL);

        AssetDatabase.CreateAsset(itemTypes, "Assets/Data/Items/Item Types.asset");

        foreach (var def in settings.filesToImport)
        {
            var stb = new STB(Path.Combine(GetDataPath(), def.stbPath));
            var stl = new STL(Path.Combine(GetDataPath(), def.stlPath));

            var assets = BuildEquipmentItems(stb, stl, itemTypes, def.type, def.outputFolder, def.nullCheckColumn, def.iconColumn, def.categoryColumn, stb.Cells[0].Count - 1);

            string prefix = $"item-{(int)def.type}";

            BuildAddressableDatabase(def.groupName, def.subgroupName, prefix, assets);
        }

        BuildAddressableDatabase("IDs", "Item Types", "item-type", new List<IDDatabase> { itemTypes });
    }

    private IDDatabase BuildItemTypeDatabase(STL itemTypeSTL)
    {
        var itemTypes = CreateInstance<IDDatabase>();
        itemTypes.ID = 1;
        itemTypes.DisplayName = "Item Types";

        for (int i = 0; i < itemTypeSTL.Entries.Count; i++)
        {
            var entry = itemTypeSTL.Entries[i];
            var data = new DataID(entry.ID, itemTypeSTL.GetText(entry.StringID, STL.Language.English), "");
            itemTypes.ids.Add(entry.ID, data);
        }

        return itemTypes;
    }

    /// <summary>
    /// Build Rose Data.
    /// </summary>
    private void BuildRoseData()
    {
        BuildAllFromSettings(settings);
    }

    /// <summary>
    /// Build an addressable database from a given list of assets.
    /// </summary>
    /// <param name="groupName">Group name.</param>
    /// <param name="prefix">Prefix.</param>
    /// <param name="data">List of assets to add to the group.</param>
    private void BuildAddressableDatabase<T>(string groupName, string subgroupName, string prefix, List<T> data) where T : UnityEngine.Object, IAddressableAsset
    {
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;

        if (settings == null)
        {
            Debug.LogError("AddressableAssetSettings not found. Please install and setup Addressables.");

            return;
        }

        AddressableAssetGroup group = settings.FindGroup(groupName);

        if (group == null)
        {
            group = settings.CreateGroup(groupName, false, false, false, null, typeof(BundledAssetGroupSchema));
        }

        string indexGroupName = "Indexes";
        AddressableAssetGroup indexGroup = settings.FindGroup(indexGroupName);

        if (indexGroup == null)
        {
            indexGroup = settings.CreateGroup(indexGroupName, false, false, false, null, typeof(BundledAssetGroupSchema));
        }

        string indexPath = $"Assets/Data/Index_{groupName}.asset";
        AddressableIndex index = AssetDatabase.LoadAssetAtPath<AddressableIndex>(indexPath);

        if (index == null)
        {
            index = CreateInstance<AddressableIndex>();
            AssetDatabase.CreateAsset(index, indexPath);
        }

        var groupEntry = new GroupEntry();

        groupEntry.groupName = subgroupName;
        groupEntry.addresses = new List<ItemIndexEntry>();

        AssetDatabase.StartAssetEditing();

        try
        {
            foreach (var asset in data)
            {
                if (asset == null)
                    continue;

                string assetPath = AssetDatabase.GetAssetPath(asset);
                if (string.IsNullOrEmpty(assetPath))
                    continue;

                string guid = AssetDatabase.AssetPathToGUID(assetPath);
                if (string.IsNullOrEmpty(guid))
                    continue;

                var existingEntry = settings.FindAssetEntry(guid);

                if (existingEntry != null)
                {
                    if (existingEntry.parentGroup != group)
                    {
                        settings.MoveEntry(existingEntry, group, false);
                    }
                }
                else
                {
                    var entry = settings.CreateOrMoveEntry(guid, group);
                    entry.address = prefix + "-" + asset.ID;

                    if (asset is IAddressableAsset labelSource && labelSource.Labels != null)
                    {
                        foreach (string label in labelSource.Labels)
                        {
                            if (!string.IsNullOrWhiteSpace(label))
                            {
                                if (!settings.GetLabels().Contains(label))
                                    settings.AddLabel(label);

                                entry.labels.Add(label); 
                            }
                        }
                    }
                }

                groupEntry.addresses.Add(new ItemIndexEntry(prefix + "-" + asset.ID, asset.DisplayName));
            }
        }

        catch (Exception ex)
        {
            Debug.Log("Issue while build addressables : " + ex.Message);
        }

        finally
        {
            AssetDatabase.StopAssetEditing();
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, group, true);
            AssetDatabase.SaveAssets();
        }

        index.groups.Add(groupEntry);

        if (index == null)
        {
            index = CreateInstance<AddressableIndex>();
            AssetDatabase.CreateAsset(index, indexPath); 
        }
        AssetDatabase.SaveAssets();

        Debug.Log($"Created AddressableGroupedIndex asset for group '{groupName}' at '{indexPath}' with {groupEntry.addresses.Count} entries.");

        string indexGuid = AssetDatabase.AssetPathToGUID(indexPath);

        if (!string.IsNullOrEmpty(indexGuid))
        {
            var existingIndexEntry = settings.FindAssetEntry(indexGuid);
            if (existingIndexEntry != null)
            {
                if (existingIndexEntry.parentGroup != indexGroup)
                {
                    settings.MoveEntry(existingIndexEntry, indexGroup, false);
                    Debug.Log($"Moved index asset '{index.name}' to group '{indexGroupName}'.");
                }
            }
            else
            {
                var newIndexEntry = settings.CreateOrMoveEntry(indexGuid, indexGroup);
                newIndexEntry.address = $"index-{groupName.ToLower()}";
                Debug.Log($"Added index asset '{index.name}' to group '{indexGroupName}' with address '{newIndexEntry.address}'.");
            }

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, indexGroup, true);
            AssetDatabase.SaveAssets();
        }
    }

    /// <summary>
    /// When drawer
    /// </summary>
    private void OnGUI()
    {
        GUILayout.Label("Settings", EditorStyles.boldLabel);
        dataPath = EditorGUILayout.TextField("Uncompressed VFS folder path", dataPath);

        GUILayout.Label("Importation Settings", EditorStyles.boldLabel);

        settings = (ImportationSettings)EditorGUILayout.ObjectField("Settings Asset", settings, typeof(ImportationSettings), false);

        if (GUILayout.Button("Open Addressables"))
        {
            EditorApplication.ExecuteMenuItem("Window/Asset Management/Addressables/Groups");
        }

        if (settings != null)
        {
            EditorGUILayout.LabelField("Items to Import:", settings.filesToImport.Count.ToString());

        }
        else
        {
            EditorGUILayout.HelpBox("Please assign an ImportationSettings asset.", MessageType.Info);
        }

        GUILayout.Label("Importing", EditorStyles.boldLabel);
        GUILayout.Label("Current Path: " + ROSEImport.GetCurrentPath());

        if (GUILayout.Button("Import 3DDATA"))
        {
            bool confirm = EditorUtility.DisplayDialog("Confirmation", "This will import every assets from the 3DDATA folder and create / convert every assets to Unity Format", "Yes", "No");

            if (confirm)
            {
                //   ResourceManager.Instance.GenerateAnimationAssets();

                // ROSEImport.ImportIcons();
                // ROSEImport.ImportSTBs();

                ROSEImport.CreatePlayerPrefabs();

                //BuildRoseData();
            }
        }

        if (GUILayout.Button("Import ALL NPC"))
        {
            bool confirm = EditorUtility.DisplayDialog("Confirmation", "This will take a lot of time, are you sure ?", "Yes", "No");

            if (confirm)
            {

                ROSEImport.ImportAllNPC();
            }
        }

        GUILayout.BeginHorizontal();

        indexNPC = EditorGUILayout.IntField("NPC ID", indexNPC);

        if (GUILayout.Button("Import NPC"))
        {
            ROSEImport.ImportNPC(indexNPC);
        }

        GUILayout.EndHorizontal();

        if (GUILayout.Button("Clear ROSE Data"))
            ROSEImport.ClearData();

        if (GUILayout.Button("Import PTL / EFT (Debug)"))
            ROSEImport.ImportParticles();

        GUILayout.Label("Maps", EditorStyles.boldLabel);

        mapListShowUnnamed = GUILayout.Toggle(mapListShowUnnamed, "Show Unnamed Maps");

        var mapData = ROSEImport.GetMapListData();
        if (mapData != null)
        {
            mapListScrollPosition = GUILayout.BeginScrollView(mapListScrollPosition, GUILayout.Height(400));
            for (var i = 0; i < mapData.stb.Cells.Count; ++i)
            {
                string mapName = mapData.stl.GetText(mapData.stb.Cells[i][27], STL.Language.English);
                if (mapName != null || mapListShowUnnamed)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("[" + i.ToString() + "] " + mapName);

                    if (GUILayout.Button("Import", GUILayout.Width(100)))
                    {
                        RoseTerrainWindow.ImportMap(i);
                    }

                    if (GUILayout.Button("Export Spawns", GUILayout.Width(100)))
                    {
                        RoseTerrainWindow.ExportSpawns(i);
                    }

                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndScrollView();
        }

        if (EditorGUIUtility.editingTextField)
        {
            wasEditing = true;
        }
        else
        {
            if (wasEditing)
            {
                wasEditing = false;
                EditorPrefs.SetString(DataPathKey, dataPath);
                ROSEImport.MaybeUpdate();
            }
        }
    }

    /// <summary>
    /// On focus.
    /// </summary>
    private void OnFocus()
    {
        if (EditorPrefs.HasKey(DataPathKey))
        {
            dataPath = EditorPrefs.GetString(DataPathKey);
        }

        ROSEImport.MaybeUpdate();
    }

    /// <summary>
    /// On lost focus.
    /// </summary>
    private void OnLostFocus()
    {
        EditorPrefs.SetString(DataPathKey, dataPath);
        ROSEImport.MaybeUpdate();
    }

    /// <summary>
    /// On destroy.
    /// </summary>
    private void OnDestroy()
    {
        EditorPrefs.SetString(DataPathKey, dataPath);
        ROSEImport.MaybeUpdate();
    }

    /// <summary>
    /// Get data path.
    /// </summary>
    /// <returns>Data path.</returns>
    public static string GetDataPath()
    {
        return EditorPrefs.GetString(DataPathKey);
    }
}