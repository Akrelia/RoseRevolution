#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;

namespace UnityRose.ImportEditor
{
    public class ROSEDatabaseWindow : EditorWindow
    {
        private RoseMapDatabase mapDatabase;
        private RoseMonsterSpawnDatabase spawnDatabase;
        private Vector2 scroll;

        public const string MapDatabasePath = ImportPaths.Database.Root + "/RoseMapDatabase.asset";
        public const string MonsterSpawnDatabasePath = ImportPaths.Database.Root + "/RoseMonsterSpawnDatabase.asset";

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
            mapDatabase = AssetDatabase.LoadAssetAtPath<RoseMapDatabase>(MapDatabasePath);
            spawnDatabase = AssetDatabase.LoadAssetAtPath<RoseMonsterSpawnDatabase>(MonsterSpawnDatabasePath);
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

            GUILayout.Label(map.name, GUILayout.ExpandWidth(true));

            GUILayout.Label(map.prefab != null ? AssetDatabase.GetAssetPath(map.prefab) : "Missing", GUILayout.ExpandWidth(true));

            GUILayout.Label(map.spawnPoints != null ? map.spawnPoints.Count.ToString() : "0", GUILayout.Width(70));

            int monsterCount = 0;

            if (spawnDatabase != null)
            {
                var spawn = spawnDatabase.maps.FirstOrDefault(x => x.MapID == map.id);

                if (spawn != null)
                {
                    monsterCount = spawn.Spawns.Count;
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

            var dto = new RevolutionShared.JSON.MapData(
                map.id,
                map.name,
                map.spawnPoints?.Select(x => new RevolutionShared.JSON.MapSpawn(
                    x.name,
                    x.position.x,
                    x.position.y,
                    x.position.z
                )).ToList() ?? new List<RevolutionShared.JSON.MapSpawn>()
            );

            string path = EditorUtility.SaveFilePanel("Export Map", Application.dataPath, map.name, "json");

            if (string.IsNullOrEmpty(path))
                return;

            string json = JsonConvert.SerializeObject(dto, Formatting.Indented);

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

            var map = spawnDatabase.maps.FirstOrDefault(x => x.MapID == mapID);

            if (map == null)
            {
                Debug.LogWarning($"Monster spawns for map {mapID} not found.");

                return;
            }

            var root = new JObject();
            var monstersArray = new JArray();

            root["MapID"] = map.MapID;
            root["MapName"] = map.MapName;
            root["Spawns"] = monstersArray;

            foreach (var monster in map.Spawns)
            {
                var basicArray = new JArray();

                foreach (var b in monster.Basic)
                {
                    basicArray.Add(new JObject
                    {
                        ["ID"] = b.ID,
                        ["Count"] = b.Count,
                        ["Description"] = b.Description
                    });
                }

                var tacticArray = new JArray();

                foreach (var t in monster.Tactic)
                {
                    tacticArray.Add(new JObject
                    {
                        ["ID"] = t.ID,
                        ["Count"] = t.Count,
                        ["Description"] = t.Description
                    });
                }

                var monsterObj = new JObject
                {
                    ["Settings"] = new JObject
                    {
                        ["Name"] = monster.Name,
                        ["MapX"] = monster.MapX,
                        ["MapY"] = monster.MapY,
                        ["ID"] = monster.ID,
                        ["WorldX"] = monster.WorldX,
                        ["WorldY"] = monster.WorldY,
                        ["WorldZ"] = monster.WorldZ,
                        ["Interval"] = monster.Interval,
                        ["LimitCount"] = monster.LimitCount,
                        ["Range"] = monster.Range,
                        ["TacticPoints"] = monster.TacticPoints
                    },
                    ["Basic"] = basicArray,
                    ["Tactic"] = tacticArray
                };

                monstersArray.Add(monsterObj);
            }

            string path = EditorUtility.SaveFilePanel("Export Monster Spawns", Application.dataPath, map.MapName + "_MonsterSpawns", "json");

            if (!string.IsNullOrEmpty(path))
            {
                System.IO.File.WriteAllText(path, root.ToString(Newtonsoft.Json.Formatting.Indented));

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
    }
}

#endif