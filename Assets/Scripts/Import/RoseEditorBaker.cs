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
using static RevolutionShared.Rose.Data.RoseEnums;
using static UnityRose.Formats.IFO;
using UnityRose.Game;

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
            public const string Parts = NPC.Root + "/Parts";
            public const string Animations = NPC.Root + "/Animations";
            public const string Avatars = NPC.Root + "/Avatars";
            public const string Controllers = NPC.Root + "/Controllers";
        }

        public static class Player
        {
            public const string Root = ImportPaths.Root + "/Player";
            public const string Prefabs = Player.Root + "/Prefabs";
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

        private static string GenerateAssetPath(string rosePath, string unityExt)
        {
            rosePath = Utils.NormalizePath(rosePath);
            var dirPath = Path.GetDirectoryName(rosePath);
            var pathName = dirPath.Length > 7 ? dirPath.Substring(7) : dirPath; // strip "3DDATA/"
            var meshName = Path.GetFileNameWithoutExtension(rosePath);

            return Utils.CombinePath(ImportPaths.Root, pathName, meshName + unityExt);
        }

        public static Mesh BakeMesh(string rosePath)
        {
            var assetPath = GenerateAssetPath(rosePath, ".mesh.asset");
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
            if (existing != null) return existing;

            var mesh = RoseMeshImporter.Import(rosePath);
            if (mesh == null) return null;

            Directory.CreateDirectory(Path.GetDirectoryName(assetPath));
            AssetDatabase.CreateAsset(mesh, assetPath);
            return mesh;
        }

        public static RoseSkeletonData BakeSkeleton(string rosePath)
        {
            var assetPath = GenerateAssetPath(rosePath, ".skel.asset");
            var existing = AssetDatabase.LoadAssetAtPath<RoseSkeletonData>(assetPath);
            if (existing != null) return existing;

            var skel = RoseSkeletonImporter.Import(rosePath);
            if (skel == null) return null;

            Directory.CreateDirectory(Path.GetDirectoryName(assetPath));
            AssetDatabase.CreateAsset(skel, assetPath);
            EditorUtility.SetDirty(skel);
            AssetDatabase.SaveAssets();

            return AssetDatabase.LoadAssetAtPath<RoseSkeletonData>(assetPath);
        }

        public static AnimationClip BakeAnimation(string rosePath, RoseSkeletonData skeleton)
        {
            var assetPath = GenerateAssetPath(rosePath, ".anim.asset");
            var clip = RoseAnimationImporter.Import(rosePath, skeleton);
            if (clip == null) return null;

            Directory.CreateDirectory(Path.GetDirectoryName(assetPath));
            AssetDatabase.CreateAsset(clip, assetPath);

            return clip;
        }

        public static Texture2D BakeTexture(string rosePath)
        {
            var fullPath = Utils.ResolvePathWithCorrectCase(DataPath, rosePath);

            if (!File.Exists(fullPath))
            {
                Debug.LogWarning("Could not find referenced texture: " + fullPath);

                return null;
            }

            var texPath = GenerateAssetPath(rosePath, Path.GetExtension(rosePath));

            if (!File.Exists(texPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(texPath));
                File.Copy(fullPath, texPath);

                AssetDatabase.ImportAsset(texPath, ImportAssetOptions.ForceSynchronousImport);
            }

            AssetDatabase.Refresh();

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);

            if (texture == null)
            {
                Debug.LogError("Texture still null after import: " + texPath);

                return null;
            }

            return texture;
        }

        public static Material BakeMaterial(string targetFolder, int materialIdx, ZSC zsc, string pathZ)
        {
            var zscName = Path.GetFileNameWithoutExtension(pathZ);

            var matFolder = Path.Combine(ImportPaths.NPC.Materials, zscName);

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


            var texture = BakeTexture(zscMat.Path);

            mat.SetTexture("_BaseMap", texture);


            AssetDatabase.CreateAsset(mat, matPath);

            var check = AssetDatabase.LoadAssetAtPath<Material>(matPath);

            EditorUtility.SetDirty(mat);

            AssetDatabase.SaveAssets();


            return mat;
        }

        private static AnimatorController BakeAnimatorController(RoseNPCInfos npc, string path)
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

        public static GameObject ImportNPC(int id)
        {

            try
            {
                var npc = BakeNpc(id);

                return npc;
            }

            finally
            {
            }
        }

        public static void ImportAllNPC()
        {
            AssetHelper.StartAssetEditing();

            try
            {
                var chr = new CHR(Utils.CombinePath(DataPath, "3DDATA/NPC/LIST_NPC.CHR"));

                foreach (var i in Enumerable.Range(0, chr.Characters.Count))
                {
                    var npc = BakeNpc(i);

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

        private static GameObject BuildNpcPrefab(RoseNPCInfos npc)
        {
            if (npc == null)
            {
                Debug.LogError("Cannot build NPC prefab: npc is null");
                return null;
            }

            var root = new GameObject(npc.npcName);


            var bones = new List<Transform>();

            // Build skeleton if available
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


            // Build mesh parts
            foreach (var part in npc.parts)
            {
                if (part == null)
                    continue;


                foreach (var model in part.models)
                {
                    if (model.mesh == null) continue;

                    var obj = new GameObject("Model");
                    obj.transform.SetParent(root.transform, false);

                    if (npc.skeleton != null && model.boneIndex == -1) // skinned
                    {
                        var renderer = obj.AddComponent<SkinnedMeshRenderer>();

                        model.mesh.bindposes = bones.Select(b => b.worldToLocalMatrix * root.transform.localToWorldMatrix).ToArray(); // see note below - bindposes must match bone order/rest pose

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

                avatar.name = $"{npc.npcName}_Avatar";

                if (!avatar.isValid)
                {
                    Debug.LogError($"Failed to build a valid avatar for NPC {npc.npcName} " + "(check that 'b1_pelvis' exists as a child Transform under root).");
                }

                else
                {
                    var avatarPath = $"{ImportPaths.NPC.Avatars}/{npc.id}.asset";

                    Directory.CreateDirectory(Path.GetDirectoryName(avatarPath));

                    if (AssetDatabase.LoadAssetAtPath<Avatar>(avatarPath) != null)
                        AssetDatabase.DeleteAsset(avatarPath);

                    AssetDatabase.CreateAsset(avatar, avatarPath);
                    EditorUtility.SetDirty(avatar);
                    AssetDatabase.SaveAssets();

                    animator.avatar = AssetDatabase.LoadAssetAtPath<Avatar>(avatarPath);
                }

                var controllerPath = $"{ImportPaths.NPC.Controllers}/{npc.id}.controller";
                Directory.CreateDirectory(Path.GetDirectoryName(controllerPath));

                var controller = BakeAnimatorController(npc, controllerPath);
                var layers = controller.layers; layers[0].defaultWeight = 1f;
                animator.runtimeAnimatorController = controller;

            }

            float scale = npc.monsterData.size / 100F;

            root.transform.localScale = new Vector3(scale, scale, scale);

            var npcComponent = root.AddComponent<RoseNpc>();
            npcComponent.data = npc;

            return root;
        }

        private static GameObject BakeNpc(int npcIdx)
        {
            var chr = new CHR(Utils.CombinePath(DataPath, "3DDATA/NPC/LIST_NPC.CHR"));

            if (!chr.Characters[npcIdx].IsEnabled)
            {
                return null;
            }

            var stbName = ResourceManager.Instance.stb_npc_list.Cells[npcIdx][1].ToString();

            var npcPath = Utils.CombinePath(ImportPaths.NPC.Data, $"[{npcIdx}]{stbName}.asset");
            var prefabPath = Utils.CombinePath(ImportPaths.NPC.Prefabs, $"[{npcIdx}]{stbName}.prefab");

            var existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (existingPrefab != null)
            {
                return existingPrefab;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(npcPath));
            Directory.CreateDirectory(Path.GetDirectoryName(prefabPath));

            var chrObj = chr.Characters[npcIdx];

            var npc = ScriptableObject.CreateInstance<RoseNPCInfos>();

            MonsterData data = new MonsterData();

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
            data.attackType = Utils.ParseInt(stb.Cells[npcIdx][16]) == 1 ? NPCAttackType.Magic : NPCAttackType.Normal;
            data.AI = Utils.ParseInt(stb.Cells[npcIdx][17]);
            data.experience = Utils.ParseInt(stb.Cells[npcIdx][18]);
            data.drop = Utils.ParseInt(stb.Cells[npcIdx][19]);
            data.moneyDrop = Utils.ParseInt(stb.Cells[npcIdx][20]);
            data.dropTableID = Utils.ParseInt(stb.Cells[npcIdx][21]);
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

            npc.id = npcIdx;
            npc.npcName = stbName;
            npc.monsterData = data;
            npc.skeleton = BakeSkeleton(chr.SkeletonFiles[chrObj.ID]);

            var zsc = new ZscImporter(Path.Combine(DataPath, "3DDATA/NPC/PART_NPC.ZSC"), ImportPaths.NPC.Parts);

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

                var anim = BakeAnimation(chr.MotionFiles[zscMotion.Animation], npc.skeleton);

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

            var npcAsset = AssetDatabase.LoadAssetAtPath<RoseNPCInfos>(npcPath);

            var test = BuildNpcPrefab(npcAsset);

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
            private readonly string targetPath;
            public readonly ZSC zsc;
            public readonly string sourcePath;

            public ZscImporter(string rosePath, string targetPath)
            {
                this.targetPath = targetPath;
                sourcePath = rosePath;
                zsc = new ZSC(rosePath);
            }

            public RoseCharPartData ImportPart(int partIdx)
            {
                var partPath = Utils.CombinePath(targetPath, "NPC_PART_" + partIdx + ".asset");

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
                    var zms = new ZMS(fullZmsPath); // needed here to read support.bones before BakeMesh discards it

                    var model = new Model
                    {
                        mesh = BakeMesh(zmsPath),
                        material = BakeMaterial(targetPath, part.TextureID, zsc, sourcePath),
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