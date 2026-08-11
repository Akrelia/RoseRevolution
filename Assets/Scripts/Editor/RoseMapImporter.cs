#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityRose;
using UnityRose.Formats;
using UnityRose.Game;
using Newtonsoft.Json.Linq;
using UnityRose.ImportEditor;

using UnityEditor;
using RevolutionShared.Rose.Data;

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
        public static GameObject ImportMap(int mapID, ImportationSettings settings)
        {
            Debug.Log("Importing map ID " + mapID + "...");

            var stb = ResourceManager.Instance.zoneSTB;

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

            roseMap.mapID = mapID;
            roseMap.mapName = mapName;

            roseMap.data = new MapData(mapID, mapName, mapSpawns);
            roseMap.data.skyID = Utils.ParseSTBInt(stb.Cells[mapID][8]);
            roseMap.data.planetID = Utils.ParseSTBInt(stb.Cells[mapID][20]);

            var dayPeriod = Utils.ParseSTBInt(stb.Cells[mapID][14]);
            var morning = Utils.ParseSTBInt(stb.Cells[mapID][15]);
            var day = Utils.ParseSTBInt(stb.Cells[mapID][16]);
            var evening = Utils.ParseSTBInt(stb.Cells[mapID][17]);
            var night = Utils.ParseSTBInt(stb.Cells[mapID][18]);

            roseMap.data.time = new MapTime(dayPeriod, morning, day, evening, night);

            AssetDatabase.SaveAssets();

            var prefabPath = $"{ImportPaths.Maps.Prefabs}/{mapName}.prefab";

            Utils.EnsureFolder(prefabPath);

            var prefab = PrefabUtility.SaveAsPrefabAsset(map, prefabPath);

            Debug.Log($"Prefab saved: {prefabPath}");

            UnityEngine.Object.DestroyImmediate(map);

            patches.Clear();
            atlasRectHash.Clear();
            atlasTexHash.Clear();
            textures.Clear();

            Resources.UnloadUnusedAssets();
            GC.Collect();

            Debug.Log("Import complete.");

            return prefab;
        }

        /// <summary>
        /// Saves the texture atlas to the project as an asset.
        /// </summary>
        /// <param name="atlas">Atlas.</param>
        /// <param name="mapName">Map name.</param>
        /// <returns>Atlas texture.</returns>
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
                spawn.transform.localPosition = Utils.r2uScale(spawnPoint.Position);

                spawn.transform.rotation = Quaternion.identity;
            }
        }

        [Obsolete("This spawn the NPC on the client side, within the Map prefab, use BuildNpcSpawns instead.")]
        /// <summary>
        /// Spawns the NPCs in the map.
        /// </summary>
        /// <param name="map"></param>
        /// <param name="patches"></param>
        /// <returns></returns>
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
                foreach (var ifoNpc in patch.IFO.NPCs)
                {
                    var npcData = npcImporter.ImportNpc(ifoNpc.ObjectID);

                    if (npcData == null) continue;

                    var npc = new GameObject("NPC_" + ifoNpc.ObjectID);
                    npc.transform.parent = npcs.transform;
                    npc.transform.localPosition = ifoNpc.Position / 100F;
                    npc.transform.rotation = Quaternion.identity;

                    var roseNpc = npc.AddComponent<EntityModelBehavior>();
                    roseNpc.data = npcData;
                }
            }

            return npcImporter;
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
                result.Add(new MapSpawn(spawnPoint.Name, Utils.r2uScale(spawnPoint.Position).ToWorldPosition()));
            }

            return result;
        }

        /// <summary>
        /// Build the NPC spawn points list from the patches.
        /// </summary>
        /// <param name="patches">Patches.</param>
        /// <returns>NPC spawns.</returns>
        public static List<MapSpawn> BuildNpcSpawns(List<RosePatch> patches)
        {
            var result = new List<MapSpawn>();

            foreach (var patch in patches)
            {
                foreach (var npc in patch.IFO.NPCs)
                {
                    result.Add(new MapSpawn($"NPC_{npc.ObjectID}", npc.Position.ToWorldPosition()));
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
}

#endif
