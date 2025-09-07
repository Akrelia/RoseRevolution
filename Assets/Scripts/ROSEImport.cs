#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using UnityRose.Formats;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityRose;
using System;

public class ROSEImport
{
    private static string dataPath = "";
    private const string DataPathKey = "ROSE_DataPath"; // TODO : remove this useless stuff, everything needed is in ROSEImportWindow

    public class MapListData
    {
        public STB stb;
        public STL stl;
    }
    private static MapListData mapListData = null;

    public static void MaybeUpdate()
    {
        string curDataPath = GetDataPath();
        if (curDataPath != dataPath)
        {
            dataPath = curDataPath;
            Update();
        }
    }

    public static string GetDataPath()
    {
        return EditorPrefs.GetString(DataPathKey);
    }

    private static void Update()
    {
        var md = new MapListData();
        md.stb = new STB(Utils.CombinePath(dataPath, "3DDATA/STB/LIST_ZONE.STB"));
        md.stl = new STL(Utils.CombinePath(dataPath, "3DDATA/STB/LIST_ZONE_S.STL"));
        mapListData = md;
    }

    public static void ClearData()
    {
        ClearUData();
        File.Delete("Assets/GameData.meta");

        if (Directory.Exists("Assets/GameData"))
            Directory.Delete("Assets/GameData", true);

        AssetDatabase.Refresh();
    }

    public static void ClearUData()
    {
        File.Delete("Assets/MapObjects.meta");
        File.Delete("Assets/NpcParts.meta");
        File.Delete("Assets/Npcs.meta");

        if (Directory.Exists("Assets/MapObjects"))
            Directory.Delete("Assets/MapObjects", true);
        if (Directory.Exists("Assets/NpcParts"))
            Directory.Delete("Assets/NpcParts", true);
        if (Directory.Exists("Assets/Npcs"))
            Directory.Delete("Assets/Npcs", true);

        AssetDatabase.Refresh();
    }

    public static string GetCurrentPath()
    {
        return string.IsNullOrEmpty(dataPath) ? GetDataPath() : dataPath;
    }

    public static MapListData GetMapListData()
    {
        return mapListData;
    }

    public static void ImportMap(int mapIdx)
    {
        Debug.Log("Importing Map #" + mapIdx);
    }

    public static void ImportNPC(int id)
    {
        AssetHelper.StartAssetEditing();

        var importer = new ChrImporter();

        importer.ImportNpc(id);

        AssetHelper.StopAssetEditing();
    }

    /// <summary>
    /// Import all the NPC.
    /// </summary>
    public static void ImportNPCs(int count)
    {
        AssetHelper.StartAssetEditing();

        try
        {
            var importer = new ChrImporter();

            for (var i = 0; i < count; ++i)
            {
                importer.ImportNpc(i);
            }
        }

        catch (Exception ex)
        {
            Debug.Log("Something went wrong while importing NPC :" + ex.Message + " - " + ex.StackTrace);
        }

        finally 
        {
            AssetHelper.StopAssetEditing();

        }
    }

    /// <summary>
    /// Import all the NPC.
    /// </summary>
    public static void ImportAllNPC()
    {
        AssetHelper.StartAssetEditing();

        try
        {
            var importer = new ChrImporter();

            for (var i = 0; i < importer.chr.Characters.Count; ++i)
            {
                importer.ImportNpc(i);
            }
        }

        catch (Exception ex)
        {
            Debug.Log("Something went wrong while importing ALL NPC :" + ex.Message + " - " + ex.StackTrace);
        }

        finally
        {
            AssetHelper.StopAssetEditing();

            Debug.Log("Something went wrong while importing all NPC : ");
        }
    }

    private static string GenerateAssetPath(string rosePath, string unityExt)
    {
        rosePath = Utils.NormalizePath(rosePath);

        var dirPath = Path.GetDirectoryName(rosePath);
        if (!dirPath.StartsWith("3DDATA", System.StringComparison.InvariantCultureIgnoreCase))
        {
            // throw new System.Exception("dirPath does not begin with 3DDATA :: " + dirPath); // Akima : removed this since we use only ROSEImport.ImportTexture
        }
        var pathName = dirPath.Substring(7);
        var meshName = Path.GetFileNameWithoutExtension(rosePath);

        return Utils.CombinePath("Assets/GameData", pathName, meshName + unityExt);
    }

    public static RoseSkeletonData ImportSkeleton(string path)
    {
        var fullPath = Utils.CombinePath(dataPath, path);
        if (!File.Exists(fullPath))
        {
            Debug.LogWarning("Could not find referenced skeleton: " + fullPath);
            return null;
        }

        var skelPath = GenerateAssetPath(path, ".skel.asset");
        if (!File.Exists(skelPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(skelPath));

            var zmd = new ZMD(fullPath);
            var skel = ScriptableObject.CreateInstance<RoseSkeletonData>();

            for (var i = 0; i < zmd.bones.Count; ++i)
            {
                var zmdBone = zmd.bones[i];
                var bone = new RoseSkeletonData.Bone();
                bone.name = zmdBone.Name;
                bone.parent = zmdBone.ParentID;
                bone.translation = zmdBone.Position;
                bone.rotation = zmdBone.Rotation;
                skel.bones.Add(bone);
            }

            for (var i = 0; i < zmd.dummies.Count; ++i)
            {
                var zmdBone = zmd.dummies[i];
                var bone = new RoseSkeletonData.Bone();
                bone.name = zmdBone.Name;
                bone.parent = zmdBone.ParentID;
                bone.translation = zmdBone.Position;
                bone.rotation = zmdBone.Rotation;
                skel.dummies.Add(bone);
            }

            AssetDatabase.CreateAsset(skel, skelPath);
            EditorUtility.SetDirty(skel);
            // AssetDatabase.SaveAssetIfDirty(skel);
            AssetDatabase.SaveAssets();
        }

        AssetHelper.StopAssetEditing();

        var saucisse = AssetDatabase.LoadAssetAtPath<RoseSkeletonData>(skelPath);

        AssetHelper.StartAssetEditing();

        return saucisse;
    }

    public static Mesh ImportMesh(string path)
    {
        var fullPath = Utils.CombinePath(dataPath, path);
        if (!File.Exists(fullPath))
        {
            Debug.LogWarning("Could not find referenced mesh: " + fullPath);
            return null;
        }

        var meshPath = GenerateAssetPath(path, ".mesh.asset");
        if (!File.Exists(meshPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(meshPath));

            var zms = new ZMS(fullPath);
            var mesh = zms.getMesh();
            AssetDatabase.CreateAsset(mesh, meshPath);
            return mesh;
        }
        return AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
    }

    public static AnimationClip ImportAnimation(string path, RoseSkeletonData skeleton)
    {
        var fullPath = Utils.CombinePath(dataPath, path);
        if (!File.Exists(fullPath))
        {
            Debug.LogWarning("Could not find referenced animation: " + fullPath);
            return null;
        }

        var animPath = GenerateAssetPath(path, ".anim.asset");
        //if (!File.Exists(animPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(animPath));

            var zmo = new ZMO(fullPath);
            var anim = zmo.BuildSkeletonAnimationClip(skeleton);
            AssetDatabase.CreateAsset(anim, animPath);
            return anim;
        }

        return AssetDatabase.LoadAssetAtPath<AnimationClip>(animPath);
    }

    public static Texture2D ImportTexture(string path, bool useFullPath, AssetHelper.ImportDone doneFn = null)
    {
        var fullPath = Utils.CombinePath(dataPath, path);

        if (useFullPath)
        {
            fullPath = path; // Akima  : Lazy way to handle relative path and fixed path
        }
        else
        {
            // Try case-insensitive path resolution for macOS compatibility
            fullPath = Utils.ResolvePathWithCorrectCase(dataPath, path);
        }

        if (!File.Exists(fullPath))
        {
            Debug.LogWarning("Could not find referenced texture: " + fullPath);
            return null;
        }

        var texPath = GenerateAssetPath(path, Path.GetExtension(path));
        if (!File.Exists(texPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(texPath));
            File.Copy(fullPath, texPath);
            AssetHelper.ImportTexture(texPath, doneFn);
        }
        else
        {
            AssetHelper.Delay(doneFn);
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
    }


    public static void ImportParticles()
    {

        PtlImporter importer = new PtlImporter();


    }

    public class ChrImporter
    {
        private string targetPath = "";
        public CHR chr = null;
        public ZscImporter zsc = null;

        public ChrImporter()
        {
            targetPath = "Assets/Npcs";
            chr = new CHR(Utils.CombinePath(dataPath, "3DDATA/NPC/LIST_NPC.CHR"));
            zsc = new ZscImporter(Path.Combine(dataPath, "3DDATA/NPC/PART_NPC.ZSC"));
        }

        public RoseNpcData ImportNpc(int npcIdx)
        {
            if (!chr.Characters[npcIdx].IsEnabled)
            {

                return null;
            }

            var stbName = ResourceManager.Instance.stb_npc_list.Cells[npcIdx][1].ToString();

            var npcPath = Utils.CombinePath(targetPath, $"[{npcIdx}]{stbName}.asset"); // Akima : using STB name for better visibility
            //if (!File.Exists(npcPath))
            {

                Directory.CreateDirectory(Path.GetDirectoryName(npcPath));

                var chrObj = chr.Characters[npcIdx];
                var npc = ScriptableObject.CreateInstance<RoseNpcData>();

                npc.stbID = npcIdx;
                npc.npcName = stbName;

                var skelFile = chr.SkeletonFiles[chrObj.ID];
                npc.skeleton = ImportSkeleton(skelFile);

                for (var i = 0; i < chrObj.Objects.Count; ++i)
                {
                    var zscPart = chrObj.Objects[i];

                    zsc.ImportPart(zscPart.Object, (partData) =>
                    { 
                        npc.parts.Add(partData);
                    });
                }

                for (var i = 0; i < chrObj.Animations.Count; ++i)
                {
                    var zscMotion = chrObj.Animations[i];
                    if (zscMotion.Animation >= 0)
                    {
                        var anim = ImportAnimation(chr.MotionFiles[zscMotion.Animation], npc.skeleton);
                        while (npc.animations.Count <= (int)zscMotion.Type)
                        {
                            npc.animations.Add(null);
                        }
                        npc.animations[(int)zscMotion.Type] = anim;
                    }
                }

                AssetDatabase.CreateAsset(npc, npcPath);
                EditorUtility.SetDirty(npc);
                AssetDatabase.SaveAssets();
            }
            return AssetDatabase.LoadAssetAtPath<RoseNpcData>(npcPath);
        }
    }
    public class PtlImporter
    {
        public List<PTL> LoadedParticles { get; } = new();

        public PtlImporter()
        {
            var path = Utils.CombinePath(dataPath, "3DDATA/EFFECT/PARTICLES");
            string[] ptlFiles = Directory.GetFiles(path, "*.PTL", SearchOption.AllDirectories);

            foreach (var file in ptlFiles)
            {
                try
                {
                    using var stream = File.OpenRead(file);
                    using var reader = new BinaryReader(stream);
                    PTL ptl = new();
                    ptl.Read(reader);

                    LoadedParticles.Add(ptl);
                }
                catch (Exception ex)
                {
                    Debug.Log(ex);
                }
            }

            for (int i = 0; i < LoadedParticles.Count; i++)
            {
                string importFolder = "Assets/GameData/PTLTextures";
                Directory.CreateDirectory(importFolder); 

                string sourceDdsPath = Path.Combine(dataPath, LoadedParticles[i].Emitters[0].Texture);
                string targetDdsPath = Path.Combine(importFolder, Path.GetFileName(LoadedParticles[i].Emitters[0].Texture));

                sourceDdsPath = NormalizePath(sourceDdsPath);

                if (File.Exists(sourceDdsPath) && !File.Exists(targetDdsPath))
                {
                    //  File.Copy(sourceDdsPath, targetDdsPath);
                    //  AssetDatabase.ImportAsset(targetDdsPath.Replace("\\", "/"));

                    var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                    mat.SetFloat("_Surface", 1);

                    AssetDatabase.CreateAsset(mat, Path.Combine(importFolder, "PTL_" + LoadedParticles[i].Emitters[0].Name + ".mat"));

                    string destTexturePath = Path.Combine(importFolder, Path.GetFileName(sourceDdsPath));
                    File.Copy(sourceDdsPath, destTexturePath, true);
                    AssetDatabase.ImportAsset(destTexturePath);

                    Texture2D mainTex = AssetDatabase.LoadAssetAtPath<Texture2D>(destTexturePath);
                    mat.SetTexture("_BaseMap", mainTex);
                    EditorUtility.SetDirty(mat);
                }

                //AssetDatabase.LoadAssetAtPath<Material>(importFolder);
                //AssetDatabase.LoadAssetAtPath<Texture2D>(Utils.CombinePath(dataPath,LoadedParticles[i].Emitters[0].Texture));
                // ImportTexture(Utils.CombinePath(dataPath, LoadedParticles[i].Emitters[0].Texture),true);
            }

            Debug.Log("Particles loaded : " + LoadedParticles.Count);
        }

        public static string NormalizePath(string rawPath)
        {
            string unified = rawPath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);

            while (unified.Contains(new string(Path.DirectorySeparatorChar, 2)))
                unified = unified.Replace(new string(Path.DirectorySeparatorChar, 2), Path.DirectorySeparatorChar.ToString());

            return Path.GetFullPath(unified);
        }
    }

    public class ZscImporter
    {
        private string targetPath = "";
        public ZSC zsc = null;

        private static Regex mapZsc = new Regex("3DDATA/([A-Z]*)/LIST_([A-Z_]*).ZSC", RegexOptions.IgnoreCase);
        private static Regex npcZsc = new Regex("(.*)/PART_NPC.ZSC", RegexOptions.IgnoreCase);

        public ZscImporter(string path)
        {
            path = Utils.NormalizePath(path);

            var mapMatches = mapZsc.Match(path);
            var npcMatches = npcZsc.Match(path);
            if (npcMatches.Success)
            {
                targetPath = Utils.NormalizePath("Assets/NpcParts");
            }
            else if (mapMatches.Success)
            {
                var baseName = mapMatches.Groups[1].Value;
                var dbName = mapMatches.Groups[2].Value;
                if (baseName == "AVATAR")
                {
                    targetPath = Utils.CombinePath("Assets/CharParts", dbName);
                }
                else
                {
                    targetPath = Utils.CombinePath("Assets/MapObjects", baseName, dbName);
                }
            }
            else
            {
                throw new System.Exception("Unexpected ZSC name...");
            }

            //    zsc = new ZSC(Utils.CombinePath(dataPath, path));
            zsc = new ZSC(path); // TODO : Check here if we have to handle Map & NPC differently
        }

        public void ImportPart(int partIdx, Action<RoseCharPartData> onComplete)
        {
            var partPath = Utils.CombinePath(targetPath, "NPC_PART_" + partIdx + ".asset");

            if (File.Exists(partPath))
            {
                var loaded = AssetDatabase.LoadAssetAtPath<RoseCharPartData>(partPath);
                onComplete?.Invoke(loaded);
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(partPath));

            var zscObj = zsc.Objects[partIdx];
            var mdl = ScriptableObject.CreateInstance<RoseCharPartData>();

            int remaining = zscObj.Models.Count;

            for (int j = 0; j < zscObj.Models.Count; ++j)
            {
                var part = zscObj.Models[j];
                var subObj = new Model
                {
                    mesh = ImportMesh(part.ModelID),
                    boneIndex = -1
                };

                ImportMaterial(part.TextureID, (mat) =>
                {
                    subObj.material = mat;
                    mdl.models.Add(subObj);

                    remaining--;

                    if (remaining == 0)
                    {
                        AssetDatabase.CreateAsset(mdl, partPath);
                        EditorUtility.SetDirty(mdl);
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();

                        EditorApplication.delayCall += () =>
                        {
                            var loaded = AssetDatabase.LoadAssetAtPath<RoseCharPartData>(partPath);
                            onComplete?.Invoke(loaded);
                        };
                    }
                });
            }
        }


        public RoseMapObjectData ImportObject(int objectIdx)
        {
            var objPath = Utils.CombinePath(targetPath, "Obj_" + objectIdx + ".asset");
            if (!File.Exists(objPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(objPath));

                var zscObj = zsc.Objects[objectIdx];
                var mdl = ScriptableObject.CreateInstance<RoseMapObjectData>();

                for (int j = 0; j < zscObj.Models.Count; ++j)
                {
                    var part = zscObj.Models[j];
                    var subObj = new RoseMapObjectData.SubObject();

                    subObj.mesh = ImportMesh(part.ModelID);
                    ImportMaterial(part.TextureID, (mat) =>
                    {
                        Debug.Log("Material ready: " + mat.name);
                        subObj.material = mat;
                    });
                    subObj.animation = null;
                    subObj.parent = part.Parent;
                    subObj.position = part.Position / 100;
                    subObj.rotation = part.Rotation;
                    subObj.scale = part.Scale;

                    /*
                    if (part.CollisionLevel == ZSC.CollisionLevelType.None)
                    {
                        subObj.colMode = 0;
                    }
                    else
                    {
                        subObj.colMode = 1;
                    }

                    if (part.AnimationFilePath != "")
                    {
                        var animPath = _basePath + "Anim_" + i.ToString() + "_" + j.ToString() + ".asset";
                        var clip = ImportNodeAnimation(animPath, part.AnimationFilePath);
                        subObj.animation = clip;
                    }
                    */

                    mdl.subObjects.Add(subObj);
                }

                AssetDatabase.CreateAsset(mdl, objPath);
                EditorUtility.SetDirty(mdl);
                AssetDatabase.SaveAssets();
            }
            return AssetDatabase.LoadAssetAtPath<RoseMapObjectData>(objPath);
        }

        public Mesh ImportMesh(int meshIdx)
        {
            return ROSEImport.ImportMesh(zsc.Models[meshIdx]);
        }

        public void ImportMaterial(int materialIdx, Action<Material> onComplete)
        {
            var zscMat = zsc.Textures[materialIdx];
            var matFolder = Path.Combine(targetPath, "Materials");
            var matPath = Path.Combine(matFolder, "Mat_" + materialIdx + ".mat");

            if (!File.Exists(matPath))
            {
                if (!Directory.Exists(matFolder))
                    Directory.CreateDirectory(matFolder);

                var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(mat, matPath);

                ImportTexture(zscMat.Path, false, () =>
                {
                    Texture2D mainTex = ImportTexture(zscMat.Path, false);
                    mat.SetTexture("_BaseMap", mainTex);
                    EditorUtility.SetDirty(mat);

                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();

                    Material loadedMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);

                    onComplete?.Invoke(loadedMat);
                });
            }
            else
            {
                Material loadedMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                onComplete?.Invoke(loadedMat);
            }
        }
    }
    public class AssetHelper
    {
        public delegate void ImportDone();

        public static void StartAssetEditing()
        {
            AssetDatabase.StartAssetEditing();
        }

        public static void StopAssetEditing()
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.StartAssetEditing();
            foreach (var lateImport in lateImportList)
            {
                lateImport();
            }
            lateImportList.Clear();
            AssetDatabase.SaveAssets();
            AssetDatabase.StopAssetEditing();
        }

        public static void ImportTexture(string path, ImportDone doneFn = null)
        {
            try
            {
                AssetDatabase.ImportAsset(path);
                if (doneFn != null)
                    lateImportList.Add(doneFn);
            }
            catch (Exception ex)
            {
                RoseDebug.LogWarning("Failed to import texture : " + ex.Message);
            }
        }

        public static void Delay(ImportDone doneFn = null)
        {
            if (doneFn != null)
                lateImportList.Add(doneFn);
        }

        public static List<ImportDone> lateImportList = new List<ImportDone>();
    }

}
#endif