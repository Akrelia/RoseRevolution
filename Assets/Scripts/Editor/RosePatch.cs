using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityRose.Formats;
using UnityRose.Import;
using UnityRose.ImportEditor;

namespace UnityRose.Game
{
    /// <summary>
    /// Rose patch (a chunk of the map).
    /// </summary>
    public class RosePatch
    {
        public string name { get; set; }
        public bool isValid { get; set; }
        public Vector2 center { get; set; }
        public string mapName;

        public DirectoryInfo assetDirectory { get; set; }
        public DirectoryInfo unityAssetDirectory { get; set; }
        public DirectoryInfo root3DDataDirectory { get; set; }

        public HIM HIM { get; set; }
        public TIL TIL { get; set; }
        public ZON ZON { get; set; }
        public IFO IFO { get; set; }
        public ZSC ZSCConst { get; set; }
        public ZSC ZSCDeco { get; set; }
        public LIT LITConst { get; set; }
        public LIT LITDeco { get; set; }

        public int col { get; set; }
        public int row { get; set; }

        public Mesh mesh { get; set; }
        public List<Tile> tiles { get; set; }

        public Dictionary<string, List<int>> edgeVertexLookup { get; set; }

        private static readonly HashSet<string> createdFolders = new HashSet<string>();
        private static readonly Dictionary<string, Texture2D> TextureCache = new();
        private static readonly Dictionary<string, Mesh> MeshCache = new();
        private static readonly Dictionary<string, Material> MaterialCache = new();
        private static readonly Dictionary<string, AnimationClip> AnimationCache = new();

        private string groundLight;

        /// <summary>
        /// Constructor.
        /// </summary>
        public RosePatch()
        {
            col = 0;
            row = 0;
            isValid = false;
        }

        /// <summary>
        /// Constructor with asset directory parameter.
        /// </summary>
        /// <param name="assetDirectory">Asset directory.</param>
        public RosePatch(DirectoryInfo assetDirectory)
        {
            this.assetDirectory = assetDirectory;
            this.root3DDataDirectory = new DirectoryInfo(this.assetDirectory.Parent.Parent.Parent.Parent.FullName);
            this.name = assetDirectory.Name.Replace(".*", "");
            this.col = 0;
            this.row = 0;
            this.isValid = false;
            this.center = Vector2.zero;
            this.mapName = assetDirectory.Parent != null ? assetDirectory.Parent.Name : "UnknownMap";

            if (!assetDirectory.Exists)
            {
                return;
            }

            char[] sep = { '_', '.' };
            string[] tokens = assetDirectory.Name.Split(sep);

            if (tokens.Length <= 1 || !int.TryParse(tokens[0], out int col) || !int.TryParse(tokens[1], out int row))
            {
                return;
            }

            if (row <= 0 || row >= 100 || col <= 0 || col >= 100)
            {
                return;
            }

            this.row = row;
            this.col = col;
            isValid = true;
        }

        /// <summary>
        /// Constructor with ZON parameter.
        /// </summary>
        /// <param name="assetDirectory">Asset directory.</param>
        /// <param name="zon">ZON File.</param>
        public RosePatch(DirectoryInfo assetDirectory, ZON zon) : this(assetDirectory)
        {
            ZON = zon;
        }

        /// <summary>
        /// Load the patch.
        /// </summary>
        /// <param name="mapID">Map ID.</param>
        /// <returns>Is loaded successful.</returns>
        public bool Load(int mapID)
        {
            if (!isValid)
            {
                Debug.LogError("Cannot load patch at path " + assetDirectory);

                return false;
            }

            HIM = new HIM(assetDirectory.Parent.FullName + "/" + name + ".HIM");
            TIL = new TIL(assetDirectory.Parent.FullName + "/" + name + ".TIL");

            if (ZON == null)  // load ZON if it was never passed to the patch constructor
            {
                ZON = new ZON(assetDirectory.Parent.FullName + "/" + assetDirectory.Parent.Name + ".ZON");
            }

            IFO = new IFO(assetDirectory.Parent.FullName + "/" + name + ".IFO");

            string litPath = EditorUtils.FixPath(assetDirectory.Parent.FullName + "\\" + name + "\\LIGHTMAP\\BUILDINGLIGHTMAPDATA.LIT");

            groundLight = EditorUtils.FixPath(assetDirectory.Parent.FullName + "\\" + name + "\\" + name + "_PLANELIGHTINGMAP.DDS");

            string zscPathDeco = EditorUtils.FixPath(RoseImporter.DataPath + "/" + ResourceManager.Instance.zoneSTB.Cells[mapID][12]);
            string zscPathCnst = EditorUtils.FixPath(RoseImporter.DataPath + "/" + ResourceManager.Instance.zoneSTB.Cells[mapID][13]);

            ZSCConst = new ZSC(zscPathCnst);
            ZSCDeco = new ZSC(zscPathDeco);

            LITConst = new LIT(litPath);
            LITDeco = new LIT(litPath.Replace("building", "object"));

            edgeVertexLookup = new Dictionary<string, List<int>>();

            return true;
        }

        /// <summary>
        /// Update the atlas texture.
        /// </summary>
        /// <param name="atlasRectHash">Atlas rectangle hash.</param>
        /// <param name="atlasTexHash">Atlas texture hash.</param>
        /// <param name="textures">Textures.</param>
        public void UpdateAtlas(ref Dictionary<string, Rect> atlasRectHash, ref Dictionary<string, Texture2D> atlasTexHash, ref List<Texture2D> textures)
        {
            for (int t_x = 0; t_x < 16; t_x++)
            {
                for (int t_y = 0; t_y < 16; t_y++)
                {
                    int tileID = TIL.Tiles[t_x, t_y].TileID;

                    var tile = ZON.Tiles[tileID];

                    var tex1 = ZON.Textures[tile.ID1];

                    if (!atlasRectHash.ContainsKey(tex1.TexPath))
                    {
                        atlasRectHash.Add(tex1.TexPath, new Rect());
                        atlasTexHash.Add(tex1.TexPath, tex1.Tex);
                        textures.Add(tex1.Tex);
                    }

                    var tex2 = ZON.Textures[tile.ID2];

                    if (!atlasRectHash.ContainsKey(tex2.TexPath))
                    {
                        atlasRectHash.Add(tex2.TexPath, new Rect());
                        atlasTexHash.Add(tex2.TexPath, tex2.Tex);
                        textures.Add(tex2.Tex);
                    }
                }
            }
        }

        /// <summary>
        /// Imports the patch into Unity.
        /// </summary>
        /// <param name="terrainParent">Terrain parent.</param>
        /// <param name="objectsParent">Objects parent.</param>
        /// <param name="atlas">Atlas.</param>
        /// <param name="atlasNormal">Atlas normal map.</param>
        /// <param name="atlasRectHash"><Atlas hash./param>
        /// <returns>False is failed.</returns>
        public bool Import(Transform terrainParent, Transform objectsParent, Texture2D atlas, Texture2D atlasNormal, Dictionary<string, Rect> atlasRectHash, Shader terrainShader, Shader objectShader)
        {
            if (isValid)
            {
                return ImportInternal(terrainParent, objectsParent, atlas, atlasNormal, atlasRectHash, terrainShader, objectShader);
            }

            Debug.LogError("Cannot Import patch_" + this.name);

            return false;
        }

        /// <summary>
        /// Imports the patch into Unity (internal implementation).
        /// </summary>
        /// <param name="terrainParent"></param>
        /// <param name="objectsParent"></param>
        /// <param name="atlas"></param>
        /// <param name="atlas_normal"></param>
        /// <param name="atlasRectHash"></param>
        /// <param name="blendNormals"></param>
        /// <returns></returns>
        private bool ImportInternal(Transform terrainParent, Transform objectsParent, Texture2D atlas, Texture2D atlas_normal, Dictionary<string, Rect> atlasRectHash, Shader terrainShader, Shader objectShader, bool blendNormals = false)
        {
            int nVertices = 64 * 64 * 4;
            Vector3[] vertices = new Vector3[nVertices];
            Vector2[] uvsBottom = new Vector2[nVertices];
            Vector2[] uvsTop = new Vector2[nVertices];
            Color[] uvsLight = new Color[nVertices];
            int[] triangles = new int[(HIM.Length - 1) * (HIM.Width - 1) * 6];

            int i_v = 0;

            float m_xStride = 2.5f;
            float m_yStride = 2.5f;
            float heightScaler = 300.0f / (m_xStride * 1.2f);
            float x_offset = row * m_xStride * 64.0f;
            float y_offset = col * m_yStride * 64.0f;
            center = new Vector2(x_offset + m_xStride * 32.0f, y_offset + m_yStride * 32.0f);

            mesh = new Mesh();

            Vector2[,] uvMatrix = new Vector2[5, 5];
            Vector2[,] uvMatrixLR = new Vector2[5, 5];
            Vector2[,] uvMatrixTB = new Vector2[5, 5];
            Vector2[,] uvMatrixLRTB = new Vector2[5, 5];
            Vector2[,] uvMatrixRotCW = new Vector2[5, 5];  // rotated 90 deg clockwise
            Vector2[,] uvMatrixRotCCW = new Vector2[5, 5];  // rotated 90 counter clockwise

            for (int uv_x = 0; uv_x < 5; uv_x++)
            {
                for (int uv_y = 0; uv_y < 5; uv_y++)
                {
                    uvMatrix[uv_y, uv_x] = new Vector2(0.25f * uv_x, 1.0f - 0.25f * uv_y);
                    uvMatrixLR[uv_y, uv_x] = new Vector2(1.0f - 0.25f * uv_x, 1.0f - 0.25f * uv_y);
                    uvMatrixTB[uv_y, uv_x] = new Vector2(0.25f * uv_x, 0.25f * uv_y);
                    uvMatrixLRTB[uv_y, uv_x] = new Vector2(1.0f - 0.25f * uv_x, 0.25f * uv_y);
                    uvMatrixRotCCW[uv_x, uv_y] = new Vector2(0.25f * uv_x, 1.0f - 0.25f * uv_y);
                    uvMatrixRotCW[uv_x, uv_y] = new Vector2(0.25f * uv_y, 1.0f - 0.25f * uv_x);
                }
            }

            tiles = new List<Tile>();

            for (int t_x = 0; t_x < 16; t_x++)
            {
                for (int t_y = 0; t_y < 16; t_y++)
                {
                    Tile tile = new Tile();
                    int tileID = TIL.Tiles[t_y, t_x].TileID;
                    string texPath1 = ZON.Textures[ZON.Tiles[tileID].ID1].TexPath;
                    string texPath2 = ZON.Textures[ZON.Tiles[tileID].ID2].TexPath;
                    tile.bottomTex = texPath1;
                    tile.topTex = texPath2;
                    tiles.Add(tile);
                }
            }

            Texture2D lightTex = SaveTexture(RoseDdsLoader.LoadFromFile(groundLight), groundLight, shared: false);

            foreach (Tile tile in tiles)
            {
                tile.bottomRect = atlasRectHash[tile.bottomTex];
                tile.topRect = atlasRectHash[tile.topTex];
            }

            Material material = new Material(terrainShader);

            material.SetTexture("_BottomTex", atlas);
            material.SetTexture("_TopTex", atlas);
            material.SetTexture("_LightTex", lightTex);
            material = SaveMaterial(material, $"{name}_Ground");

            float l = HIM.Length - 1;
            float w = HIM.Width - 1;

            int triangleID = 0;

            for (int x = 0; x < HIM.Length - 1; x++)
            {
                for (int y = 0; y < HIM.Width - 1; y++)
                {
                    int a = i_v++;
                    int b = i_v++;
                    int c = i_v++;
                    int d = i_v++;

                    uvsLight[a] = new Color((float)y / w, 1.0f - (float)x / l, 0.0f);
                    uvsLight[b] = new Color((float)y / w, 1.0f - (float)(x + 1) / l, 0.0f);
                    uvsLight[c] = new Color((float)(y + 1) / w, 1.0f - (float)(x + 1) / l, 0.0f);
                    uvsLight[d] = new Color((float)(y + 1) / w, 1.0f - (float)(x) / l, 0.0f);

                    vertices[a] = new Vector3(x * m_xStride + x_offset, HIM.Heights[x, y] / heightScaler, y * m_yStride + y_offset);
                    vertices[b] = new Vector3((x + 1) * m_xStride + x_offset, HIM.Heights[x + 1, y] / heightScaler, y * m_yStride + y_offset);
                    vertices[c] = new Vector3((x + 1) * m_xStride + x_offset, HIM.Heights[x + 1, y + 1] / heightScaler, (y + 1) * m_yStride + y_offset);
                    vertices[d] = new Vector3(x * m_xStride + x_offset, HIM.Heights[x, y + 1] / heightScaler, (y + 1) * m_yStride + y_offset);

                    if (y == 0)
                    {
                        EditorUtils.addVertexToLookup(edgeVertexLookup, vertices[a].ToString(), a);
                        EditorUtils.addVertexToLookup(edgeVertexLookup, vertices[a].ToString(), b);
                    }

                    if (y == HIM.Width - 1)
                    {
                        EditorUtils.addVertexToLookup(edgeVertexLookup, vertices[a].ToString(), d);
                        EditorUtils.addVertexToLookup(edgeVertexLookup, vertices[a].ToString(), c);
                    }

                    if (x == 0)
                    {
                        EditorUtils.addVertexToLookup(edgeVertexLookup, vertices[a].ToString(), a);
                        EditorUtils.addVertexToLookup(edgeVertexLookup, vertices[a].ToString(), d);
                    }

                    if (x == HIM.Length - 1)
                    {
                        EditorUtils.addVertexToLookup(edgeVertexLookup, vertices[a].ToString(), b);
                        EditorUtils.addVertexToLookup(edgeVertexLookup, vertices[a].ToString(), c);
                    }

                    int tileX = x / 4;
                    int tileY = y / 4;
                    int tileID = tileY * 16 + tileX;

                    ZON.RotationType rotation = ZON.Tiles[TIL.Tiles[tileX, tileY].TileID].Rotation;
                    Vector2[,] rotMatrix;

                    switch (rotation)
                    {
                        case ZON.RotationType.Normal:
                            rotMatrix = uvMatrix;
                            break;
                        case ZON.RotationType.LeftRight:
                            rotMatrix = uvMatrixLR;
                            break;
                        case ZON.RotationType.LeftRightTopBottom:
                            rotMatrix = uvMatrixLRTB;
                            break;
                        case ZON.RotationType.Rotate90Clockwise:
                            rotMatrix = uvMatrixRotCW;
                            break;
                        case ZON.RotationType.Rotate90CounterClockwise:
                            rotMatrix = uvMatrixRotCCW;
                            break;
                        case ZON.RotationType.TopBottom:
                            rotMatrix = uvMatrixTB;
                            break;
                        default:
                            rotMatrix = uvMatrix;
                            break;
                    }

                    uvsTop[a] = tiles[tileID].GetUVTop(rotMatrix[x % 4, y % 4]);
                    uvsTop[b] = tiles[tileID].GetUVTop(rotMatrix[(x % 4 + 1) % 5, y % 4]);
                    uvsTop[c] = tiles[tileID].GetUVTop(rotMatrix[(x % 4 + 1) % 5, (y % 4 + 1) % 5]);
                    uvsTop[d] = tiles[tileID].GetUVTop(rotMatrix[x % 4, (y % 4 + 1) % 5]);

                    uvsBottom[a] = tiles[tileID].GetUVBottom(rotMatrix[x % 4, y % 4]);
                    uvsBottom[b] = tiles[tileID].GetUVBottom(rotMatrix[(x % 4 + 1) % 5, y % 4]);
                    uvsBottom[c] = tiles[tileID].GetUVBottom(rotMatrix[(x % 4 + 1) % 5, (y % 4 + 1) % 5]);
                    uvsBottom[d] = tiles[tileID].GetUVBottom(rotMatrix[x % 4, (y % 4 + 1) % 5]);

                    triangles[triangleID++] = a;
                    triangles[triangleID++] = d;
                    triangles[triangleID++] = b;

                    triangles[triangleID++] = b;
                    triangles[triangleID++] = d;
                    triangles[triangleID++] = c;
                }
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvsBottom;
            mesh.uv2 = uvsTop;
            mesh.colors = uvsLight;

            mesh.RecalculateNormals();

            if (blendNormals)
            {
                Vector3[] normals = new Vector3[mesh.vertexCount];
                Dictionary<String, List<int>> vertexLookup = new Dictionary<string, List<int>>();
                for (int i = 0; i < mesh.vertexCount; i++)
                    EditorUtils.addVertexToLookup(vertexLookup, mesh.vertices[i].ToString(), i);

                foreach (KeyValuePair<String, List<int>> entry in vertexLookup)
                {
                    Vector3 avg = Vector3.zero;
                    foreach (int id in entry.Value)
                        avg += mesh.normals[id];

                    avg.Normalize();

                    foreach (int id in entry.Value)
                        normals[id] = avg;
                }

                mesh.normals = normals;
            }

            EditorUtils.calculateMeshTangents(mesh);
            mesh.RecalculateBounds();
            mesh.Optimize();

            mesh = SaveMesh(mesh, $"{name}_Ground");

            GameObject patchObject = new GameObject();
            patchObject.name = "patch_" + this.name;

            patchObject.AddComponent<MeshFilter>().mesh = mesh;
            patchObject.AddComponent<MeshRenderer>();
            patchObject.AddComponent<MeshCollider>();

            MeshRenderer patchRenderer = patchObject.GetComponent<MeshRenderer>();
            patchRenderer.material = material;
            patchObject.transform.parent = terrainParent;
            patchObject.layer = LayerMask.NameToLayer("Floor");

            GameObject deco = new GameObject();
            deco.name = "deco_" + this.name;
            deco.transform.parent = objectsParent;
            deco.layer = LayerMask.NameToLayer("MapObjects");

            ImportObjectGroup(IFO.Decoration, ZSCDeco, LITDeco, deco.transform, "Deco", objectShader);

            GameObject cnst = new GameObject();
            cnst.name = "cnst_" + this.name;
            cnst.transform.parent = objectsParent;
            cnst.layer = LayerMask.NameToLayer("MapObjects");

            ImportObjectGroup(IFO.Construction, ZSCConst, LITConst, deco.transform, "Const", objectShader);

            return true;
        }  // Import()

        /// <summary>
        /// Shared body for the old Decoration and Construction loops.
        /// </summary>
        private void ImportObjectGroup(List<IFO.BaseIFO> ifoObjects, ZSC zsc, LIT lit, Transform parent, string groupName, Shader objectShader)
        {
            for (int obj = 0; obj < ifoObjects.Count; obj++)
            {
                IFO.BaseIFO ifo = ifoObjects[obj];
                GameObject terrainObject = new GameObject();
                terrainObject.layer = LayerMask.NameToLayer("MapObjects");
                terrainObject.name = $"{groupName}_{ifo.MapPosition.x}_{ifo.MapPosition.y}";


                terrainObject.transform.SetParent(parent, false);
                terrainObject.transform.localPosition = (ifo.Position / 100.0f);

                //   Debug.Log($"IFO local={terrainObject.transform.localPosition} world={terrainObject.transform.position}");
                bool isAnimated = false;
                AnimationClip clip = new AnimationClip();
                clip.legacy = true;

                var zscObj = zsc.Objects[ifo.ObjectID];

                bool hasLitEntry = obj < lit.Objects.Count;

                if (!hasLitEntry)
                {
                    Debug.LogWarning($"{groupName}_{obj}: no matching LIT entry (LIT has {lit.Objects.Count}), lightmap skipped for this instance.");
                }

                for (int part = 0; part < zscObj.Models.Count; part++)
                {
                    try
                    {
                        ZSC.Object.Model model = zscObj.Models[part];
                        string zmsPath = root3DDataDirectory.Parent.FullName + "/" + zsc.Models[model.ModelID].Replace("\\", "/");
                        string texPath = zsc.Textures[model.TextureID].Path;

                        Texture2D mainTex = SaveTexture(RoseDdsLoader.LoadFromFile(Path.Combine(RoseImporter.DataPath, texPath)), texPath);

                        bool hasLitPart = hasLitEntry && part < lit.Objects[obj].Parts.Count;

                        string lightPath = null;
                        Vector2 lmOffset = Vector2.zero;
                        Vector2 lmScale = Vector2.one;

                        if (hasLitPart)
                        {
                            LIT.Object.Part lmData = lit.Objects[obj].Parts[part];
                            lightPath = EditorUtils.FixPath(assetDirectory.Parent.FullName + "\\" + this.name + "\\LIGHTMAP\\" + lmData.DDSName);

                            float objScale = 1.0f / lmData.ObjectsPerWidth;
                            float rowNum = (float)Math.Floor((double)lmData.MapPosition / lmData.ObjectsPerWidth);
                            float colNum = (float)lmData.MapPosition % lmData.ObjectsPerWidth;

                            lmOffset = new Vector2(colNum * objScale, rowNum * objScale);
                            lmScale = new Vector2(objScale, objScale);
                        }

                        var zms = new ZMS(zmsPath, lmScale, lmOffset);

                        Material mat;

                        mat = new Material(objectShader);
                        mat.SetTexture("_MainTex", mainTex);

                        if (hasLitPart)
                        {
                            Texture2D lightTexture = SaveTexture(RoseDdsLoader.LoadFromFile(lightPath), lightPath, shared: false);
                            mat.SetTexture("_LightTex", lightTexture);
                        }

                        mat = SaveMaterial(mat, $"{texPath}_{(hasLitPart ? lightPath : "nolit")}_{objectShader.name}");

                        GameObject modelObject = new GameObject();
                        modelObject.layer = LayerMask.NameToLayer("MapObjects");
                        modelObject.transform.parent = terrainObject.transform;

                        modelObject.transform.localScale = model.Scale;
                        modelObject.transform.localPosition = (model.Position / 100.0f);
                        modelObject.transform.rotation = model.Rotation;

                        var meshKey = $"{mapName}_{zmsPath}_{lmOffset}_{lmScale}";
                        var mesh = SaveMesh(zms.getMesh(), meshKey);
                        modelObject.AddComponent<MeshFilter>().mesh = mesh;
                        modelObject.AddComponent<MeshRenderer>();
                        modelObject.name = new DirectoryInfo(zmsPath).Name;
                        MeshRenderer renderer = modelObject.GetComponent<MeshRenderer>();
                        renderer.material = mat;

                        if (model.CollisionLevel != ZSC.CollisionLevelType.None)
                            modelObject.AddComponent<MeshCollider>();

                        string zmoPath = model.Motion;
                        if (zmoPath != null && zmoPath.ToLower().Contains("zmo"))
                        {
                            isAnimated = true;
                            var zmo = new ZMO(Path.Combine(RoseImporter.DataPath, model.Motion), false, true);
                            clip = zmo.buildAnimationClip(modelObject.name, clip);
                        }
                        else
                        {
                            modelObject.isStatic = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Error while loading {groupName} object: " + ex.Message);
                    }
                }

                terrainObject.transform.rotation = ifo.Rotation;
                terrainObject.transform.localScale = ifo.Scale;

                if (isAnimated)
                {
                    Animation animation = terrainObject.GetComponent<Animation>();
                    if (animation == null)
                        animation = terrainObject.AddComponent<Animation>();

                    clip = SaveAnimationClip(clip, $"{name}_{groupName}_{terrainObject.name}_{obj}");
                    clip.wrapMode = WrapMode.Loop;
                    animation.AddClip(clip, terrainObject.name);
                    animation.clip = clip;
                }
                else
                {
                    terrainObject.isStatic = true;
                }
            }
        }

        /// <summary>
        /// Clear the cache.
        /// </summary>
        public static void ClearCache()
        {
            TextureCache.Clear();
            MeshCache.Clear();
            MaterialCache.Clear();
            AnimationCache.Clear();
            createdFolders.Clear();
        }

        /// <summary>
        /// Save the mesh.
        /// </summary>
        /// <param name="mesh">Mesh.</param>
        /// <param name="key">Key of the mesh.</param>
        /// <returns>Mesh.</returns>
        private Mesh SaveMesh(Mesh mesh, string key)
        {
            if (mesh == null)
                return null;

            var cacheKey = key.ToLower();

            if (MeshCache.TryGetValue(cacheKey, out var cached))
            {
                UnityEngine.Object.DestroyImmediate(mesh); // the freshly-built mesh is redundant, drop it

                return cached;
            }

            string folder = key.Contains("_Ground") ? MapMeshFolder : GameDataPaths.Maps.SharedMeshes;
            EnsureCachedFolder($"{folder}/dummy.asset");

            string safeName = SafeFileName(cacheKey);
            string path = $"{folder}/{safeName}.asset";

            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);

            if (existing != null)
            {
                MeshCache[cacheKey] = existing;
                UnityEngine.Object.DestroyImmediate(mesh);
                return existing;
            }

            mesh.name = safeName;
            AssetDatabase.CreateAsset(mesh, path);
            MeshCache[cacheKey] = mesh;
            return mesh;
        }

        /// <summary>
        /// Save the material.
        /// </summary>
        /// <param name="mat"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        private Material SaveMaterial(Material mat, string key)
        {
            if (mat == null)
            {
                return null;
            }

            if (MaterialCache.TryGetValue(key, out var cached))
            {
                GameObject.DestroyImmediate(mat);

                return cached;
            }

            string folder = key.Contains("Ground") || key.Contains("Lightmap") ? MapMaterialFolder : GameDataPaths.Maps.SharedMaterials;
            EnsureCachedFolder($"{folder}/dummy.mat");

            string safeName = SafeFileName(key);
            string path = $"{folder}/{safeName}.mat";

            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(mat);
                MaterialCache[key] = existing;

                return existing;
            }

            AssetDatabase.CreateAsset(mat, path);

            MaterialCache[key] = mat;

            return mat;
        }

        private Texture2D SaveTexture(Texture2D tex, string rosePath, bool shared = true)
        {
            if (tex == null)
            {
                return null;
            }

            string safeName = SafeFileName(rosePath);

            var path = shared ? $"{GameDataPaths.Maps.Shared}/Textures/{safeName}.asset" : $"{MapRoot}/Lightmaps/{safeName}.asset";

            if (TextureCache.TryGetValue(path, out var alreadySaved))
            {
                return alreadySaved;
            }

            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

            if (existing != null)
            {
                TextureCache[path] = existing;

                return existing;
            }

            EnsureCachedFolder(path);
            AssetDatabase.CreateAsset(tex, path);
            TextureCache[path] = tex;
            return tex;
        }

        private AnimationClip SaveAnimationClip(AnimationClip clip, string key)
        {
            if (clip == null)
                return null;

            if (AnimationCache.TryGetValue(key, out var cached))
                return cached;

            string folder = GameDataPaths.Maps.Animations; ;
            EnsureCachedFolder($"{folder}/dummy.anim");

            string safeName = SafeFileName(key);
            string path = $"{folder}/{safeName}.anim";

            var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);

            if (existing != null)
            {
                AnimationCache[key] = existing;
                return existing;
            }

            AssetDatabase.CreateAsset(clip, path);
            AnimationCache[key] = clip;
            return clip;
        }

        private static string SafeFileName(string key) => key.Replace("\\", "_").Replace("/", "_").Replace(":", "_");

        public static void EnsureCachedFolder(string assetPath)
        {
            string folder = Path.GetDirectoryName(assetPath)?.Replace("\\", "/");

            if (string.IsNullOrEmpty(folder))
                return;

            if (createdFolders.Contains(folder))
                return;

            string[] parts = folder.Split('/');
            string current = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];

                if (!AssetDatabase.IsValidFolder(next) && !createdFolders.Contains(next))
                {
                    string guid = AssetDatabase.CreateFolder(current, parts[i]);
                    string createdPath = AssetDatabase.GUIDToAssetPath(guid);

                    createdFolders.Add(createdPath);
                }

                current = next;
            }

            createdFolders.Add(folder);
        }

        private string MapRoot => $"{GameDataPaths.Maps.Root}/{mapName}";

        private string MapMeshFolder => $"{MapRoot}/Meshes"; // TODO : use ImportContext

        private string MapMaterialFolder => $"{MapRoot}/Materials";
    }

    /// <summary>
    /// Tile.
    /// </summary>
    public class Tile
    {
        public Rect bottomRect { get; set; }
        public Rect topRect { get; set; }
        public string bottomTex { get; set; }
        public string topTex { get; set; }

        public Tile()
        {
        }

        public void setRects(Rect bot, Rect top)
        {
            bottomRect = bot;
            topRect = top;
        }

        public Vector2 GetUVTop(Vector2 uv)
        {
            if (uv.x < 0.01f) uv.x += 0.01f;
            else if (uv.x > 0.99f) uv.x *= 0.99f;
            if (uv.y < 0.01f) uv.y += 0.01f;
            else if (uv.y > 0.99f) uv.y *= 0.99f;

            return new Vector2((uv.x * topRect.width) + topRect.x, (uv.y * topRect.height) + topRect.y);
        }

        public Vector2 GetUVBottom(Vector2 uv)
        {
            if (uv.x < 0.01f) uv.x += 0.01f;
            else if (uv.x > 0.99f) uv.x *= 0.99f;
            if (uv.y < 0.01f) uv.y += 0.01f;
            else if (uv.y > 0.99f) uv.y *= 0.99f;

            return new Vector2((uv.x * bottomRect.width) + bottomRect.x, (uv.y * bottomRect.height) + bottomRect.y);
        }
    }
}