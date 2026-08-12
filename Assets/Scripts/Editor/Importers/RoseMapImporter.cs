using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityRose.Game;
using UnityRose.ImportEditor;
using UnityEditor;
using RevolutionShared.Rose.Data;
using UnityRose.Formats;

namespace UnityRose.Import
{
    /// <summary>
    /// Rose map importer.
    /// </summary>
    public static class RoseMapImporter
    {
        /// <summary>
        /// Import a map and register it in the internal database.
        /// </summary>
        /// <param name="mapID">Map ID.</param>
        /// <param name="settings">Settings.</param>
        /// <returns>Map.</returns>
        public static GameObject ImportFullMap(int mapID, ImportationSettings settings)
        {
            var prefab = ImportMap(mapID, settings);

            RegisterMapInInternalDB(mapID);

            return prefab;
        }

        /// <summary>
        /// Import a map.
        /// </summary>
        /// <param name="mapID">Map ID.</param>
        /// <returns></returns>
        private static GameObject ImportMap(int mapID, ImportationSettings settings)
        {
            Debug.Log("Importing map ID " + mapID + "...");

            var stb = ResourceManager.Instance.zoneSTB;

            var mapName = stb.Cells[mapID][1].ToString();

            var zonPath = EditorUtils.FixPath(stb.Cells[mapID][2].ToString());
            var mapDirectoryRelative = Path.GetDirectoryName(zonPath);
            var mapDirectory = Path.Combine(RoseImporter.DataPath, mapDirectoryRelative);

            var dirs = new DirectoryInfo(mapDirectory);

            var map = new GameObject(mapName);
            var roseMap = map.AddComponent<RoseMap>();

            var terrain = new GameObject("Ground") { layer = LayerMask.NameToLayer("Floor") };
            terrain.transform.SetParent(map.transform);

            var terrainObjects = new GameObject("Objects") { layer = LayerMask.NameToLayer("MapObjects") };
            terrainObjects.transform.SetParent(map.transform);

            var patches = new List<RosePatch>();
            var atlasRectHash = new Dictionary<string, Rect>();
            var atlasTexHash = new Dictionary<string, Texture2D>();
            var textures = new List<Texture2D>();

            foreach (var dir in dirs.GetDirectories())
            {
                if (dir.Name.Contains(".")) continue;

                var patch = new RosePatch(dir);

                var valid = patch.Load(mapID);

                if (valid)
                {
                    patch.UpdateAtlas(ref atlasRectHash, ref atlasTexHash, ref textures);

                    patches.Add(patch);
                }
            }

            var atlas = BuildTextureAtlas(atlasRectHash.Count, textures, settings.anisotropyLevel, out var rects);

            atlas = SaveAtlas(atlas, mapName);

            int rectID = 0;

            foreach (var key in atlasTexHash.Keys)
            {
                atlasRectHash[key] = rects[rectID++];
            }

            RosePatch.ClearCache();

            AssetDatabase.StartAssetEditing();

            try
            {
                foreach (var patch in patches)
                {
                    patch.Import(terrain.transform, terrainObjects.transform, atlas, atlas, atlasRectHash, settings.terrainShader, settings.objectShader);
                }
            }

            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            BlendSeamNormals(patches);

            terrainObjects.transform.SetParent(map.transform);

            terrainObjects.transform.localScale = new Vector3(1, 1, -1);
            terrainObjects.transform.Rotate(0, -90, 0);
            terrainObjects.transform.position = new Vector3(5200, 0, 5200);

            SpawnSpawnPoints(map, patches); // Could be removed but we keep this for debug

            var mapSpawns = BuildSpawnPoints(patches);
            var mobSpawnData = BuildEnemySpawnData(mapID, patches);
            var npcSpawns = BuildNpcSpawns(patches);

            RegisterSpawnInInternalDB(mapID, patches);

            roseMap.mapID = mapID;
            roseMap.mapName = mapName;

            roseMap.data = new MapData(mapID, mapName, mapSpawns, npcSpawns);
            roseMap.data.skyID = EditorUtils.ParseSTBInt(stb.Cells[mapID][8]);
            roseMap.data.planetID = EditorUtils.ParseSTBInt(stb.Cells[mapID][20]);

            var dayPeriod = EditorUtils.ParseSTBInt(stb.Cells[mapID][14]);
            var morning = EditorUtils.ParseSTBInt(stb.Cells[mapID][15]);
            var day = EditorUtils.ParseSTBInt(stb.Cells[mapID][16]);
            var evening = EditorUtils.ParseSTBInt(stb.Cells[mapID][17]);
            var night = EditorUtils.ParseSTBInt(stb.Cells[mapID][18]);

            roseMap.data.time = new MapTime(dayPeriod, morning, day, evening, night);

            AssetDatabase.SaveAssets();

            var prefabPath = $"{GameDataPaths.Maps.Prefabs}/{mapName}.prefab";

            EditorUtils.EnsureFolder(prefabPath);

            var prefab = PrefabUtility.SaveAsPrefabAsset(map, prefabPath);

            Debug.Log($"Prefab saved: {prefabPath}");

            UnityEngine.Object.DestroyImmediate(map);

            patches.Clear();
            atlasRectHash.Clear();
            atlasTexHash.Clear();
            textures.Clear();

            Resources.UnloadUnusedAssets(); // Still useful ? 
            GC.Collect();

            Debug.Log("Import complete.");

            return prefab;
        }

        /// <summary>
        /// Register a map in the internal database.
        /// </summary>
        /// <param name="id">ID.</param>
        private static void RegisterMapInInternalDB(int id)
        {
            var mapData = ROSEMapListCache.Get();

            string folder = GameDataPaths.Database.Root;

            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder(GameDataPaths.Root, "Databases");
            }

            string path = $"{folder}/MapDatabase.asset";

            var database = AssetDatabase.LoadAssetAtPath<MapDatabase>(path);

            if (database == null)
            {
                database = ScriptableObject.CreateInstance<MapDatabase>();

                AssetDatabase.CreateAsset(database, path);
            }

            EditorUtils.EnsureAddressable(path, nameof(MapDatabase));

            string displayName = mapData.stl.GetText(mapData.stb.Cells[id][27], STL.Language.English);

            string prefabName = mapData.stb.Cells[id][1];

            if (string.IsNullOrEmpty(displayName))
            {
                displayName = "Map_" + id;
            }

            string prefabPath = $"{GameDataPaths.Maps.Prefabs}/{prefabName}.prefab";

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (prefab == null)
            {
                Debug.LogWarning($"Prefab not found for map {id} ({displayName})");

                return;
            }

            var roseMap = prefab.GetComponent<RoseMap>();

            var existing = database.maps.Find(x => x.id == id);

            if (existing != null)
            {
                existing.prefab = prefab;
                existing.data = roseMap.data;
            }

            else
            {
                database.maps.Add(new RoseMapEntry
                {
                    id = id,
                    prefab = prefab,
                    data = roseMap.data
                });
            }

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Register a map spawn in the internal database.
        /// </summary>
        /// <param name="mapID"></param>
        private static void RegisterSpawnInInternalDB(int mapID, List<RosePatch> patches)
        {
            var database = AssetDatabase.LoadAssetAtPath<MonsterSpawnDatabase>(ROSEExportWindow.MonsterSpawnDatabasePath);

            if (database == null)
            {
                database = ScriptableObject.CreateInstance<MonsterSpawnDatabase>();

                EditorUtils.EnsureFolder(Path.GetDirectoryName(ROSEExportWindow.MonsterSpawnDatabasePath));

                AssetDatabase.CreateAsset(database, ROSEExportWindow.MonsterSpawnDatabasePath);
            }

            EditorUtils.EnsureAddressable(ROSEExportWindow.MonsterSpawnDatabasePath, nameof(MonsterSpawnDatabase)); // Shouldn't be useful but just in case

            database.spawns.RemoveAll(m => m != null && m.spawnData != null && m.spawnData.ID == mapID);

            database.spawns.Add(BuildEnemySpawnData(mapID, patches));

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Saves the texture atlas to the project as an asset.
        /// </summary>
        /// <param name="atlas">Atlas.</param>
        /// <param name="mapName">Map name.</param>
        /// <returns>Atlas texture.</returns>
        private static Texture2D SaveAtlas(Texture2D atlas, string mapName)
        {
            const string folder = GameDataPaths.Maps.Atlas;

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string path = $"{folder}/{mapName}_TerrainAtlas.asset";

            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(atlas);
                return existing;
            }

            AssetDatabase.CreateAsset(atlas, path);
            return atlas;
        }

        /// <summary>
        /// Builds a texture atlas from the given textures.
        /// </summary>
        /// <param name="tileCount">Tile ocunt.</param>
        /// <param name="textures">Textures.</param>
        /// <param name="anisotropicLevel">Ansitropic level.</param>
        /// <param name="rects">Rectangles.</param>
        /// <returns>Texture atlas.</returns>
        /// <exception cref="Exception">Exceding the atlas max values.</exception>
        private static Texture2D BuildTextureAtlas(int tileCount, List<Texture2D> textures, int anisotropicLevel, out Rect[] rects)
        {
            int width, height;
            if (tileCount <= 16) width = height = 4 * 256;
            else if (tileCount <= 32) { width = 8 * 256; height = 4 * 256; }
            else if (tileCount <= 64) { width = 8 * 256; height = 8 * 256; }
            else if (tileCount <= 128) { width = 16 * 256; height = 8 * 256; }
            else if (tileCount <= 256) { width = 16 * 256; height = 16 * 256; }
            else throw new Exception("Number of tiles in terrain is larger than supported by terrain atlas");

            var atlas = new Texture2D(width, height);

            rects = atlas.PackTextures(textures.ToArray(), 0, Math.Max(width, height));

            atlas.anisoLevel = anisotropicLevel;
            atlas.Apply();

            return atlas;
        }

        /// <summary>
        /// Blends the normals of the seam vertices between patches to avoid lighting artifacts.
        /// </summary>
        /// <param name="patches">Patches.</param>
        private static void BlendSeamNormals(List<RosePatch> patches)
        {
            var patchNormalLookup = new Dictionary<string, List<PatchNormalIndex>>();
            var patchID = 0;

            foreach (var patch in patches)
            {
                foreach (var vertex in patch.edgeVertexLookup.Keys)
                {
                    var ids = new List<PatchNormalIndex>();

                    foreach (var id in patch.edgeVertexLookup[vertex])
                        ids.Add(new PatchNormalIndex(patchID, id));

                    if (!patchNormalLookup.ContainsKey(vertex))
                        patchNormalLookup.Add(vertex, ids);
                    else
                        patchNormalLookup[vertex].AddRange(ids);
                }

                patchID++;
            }

            foreach (var vertex in patchNormalLookup.Keys)
            {
                var avg = Vector3.zero;

                foreach (var entry in patchNormalLookup[vertex])
                {
                    avg += patches[entry.patchID].mesh.normals[entry.normalID];
                }

                avg.Normalize();

                foreach (var entry in patchNormalLookup[vertex])
                {
                    patches[entry.patchID].mesh.normals[entry.normalID] = avg;
                }
            }
        }

        /// <summary>
        /// Spawns the spawn points in the map (for debug purposes now).
        /// </summary>
        /// <param name="map">Map.</param>
        /// <param name="patches">Patches.</param>
        private static void SpawnSpawnPoints(GameObject map, List<RosePatch> patches)
        {
            var spawns = new GameObject("Spawn Points");

            spawns.transform.position = new Vector3(5200 * 2F, 0, 0);
            spawns.transform.Rotate(0, -90F, 0);
            spawns.transform.SetParent(map.transform);

            foreach (var spawnPoint in patches[0].ZON.SpawnPoints)
            {
                var spawn = new GameObject(spawnPoint.Name);

                spawn.transform.parent = spawns.transform;
                spawn.transform.localPosition = Utils.RoseToUnityScale(spawnPoint.Position);

                spawn.transform.rotation = Quaternion.identity;
            }
        }

        /// <summary>
        /// Build enemies spawn data.
        /// </summary>
        /// <param name="mapID">Map ID.</param>
        /// <returns>Enemy Spawn.</returns>
        public static EnemySpawnSO BuildEnemySpawnData(int mapID, List<RosePatch> patches)
        {
            var stbNPC = ResourceManager.Instance.npcSTB;

            var data = new SpawnData
            {
                ID = mapID,
                MapName = ResourceManager.Instance.zoneSTB.Cells[mapID][1].ToString(),
                Spawners = new List<EnemySpawner>()
            };

            foreach (var patch in patches)
            {
                foreach (var monster in patch.IFO.Monsters)
                {
                    var spawner = new EnemySpawner
                    {
                        Settings = new SpawnSettings
                        {
                            Name = monster.Name,
                            MapX = monster.MapPosition.x,
                            MapY = monster.MapPosition.y,
                            ID = monster.ObjectID,
                            WorldX = monster.Position.x,
                            WorldY = monster.Position.z,
                            WorldZ = monster.Position.y,
                            Interval = monster.Interval,
                            LimitCount = monster.Limit,
                            Range = monster.Range,
                            TacticPoints = monster.TacticPoints
                        },
                        Basic = new List<EnemySpawn>(),
                        Tactic = new List<EnemySpawn>()
                    };

                    foreach (var spawn in monster.Basic)
                    {
                        spawner.Basic.Add(new EnemySpawn
                        {
                            ID = spawn.ID,
                            Count = spawn.Count,
                            Description = stbNPC.Cells[spawn.ID][1].ToString()
                        });
                    }

                    foreach (var spawn in monster.Tactic)
                    {
                        spawner.Tactic.Add(new EnemySpawn
                        {
                            ID = spawn.ID,
                            Count = spawn.Count,
                            Description = stbNPC.Cells[spawn.ID][1].ToString()
                        });
                    }

                    data.Spawners.Add(spawner);
                }
            }

            string path = $"{GameDataPaths.Root}/Spawns/{mapID}.asset";

            EditorUtils.EnsureFolder(path);

            var entry = AssetDatabase.LoadAssetAtPath<EnemySpawnSO>(path);

            if (entry == null)
            {
                entry = ScriptableObject.CreateInstance<EnemySpawnSO>();
                AssetDatabase.CreateAsset(entry, path);
            }

            entry.spawnData = data;

            EditorUtility.SetDirty(entry);

            return entry;
        }

        /// <summary>
        /// Build the spawn points list from the patches.
        /// </summary>
        /// <param name="patches">Patches.</param>
        /// <returns>Map Spawn.</returns>
        public static List<MapSpawn> BuildSpawnPoints(List<RosePatch> patches)
        {
            var result = new List<MapSpawn>();

            foreach (var spawnPoint in patches[0].ZON.SpawnPoints)
            {
                result.Add(new MapSpawn(spawnPoint.Name, Utils.RoseToUnityScale(spawnPoint.Position).ToWorldPosition()));
            }

            return result;
        }

        /// <summary>
        /// Build the NPC spawn points list from the patches.
        /// </summary>
        /// <param name="patches">Patches.</param>
        /// <returns>NPC spawns.</returns>
        public static List<NPCSpawn> BuildNpcSpawns(List<RosePatch> patches)
        {
            var result = new List<NPCSpawn>();

            foreach (var patch in patches)
            {
                foreach (var npc in patch.IFO.NPCs)
                {
                    result.Add(new NPCSpawn(npc.ObjectID, $"NPC_{npc.ObjectID}", npc.Position.ToWorldPosition()));
                }
            }

            return result;
        }

        /// <summary>
        /// Patch normal index structure.
        /// </summary>
        private struct PatchNormalIndex
        {
            public int patchID;
            public int normalID;

            public PatchNormalIndex(int patchID, int normalID)
            {
                this.patchID = patchID;
                this.normalID = normalID;
            }
        }
    }

    /// <summary>
    /// ROSE Map list cache.
    /// </summary>
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
                stb = new STB(Path.Combine(current, "3DDATA/STB/LIST_ZONE.STB")),
                stl = new STL(Path.Combine(current, "3DDATA/STB/LIST_ZONE_S.STL"))
            };
        }

        public static bool MapPrefabExists(int mapID)
        {
            var mapData = ROSEMapListCache.Get();

            string mapName = Path.GetFileNameWithoutExtension(EditorUtils.FixPath(mapData.stb.Cells[mapID][1].ToString()));

            string prefabPath = $"{GameDataPaths.Maps.Prefabs}/{mapName}.prefab";

            return AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null;
        }

    }
}