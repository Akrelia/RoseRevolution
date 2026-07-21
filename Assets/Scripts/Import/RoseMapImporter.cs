using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityRose;
using UnityRose.Formats;
using UnityRose.Game;
using Newtonsoft.Json.Linq;
using static UnityRose.Formats.ZON;
using UnityEngine.Rendering.Universal;
using UnityRose.ImportEditor;




#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityRose.Import
{
    /// <summary>
    /// Rose map importer.
    /// </summary>
    public static class RoseMapImporter
    {
        /// <summary>
        /// Import a map.
        /// </summary>
        /// <param name="mapID">Map ID.</param>
        /// <returns></returns>
        public static GameObject ImportMap(int mapID)
        {
            Debug.Log("Importing map ID " + mapID + "...");

            var stb = ResourceManager.Instance.stb_zone;

            var mapName = stb.Cells[mapID][1].ToString();

            var zonPath = Utils.FixPath(stb.Cells[mapID][2].ToString());
            var mapDirectoryRelative = Path.GetDirectoryName(zonPath);
            var mapDirectory = Path.Combine(RoseDataSource.DataPath, mapDirectoryRelative);

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

            var atlas = BuildTextureAtlas(atlasRectHash.Count, textures, out var rects);

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
                    patch.Import(terrain.transform, terrainObjects.transform, atlas, atlas, atlasRectHash);
                }
            }

            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            BlendSeamNormals(patches);

            terrainObjects.transform.localScale = new Vector3(1, 1, -1);
            terrainObjects.transform.Rotate(0, -90, 0);
            terrainObjects.transform.position = new Vector3(5200, 0, 5200);

            SpawnSpawnPoints(map, patches);
            SpawnNpcs(map, patches); // TODO : remove later

            var monsters = new GameObject("Monsters");
            monsters.transform.SetParent(map.transform);
            monsters.transform.localScale = new Vector3(1, 1, -1);
            monsters.transform.Rotate(0, -90, 0);
            monsters.transform.position = new Vector3(5200, 0, 5200);

            roseMap.mapID = mapID;
            roseMap.mapName = mapName;
            roseMap.spawns = BuildSpawnPoints(patches);

            AssetDatabase.SaveAssets();

            var prefabPath = $"{ImportPaths.Maps.Prefabs}/{mapName}.prefab";

            Utils.EnsureFolder(prefabPath);

            var prefab = PrefabUtility.SaveAsPrefabAsset(map, prefabPath);

            Debug.Log($"Prefab saved: {prefabPath}");

            // Nettoyage mémoire
            UnityEngine.Object.DestroyImmediate(map);

            patches.Clear();
            atlasRectHash.Clear();
            atlasTexHash.Clear();
            textures.Clear();

            Resources.UnloadUnusedAssets();
            GC.Collect();

            Debug.Log("Import complete and memory cleaned.");

            return prefab;
        }

        private static Texture2D SaveAtlas(Texture2D atlas, string mapName)
        {
            const string folder = ImportPaths.Maps.Atlas;

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

        private static Texture2D BuildTextureAtlas(int tileCount, List<Texture2D> textures, out Rect[] rects)
        {
            int width, height; // must be powers of 2
            if (tileCount <= 16) width = height = 4 * 256;
            else if (tileCount <= 32) { width = 8 * 256; height = 4 * 256; }
            else if (tileCount <= 64) { width = 8 * 256; height = 8 * 256; }
            else if (tileCount <= 128) { width = 16 * 256; height = 8 * 256; }
            else if (tileCount <= 256) { width = 16 * 256; height = 16 * 256; }
            else throw new Exception("Number of tiles in terrain is larger than supported by terrain atlas");

            var atlas = new Texture2D(width, height);
            rects = atlas.PackTextures(textures.ToArray(), 0, Math.Max(width, height));
            atlas.anisoLevel = 11;
            atlas.Apply();

            return atlas;
        }

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
                    avg += patches[entry.patchID].m_mesh.normals[entry.normalID];
                }
             
                avg.Normalize();

                foreach (var entry in patchNormalLookup[vertex])
                {
                    patches[entry.patchID].m_mesh.normals[entry.normalID] = avg;
                }
            }
        }

        private static void SpawnSpawnPoints(GameObject map, List<RosePatch> patches)
        {
            var spawns = new GameObject("Spawn Points");

            spawns.transform.position = new Vector3(5200 * 2F, 0, 0);
            spawns.transform.Rotate(0, -90F, 0);
            spawns.transform.SetParent(map.transform);

            foreach (var spawnPoint in patches[0].m_ZON.SpawnPoints)
            {
                var spawn = new GameObject(spawnPoint.Name);

                spawn.transform.parent = spawns.transform;
                spawn.transform.localPosition = Utils.r2uScale(spawnPoint.Position);
                spawn.transform.rotation = Quaternion.identity;
            }
        }

        public static List<SpawnData> BuildSpawnPoints(List<RosePatch> patches)
        {
            var result = new List<SpawnData>();

            foreach (var spawnPoint in patches[0].m_ZON.SpawnPoints)
            {
                result.Add(new SpawnData
                {
                    name = spawnPoint.Name,
                    position = Utils.r2uScale(spawnPoint.Position)
                });
            }

            return result;
        }

        private static RoseNpcImporter SpawnNpcs(GameObject map, List<RosePatch> patches)
        {
            var npcs = new GameObject("NPCs");
            npcs.transform.SetParent(map.transform);
            npcs.transform.localScale = new Vector3(1.0f, 1.0f, -1.0f);
            npcs.transform.Rotate(0, -90F, 0);
            npcs.transform.position = new Vector3(5200, 0, 5200);

            var npcImporter = new RoseNpcImporter();

            foreach (var patch in patches)
            {
                foreach (var ifoNpc in patch.m_IFO.NPCs)
                {
                    var npcData = npcImporter.ImportNpc(ifoNpc.ObjectID);
                    if (npcData == null) continue;

                    var npc = new GameObject("NPC_" + ifoNpc.ObjectID);
                    npc.transform.parent = npcs.transform;
                    npc.transform.localPosition = ifoNpc.Position / 100F;
                    npc.transform.rotation = Quaternion.identity;

                    var roseNpc = npc.AddComponent<RoseNpc>();
                    roseNpc.data = npcData;
                }
            }

            return npcImporter;
        }

        public static string BuildSpawnExportJson(int mapID) // Remove it, useless
        {
            var stb = ResourceManager.Instance.stb_zone; // TODO : use the Map Database for that
            var stbNPC = ResourceManager.Instance.stb_npc_list; // TODO : use the Map Database for that

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

            var root = new JObject();
            var monstersArray = new JArray();

            root["Spawns"] = monstersArray;
            root["MapID"] = mapID;
            root["MapName"] = stb.Cells[mapID][1];

            foreach (var patch in patches)
            {
                foreach (var monster in patch.m_IFO.Monsters)
                {
                    var basicArray = new JArray();

                    foreach (var b in monster.Basic)
                    {
                        basicArray.Add(new JObject
                        {
                            ["ID"] = b.ID,
                            ["Count"] = b.Count,
                            ["Description"] = stbNPC.Cells[b.ID][1]
                        });
                    }

                    var tacticArray = new JArray();
                    foreach (var t in monster.Tactic)
                    {
                        tacticArray.Add(new JObject
                        {
                            ["ID"] = t.ID,
                            ["Count"] = t.Count,
                            ["Description"] = stbNPC.Cells[t.ID][1]
                        });
                    }

                    var monsterObj = new JObject
                    {
                        ["Settings"] = new JObject
                        {
                            ["Name"] = monster.Name,
                            ["MapX"] = monster.MapPosition.x,
                            ["MapY"] = monster.MapPosition.y,
                            ["ID"] = monster.ObjectID,
                            ["WorldX"] = (monster.Position.x + 520000.0f) / 100F,
                            ["WorldY"] = (monster.Position.y + 520000.0f) / 100F,
                            ["WorldZ"] = monster.Position.z / -10000F,
                            ["Interval"] = monster.Interval,
                            ["LimitCount"] = monster.Limit,
                            ["Range"] = monster.Range,
                            ["TacticPoints"] = monster.TacticPoints,
                        },
                        ["Basic"] = basicArray,
                        ["Tactic"] = tacticArray
                    };

                    monstersArray.Add(monsterObj);
                }
            }

            return root.ToString(Newtonsoft.Json.Formatting.Indented);
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
}