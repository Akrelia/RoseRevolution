#if UNITY_EDITOR

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
using static UnityRose.ImportEditor.ImportPaths;

namespace UnityRose.ImportEditor
{
    public static class ImportPaths
    {
        public const string Root = "Assets/GameData";

        public static class Database
        {
            public const string Root = ImportPaths.Root + "/Databases";
        }

        public static class NPC
        {
            public const string Root = ImportPaths.Root + "/NPC";
            public const string Data = NPC.Root + "/Data";
            public const string Prefabs = NPC.Root + "/Prefabs";
            public const string Materials = NPC.Root + "/Materials";
            public const string Motions = NPC.Root + "/Motions";
            public const string Textures = NPC.Root + "/Textures";
            public const string Parts = NPC.Root + "/Parts";
            public const string Meshes = NPC.Parts + "/Meshes";
            public const string Animations = NPC.Root + "/Animations";
            public const string Avatars = NPC.Root + "/Avatars";
            public const string Controllers = NPC.Root + "/Controllers";
        }

        public static class Player
        {
            public const string Root = ImportPaths.Root + "/Player";
            public const string Prefabs = Player.Root + "/Prefabs";
            public const string Meshes = Player.Root + "/Meshes";
            public const string Materials = Player.Root + "/Materials";
            public const string Textures = Player.Root + "/Textures";
        }

        public static class Maps
        {
            public const string Root = ImportPaths.Root + "/Maps";
            public const string Patches = Maps.Root + "/Patches";
            public const string Prefabs = Maps.Root + "/Prefabs";
            public const string Chunks = Maps.Root + "/Chunks";
            public const string Animations = Maps.Root + "/Animations";
            public const string Shared = Maps.Root + "/Shared";
            public const string Atlas = Maps.Root + "/Atlas";
        }

        public static class Items
        {
            public const string Root = ImportPaths.Root + "/Items";
            public const string Data = Items.Root + "/Data";
            public const string Prefabs = Items.Root + "/Prefabs";
        }

        public static class Icons
        {
            public const string Root = ImportPaths.Root + "/Icons";
            public const string Data = Icons.Root + "/Data";
        }

        /// <summary>
        /// Everything Bake* needs to know about WHERE to write assets, independent of
        /// whether the thing being baked is an NPC or a player equipment piece.
        /// NPCImportContext and EquipmentImportContext just populate this with
        /// different folder layouts - all Bake* methods work off this base type.
        /// </summary>
        public class ImportContext
        {
            public string Root;
            public string Data;
            public string Prefab;
            public string Meshes;
            public string Materials;
            public string Textures;
            public string Motions;
            public string Animations;
            public string Avatars;
            public string Controllers;

            public int Id;
            public string Name;
        }

        public class NPCImportContext : ImportContext
        {
            public NPCImportContext(int id, string category, string name)
            {
                Id = id;
                Name = name;

                var folderName = $"[{id}] {name}";

                Root = $"{ImportPaths.NPC.Root}/{category}/{folderName}";

                Data = $"{Root}/Data";
                Prefab = $"{Root}/Prefab";
                Meshes = $"{Root}/Meshes";
                Materials = $"{Root}/Materials";
                Textures = $"{Root}/Textures";
                Motions = $"{Root}/Motions";
                Animations = $"{Root}/Animations";
                Avatars = $"{Root}/Avatars";
                Controllers = $"{Root}/Controllers";
            }
        }

        /// <summary>
        /// Flat, shared layout for player equipment - unlike NPCs, equipment pieces
        /// don't each get their own subfolder; they all share
        /// Assets/GameData/Player/{Meshes,Materials,Textures}, so identical meshes and
        /// textures reused across many equipment IDs (common in ROSE) get naturally
        /// deduplicated by BakeMesh/BakeMaterial's "does this asset path already
        /// exist" checks - same mechanism as NPCs, just one shared folder instead of
        /// one folder per item.
        /// </summary>
        public class EquipmentImportContext : ImportContext
        {
            public EquipmentImportContext()
            {
                Root = ImportPaths.Player.Root;
                Prefab = ImportPaths.Player.Prefabs;
                Meshes = ImportPaths.Player.Meshes;
                Materials = ImportPaths.Player.Materials;
                Textures = ImportPaths.Player.Textures;
            }
        }
    }

    public static class ROSEEditorBaker
    {
        private const string DataPathKey = "ROSE_DataPath";

        public static string DataPath
        {
            get => EditorPrefs.GetString(DataPathKey);
            set
            {
                EditorPrefs.SetString(DataPathKey, value);
                RoseDataSource.DataPath = value; // keep the runtime-safe layer in sync
            }
        }

        public static void ClearData()
        {
            File.Delete("Assets/GameData.meta");

            if (Directory.Exists(ImportPaths.Root))
            {
                Directory.Delete(ImportPaths.Root, true);
            }

            AssetDatabase.Refresh();
        }

        private static string GenerateTexturePath(string rosePath, ImportContext context)
        {
            var name = Path.GetFileName(rosePath);
            return $"{context.Textures}/{name}";
        }

        private static string GenerateAnimationPath(string rosePath, ImportContext context)
        {
            var name = Path.GetFileNameWithoutExtension(rosePath);
            return $"{context.Animations}/{name}.anim.asset";
        }

        private static string GenerateMeshPath(string rosePath, ImportContext context)
        {
            var name = Path.GetFileNameWithoutExtension(rosePath);
            return $"{context.Meshes}/{name}.mesh.asset";
        }

        public static Mesh BakeMesh(string rosePath, ImportContext context)
        {
            var assetPath = GenerateMeshPath(rosePath, context);

            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);

            if (existing != null)
            {
                return existing;
            }

            if (File.Exists(assetPath))
            {
                AssetDatabase.DeleteAsset(assetPath);
            }

            var mesh = RoseMeshImporter.Import(rosePath);

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

        public static RoseSkeletonData BakeSkeleton(string rosePath, ImportContext context)
        {
            var assetPath = GenerateMeshPath(rosePath, context).Replace(".mesh.asset", ".skel.asset");

            var existing = AssetDatabase.LoadAssetAtPath<RoseSkeletonData>(assetPath);
            if (existing != null)
                return existing;

            var skeleton = RoseSkeletonImporter.Import(rosePath);
            if (skeleton == null)
                return null;

            Directory.CreateDirectory(Path.GetDirectoryName(assetPath));

            skeleton.name = Path.GetFileNameWithoutExtension(rosePath);

            AssetDatabase.CreateAsset(skeleton, assetPath);

            return skeleton;
        }

        public static AnimationClip BakeAnimation(string rosePath, RoseSkeletonData skeleton, ImportContext context)
        {
            var assetPath = GenerateAnimationPath(rosePath, context);

            var clip = RoseAnimationImporter.Import(rosePath, skeleton);
            if (clip == null) return null;

            Directory.CreateDirectory(Path.GetDirectoryName(assetPath));
            AssetDatabase.CreateAsset(clip, assetPath);

            return clip;
        }

        public static Texture2D BakeTexture(string rosePath, ImportContext context)
        {
            var fullPath = Utils.ResolvePathWithCorrectCase(DataPath, rosePath);

            if (!File.Exists(fullPath))
            {
                Debug.LogWarning("Missing texture " + fullPath);
                return null;
            }

            var fileName = Path.GetFileName(rosePath);
            var assetPath = $"{context.Textures}/{fileName}".Replace("\\", "/");

            if (!File.Exists(assetPath))
            {
                Directory.CreateDirectory(context.Textures);

                File.Copy(fullPath, assetPath, true);

                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                AssetDatabase.Refresh();
            }

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);

            if (texture == null)
            {
                Debug.LogWarning($"Failed loading imported texture: {assetPath}");

                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

                texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            }

            return texture;
        }

        public static Material BakeMaterial(int materialIdx, ZSC zsc, string pathZ, ImportContext context)
        {
            var zscName = Path.GetFileNameWithoutExtension(pathZ);

            var matFolder = Path.Combine(context.Materials, zscName);
            var matPath = Path.Combine(matFolder, "Mat_" + materialIdx + ".mat").Replace("\\", "/");

            var existing = AssetDatabase.LoadAssetAtPath<Material>(matPath);

            if (existing != null)
            {
                var tex = existing.GetTexture("_BaseMap");

                if (tex != null)
                    return existing;

                Debug.LogWarning($"Material exists but missing texture: {matPath}");

                AssetDatabase.DeleteAsset(matPath);
            }

            Directory.CreateDirectory(matFolder);

            var shader = Shader.Find("Universal Render Pipeline/Unlit");

            if (shader == null)
            {
                Debug.LogError("URP Unlit shader not found");
                return null;
            }

            var mat = new Material(shader);

            var zscMat = zsc.Textures[materialIdx];

            var texture = BakeTexture(zscMat.Path, context);

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
                mat.SetInt("_Cull", zscMat.TwoSided? (int)UnityEngine.Rendering.CullMode.Off: (int)UnityEngine.Rendering.CullMode.Back);

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

        public static List<Transform> BuildSkeleton(GameObject root, RoseSkeletonData skeleton)
        {
            var bones = new List<Transform>();

            foreach (var bone in skeleton.bones)
            {
                var obj = new GameObject(bone.name);

                obj.transform.SetParent(root.transform, false);

                obj.transform.localPosition = bone.translation;
                obj.transform.localRotation = bone.rotation;

                bones.Add(obj.transform);
            }

            for (int i = 0; i < skeleton.bones.Count; i++)
            {
                int parent = skeleton.bones[i].parent;

                if (parent >= 0 && parent < bones.Count)
                    bones[i].SetParent(bones[parent], false);
            }

            return bones;
        }

        private static AnimatorController BakeAnimatorController(EntitySO npc, string path)
        {
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != null)
                AssetDatabase.DeleteAsset(path);

            var controller = AnimatorController.CreateAnimatorControllerAtPath(path);

            var stateMachine = controller.layers[0].stateMachine;

            AnimatorState defaultState = null;

            for (int i = 0; i < npc.animations.Count; i++)
            {
                var animation = npc.animations[i];

                if (animation == null)
                    continue;

                var state = stateMachine.AddState($"Animation_{i}");
                state.motion = animation;

                if (defaultState == null)
                    defaultState = state;
            }

            if (defaultState != null)
                stateMachine.defaultState = defaultState;

            var layers = controller.layers;
            layers[0].defaultWeight = 1f;
            controller.layers = layers;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            return AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        }

        /// <summary>
        /// Import NPC.
        /// </summary>
        /// <param name="id">ID.</param>
        /// <returns>Imported NPC.</returns>
        public static GameObject ImportNPC(int id)
        {
            try
            {
                var npc = BakeEntity(id);

                return npc;
            }

            catch (Exception ex)
            {
                Debug.LogException(ex);

                return null;
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
                var chr = new CHR(Utils.CombinePath(DataPath, "3DDATA/NPC/LIST_NPC.CHR"));

                foreach (var i in Enumerable.Range(0, chr.Characters.Count))
                {
                    var npc = BakeEntity(i);

                    if (npc != null)
                    {
                        //       RegisterNPCInInternalDB(npc);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("Something went wrong while importing ALL NPC: " + ex.Message + "\n" + ex.StackTrace);
            }

            finally
            {
                AssetHelper.StopAssetEditing();
            }
        }

        /// <summary>
        /// Build the NPC Prefab.
        /// </summary>
        /// <param name="npc">NPC.</param>
        /// <param name="context">Context.</param>
        /// <returns>Game object.</returns>
        private static GameObject BuildEntityModelPrefab(EntitySO npc, NPCImportContext context)
        {
            if (npc == null)
            {
                Debug.LogError("Cannot build NPC prefab: npc is null");

                return null;
            }

            var root = new GameObject(npc.monsterData.displayName);

            var bones = new List<Transform>();

            if (npc.skeleton != null)
            {
                foreach (var bone in npc.skeleton.bones)
                {
                    var boneObject = new GameObject(bone.name);

                    boneObject.transform.SetParent(root.transform, false);

                    boneObject.transform.localPosition = bone.translation;
                    boneObject.transform.localRotation = bone.rotation;

                    bones.Add(boneObject.transform);
                }

                for (int i = 0; i < npc.skeleton.bones.Count; i++)
                {
                    var parent = npc.skeleton.bones[i].parent;

                    if (parent >= 0 && parent < bones.Count)
                    {
                        bones[i].SetParent(bones[parent], false);
                    }

                    else
                    {
                        bones[i].SetParent(root.transform, false);
                    }
                }

                foreach (var dummy in npc.skeleton.dummies)
                {
                    var dummyObject = new GameObject(dummy.name);

                    dummyObject.transform.localPosition = dummy.translation;
                    dummyObject.transform.localRotation = dummy.rotation;

                    dummyObject.transform.SetParent(root.transform, false);

                    bones.Add(dummyObject.transform);
                }
            }

            foreach (var part in npc.parts)
            {
                if (part == null)
                {
                    continue;
                }

                foreach (var model in part.models)
                {
                    if (model.mesh == null) continue;

                    var obj = new GameObject("Model");

                    obj.transform.SetParent(root.transform, false);

                    if (npc.skeleton != null && model.boneIndex == -1) // skinned
                    {
                        var renderer = obj.AddComponent<SkinnedMeshRenderer>();

                        model.mesh.bindposes = bones.Select(b => b.worldToLocalMatrix * root.transform.localToWorldMatrix).ToArray();

                        renderer.sharedMesh = model.mesh;
                        renderer.sharedMaterial = model.material;

                        if (bones.Count > 0)
                        {
                            renderer.rootBone = bones[0];
                            renderer.bones = bones.ToArray();
                        }
                    }
                    else // rigid, attached to a single bone/dummy
                    {
                        var filter = obj.AddComponent<MeshFilter>();
                        var renderer = obj.AddComponent<MeshRenderer>();
                        filter.sharedMesh = model.mesh;
                        renderer.sharedMaterial = model.material;

                        if (model.boneIndex >= 0 && model.boneIndex < bones.Count)
                        {
                            obj.transform.SetParent(bones[model.boneIndex], false);
                        }
                    }
                }
            }

            if (npc.animations != null && npc.animations.Count > 0)
            {
                var animator = root.AddComponent<Animator>();

                var avatar = AvatarBuilder.BuildGenericAvatar(root, "b1_pelvis");

                avatar.name = $"{npc.monsterData.displayName}_Avatar";

                if (!avatar.isValid)
                {
                    Debug.LogError($"Failed to build a valid avatar for NPC {npc.monsterData.displayName} " + "(check that 'b1_pelvis' exists as a child Transform under root).");
                }

                else
                {
                    var avatarPath = $"{context.Avatars}/{npc.monsterData.displayName}_Avatar.asset";
                    Directory.CreateDirectory(Path.GetDirectoryName(avatarPath));

                    if (AssetDatabase.LoadAssetAtPath<Avatar>(avatarPath) != null)
                        AssetDatabase.DeleteAsset(avatarPath);

                    AssetDatabase.CreateAsset(avatar, avatarPath);
                    EditorUtility.SetDirty(avatar);
                    AssetDatabase.SaveAssets();

                    animator.avatar = AssetDatabase.LoadAssetAtPath<Avatar>(avatarPath);
                }

                var controllerPath = $"{context.Controllers}/{npc.monsterData.displayName}.controller";
                Directory.CreateDirectory(Path.GetDirectoryName(controllerPath));

                var controller = BakeAnimatorController(npc, controllerPath);
                var layers = controller.layers; layers[0].defaultWeight = 1f;

                animator.runtimeAnimatorController = controller;
            }

            float scale = npc.monsterData.size / 100F;

            root.transform.localScale = new Vector3(scale, scale, scale);

            var entityComponent = root.AddComponent<EntityModelBehavior>();

            entityComponent.data = npc;

            return root;
        }

        /// <summary>
        /// Bake the NPC.
        /// </summary>
        /// <param name="npcIdx">NPC Index.</param>
        /// <returns>Baked NPC.</returns>
        private static GameObject BakeEntity(int npcIdx)
        {
            var chr = new CHR(Utils.CombinePath(DataPath, "3DDATA/NPC/LIST_NPC.CHR"));

            if (!chr.Characters[npcIdx].IsEnabled)
            {
                return null;
            }

            var stbName = ResourceManager.Instance.stb_npc_list.Cells[npcIdx][1].ToString();

            var context = new NPCImportContext(npcIdx, "Monsters", stbName);

            var npcPath = Utils.CombinePath(context.Data, $"[{npcIdx}]{stbName}.asset");
            var prefabPath = Utils.CombinePath(context.Prefab, $"[{npcIdx}]{stbName}.prefab");

            var existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (existingPrefab != null)
            {
                return existingPrefab;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(npcPath));
            Directory.CreateDirectory(Path.GetDirectoryName(prefabPath));

            var chrObj = chr.Characters[npcIdx];

            var npc = ScriptableObject.CreateInstance<EntitySO>();

            EnemyData data = new EnemyData();

            var stb = ResourceManager.Instance.stb_npc_list;

            data.id = npcIdx;
            data.displayName = stb.Cells[npcIdx][1];
            data.moveSpeed = Utils.ParseInt(stb.Cells[npcIdx][3]);
            data.runSpeed = Utils.ParseInt(stb.Cells[npcIdx][4]);
            data.size = Utils.ParseInt(stb.Cells[npcIdx][5]);
            data.rightWeaponID = Utils.ParseInt(stb.Cells[npcIdx][6]);
            data.leftWeaponID = Utils.ParseInt(stb.Cells[npcIdx][7]);
            data.level = Utils.ParseInt(stb.Cells[npcIdx][8]);
            data.healthPoints = Utils.ParseInt(stb.Cells[npcIdx][9]);
            data.attack = Utils.ParseInt(stb.Cells[npcIdx][10]);
            data.accuracy = Utils.ParseInt(stb.Cells[npcIdx][11]);
            data.defense = Utils.ParseInt(stb.Cells[npcIdx][12]);
            data.magicDefense = Utils.ParseInt(stb.Cells[npcIdx][13]);
            data.flee = Utils.ParseInt(stb.Cells[npcIdx][14]);
            data.attackSpeed = Utils.ParseInt(stb.Cells[npcIdx][15]);
            data.attackType = Utils.ParseInt(stb.Cells[npcIdx][16]) == 1 ? AttackType.Magic : AttackType.Normal;
            data.AI = Utils.ParseInt(stb.Cells[npcIdx][17]);
            data.experience = Utils.ParseInt(stb.Cells[npcIdx][18]);
            data.dropTableID = Utils.ParseInt(stb.Cells[npcIdx][19]);
            data.moneyDrop = Utils.ParseInt(stb.Cells[npcIdx][20]);
            data.dropChance = Utils.ParseInt(stb.Cells[npcIdx][21]); // TODO : Make some smart read, like this field should always be between 1 and 100
            data.attackRange = Utils.ParseInt(stb.Cells[npcIdx][27]);
            data.characterType = Utils.ParseInt(stb.Cells[npcIdx][28]);
            data.faceIconID = Utils.ParseInt(stb.Cells[npcIdx][30]);
            data.generalSoundEffectID = Utils.ParseInt(stb.Cells[npcIdx][31]);
            data.attackedSoundEffectID = Utils.ParseInt(stb.Cells[npcIdx][33]);
            data.attackEffectID = Utils.ParseInt(stb.Cells[npcIdx][34]);
            data.dyingSoundID = Utils.ParseInt(stb.Cells[npcIdx][36]);
            data.isPartyQuestMonster = Utils.ParseBool(stb.Cells[npcIdx][39]);
            data.glowColor = Utils.ParseRgbColor(stb.Cells[npcIdx][40]);
            data.localizationID = Utils.ParseInt(stb.Cells[npcIdx][41]);
            data.eventTriggerDeath = stb.Cells[npcIdx][42];

            npc.monsterData = data;
            npc.skeleton = BakeSkeleton(chr.SkeletonFiles[chrObj.ID], context);

            var zsc = new ZscImporter(Path.Combine(DataPath, "3DDATA/NPC/PART_NPC.ZSC"), context);

            foreach (var zscPart in chrObj.Objects)
            {
                var part = zsc.ImportPart(zscPart.Object);

                if (part != null)
                {
                    npc.parts.Add(part);
                }
            }

            foreach (var zscMotion in chrObj.Animations)
            {
                if (zscMotion.Animation < 0)
                {
                    continue;
                }

                var anim = BakeAnimation(chr.MotionFiles[zscMotion.Animation], npc.skeleton, context);

                while (npc.animations.Count <= (int)zscMotion.Type)
                {
                    npc.animations.Add(null);
                }

                npc.animations[(int)zscMotion.Type] = anim;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            AssetDatabase.CreateAsset(npc, npcPath);

            EditorUtility.SetDirty(npc);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var npcAsset = AssetDatabase.LoadAssetAtPath<EntitySO>(npcPath);

            var test = BuildEntityModelPrefab(npcAsset, context);

            if (test == null)
            {
                Debug.LogError($"Failed to build prefab for NPC {npcIdx} - {stbName}");
                return null;
            }

            var prefab = PrefabUtility.SaveAsPrefabAsset(test, prefabPath);

            Debug.Log($"NPC prefab created: {prefabPath}");

            GameObject.DestroyImmediate(test);

            return prefab;
        }

        public class ZscImporter
        {
            public readonly ZSC zsc;
            public readonly string sourcePath;
            private readonly ImportContext context;

            public ZscImporter(string rosePath, ImportContext context)
            {
                sourcePath = rosePath;
                zsc = new ZSC(rosePath);
                this.context = context;
            }

            public RoseCharPartData ImportPart(int partIdx)
            {
                var partPath = Utils.CombinePath(context.Root, "Parts", $"NPC_PART_{partIdx}.asset");

                var existing = AssetDatabase.LoadAssetAtPath<RoseCharPartData>(partPath);

                if (existing != null)
                    return existing;

                Directory.CreateDirectory(Path.GetDirectoryName(partPath));

                var zscObj = zsc.Objects[partIdx];

                var mdl = ScriptableObject.CreateInstance<RoseCharPartData>();

                foreach (var part in zscObj.Models)
                {
                    var zmsPath = zsc.Models[part.ModelID];

                    var fullZmsPath = Utils.CombinePath(DataPath, zmsPath);

                    var zms = new ZMS(fullZmsPath);

                    var model = new Model
                    {
                        mesh = BakeMesh(zmsPath, context),
                        material = BakeMaterial(part.TextureID, zsc, sourcePath, context),
                        boneIndex = zms.support.bones ? (short)-1 : (short)part.BoneIndex
                    };

                    mdl.models.Add(model);
                }

                AssetDatabase.CreateAsset(mdl, partPath);

                EditorUtility.SetDirty(mdl);

                AssetDatabase.SaveAssets();

                return mdl;
            }
        }

        public static void ImportIcons()
        {
            AssetDatabase.StartAssetEditing();

            var iconsPath = Path.Combine(DataPath, "3DDATA", "CONTROL", "RES");
            var destFolder = "Assets/Resources/Icons";
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder(destFolder)) AssetDatabase.CreateFolder("Assets/Resources", "Icons");

            var textures = new List<Texture2D>();
            foreach (var file in Directory.GetFiles(iconsPath))
            {
                var fileName = Path.GetFileName(file);
                if (!fileName.StartsWith("ICON") || Path.GetExtension(file).ToLower() != ".dds") continue;

                var destPath = Path.Combine(destFolder, fileName).Replace("\\", "/");
                System.IO.File.Copy(file, destPath, true);
                AssetDatabase.ImportAsset(destPath, ImportAssetOptions.ForceUpdate);

                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(destPath);
                if (tex != null) textures.Add(tex);
                else Debug.LogWarning($"Impossible to load {destPath} as Texture2D.");
            }

            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();

            var dbPath = "Assets/Resources/IconsDatabase.asset";
            var db = AssetDatabase.LoadAssetAtPath<IconsDatabase>(dbPath);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<IconsDatabase>();
                AssetDatabase.CreateAsset(db, dbPath);
            }
            db.icons = textures;
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();

            Debug.Log($"Import finished : {textures.Count} icon imported.");
        }

        public static void CreatePlayerPrefabs()
        {
            var player = new RosePlayer();

            var prefabPath = ImportPaths.Player.Prefabs + "/Player.prefab";

            Utils.EnsureFolder(prefabPath);

            PrefabUtility.SaveAsPrefabAsset(player.player, prefabPath);

            GameObject.DestroyImmediate(player.player);
        }

        public static class AssetHelper
        {
            public delegate void ImportDone();

            public static readonly List<ImportDone> lateImportList = new();

            public static void StartAssetEditing() => AssetDatabase.StartAssetEditing();

            public static void StopAssetEditing()
            {
                AssetDatabase.StopAssetEditing();

                foreach (var lateImport in lateImportList)
                    lateImport();

                lateImportList.Clear();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            public static void ImportTexture(string path, ImportDone doneFn = null)
            {
                try
                {
                    AssetDatabase.ImportAsset(path);
                    if (doneFn != null) lateImportList.Add(doneFn);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("Failed to import texture: " + ex.Message);
                }
            }

            public static void Delay(ImportDone doneFn = null)
            {
                if (doneFn != null) lateImportList.Add(doneFn);
            }
        }
    }
}
#endif