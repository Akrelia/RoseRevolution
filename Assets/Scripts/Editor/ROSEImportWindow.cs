#if UNITY_EDITOR


using UnityEngine;
using UnityEditor;
using UnityRose.Formats;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityRose;
using UnityRose.Import;
using System.IO;
using System;
using System.Collections.Generic;
using static AddressableIndex;
using System.Text;
using UnityRose.Game;
using System.Linq;
using static UnityRose.Formats.ZON;
using static RevolutionShared.Rose.Data.RoseEnums;

namespace UnityRose.ImportEditor
{
    /// <summary>
    /// Rose Import Window.
    /// </summary>
    public class ROSEImportWindow : EditorWindow
    {
        private bool wasEditing = false;
        private int indexNPC = 0;
        private string dataPath = "";
        private Vector2 mapListScrollPosition;
        private bool mapListShowUnnamed = false;
        private ImportationSettings settings;

        [MenuItem("ROSE Online/Data Importer")]
        static void Init()
        {
            var window = GetWindow<ROSEImportWindow>(true, "ROSE Data Import");
            window.Show();
        }

        /// <summary>
        /// Builds the equipment items from the given STB and STL files and creates assets for them.
        /// </summary>
        /// <param name="stb"></param>
        /// <param name="stl"></param>
        /// <param name="itemTypes"></param>
        /// <param name="itemType"></param>
        /// <param name="path"></param>
        /// <param name="nullCheckColumnIndex"></param>
        /// <param name="iconColumnIndex"></param>
        /// <param name="categoryColumnIndex"></param>
        /// <param name="stringIDIndex"></param>
        /// <returns></returns>
        private List<EquipmentItemData> BuildEquipmentItems(STB stb, STL stl, IDDatabase itemTypes, ItemType itemType, string path, int nullCheckColumnIndex, int iconColumnIndex, int categoryColumnIndex, int stringIDIndex)
        {
            AssetDatabase.StartAssetEditing();

            var items = new List<EquipmentItemData>();

            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            for (int i = 0; i < stb.Cells.Count; i++)
            {
                var cell = stb.Cells[i];

                if (string.IsNullOrEmpty(cell[nullCheckColumnIndex])) continue;

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

                if (!string.IsNullOrEmpty(name))
                {
                    AssetDatabase.CreateAsset(data, Path.Combine(path, $"({i}){name}.asset"));
                }
            }

            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();

            return items;
        }

        /// <summary>
        /// Builds all assets from the given importation settings, including item types and equipment items, and creates addressable databases for them.
        /// </summary>
        /// <param name="settings"></param>
        private void BuildAllFromSettings(ImportationSettings settings)
        {
            var itemTypeSTL = new STL(Path.Combine(ROSEEditorBaker.DataPath, "3DDATA/STB/STR_ITEMTYPE.STL"));
            var itemTypes = BuildItemTypeDatabase(itemTypeSTL);
            AssetDatabase.CreateAsset(itemTypes, "Assets/Data/Items/Item Types.asset");

            foreach (var def in settings.filesToImport)
            {
                var stb = new STB(Path.Combine(ROSEEditorBaker.DataPath, def.stbPath));
                var stl = new STL(Path.Combine(ROSEEditorBaker.DataPath, def.stlPath));

                var assets = BuildEquipmentItems(stb, stl, itemTypes, def.type, def.outputFolder,
                    def.nullCheckColumn, def.iconColumn, def.categoryColumn, stb.Cells[0].Count - 1);

                BuildAddressableDatabase(def.groupName, def.subgroupName, $"item-{(int)def.type}", assets);
            }

            BuildAddressableDatabase("IDs", "Item Types", "item-type", new List<IDDatabase> { itemTypes });
        }

        /// <summary>
        /// Builds the item type database from the given STL file and creates an asset for it.
        /// </summary>
        /// <param name="itemTypeSTL"></param>
        /// <returns></returns>
        private IDDatabase BuildItemTypeDatabase(STL itemTypeSTL)
        {
            var itemTypes = CreateInstance<IDDatabase>();
            itemTypes.ID = 1;
            itemTypes.DisplayName = "Item Types";

            foreach (var entry in itemTypeSTL.Entries)
            {
                var data = new DataID(entry.ID, itemTypeSTL.GetText(entry.StringID, STL.Language.English), "");
                itemTypes.ids.Add(entry.ID, data);
            }
            return itemTypes;
        }

        /// <summary>
        /// Builds the addressable database for the given group and subgroup names, prefix, and list of assets, and creates an index asset for it.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="groupName"></param>
        /// <param name="subgroupName"></param>
        /// <param name="prefix"></param>
        /// <param name="data"></param>
        private void BuildAddressableDatabase<T>(string groupName, string subgroupName, string prefix, List<T> data)
            where T : UnityEngine.Object, IAddressableAsset
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("AddressableAssetSettings not found. Please install and setup Addressables.");
                return;
            }

            var group = settings.FindGroup(groupName) ?? settings.CreateGroup(groupName, false, false, false, null, typeof(BundledAssetGroupSchema));
            var indexGroup = settings.FindGroup("Indexes") ?? settings.CreateGroup("Indexes", false, false, false, null, typeof(BundledAssetGroupSchema));

            var indexPath = $"Assets/Data/Index_{groupName}.asset";
            var index = AssetDatabase.LoadAssetAtPath<AddressableIndex>(indexPath);
            if (index == null)
            {
                index = CreateInstance<AddressableIndex>();
                AssetDatabase.CreateAsset(index, indexPath);
            }

            var groupEntry = new GroupEntry { groupName = subgroupName, addresses = new List<ItemIndexEntry>() };

            AssetDatabase.StartAssetEditing();

            try
            {
                foreach (var asset in data)
                {
                    if (asset == null) continue;
                    var assetPath = AssetDatabase.GetAssetPath(asset);

                    if (string.IsNullOrEmpty(assetPath)) continue;

                    var guid = AssetDatabase.AssetPathToGUID(assetPath);

                    if (string.IsNullOrEmpty(guid)) continue;

                    var existingEntry = settings.FindAssetEntry(guid);

                    if (existingEntry != null)
                    {
                        if (existingEntry.parentGroup != group)
                            settings.MoveEntry(existingEntry, group, false);
                    }

                    else
                    {
                        var entry = settings.CreateOrMoveEntry(guid, group);
                        entry.address = prefix + "-" + asset.ID;

                        if (asset is IAddressableAsset labelSource && labelSource.Labels != null)
                        {
                            foreach (var label in labelSource.Labels)
                            {
                                if (string.IsNullOrWhiteSpace(label)) continue;
                                if (!settings.GetLabels().Contains(label)) settings.AddLabel(label);
                                entry.labels.Add(label);
                            }
                        }
                    }

                    groupEntry.addresses.Add(new ItemIndexEntry(prefix + "-" + asset.ID, asset.DisplayName));
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("Issue while building addressables: " + ex.Message);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, group, true);
                AssetDatabase.SaveAssets();
            }

            index.groups.Add(groupEntry);
            AssetDatabase.SaveAssets();

            var indexGuid = AssetDatabase.AssetPathToGUID(indexPath);
            if (!string.IsNullOrEmpty(indexGuid))
            {
                var existingIndexEntry = settings.FindAssetEntry(indexGuid);
                if (existingIndexEntry != null)
                {
                    if (existingIndexEntry.parentGroup != indexGroup)
                        settings.MoveEntry(existingIndexEntry, indexGroup, false);
                }
                else
                {
                    var newIndexEntry = settings.CreateOrMoveEntry(indexGuid, indexGroup);
                    newIndexEntry.address = $"index-{groupName.ToLower()}";
                }
                settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, indexGroup, true);
                AssetDatabase.SaveAssets();
            }
        }

        /// <summary>
        /// Import all maps.
        /// </summary>
        public void ImportAllMaps()
        {
            var mapData = ROSEMapListCache.Get();

            if (mapData == null)
                return;

            string folder = "Assets/Data/Maps";

            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder("Assets/Data", "Maps");
            }

            string dbPath = $"{folder}/RoseMapDatabase.asset";

            var database = AssetDatabase.LoadAssetAtPath<RoseMapDatabase>(dbPath);

            if (database == null)
            {
                database = CreateInstance<RoseMapDatabase>();

                AssetDatabase.CreateAsset(database, dbPath);
            }

            database.maps.Clear();

            AssetDatabase.StartAssetEditing();

            try
            {
                for (int i = 0; i < mapData.stb.Cells.Count; i++)
                {
                    string name = mapData.stl.GetText(mapData.stb.Cells[i][27], STL.Language.English);

                    if (string.IsNullOrEmpty(name))
                    {
                        name = "Map_" + i;
                    }

                    Debug.Log($"Importing map {i} : {name}");

                    RoseMapImporter.ImportMap(i);

                    string prefabPath = $"Assets/Data/Rose/{name}.prefab";

                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

                    if (prefab == null)
                    {
                        Debug.LogWarning($"Prefab not found for {name}");

                        continue;
                    }

                    database.maps.Add(new RoseMapEntry
                    {
                        id = i,
                        name = name,
                        prefab = prefab
                    });
                }
            }

            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Register a map in the internal database.
        /// </summary>
        /// <param name="id">ID.</param>
        private void RegisterMapInInternalDB(int id)
        {
            var mapData = ROSEMapListCache.Get();

            string folder = "Assets/Data/Databases";

            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder("Assets/Data", "Databases");
            }

            string path = $"{folder}/RoseMapDatabase.asset";

            var database = AssetDatabase.LoadAssetAtPath<RoseMapDatabase>(path);

            if (database == null)
            {
                database = CreateInstance<RoseMapDatabase>();

                AssetDatabase.CreateAsset(database, path);
            }

            string displayName = mapData.stl.GetText(mapData.stb.Cells[id][27], STL.Language.English);

            string prefabName = mapData.stb.Cells[id][1];

            if (string.IsNullOrEmpty(name))
            {
                name = "Map_" + id;
            }

            string prefabPath = $"Assets/Prefabs/Rose/{prefabName}.prefab";

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (prefab == null)
            {
                Debug.LogWarning($"Prefab not found for map {id} ({name})");

                return;
            }

            var roseMap = prefab.GetComponent<RoseMap>();

            var existing = database.maps.Find(x => x.id == id);

            if (existing != null)
            {
                existing.name = displayName;
                existing.prefab = prefab;
                existing.spawnPoints = roseMap != null ? roseMap.spawns : new List<SpawnData>();
            }

            else
            {
                database.maps.Add(new RoseMapEntry
                {
                    id = id,
                    name = displayName,
                    prefab = prefab,
                    spawnPoints = roseMap != null ? roseMap.spawns : new List<SpawnData>()
                });
            }

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
        }

        private void RegisterSpawnInInternalDB(int mapID)
        {
            var database = LoadMonsterSpawnDatabase();

            if (database == null)
            {
                database = CreateInstance<RoseMonsterSpawnDatabase>();
                AssetDatabase.CreateAsset(database, ROSEDatabaseWindow.MonsterSpawnDatabasePath);
            }

            database.maps.RemoveAll(x => x.MapID == mapID);

            MapSpawnData spawnData = new MapSpawnData();

            database.maps.Add(BuildSpawnData(mapID));

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
        }

        private void RegisterNpcInInternalDB(GameObject prefab, RoseNPCInfos npc)
        {
            string folder = "Assets/Data/Databases";

            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder("Assets/Data", "Databases");
            }

            string path = $"{folder}/RoseNpcDatabase.asset";

            var database = AssetDatabase.LoadAssetAtPath<RoseNPCDatabase>(path);

            if (database == null)
            {
                database = ScriptableObject.CreateInstance<RoseNPCDatabase>();

                AssetDatabase.CreateAsset(database, path);
            }

            var existing = database.npcs.Find(x => x.id == npc.id);

            if (existing != null)
            {
                existing.name = npc.npcName;
                existing.prefab = prefab;
                existing.data = npc;
            }

            else
            {
                database.npcs.Add(new RoseNPCEntry
                {
                    id = npc.id,
                    name = npc.npcName,
                    prefab = prefab,
                    data = npc
                });
            }


            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
        }

        public static MapSpawnData BuildSpawnData(int mapID)
        {
            var stb = ResourceManager.Instance.stb_zone;
            var stbNPC = ResourceManager.Instance.stb_npc_list;

            var mapDirectory = Path.Combine(RoseDataSource.DataPath, Path.GetDirectoryName(Utils.FixPath(stb.Cells[mapID][2].ToString())));

            var dirs = new DirectoryInfo(mapDirectory);
            var patches = new List<RosePatch>();

            foreach (var dir in dirs.GetDirectories())
            {
                if (dir.Name.Contains(".")) continue;

                var patch = new RosePatch(dir);
                var valid = patch.Load(mapID);

                if (valid)
                patches.Add(patch);
            }


            var data = new MapSpawnData
            {
                MapID = mapID,
                MapName = stb.Cells[mapID][1].ToString(),
                Spawns = new List<MonsterSpawnData>()
            };

            foreach (var patch in patches)
            {
                foreach (var monster in patch.m_IFO.Monsters)
                {
                    var spawn = new MonsterSpawnData
                    {
                        Name = monster.Name,
                        MapX = monster.MapPosition.x,
                        MapY = monster.MapPosition.y,
                        ID = monster.ObjectID,
                        WorldX = (monster.Position.x + 520000.0f) / 100F,
                        WorldY = (monster.Position.y + 520000.0f) / 100F,
                        WorldZ = monster.Position.z / -10000F,
                        Interval = monster.Interval,
                        LimitCount = monster.Limit,
                        Range = monster.Range,
                        TacticPoints = monster.TacticPoints,
                        Basic = new List<MonsterEntryData>(),
                        Tactic = new List<MonsterEntryData>()
                    };

                    foreach (var b in monster.Basic)
                    {
                        spawn.Basic.Add(new MonsterEntryData
                        {
                            ID = b.ID,
                            Count = b.Count,
                            Description = stbNPC.Cells[b.ID][1].ToString()
                        });
                    }

                    foreach (var t in monster.Tactic)
                    {
                        spawn.Tactic.Add(new MonsterEntryData
                        {
                            ID = t.ID,
                            Count = t.Count,
                            Description = stbNPC.Cells[t.ID][1].ToString()
                        });
                    }

                    data.Spawns.Add(spawn);
                }
            }

            return data;
        }

        /// <summary>
        /// Exports the spawns for the specified map ID to a JSON file.
        /// </summary>
        /// <param name="mapID">Map ID.</param>
        public static void ExportSpawns(int mapID)
        {
            var stb = ResourceManager.Instance.stb_zone;

            var path = EditorUtility.SaveFilePanel("Export Spawns", Application.dataPath, stb.Cells[mapID][1].ToString(), "json");

            if (!string.IsNullOrEmpty(path))
            {
                Debug.Log("Exporting spawns for map ID " + mapID + "...");

                var json = RoseMapImporter.BuildSpawnExportJson(mapID);

                System.IO.File.WriteAllText(path, json, Encoding.Unicode);

                Debug.Log("Export spawns for map ID " + mapID + " done!");
            }
        }

        /// <summary>
        /// Loads the RoseMapDatabase asset.
        /// </summary>
        /// <returns>Map database.</returns>
        private RoseMapDatabase LoadMapDatabase()
        {
            return AssetDatabase.LoadAssetAtPath<RoseMapDatabase>(ROSEDatabaseWindow.MapDatabasePath);
        }

        /// <summary>
        /// Loads the RoseMapDatabase asset.
        /// </summary>
        /// <returns>Map database.</returns>
        private RoseMonsterSpawnDatabase LoadMonsterSpawnDatabase()
        {
            return AssetDatabase.LoadAssetAtPath<RoseMonsterSpawnDatabase>(ROSEDatabaseWindow.MonsterSpawnDatabasePath);
        }

        private bool MapPrefabExists(int mapID)
        {
            var mapData = ROSEMapListCache.Get();

            string mapName = Path.GetFileNameWithoutExtension(Utils.FixPath(mapData.stb.Cells[mapID][1].ToString()));

            string prefabPath = $"Assets/Prefabs/Rose/{mapName}.prefab";

            return AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null;
        }

        /// <summary>
        /// Draws the GUI for the ROSE Import Window.
        /// </summary>
        private void OnGUI()
        {
            var centeredStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };

            var mapDatabase = LoadMapDatabase();
            var spawnDatabase = LoadMonsterSpawnDatabase();

            GUILayout.Label("Settings", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();

            dataPath = EditorGUILayout.TextField("Uncompressed VFS folder path", dataPath);

            if (GUILayout.Button("Browse", GUILayout.Width(80)))
            {
                string path = EditorUtility.OpenFolderPanel("Select ROSE VFS folder", dataPath, "");

                if (!string.IsNullOrEmpty(path))
                {
                    dataPath = path;
                    ROSEEditorBaker.DataPath = dataPath;
                    ROSEMapListCache.MaybeUpdate();
                }
            }

            GUILayout.EndHorizontal();

            /*    GUILayout.Label("Importation Settings", EditorStyles.boldLabel);
                settings = (ImportationSettings)EditorGUILayout.ObjectField("Settings Asset", settings, typeof(ImportationSettings), false);

                if (GUILayout.Button("Open Addressables"))
                    EditorApplication.ExecuteMenuItem("Window/Asset Management/Addressables/Groups");

                if (settings != null)
                {
                    EditorGUILayout.LabelField("Items to Import:", settings.filesToImport.Count.ToString());
                }

                else
                {
                    EditorGUILayout.HelpBox("Please assign an ImportationSettings asset.", MessageType.Info);
                }

                */

            GUILayout.Label("Importing", EditorStyles.boldLabel);
            GUILayout.Label("Current Path: " + ROSEEditorBaker.DataPath);

            if (GUILayout.Button("Import Characters"))
            {
                if (EditorUtility.DisplayDialog("Confirmation", "This will import every assets for Characters (animations, meshs)", "Yes", "No"))
                {
                    ROSEEditorBaker.CreatePlayerPrefabs();
                }
            }

            if (GUILayout.Button("Import ALL Maps"))
            {
                if (EditorUtility.DisplayDialog("Import ALL Maps", "This will import every ROSE map and rebuild the map database.", "Yes", "No"))
                {
                    ImportAllMaps();
                }
            }

            /*
            if (GUILayout.Button("Import ALL NPC"))
            {
                if (EditorUtility.DisplayDialog("Confirmation", "This will take a lot of time, are you sure ?", "Yes", "No"))
                {
                    ROSEEditorBaker.ImportAllNPC();
                }
            }

            */


            GUILayout.BeginHorizontal();

            indexNPC = EditorGUILayout.IntField("NPC ID", indexNPC);

            if (GUILayout.Button("Import NPC"))
            {
                var npc = ROSEEditorBaker.ImportNPC(indexNPC);

                if (npc)
                {
                    RegisterNpcInInternalDB(npc, npc.GetComponent<RoseNpc>().data);
                }

                else
                {
                    Debug.Log($"Failed to import NPC with ID {indexNPC}");
                }
            }

            GUILayout.EndHorizontal();

            if (GUILayout.Button("Clear ROSE Data"))
            {
            //    ROSEEditorBaker.ClearData();
            }

            GUILayout.Label("Maps", EditorStyles.boldLabel);

            mapListShowUnnamed = GUILayout.Toggle(mapListShowUnnamed, "Show Unnamed Maps");

            var mapData = ROSEMapListCache.Get();

            GUILayout.BeginHorizontal();

            if (mapData != null)
            {

                GUILayout.Label("Name", GUILayout.ExpandWidth(true));
                GUILayout.Label("Prefab", centeredStyle, GUILayout.Width(50));
                GUILayout.Label("Data", centeredStyle, GUILayout.Width(50));
                GUILayout.Label("Spawns", centeredStyle, GUILayout.Width(80));
                GUILayout.Label("Action", GUILayout.Width(100));
                GUILayout.EndHorizontal();

                mapListScrollPosition = GUILayout.BeginScrollView(mapListScrollPosition, GUILayout.Height(400));

                for (var i = 0; i < mapData.stb.Cells.Count; ++i)
                {
                    string mapName = mapData.stl.GetText(mapData.stb.Cells[i][27], STL.Language.English);

                    if (mapName == null && !mapListShowUnnamed)
                        continue;

                    bool mapDataExist = mapDatabase != null && mapDatabase.maps.Any(x => x.id == i);
                    bool spawnsExist = spawnDatabase != null && spawnDatabase.maps.Any(x => x.MapID == i);

                    GUILayout.BeginHorizontal();

                    GUILayout.Label($"[{i}] {mapName}", GUILayout.ExpandWidth(true));

                    GUILayout.Label(MapPrefabExists(i) ? "✔️" : "", centeredStyle, GUILayout.Width(50));

                    GUILayout.Label(mapDataExist ? "✔️" : "", centeredStyle, GUILayout.Width(50));

                    GUILayout.Label(spawnsExist ? "✔️" : "", centeredStyle, GUILayout.Width(80));

                    if (GUILayout.Button("Import", GUILayout.Width(100)))
                    {
                        var map = RoseMapImporter.ImportMap(i);

                        RegisterMapInInternalDB(i);
                        RegisterSpawnInInternalDB(i);
                    }


                    GUILayout.EndHorizontal();
                }

                GUILayout.EndScrollView();
            }

            if (EditorGUIUtility.editingTextField)
            {
                wasEditing = true;
            }

            else if (wasEditing)
            {
                wasEditing = false;

                ROSEEditorBaker.DataPath = dataPath;

                ROSEMapListCache.MaybeUpdate();
            }
        }

        private void OnEnable()
        {
            SyncDataPath();
        }

        private void OnFocus()
        {
            SyncDataPath();
            ROSEMapListCache.MaybeUpdate();
        }

        private void OnLostFocus()
        {
            ROSEEditorBaker.DataPath = dataPath;
            ROSEMapListCache.MaybeUpdate();
        }

        private void OnDestroy()
        {
            ROSEEditorBaker.DataPath = dataPath;
            ROSEMapListCache.MaybeUpdate();
        }

        private void SyncDataPath()
        {
            dataPath = EditorPrefs.GetString("ROSE_DataPath");
            ROSEEditorBaker.DataPath = dataPath;
        }
    }

    public static class ROSEMapListCache
    {
        public class MapListData
        {
            public STB stb;
            public STL stl;
        }

        private static MapListData _data;
        private static string _lastPath = "";

        public static MapListData Get() => _data;

        public static void MaybeUpdate()
        {
            var current = ROSEEditorBaker.DataPath;
            if (current == _lastPath) return;
            _lastPath = current;

            _data = new MapListData
            {
                stb = new STB(Utils.CombinePath(current, "3DDATA/STB/LIST_ZONE.STB")),
                stl = new STL(Utils.CombinePath(current, "3DDATA/STB/LIST_ZONE_S.STL"))
            };
        }
    }
}
#endif