using UnityEngine;
using UnityEditor;
using UnityRose.Formats;
using UnityRose.Import;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Animations;
using RevolutionShared.Rose.Data.NPC;
using static UnityRose.Formats.IFO;
using UnityRose.Game;
using RevolutionShared.Rose.Data;
using static UnityRose.Import.GameDataPaths;
using GluonGui.WorkspaceWindow.Views.WorkspaceExplorer;
using static UnityEditor.U2D.ScriptablePacker;
using Unity.VisualScripting;

namespace UnityRose.ImportEditor
{
    /// <summary>
    /// Everything common in the Rose Import system.
    /// </summary>
    public static class ROSEEditorBaker
    {
        private const string DataPathKey = "ROSE_DataPath";

        public static string DataPath
        {
            get => EditorPrefs.GetString(DataPathKey);
            set
            {
                EditorPrefs.SetString(DataPathKey, value);
                RoseImporter.DataPath = value; // keep the runtime-safe layer in sync
            }
        }

        /// <summary>
        /// Clear all imported data from the project.
        /// </summary>
        public static void ClearData()
        {
            File.Delete($"{GameDataPaths.Root}.meta");

            if (Directory.Exists(Root))
            {
                Directory.Delete(GameDataPaths.Root, true);
            }

            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Import a mesh.
        /// </summary>
        /// <param name="rosePath">Rose Path.</param>
        /// <param name="context">Context.</param>
        /// <returns>Mesh.</returns>
        public static Mesh ImportMesh(string rosePath, ImportContext context)
        {
            return ImportMesh(rosePath, context.Meshes);
        }

        /// <summary>
        /// Import a mesh.
        /// </summary>
        /// <param name="rosePath">Rose Path.</param>
        /// <param name="context">Context.</param>
        /// <returns>Mesh.</returns>
        public static Mesh ImportMesh(string rosePath, string saveDirectory)
        {
            var name = Path.GetFileNameWithoutExtension(rosePath);

            var assetPath = $"{saveDirectory}/{name}.mesh.asset";

            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);

            if (existing != null)
            {
                return existing;
            }

            if (File.Exists(assetPath))
            {
                AssetDatabase.DeleteAsset(assetPath);
            }

            var mesh = new ZMS(Path.Combine(RoseImporter.DataPath, rosePath)).getMesh();

            if (mesh == null)
            {
                return null;
            }

            if (AssetDatabase.Contains(mesh))
            {
                return mesh;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(assetPath));


            mesh.name = Path.GetFileNameWithoutExtension(assetPath);

            var existingPath = AssetDatabase.GetAssetPath(mesh);

            if (!string.IsNullOrEmpty(existingPath))
            {
                return mesh;
            }

            AssetDatabase.CreateAsset(mesh, assetPath);

            return mesh;
        }

        /// <summary>
        /// Import a skeleton from a ZMS file.
        /// </summary>
        /// <param name="rosePath">Rose path.</param>
        /// <param name="context">Context.</param>
        /// <returns>Skeleton.</returns>
        public static RoseSkeletonData ImportSkeleton(string rosePath, ImportContext context)
        {
            var name = Path.GetFileNameWithoutExtension(rosePath);

            var assetPath =  $"{context.Meshes}/{name}.mesh.asset";

            assetPath = assetPath.Replace(".mesh.asset", ".skel.asset");

            var existing = AssetDatabase.LoadAssetAtPath<RoseSkeletonData>(assetPath);

            if (existing != null)
            {
                return existing;
            }

            var skeleton = CreateSkeleton(rosePath);

            if (skeleton == null)
            {
                return null;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(assetPath));

            skeleton.name = Path.GetFileNameWithoutExtension(rosePath);

            AssetDatabase.CreateAsset(skeleton, assetPath);

            return skeleton;
        }

        /// <summary>
        /// Import an animation based off a skeleton.
        /// </summary>
        /// <param name="rosePath">Rose path.</param>
        /// <param name="skeleton">Skeleton.</param>
        /// <param name="context">Context.</param>
        /// <returns>Animation skeleton.</returns>
        public static AnimationClip ImportAnimation(string rosePath, RoseSkeletonData skeleton, ImportContext context)
        {
            var name = Path.GetFileNameWithoutExtension(rosePath);

            var assetPath = $"{context.Animations}/{name}.anim.asset";

            var clip = ImportClip(rosePath, skeleton);

            if (clip == null) return null;

            Directory.CreateDirectory(Path.GetDirectoryName(assetPath));

            AssetDatabase.CreateAsset(clip, assetPath);

            return clip;
        }

        /// <summary>
        /// Import a texture from the ROSE data path into the Unity project, optionally using a custom DDS loader.
        /// </summary>
        /// <param name="rosePath">Rose path.</param>
        /// <param name="context"Context.></param>
        /// <param name="useRoseLoader">Use role loader.</param>
        /// <returns></returns>
        public static Texture2D ImportTexture(string rosePath, ImportContext context, bool useRoseLoader = false)
        {
            return ImportTexture(rosePath, context.Textures, useRoseLoader);
        }

        /// <summary>
        /// Import a texture from the ROSE data path into the Unity project, optionally using a custom DDS loader.
        /// </summary>
        /// <param name="rosePath">Rose path.</param>
        /// <param name="context"Context.></param>
        /// <param name="useRoseLoader">Use role loader.</param>
        /// <returns></returns>
        public static Texture2D ImportTexture(string rosePath, string saveDirectory, bool useRoseLoader = false)
        {
            var fullPath = EditorUtils.ResolvePathWithCorrectCase(DataPath, rosePath);

            if (!File.Exists(fullPath))
            {
                Debug.LogWarning("Missing texture " + fullPath);

                return null;
            }

            string fileName = Path.GetFileNameWithoutExtension(rosePath);
            string assetPath = $"{saveDirectory}/{fileName}" + (useRoseLoader ? ".asset" : Path.GetExtension(rosePath));
            assetPath = assetPath.Replace("\\", "/");

            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);

            if (existing != null)
            {
                return existing;
            }

            EditorUtils.EnsureFolder(assetPath);

            if (useRoseLoader) // Akima : At term, we will use our own loader for DDS files, since we have to convert a lot of textures for Unity to load them in the first place.
            {
                var texture = RoseDdsLoader.LoadFromFile(fullPath);

                if (texture == null)
                {
                    Debug.LogWarning($"Failed loading DDS texture: {fullPath}");
                    return null;
                }

                texture.name = fileName;

                AssetDatabase.CreateAsset(texture, assetPath);
                EditorUtility.SetDirty(texture);

                return texture;
            }

            File.Copy(fullPath, assetPath, true);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh();

            var importedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);

            if (importedTexture == null)
            {
                Debug.LogWarning($"Failed loading imported texture: {assetPath}");

                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
                importedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            }
            return importedTexture;
        }

        /// <summary>
        /// Create a material.
        /// </summary>
        /// <param name="rosePath"></param>
        /// <param name="texture"></param>
        /// <param name="shader"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static Material ImportMaterial(string rosePath, Texture2D texture, Shader shader, ImportContext context)
        {
            return ImportMaterial(rosePath, texture, shader, context.Materials);
        }

        /// <summary>
        /// Create a material.
        /// </summary>
        /// <param name="rosePath"></param>
        /// <param name="texture"></param>
        /// <param name="shader"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static Material ImportMaterial(string rosePath, Texture2D texture, Shader shader, string saveDirectory)
        {
            var fullPath = EditorUtils.ResolvePathWithCorrectCase(DataPath, rosePath);

            if (!File.Exists(fullPath))
            {
                Debug.LogWarning("Missing texture : " + fullPath);

                return null;
            }

            var fileName = Path.GetFileNameWithoutExtension(rosePath) + ".asset";
            var assetPath = $"{saveDirectory}/{fileName}".Replace("\\", "/");

            var existing = AssetDatabase.LoadAssetAtPath<Material>(assetPath);

            if (existing != null)
            {
                return existing;
            }

            Directory.CreateDirectory(saveDirectory);

            var material = new Material(shader);

            material.mainTexture = texture;

            material.SetFloat("_Mode", 4); // Additive
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;

            material.SetFloat("_Surface", 1); // Transparent
            material.SetFloat("_Blend", 0);   // Alpha

            material.SetOverrideTag("RenderType", "Transparent");

            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            material.name = Path.GetFileNameWithoutExtension(rosePath);

            AssetDatabase.CreateAsset(material, assetPath);

            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();

            return material;
        }

        /// <summary>
        /// Create equipment material from ZSC data, using the specified material index.
        /// </summary>
        /// <param name="materialIdx"></param>
        /// <param name="zsc"></param>
        /// <param name="pathZ"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static Material ImportEquipmentMaterial(int materialIdx, ZSC zsc, string pathZ, ImportContext context)
        {
            var zscName = Path.GetFileNameWithoutExtension(pathZ);

            var matFolder = Path.Combine(context.Materials, zscName);
            var matPath = Path.Combine(matFolder, "Mat_" + materialIdx + ".mat").Replace("\\", "/");

            var existing = AssetDatabase.LoadAssetAtPath<Material>(matPath);

            if (existing != null)
            {
                var tex = existing.GetTexture("_BaseMap");

                if (tex != null)
                {
                    return existing;
                }

                Debug.LogWarning($"Material exists but missing texture: {matPath}");

                AssetDatabase.DeleteAsset(matPath);
            }

            Directory.CreateDirectory(matFolder);

            var shader = Shader.Find("Universal Render Pipeline/Unlit"); // Use settings

            var mat = new Material(shader);

            var zscMat = zsc.Textures[materialIdx];

            var texture = ImportTexture(zscMat.Path, context);

            if (texture == null)
            {
                Debug.LogWarning($"Missing texture for material {matPath}: {zscMat.Path}");
            }
            else
            {
                mat.SetTexture("_BaseMap", texture);
                mat.mainTexture = texture;

                // Transparency
                if (zscMat.AlphaEnabled || zscMat.Alpha < 1f)
                {
                    mat.SetFloat("_Surface", 1); // Transparent
                    mat.SetFloat("_Blend", 0);   // Alpha

                    mat.SetOverrideTag("RenderType", "Transparent");

                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", zscMat.ZWriteEnabled ? 1 : 0);

                    mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

                    var color = mat.color;
                    color.a = Mathf.Clamp01(zscMat.Alpha);
                    mat.color = color;
                }

                else
                {
                    mat.SetFloat("_Surface", 0);
                    mat.SetInt("_ZWrite", 1);
                }

                // Backface culling
                mat.SetInt("_Cull", zscMat.TwoSided ? (int)UnityEngine.Rendering.CullMode.Off : (int)UnityEngine.Rendering.CullMode.Back);

                // ZTest
                mat.SetInt("_ZTest", zscMat.ZTestEnabled ? (int)UnityEngine.Rendering.CompareFunction.LessEqual : (int)UnityEngine.Rendering.CompareFunction.Always);

                // Alpha test
                if (zscMat.AlphaTestEnabled)
                {
                    mat.EnableKeyword("_ALPHATEST_ON");
                    mat.SetFloat("_Cutoff", zscMat.AlphaReference / 255f);
                }
            }

            AssetDatabase.CreateAsset(mat, matPath);

            EditorUtility.SetDirty(mat);

            AssetDatabase.SaveAssets();

            return mat;
        }

        /// <summary>
        /// Create a skybox material with day and night textures.
        /// </summary>
        /// <param name="textureDayPath">Texture day path.</param>
        /// <param name="textureNightPath">Texture night path.</param>
        /// <param name="name">Name.</param>
        /// <param name="context">Context.</param>
        /// <returns>Skybox material.</returns>
        public static Material ImportSkyxboxMaterial(string textureDayPath, string textureNightPath, string name, SkyboxImportContext context)
        {
            var savePath = context.Materials + "/" + name;

            var existing = AssetDatabase.LoadAssetAtPath<Material>(savePath);

            if (existing != null)
            {
                var existingDayTexture = existing.GetTexture("_DayTex");
                var existingNightTexture = existing.GetTexture("_NightTex");

                if (existingDayTexture != null && existingNightTexture != null)
                {
                    return existing;
                }

                AssetDatabase.DeleteAsset(savePath);
            }

            var folder = Path.GetDirectoryName(savePath);

            if (!string.IsNullOrEmpty(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var shader = Shader.Find("Rose Revolution/Skybox");

            if (shader == null)
            {
                Debug.LogError("Rose Revolution/Skybox shader not found");
                return null;
            }

            var dayTexture = ImportTexture(textureDayPath, context);

            var nightTexture = ImportTexture(textureNightPath, context);

            if (dayTexture == null || nightTexture == null)
            {
                Debug.LogWarning($"Missing skybox texture for material: {savePath}");

                return null;
            }

            var material = new Material(shader);
            material.name = Path.GetFileNameWithoutExtension(savePath);

            material.SetTexture("_DayTex", dayTexture);
            material.SetTexture("_NightTex", nightTexture);

            AssetDatabase.CreateAsset(material, savePath);

            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();

            return material;
        }

        /// <summary>
        /// Import an animation clip from a ZMO file, using the specified skeleton.
        /// </summary>
        /// <param name="rosePath">Rose path.</param>
        /// <param name="skeleton">Skeleton.</param>
        /// <returns>Clip.</returns>
        public static AnimationClip ImportClip(string rosePath, RoseSkeletonData skeleton)
        {
            var fullPath = Path.Combine(RoseImporter.DataPath, rosePath);

            if (!File.Exists(fullPath))
            {
                Debug.LogWarning("Could not find referenced animation: " + fullPath);

                return null;
            }

            var clip = new ZMO(fullPath).BuildSkeletonAnimationClip(skeleton);

            clip.name = Path.GetFileNameWithoutExtension(rosePath);

            return clip;
        }

        /// <summary>
        /// Create a skeleton from a ZMS file.
        /// </summary>
        /// <param name="rosePath">Rose Path.</param>
        /// <returns></returns>
        public static RoseSkeletonData CreateSkeleton(string rosePath)
        {
            var fullPath = Path.Combine(RoseImporter.DataPath, rosePath);

            if (!File.Exists(fullPath))
            {
                Debug.LogWarning("Could not find referenced skeleton: " + fullPath);

                return null;
            }

            var zmd = new ZMD(fullPath);
            var skel = ScriptableObject.CreateInstance<RoseSkeletonData>();
            skel.name = Path.GetFileNameWithoutExtension(rosePath);

            foreach (var bone in zmd.bones)
            {
                skel.bones.Add(new RoseSkeletonData.Bone
                {
                    name = bone.Name,
                    parent = bone.ParentID,
                    translation = bone.Position,
                    rotation = bone.Rotation
                });
            }

            foreach (var dummy in zmd.dummies)
            {
                skel.dummies.Add(new RoseSkeletonData.Bone
                {
                    name = dummy.Name,
                    parent = dummy.ParentID,
                    translation = dummy.Position,
                    rotation = dummy.Rotation
                });
            }

            return skel;
        }
    }
}