using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
using UnityEngine.AddressableAssets;
using Codice.Client.BaseCommands;
using UnityRose.Import;

namespace UnityRose.ImportEditor
{
    /// <summary>
    /// ROse Database Window.
    /// </summary>
    public class ROSEExportWindow : EditorWindow
    {
        private Vector2 scroll;
        private NPCDatabase npcDatabase;
        private MapDatabase mapDatabase;
        private DropTableDatabase dropTableDatabase;
        private MonsterSpawnDatabase spawnDatabase;

        /// <summary>
        /// Open the window.
        /// </summary>
        [MenuItem("ROSE Online/Data Exporter")]
        public static void Open()
        {
            var window = GetWindow<ROSEExportWindow>("ROSE Exporter");

            window.minSize = new Vector2(900, 600);
            window.position = new Rect(window.position.x, window.position.y, 900, 600);
        }

        /// <summary>
        /// When enabled.
        /// </summary>
        private void OnEnable()
        {
            LoadDatabases();
        }

        /// <summary>
        /// Load every databases.
        /// </summary>
        private void LoadDatabases()
        {
            mapDatabase = Addressables.LoadAssetAsync<MapDatabase>(nameof(MapDatabase)).WaitForCompletion();
            spawnDatabase = Addressables.LoadAssetAsync<MonsterSpawnDatabase>(nameof(MonsterSpawnDatabase)).WaitForCompletion();
            npcDatabase = Addressables.LoadAssetAsync<NPCDatabase>(nameof(NPCDatabase)).WaitForCompletion();
            dropTableDatabase = Addressables.LoadAssetAsync<DropTableDatabase>(nameof(DropTableDatabase)).WaitForCompletion();
        }

        /// <summary>
        /// Drawing the GUI.
        /// </summary>
        private void OnGUI()
        {
            if (mapDatabase == null)
            {
                EditorGUILayout.HelpBox("Map Database not found.", MessageType.Warning);

                if (GUILayout.Button("Reload"))
                {
                    LoadDatabases();
                }

                return;
            }

            GUILayout.Label("ROSE Databases", EditorStyles.boldLabel);

            GUILayout.Space(5);

            EditorGUILayout.LabelField("Maps", mapDatabase.maps.Count.ToString());

            GUILayout.Space(10);

            DrawMapHeader();

            scroll = GUILayout.BeginScrollView(scroll);

            foreach (var map in mapDatabase.maps.OrderBy(x => x.id))
            {
                DrawMapRow(map);
            }

            GUILayout.EndScrollView();

            GUILayout.Space(10);

            if (GUILayout.Button("Export Maps Data ..."))
            {
                ExportMaps();
            }

            if (GUILayout.Button("Export Enemies Data ..."))
            {
                ExportEnemies();
            }

            if (GUILayout.Button("Export Drop Tables ..."))
            {
                ExportDrops();
            }
        }

        /// <summary>
        /// Draw the header for the map list.
        /// </summary>
        private void DrawMapHeader()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.Label("ID", GUILayout.Width(50));
            GUILayout.Label("Name", GUILayout.ExpandWidth(true));
            GUILayout.Label("Prefab", GUILayout.ExpandWidth(true));
            GUILayout.Label("Spawns", GUILayout.Width(70));
            GUILayout.Label("Monster Spawns", GUILayout.Width(120));
            GUILayout.Label("Exports", GUILayout.Width(200));

            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draw a row for a map entry.
        /// </summary>
        /// <param name="map">Map.</param>
        private void DrawMapRow(RoseMapEntry map)
        {
            GUILayout.BeginHorizontal();

            GUILayout.Label(map.id.ToString(), GUILayout.Width(50));

            GUILayout.Label(map.data.mapName, GUILayout.ExpandWidth(true));

            GUILayout.Label(map.prefab != null ? AssetDatabase.GetAssetPath(map.prefab) : "Missing", GUILayout.ExpandWidth(true));

            GUILayout.Label(map.data.spawns != null ? map.data.spawns.Count.ToString() : "0", GUILayout.Width(70));

            int monsterCount = 0;

            if (spawnDatabase != null)
            {
                var spawn = spawnDatabase.spawns.FirstOrDefault(x => x != null && x.spawnData != null && x.spawnData.ID == map.id);

                if (spawn != null)
                {
                    monsterCount = spawn.spawnData.Spawners.Count;
                }
            }

            GUILayout.Label(monsterCount.ToString(), GUILayout.Width(120));

            if (GUILayout.Button("Map Data", GUILayout.Width(80)))
            {
                SaveAsJSONFile($"[{map.id}] {map.data.mapName}", map.data);
            }

            if (GUILayout.Button("Monster Spawns", GUILayout.Width(120)))
            {
                ExportMonsterSpawns(map);
            }

            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Save data to a JSON file.
        /// </summary>
        /// <typeparam name="T">Data type.</typeparam>
        /// <param name="fileName">File name.</param>
        /// <param name="data">Data.</param>
        private void SaveAsJSONFile<T>(string fileName, T data)
        {
            string name = typeof(T).Name;

            string path = EditorUtility.SaveFilePanel($"Export {name} file", Application.dataPath, fileName, "json");

            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            SaveToJSONFile(path, data);

            Debug.Log($"Exported ({name}) {fileName} to JSON : {path}");
        }

        /// <summary>
        /// Save data to a JSON file in a directory.
        /// </summary>
        /// <typeparam name="T">Type.</typeparam>
        /// <param name="fileName">File name.</param>
        /// <param name="directory">Directory.</param>
        /// <param name="data">Data.</param>
        private void SaveToJSONFile<T>(string fileName, string directory, T data)
        {
            string path = Path.Combine(directory, fileName + ".json");

            SaveToJSONFile(path, data);
        }

        /// <summary>
        /// Save data to a JSON file at a specific path.
        /// </summary>
        /// <typeparam name="T">Type.</typeparam>
        /// <param name="path">OS Path.</param>
        /// <param name="data">Data.</param>
        private void SaveToJSONFile<T>(string path, T data)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            string json = JsonConvert.SerializeObject(data, Formatting.Indented);

            File.WriteAllText(path, json);
        }

        /// <summary>
        /// Export monster spawns for a specific map to a JSON file.
        /// </summary>
        /// <param name="mapID">Map ID.</param>
        private void ExportMonsterSpawns(RoseMapEntry entry)
        {
            if (spawnDatabase == null)
            {
                Debug.LogWarning("Monster spawn database not found.");

                return;
            }

            var spawn = spawnDatabase.spawns.FirstOrDefault(x => x != null && x.spawnData != null && x.spawnData.ID == entry.id);

            if (spawn == null)
            {
                Debug.LogWarning($"Monster spawns for map {entry.id} not found.");

                return;
            }

            SaveAsJSONFile($"[{entry.data.mapName}] Spawns", spawn.spawnData);
        }

        /// <summary>
        /// Export all maps to JSON files.
        /// </summary>
        private void ExportMaps()
        {
            Debug.Log("Exporting all maps ...");

            string directory = EditorUtility.OpenFolderPanel("Export Maps", Application.dataPath, "");

            if (string.IsNullOrEmpty(directory))
            {
                return;
            }

            foreach (var map in mapDatabase.maps.OrderBy(x => x.id))
            {
                SaveToJSONFile($"[{map.id}] {map.data.mapName}", directory, map.data);
            }

            Debug.Log($"Exported {mapDatabase.maps.Count} maps to JSON : {directory}");
        }

        /// <summary>
        /// Export the drop tables to JSON files.
        /// </summary>
        private void ExportDrops()
        {
            Debug.Log("Starting drop tables export ...");

            string directory = EditorUtility.OpenFolderPanel("Export Drop Tables", "", "");

            if (string.IsNullOrEmpty(directory))
            {
                Debug.Log("Drop tables export canceled");

                return;
            }

            foreach (var entry in dropTableDatabase.entries.OrderBy(x => x.id))
            {
                var table = RoseExport.ExportDropTable(ResourceManager.Instance.dropSTB, entry.id);

                SaveToJSONFile($"[{entry.id}]", directory, table);
            }

            Debug.Log($"Exported {dropTableDatabase.entries.Count} drop tables to JSON : {directory}");
        }

        /// <summary>
        /// Export the enemies.
        /// </summary>
        public void ExportEnemies()
        {
            if (npcDatabase == null)
            {
                Debug.LogWarning("Enemies database not found.");

                return;
            }

            var folder = EditorUtility.OpenFolderPanel("Export Enemies", "", "");

            if (string.IsNullOrEmpty(folder))
            {
                Debug.Log("Enemies export canceled");

                return;
            }

            for (int i = 0; i < npcDatabase.entries.Count; i++)
            {
                var enemy = npcDatabase.entries[i].data.monsterData;

                string fileName = $"[{enemy.ID}]{enemy.displayName}";

                SaveToJSONFile(fileName, folder, enemy);
            }

            Debug.Log($"Exported {npcDatabase.entries.Count} enemies to {folder}");
        }

        public static readonly string MapDatabasePath = GameDataPaths.Database.Root + $"/{nameof(MapDatabase)}.asset";
        public static readonly string MonsterSpawnDatabasePath = GameDataPaths.Database.Root + $"/{nameof(MonsterSpawnDatabase)}.asset";
        public static readonly string NPCDatabasePath = GameDataPaths.Database.Root + $"/{nameof(NPCDatabase)}.asset";
    }
}