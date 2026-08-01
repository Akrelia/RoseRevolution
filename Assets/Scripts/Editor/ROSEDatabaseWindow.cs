#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;
using RevolutionShared.Rose.Data.NPC;
using RevolutionShared.Rose.Data;

namespace UnityRose.ImportEditor
{
    public class ROSEDatabaseWindow : EditorWindow
    {
        private MapDatabase mapDatabase;
        private MonsterSpawnDatabase spawnDatabase;
        private NPCDatabase npcDatabase;
        private Vector2 scroll;

        public const string MapDatabasePath = ImportPaths.Database.Root + "/MapDatabase.asset";
        public const string MonsterSpawnDatabasePath = ImportPaths.Database.Root + "/MonsterSpawnDatabase.asset";
        public const string NPCDatabasePath = ImportPaths.Database.Root + "/NpcDatabase.asset";

        [MenuItem("ROSE Online/Database Viewer")]
        static void Open()
        {
            GetWindow<ROSEDatabaseWindow>("ROSE Database Viewer");
        }

        private void OnEnable()
        {
            LoadDatabases();
        }

        private void LoadDatabases()
        {
            mapDatabase = AssetDatabase.LoadAssetAtPath<MapDatabase>(MapDatabasePath);
            spawnDatabase = AssetDatabase.LoadAssetAtPath<MonsterSpawnDatabase>(MonsterSpawnDatabasePath);
            npcDatabase = AssetDatabase.LoadAssetAtPath<NPCDatabase>(NPCDatabasePath);
        }

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

            DrawHeader();

            scroll = GUILayout.BeginScrollView(scroll);

            foreach (var map in mapDatabase.maps.OrderBy(x => x.id))
            {
                DrawMapRow(map);
            }

            GUILayout.EndScrollView();

            GUILayout.Space(10);

            if (GUILayout.Button("Export All"))
            {
                ExportAll();
            }


            if (GUILayout.Button("Export Enemies"))
            {
                ExportEnemies();
            }
        }

        private void DrawHeader()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.Label("ID", GUILayout.Width(50));
            GUILayout.Label("Name", GUILayout.ExpandWidth(true));
            GUILayout.Label("Prefab", GUILayout.ExpandWidth(true));
            GUILayout.Label("Spawns", GUILayout.Width(70));
            GUILayout.Label("Monster Spawns", GUILayout.Width(120));
            GUILayout.Label("", GUILayout.Width(80));

            GUILayout.EndHorizontal();
        }

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
                var spawn = spawnDatabase.maps.FirstOrDefault(x => x != null && x.spawnData != null && x.spawnData.ID == map.id);

                if (spawn != null)
                {
                    monsterCount = spawn.spawnData.Spawners.Count;
                }
            }

            GUILayout.Label(monsterCount.ToString(), GUILayout.Width(120));


            if (GUILayout.Button("Export", GUILayout.Width(80)))
            {
                ExportMap(map.id);

                ExportMonsterSpawns(map.id);
            }

            GUILayout.EndHorizontal();
        }

        private void ExportMap(int id)
        {
            var map = mapDatabase.GetMapById(id);

            if (map == null)
            {
                Debug.LogWarning($"Map {id} not found.");
                return;
            }

            string path = EditorUtility.SaveFilePanel("Export Map", Application.dataPath, map.data.mapName, "json");

            if (string.IsNullOrEmpty(path))
                return;

            string json = JsonConvert.SerializeObject(map.data, Formatting.Indented);

            File.WriteAllText(path, json);

            Debug.Log($"Exported map {map.id} : {path}");
        }

        private void ExportMonsterSpawns(int mapID)
        {
            if (spawnDatabase == null)
            {
                Debug.LogWarning("Monster spawn database not found.");

                return;
            }

            var map = spawnDatabase.maps.FirstOrDefault(x => x != null && x.spawnData != null && x.spawnData.ID == mapID);

            if (map == null)
            {
                Debug.LogWarning($"Monster spawns for map {mapID} not found.");

                return;
            }

            string path = EditorUtility.SaveFilePanel("Export Monster Spawns", Application.dataPath, map.spawnData.MapName + "_MonsterSpawns", "json");

            if (!string.IsNullOrEmpty(path))
            {
                File.WriteAllText(path, JsonConvert.SerializeObject(map.spawnData, Formatting.Indented));

                Debug.Log($"Exported monster spawns for map {mapID}");
            }
        }

        private void ExportAll()
        {
            Debug.Log("Export all maps");

            foreach (var map in mapDatabase.maps.OrderBy(x => x.id))
            {
                ExportMap(map.id);
            }
        }

        public void ExportEnemies()
        {
            var folder = EditorUtility.OpenFolderPanel("Export Enemies", "", "");

            if (string.IsNullOrEmpty(folder))
                return;

            var npcs = npcDatabase.entries;

            for (int i = 0; i < npcs.Count; i++)
            {
                var enemy = npcs[i].data.monsterData;

                var settings = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    Converters = { new Newtonsoft.Json.Converters.StringEnumConverter() }
                };

                string json = JsonConvert.SerializeObject(enemy, settings);

                string fileName = $"[{enemy.ID}]{enemy.displayName}.json";
                string path = Path.Combine(folder, fileName);

                File.WriteAllText(path, json);
            }

            Debug.Log($"Exported {npcs.Count} enemies to {folder}");
        }
    }
}

#endif