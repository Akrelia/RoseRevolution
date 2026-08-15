using UnityEngine;
using UnityEditor;
using UnityRose.Formats;
using UnityRose.Import;
using System.IO;
using System;
using System.Collections.Generic;
using UnityRose.Game;
using System.Linq;
using RevolutionShared.Rose.Data;
using RevolutionShared.Rose.Data.Equipment;
using UnityEngine.AddressableAssets;

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

        private MapDatabase mapDatabase;
        private MonsterSpawnDatabase spawnDatabase;

        private GUIStyle centeredStyle;

        [MenuItem("ROSE Online/Data Importer")]
        private static void Init()
        {
            var window = GetWindow<ROSEImportWindow>(true, "ROSE Data Import");

            window.Show();
        }

        /// <summary>
        /// When enabled.
        /// </summary>
        private void OnEnable()
        {
            settings = Addressables.LoadAssetAsync<ImportationSettings>("DefaultImportSettings").WaitForCompletion();

            if (settings == null)
            {
                Debug.LogError("Default import settings is missing, please set one or create one");
            }

            mapDatabase = AssetDatabase.LoadAssetAtPath<MapDatabase>(ROSEExportWindow.MapDatabasePath);
            spawnDatabase = AssetDatabase.LoadAssetAtPath<MonsterSpawnDatabase>(ROSEExportWindow.MonsterSpawnDatabasePath);

            SyncDataPath();
        }

        /// <summary>
        /// Draws the GUI for the ROSE Import Window.
        /// </summary>
        private void OnGUI()
        {
            if (centeredStyle == null)
            {
                centeredStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter };
            }

            GUILayout.Label("Settings", EditorStyles.boldLabel);
            settings = (ImportationSettings)EditorGUILayout.ObjectField("Settings", settings, typeof(ImportationSettings), false);

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

            GUILayout.Label("Importing", EditorStyles.boldLabel);
            GUILayout.Label("Current Path: " + ROSEEditorBaker.DataPath);

            if (GUILayout.Button("Import Player Skeletons"))
            {
                if (EditorUtility.DisplayDialog("Confirmation", "This will import every assets for Characters (animations, meshs)", "Yes", "No"))
                {
                    RoseCharacterImporter.ImportSkeletons();
                }
            }

            if (GUILayout.Button("Import ALL Equipments"))
            {
                if (EditorUtility.DisplayDialog("Confirmation", "This will bake every body/armor/weapon/etc. ZSC into prefabs and rebuild the Avatar database.", "Yes", "No"))
                {
                    RoseEquipmentImporter.ImportAllEquipment(170, settings.playerShader);
                }
            }

            if (GUILayout.Button("Import ALL Maps (Disabled)"))
            {
                if (EditorUtility.DisplayDialog("Confirmation", "This will import every ROSE map and rebuild the map database.", "Yes", "No"))
                {

                }
            }

            if (GUILayout.Button("Import Drop Tables"))
            {
                if (EditorUtility.DisplayDialog("Confirmation", "This will import every drop tables.", "Yes", "No"))
                {
                    RoseImporter.ImportDropTables();
                }
            }

            if (GUILayout.Button("Import Skyboxes"))
            {
                RoseImporter.ImportSkyboxes();
            }

            if (GUILayout.Button("Import Icons"))
            {
                RoseImporter.ImportIcons();
            }

            if (GUILayout.Button("Import Effects"))
            {
                if (EditorUtility.DisplayDialog("Confirmation", "This will import every particles / effects", "Yes", "No"))
                {
                    RoseEffectImporter.ImportEffects(dataPath, settings.effectShader);
                }
            }

            GUILayout.BeginHorizontal();

            indexNPC = EditorGUILayout.IntField("NPC ID", indexNPC);

            if (GUILayout.Button("Import NPC"))
            {
                RoseNPCImporter.ImportNPC(indexNPC, settings.entityShader);
            }

            GUILayout.EndHorizontal();

            if (GUILayout.Button("Clear ROSE Data"))
            {
                if (EditorUtility.DisplayDialog("Confirmation", "This will delete ALL imported data and you'll have to reimport everything", "Yes", "No"))
                {
                    ROSEEditorBaker.ClearData();
                }
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
                    bool spawnsExist = spawnDatabase != null && spawnDatabase.spawns.Any(x => x != null && x.spawnData != null && x.spawnData.ID == i);

                    GUILayout.BeginHorizontal();

                    GUILayout.Label($"[{i}] {mapName}", GUILayout.ExpandWidth(true));

                    GUILayout.Label(ROSEMapListCache.MapPrefabExists(i) ? "✔️" : "", centeredStyle, GUILayout.Width(50));

                    GUILayout.Label(mapDataExist ? "✔️" : "", centeredStyle, GUILayout.Width(50));

                    GUILayout.Label(spawnsExist ? "✔️" : "", centeredStyle, GUILayout.Width(80));

                    if (GUILayout.Button("Import", GUILayout.Width(100)))
                    {
                        var map = RoseMapImporter.ImportFullMap(i, settings);
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

        /// <summary>
        /// When focused.
        /// </summary>
        private void OnFocus()
        {
            SyncDataPath();

             ROSEMapListCache.MaybeUpdate();
        }

        /// <summary>
        /// When lost focus.
        /// </summary>
        private void OnLostFocus()
        {
            SyncDataPath();

             ROSEMapListCache.MaybeUpdate();
        }

        /// <summary>
        /// When destroyed.
        /// </summary>
        private void OnDestroy()
        {
            SyncDataPath();

            ROSEMapListCache.MaybeUpdate();
        }

        /// <summary>
        /// Syncs the data path with the editor preferences and updates the ROSEEditorBaker's data path.
        /// </summary>
        private void SyncDataPath()
        {
            dataPath = EditorPrefs.GetString("ROSE_DataPath");

            ROSEEditorBaker.DataPath = dataPath;
        }
    }
}