#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityRose.Formats;
using System.Linq;
using UnityRose.Import;

namespace UnityRose.Game
{
    public class RosePatch
    {
        private const bool realTimeBaking = false;
        private const bool blendNormals = false;

        private const string DataRoot = "Assets/Data/Patchs";

        public DirectoryInfo m_assetDir { get; set; }
        public DirectoryInfo m_unityAssetDir { get; set; }
        public DirectoryInfo m_3dDataDir { get; set; }
        public string m_name { get; set; }
        public bool m_isValid { get; set; }
        public Vector2 center { get; set; }
        private string mapName;

        public HIM m_HIM { get; set; }
        public TIL m_TIL { get; set; }
        public ZON m_ZON { get; set; }
        public IFO m_IFO { get; set; }
        public ZSC m_ZSC_Cnst { get; set; }
        public ZSC m_ZSC_Deco { get; set; }
        public LIT m_LIT_Cnst { get; set; }
        public LIT m_LIT_Deco { get; set; }
        // TODO: Add MOV, and any others here

        public int m_Col { get; set; }
        public int m_Row { get; set; }

        public Mesh m_mesh { get; set; }
        public List<Tile> m_tiles { get; set; }

        public Dictionary<String, List<int>> edgeVertexLookup { get; set; }

        private static readonly Dictionary<string, Texture2D> TextureCache = new();
        private static readonly Dictionary<string, Mesh> MeshCache = new();
        private static readonly Dictionary<string, Material> MaterialCache = new();
        private static readonly Dictionary<string, AnimationClip> AnimationCache = new();
        private static readonly HashSet<string> createdFolders = new HashSet<string>();

        private string groundLight;
        private string patchFolder; // Assets/Data/Patchs/<mapName_or_dir>/<patchName>

        public RosePatch()
        {
            this.m_Col = 0;
            this.m_Row = 0;
            this.m_isValid = false;
        }

        public RosePatch(DirectoryInfo assetDir)
        {
            this.m_assetDir = assetDir;
            this.m_3dDataDir = new DirectoryInfo(this.m_assetDir.Parent.Parent.Parent.Parent.FullName);
            this.m_name = assetDir.Name.Replace(".*", "");
            this.m_Col = 0;
            this.m_Row = 0;
            this.m_isValid = false;
            this.center = new Vector2(0.0f, 0.0f);


            if (assetDir.Exists)
            {
                // figure out row and column
                char[] sep = { '_', '.' };
                string[] tokens = assetDir.Name.Split(sep);

                if (tokens.Length > 1) // Akima : lazy way to check if this is about a legit patch folder, if you need proper customization, it's here
                {
                    int col = int.Parse(tokens[0]);
                    int row = int.Parse(tokens[1]);

                    // figure out if the given name exists and this patch is valid
                    if (row > 0 && row < 100 && col > 0 && col < 100)
                        m_isValid = true;

                    if (m_isValid)
                    {
                        m_Row = row;
                        m_Col = col;
                    }
                }

                else
                {
                    m_isValid = false;
                }
            }
            else
                m_isValid = false;

            // e.g. Assets/Data/Patchs/EJT01/1_1
            mapName = assetDir.Parent != null ? assetDir.Parent.Name : "UnknownMap";

            patchFolder = $"{DataRoot}/{mapName}/{m_name}";
        }

        public RosePatch(DirectoryInfo assetDir, ZON zon)
            : this(assetDir)
        {
            this.m_ZON = zon;
        }

        public bool Load(int mapID)
        {
            if (!m_isValid)
            {
                Debug.LogError("Cannot load patch at path " + this.m_assetDir);

                return false;
            }

            // TODO: add error handling for failure to load the following files
            this.m_HIM = new HIM(this.m_assetDir.Parent.FullName + "/" + this.m_name + ".HIM");
            this.m_TIL = new TIL(this.m_assetDir.Parent.FullName + "/" + this.m_name + ".TIL");
            if (this.m_ZON == null)  // load ZON if it was never passed to the patch constructor
                this.m_ZON = new ZON(this.m_assetDir.Parent.FullName + "/" + this.m_assetDir.Parent.Name + ".ZON");
            this.m_IFO = new IFO(this.m_assetDir.Parent.FullName + "/" + this.m_name + ".IFO");

            string litPath = Utils.FixPath(this.m_assetDir.Parent.FullName + "\\" + this.m_name + "\\LIGHTMAP\\BUILDINGLIGHTMAPDATA.LIT");
            groundLight = Utils.FixPath(this.m_assetDir.Parent.FullName + "\\" + this.m_name + "\\" + this.m_name + "_PLANELIGHTINGMAP.DDS");

            string zscPathDeco = Utils.FixPath(RoseDataSource.DataPath + "/" + ResourceManager.Instance.stb_zone.Cells[mapID][12]);
            string zscPathCnst = Utils.FixPath(RoseDataSource.DataPath + "/" + ResourceManager.Instance.stb_zone.Cells[mapID][13]);

            m_ZSC_Cnst = new ZSC(zscPathCnst);
            m_ZSC_Deco = new ZSC(zscPathDeco);

            m_LIT_Cnst = new LIT(litPath);
            m_LIT_Deco = new LIT(litPath.Replace("building", "object"));
            // TODO: add any new file loads here

            edgeVertexLookup = new Dictionary<string, List<int>>();

            return true;
        }

        public void UpdateAtlas(ref Dictionary<string, Rect> atlasRectHash, ref Dictionary<string, Texture2D> atlasTexHash, ref List<Texture2D> textures)
        {
            for (int t_x = 0; t_x < 16; t_x++)
            {
                for (int t_y = 0; t_y < 16; t_y++)
                {
                    int tileID = m_TIL.Tiles[t_x, t_y].TileID;
                    string texPath1 = m_ZON.Textures[m_ZON.Tiles[tileID].ID1].TexPath;
                    string texPath2 = m_ZON.Textures[m_ZON.Tiles[tileID].ID2].TexPath;

                    Texture2D tex1 = m_ZON.Textures[m_ZON.Tiles[tileID].ID1].Tex;
                    Texture2D tex2 = m_ZON.Textures[m_ZON.Tiles[tileID].ID2].Tex;
                    // Adding an existing texture to atlas will cause an exception, so catch it but do nothing
                    // as this is expected to happen
                    try
                    {
                        atlasRectHash.Add(texPath1, new Rect());
                        atlasTexHash.Add(texPath1, tex1);
                        textures.Add(tex1);
                    }
                    catch (Exception e) { }

                    try
                    {
                        atlasRectHash.Add(texPath2, new Rect());
                        atlasTexHash.Add(texPath2, tex2);
                        textures.Add(tex2);
                    }
                    catch (Exception e) { }
                }
            }
        }

        public bool Import(Transform terrainParent, Transform objectsParent, Texture2D atlas, Texture2D atlas_normal, Dictionary<string, Rect> atlasRectHash)
        {
            if (!m_isValid)
            {
                Debug.LogError("Cannot Import patch_" + this.m_name);
                return false;
            }

            return ImportInternal(terrainParent, objectsParent, atlas, atlas_normal, atlasRectHash);
        }

        private bool ImportInternal(Transform terrainParent, Transform objectsParent, Texture2D atlas, Texture2D atlas_normal, Dictionary<string, Rect> atlasRectHash)
        {
            int nVertices = 64 * 64 * 4;
            Vector3[] vertices = new Vector3[nVertices];
            Vector2[] uvsBottom = new Vector2[nVertices];
            Vector2[] uvsTop = new Vector2[nVertices];
            Color[] uvsLight = new Color[nVertices];
            int[] triangles = new int[(m_HIM.Length - 1) * (m_HIM.Width - 1) * 6];

            int i_v = 0;      // vertex index

            // TODO: move these hardcoded values to a more appropriate place
            float m_xStride = 2.5f;
            float m_yStride = 2.5f;
            float heightScaler = 300.0f / (m_xStride * 1.2f);
            float x_offset = this.m_Row * m_xStride * 64.0f;
            float y_offset = this.m_Col * m_yStride * 64.0f;
            center = new Vector2(x_offset + m_xStride * 32.0f, y_offset + m_yStride * 32.0f);

            m_mesh = new Mesh();

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
                    uvMatrix[uv_y, uv_x] = new Vector2(0.25f * (float)uv_x, 1.0f - 0.25f * (float)uv_y);
                    uvMatrixLR[uv_y, uv_x] = new Vector2(1.0f - 0.25f * (float)uv_x, 1.0f - 0.25f * (float)uv_y);
                    uvMatrixTB[uv_y, uv_x] = new Vector2(0.25f * (float)uv_x, 0.25f * (float)uv_y);
                    uvMatrixLRTB[uv_y, uv_x] = new Vector2(1.0f - 0.25f * (float)uv_x, 0.25f * (float)uv_y);
                    uvMatrixRotCCW[uv_x, uv_y] = new Vector2(0.25f * (float)uv_x, 1.0f - 0.25f * (float)uv_y);
                    uvMatrixRotCW[uv_x, uv_y] = new Vector2(0.25f * (float)uv_y, 1.0f - 0.25f * (float)uv_x);
                }
            }

            m_tiles = new List<Tile>();

            for (int t_x = 0; t_x < 16; t_x++)
            {
                for (int t_y = 0; t_y < 16; t_y++)
                {
                    Tile tile = new Tile();
                    int tileID = m_TIL.Tiles[t_y, t_x].TileID;
                    string texPath1 = m_ZON.Textures[m_ZON.Tiles[tileID].ID1].TexPath;
                    string texPath2 = m_ZON.Textures[m_ZON.Tiles[tileID].ID2].TexPath;
                    tile.bottomTex = texPath1;
                    tile.topTex = texPath2;
                    m_tiles.Add(tile);
                }
            }

            Texture2D lightTex = SaveTexture(RoseTextureImporter.Import(groundLight), groundLight, shared: false);

            foreach (Tile tile in m_tiles)
            {
                tile.bottomRect = atlasRectHash[tile.bottomTex];
                tile.topRect = atlasRectHash[tile.topTex];
            }

            Material material = new Material(Shader.Find("Custom/TerrainShader2"));
            material.SetTexture("_BottomTex", atlas);
            material.SetTexture("_TopTex", atlas);
            material.SetTexture("_LightTex", lightTex);
            material = SaveMaterial(material, $"{m_name}_Ground");

            float l = m_HIM.Length - 1;
            float w = m_HIM.Width - 1;

            int triangleID = 0;
            for (int x = 0; x < m_HIM.Length - 1; x++)
            {
                for (int y = 0; y < m_HIM.Width - 1; y++)
                {
                    int a = i_v++;
                    int b = i_v++;
                    int c = i_v++;
                    int d = i_v++;

                    uvsLight[a] = new Color((float)y / w, 1.0f - (float)x / l, 0.0f);
                    uvsLight[b] = new Color((float)y / w, 1.0f - (float)(x + 1) / l, 0.0f);
                    uvsLight[c] = new Color((float)(y + 1) / w, 1.0f - (float)(x + 1) / l, 0.0f);
                    uvsLight[d] = new Color((float)(y + 1) / w, 1.0f - (float)(x) / l, 0.0f);

                    vertices[a] = new Vector3(x * m_xStride + x_offset, m_HIM.Heights[x, y] / heightScaler, y * m_yStride + y_offset);
                    vertices[b] = new Vector3((x + 1) * m_xStride + x_offset, m_HIM.Heights[x + 1, y] / heightScaler, y * m_yStride + y_offset);
                    vertices[c] = new Vector3((x + 1) * m_xStride + x_offset, m_HIM.Heights[x + 1, y + 1] / heightScaler, (y + 1) * m_yStride + y_offset);
                    vertices[d] = new Vector3(x * m_xStride + x_offset, m_HIM.Heights[x, y + 1] / heightScaler, (y + 1) * m_yStride + y_offset);

                    if (y == 0)
                    {
                        Utils.addVertexToLookup(edgeVertexLookup, vertices[a].ToString(), a);
                        Utils.addVertexToLookup(edgeVertexLookup, vertices[a].ToString(), b);
                    }
                    if (y == m_HIM.Width - 1)
                    {
                        Utils.addVertexToLookup(edgeVertexLookup, vertices[a].ToString(), d);
                        Utils.addVertexToLookup(edgeVertexLookup, vertices[a].ToString(), c);
                    }
                    if (x == 0)
                    {
                        Utils.addVertexToLookup(edgeVertexLookup, vertices[a].ToString(), a);
                        Utils.addVertexToLookup(edgeVertexLookup, vertices[a].ToString(), d);
                    }
                    if (x == m_HIM.Length - 1)
                    {
                        Utils.addVertexToLookup(edgeVertexLookup, vertices[a].ToString(), b);
                        Utils.addVertexToLookup(edgeVertexLookup, vertices[a].ToString(), c);
                    }

                    int tileX = x / 4;
                    int tileY = y / 4;
                    int tileID = tileY * 16 + tileX;

                    ZON.RotationType rotation = m_ZON.Tiles[m_TIL.Tiles[tileX, tileY].TileID].Rotation;
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

                    uvsTop[a] = m_tiles[tileID].GetUVTop(rotMatrix[x % 4, y % 4]);
                    uvsTop[b] = m_tiles[tileID].GetUVTop(rotMatrix[(x % 4 + 1) % 5, y % 4]);
                    uvsTop[c] = m_tiles[tileID].GetUVTop(rotMatrix[(x % 4 + 1) % 5, (y % 4 + 1) % 5]);
                    uvsTop[d] = m_tiles[tileID].GetUVTop(rotMatrix[x % 4, (y % 4 + 1) % 5]);

                    uvsBottom[a] = m_tiles[tileID].GetUVBottom(rotMatrix[x % 4, y % 4]);
                    uvsBottom[b] = m_tiles[tileID].GetUVBottom(rotMatrix[(x % 4 + 1) % 5, y % 4]);
                    uvsBottom[c] = m_tiles[tileID].GetUVBottom(rotMatrix[(x % 4 + 1) % 5, (y % 4 + 1) % 5]);
                    uvsBottom[d] = m_tiles[tileID].GetUVBottom(rotMatrix[x % 4, (y % 4 + 1) % 5]);

                    triangles[triangleID++] = a;
                    triangles[triangleID++] = d;
                    triangles[triangleID++] = b;

                    triangles[triangleID++] = b;
                    triangles[triangleID++] = d;
                    triangles[triangleID++] = c;
                }
            }

            m_mesh.vertices = vertices;
            m_mesh.triangles = triangles;
            m_mesh.uv = uvsBottom;
            m_mesh.uv2 = uvsTop;
            m_mesh.colors = uvsLight;

            m_mesh.RecalculateNormals();

            if (blendNormals)
            {
                Vector3[] normals = new Vector3[m_mesh.vertexCount];
                Dictionary<String, List<int>> vertexLookup = new Dictionary<String, List<int>>();
                for (int i = 0; i < m_mesh.vertexCount; i++)
                    Utils.addVertexToLookup(vertexLookup, m_mesh.vertices[i].ToString(), i);

                foreach (KeyValuePair<String, List<int>> entry in vertexLookup)
                {
                    Vector3 avg = Vector3.zero;
                    foreach (int id in entry.Value)
                        avg += m_mesh.normals[id];

                    avg.Normalize();

                    foreach (int id in entry.Value)
                        normals[id] = avg;
                }

                m_mesh.normals = normals;
            }

            Utils.calculateMeshTangents(m_mesh);
            m_mesh.RecalculateBounds();
            m_mesh.Optimize();

            m_mesh = SaveMesh(m_mesh, $"{m_name}_Ground");

            GameObject patchObject = new GameObject();
            patchObject.name = "patch_" + this.m_name;

            patchObject.AddComponent<MeshFilter>().mesh = m_mesh;
            patchObject.AddComponent<MeshRenderer>();
            patchObject.AddComponent<MeshCollider>();

            MeshRenderer patchRenderer = patchObject.GetComponent<MeshRenderer>();
            patchRenderer.material = material;
            patchObject.transform.parent = terrainParent;
            patchObject.layer = LayerMask.NameToLayer("Floor");

            GameObject deco = new GameObject();
            deco.name = "deco_" + this.m_name;
            deco.transform.parent = objectsParent;
            deco.layer = LayerMask.NameToLayer("MapObjects");

            ImportObjectGroup(m_IFO.Decoration, m_ZSC_Deco, m_LIT_Deco, deco.transform, "Deco");

            GameObject cnst = new GameObject();
            cnst.name = "cnst_" + this.m_name;
            cnst.transform.parent = objectsParent;
            cnst.layer = LayerMask.NameToLayer("MapObjects");

            ImportObjectGroup(m_IFO.Construction, m_ZSC_Cnst, m_LIT_Cnst, deco.transform, "Const");

            return true;
        }  // Import()

        /// <summary>
        /// Shared body for the old Decoration and Construction loops.
        /// </summary>
        private void ImportObjectGroup(List<IFO.BaseIFO> ifoObjects, ZSC zsc, LIT lit, Transform parent, string groupName)
        {
            for (int obj = 0; obj < ifoObjects.Count; obj++)
            {
                IFO.BaseIFO ifo = ifoObjects[obj];
                GameObject terrainObject = new GameObject();
                terrainObject.layer = LayerMask.NameToLayer("MapObjects");
                terrainObject.name = $"{groupName}_{ifo.MapPosition.x}_{ifo.MapPosition.y}";
                terrainObject.transform.parent = parent;
                terrainObject.transform.localPosition = (ifo.Position / 100.0f);
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
                        string zmsPath = m_3dDataDir.Parent.FullName + "/" + zsc.Models[model.ModelID].Replace("\\", "/");
                        string texPath = zsc.Textures[model.TextureID].Path;

                        Texture2D mainTex = SaveTexture(RoseTextureImporter.Import(texPath), texPath);

                        bool hasLitPart = hasLitEntry && part < lit.Objects[obj].Parts.Count;

                        string lightPath = null;
                        Vector2 lmOffset = Vector2.zero;
                        Vector2 lmScale = Vector2.one;

                        if (hasLitPart)
                        {
                            LIT.Object.Part lmData = lit.Objects[obj].Parts[part];
                            lightPath = Utils.FixPath(this.m_assetDir.Parent.FullName + "\\" + this.m_name + "\\LIGHTMAP\\" + lmData.DDSName);

                            float objScale = 1.0f / (float)lmData.ObjectsPerWidth;
                            float rowNum = (float)Math.Floor((double)lmData.MapPosition / lmData.ObjectsPerWidth);
                            float colNum = (float)lmData.MapPosition % lmData.ObjectsPerWidth;

                            lmOffset = new Vector2(colNum * objScale, rowNum * objScale);
                            lmScale = new Vector2(objScale, objScale);
                        }

                        var zms = new ZMS(zmsPath, lmScale, lmOffset);

                        Material mat;


                        mat = new Material(Shader.Find("Custom/ObjectShader"));
                        mat.SetTexture("_MainTex", mainTex);

                        if (hasLitPart)
                        {
                            Texture2D lightTexture = SaveTexture(RoseTextureImporter.Import(lightPath), lightPath, shared: false);
                            mat.SetTexture("_LightTex", lightTexture);
                        }
                        
                        mat = SaveMaterial(mat, $"{texPath}_{(hasLitPart ? lightPath : "nolit")}_{Shader.Find(realTimeBaking ? "Standard" : "Custom/ObjectShader").name}");

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
                            var zmo = new ZMO(Path.Combine(RoseDataSource.DataPath, model.Motion), false, true);
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

                    clip = SaveAnimationClip(clip, $"{m_name}_{groupName}_{terrainObject.name}_{obj}");
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

        public static void ClearCache()
        {
            TextureCache.Clear();
            MeshCache.Clear();
            MaterialCache.Clear();
            AnimationCache.Clear();
            createdFolders.Clear();
        }

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

            string folder = key.Contains("_Ground") ? MapMeshFolder : SharedMeshFolder;
            EnsureFolder($"{folder}/dummy.asset");

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

        private Material SaveMaterial(Material mat, string key)
        {
            if (mat == null)
                return null;

            if (MaterialCache.TryGetValue(key, out var cached))
            {
                UnityEngine.Object.DestroyImmediate(mat);
                return cached;
            }

            string folder = key.Contains("Ground") || key.Contains("Lightmap") ? MapMaterialFolder : SharedMaterialFolder;
            EnsureFolder($"{folder}/dummy.mat");

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
                return null;

            string safeName = SafeFileName(rosePath);
            var path = shared ? $"Assets/Data/Shared/Textures/{safeName}.asset" : $"{MapRoot}/Lightmaps/{safeName}.asset";

            if (TextureCache.TryGetValue(path, out var alreadySaved))
                return alreadySaved;

            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null)
            {
                TextureCache[path] = existing;
                return existing;
            }

            EnsureFolder(path);
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

            string folder = "Assets/Data/Animations";
            EnsureFolder($"{folder}/dummy.anim");

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

        private static string SafeFileName(string key) =>
            key.Replace("\\", "_").Replace("/", "_").Replace(":", "_");

        private static void EnsureFolder(string assetPath)
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

                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                createdFolders.Add(next);
                current = next;
            }

            createdFolders.Add(folder);
        }

        private string MapRoot => $"Assets/Data/Rose/{mapName}";

        private string MapMeshFolder => $"{MapRoot}/Meshes";

        private string MapMaterialFolder => $"{MapRoot}/Materials";

        private string SharedMeshFolder => "Assets/Data/Shared/Meshes";

        private string SharedMaterialFolder => "Assets/Data/Shared/Materials";
    }


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

#endif