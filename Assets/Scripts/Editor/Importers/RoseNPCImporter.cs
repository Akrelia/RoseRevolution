using System.IO;
using System.Linq;
using System;
using UnityEditor;
using UnityEngine;
using UnityRose.Formats;
using static UnityRose.ImportEditor.ROSEEditorBaker;
using RevolutionShared.Rose.Data.NPC;
using RevolutionShared.Rose.Data;
using UnityRose;
using System.Collections.Generic;
using UnityEditor.Animations;
using static UnityRose.Import.GameDataPaths;
using UnityRose.Import;

/// <summary>
/// Rose NPC Importer.
/// </summary>
public static class RoseNPCImporter
{
    /// <summary>
    /// Import a NPC.
    /// </summary>
    /// <param name="id">ID.</param>
    public static void ImportNPC(int id)
    {
        var npc = CreateNPC(id);

        if (npc)
        {
            RegisterNpcInInternalDB(npc, npc.GetComponent<EntityModelBehavior>().data);
        }

        else
        {
            Debug.LogError($"Failed to import NPC with ID {id}");
        }
    }

    /// <summary>
    /// Import NPC.
    /// </summary>
    /// <param name="id">ID.</param>
    /// <returns>Imported NPC.</returns>
    public static GameObject CreateNPC(int id)
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
    /// Bake the NPC.
    /// </summary>
    /// <param name="npcIdx">NPC Index.</param>
    /// <returns>Baked NPC.</returns>
    private static GameObject BakeEntity(int npcIdx)
    {
        var chr = new CHR(Path.Combine(DataPath, "3DDATA/NPC/LIST_NPC.CHR"));

        if (!chr.Characters[npcIdx].IsEnabled)
        {
            return null;
        }

        var stbName = ResourceManager.Instance.npcSTB.Cells[npcIdx][1].ToString();

        var context = new NPCImportContext(npcIdx, "Monsters", stbName);

        var npcPath = Path.Combine(context.Data, $"[{npcIdx}]{stbName}.asset");
        var prefabPath = Path.Combine(context.Prefab, $"[{npcIdx}]{stbName}.prefab");

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

        var stb = ResourceManager.Instance.npcSTB;

        data.id = npcIdx;
        data.displayName = stb.Cells[npcIdx][1];
        data.moveSpeed = EditorUtils.ParseSTBInt(stb.Cells[npcIdx][3]);
        data.runSpeed = EditorUtils.ParseSTBInt(stb.Cells[npcIdx][4]);
        data.size = EditorUtils.ParseSTBInt(stb.Cells[npcIdx][5]);
        data.rightWeaponID = EditorUtils.ParseSTBInt(stb.Cells[npcIdx][6]);
        data.leftWeaponID = EditorUtils.ParseSTBInt(stb.Cells[npcIdx][7]);
        data.level = EditorUtils.ParseSTBInt(stb.Cells[npcIdx][8]);
        data.healthPoints = EditorUtils.ParseSTBInt(stb.Cells[npcIdx][9]);
        data.attack = EditorUtils.ParseSTBInt(stb.Cells[npcIdx][10]);
        data.accuracy = EditorUtils.ParseSTBInt(stb.Cells[npcIdx][11]);
        data.defense = EditorUtils.ParseSTBInt(stb.Cells[npcIdx][12]);
        data.magicDefense = EditorUtils.ParseSTBInt(stb.Cells[npcIdx][13]);
        data.flee = EditorUtils.ParseSTBInt(stb.Cells[npcIdx][14]);
        data.attackSpeed = EditorUtils.ParseSTBInt(stb.Cells[npcIdx][15]);
        data.attackType = EditorUtils.ParseSTBInt(stb.Cells[npcIdx][16]) == 1 ? AttackType.Magic : AttackType.Normal;
        data.AI = EditorUtils.ParseSTBInt(stb.Cells[npcIdx][17]);
        data.experience = EditorUtils.ParseSTBInt(stb.Cells[npcIdx][18]);
        data.dropTableID = EditorUtils.ParseSTBInt(stb.Cells[npcIdx][19]);
        data.moneyDrop = EditorUtils.ParseSTBInt(stb.Cells[npcIdx][20]);
        data.dropChance = EditorUtils.ParseSTBInt(stb.Cells[npcIdx][21]); // TODO : Make some smart read, like this field should always be between 1 and 100
        data.attackRange = EditorUtils.ParseSTBInt(stb.Cells[npcIdx][27]);
        data.characterType = EditorUtils.ParseSTBInt(stb.Cells[npcIdx][28]);
        data.faceIconID = EditorUtils.ParseSTBInt(stb.Cells[npcIdx][30]);
        data.generalSoundEffectID = EditorUtils.ParseSTBInt(stb.Cells[npcIdx][31]);
        data.attackedSoundEffectID = EditorUtils.ParseSTBInt(stb.Cells[npcIdx][33]);
        data.attackEffectID = EditorUtils.ParseSTBInt(stb.Cells[npcIdx][34]);
        data.dyingSoundID = EditorUtils.ParseSTBInt(stb.Cells[npcIdx][36]);
        data.isPartyQuestMonster = EditorUtils.ParseSTBBool(stb.Cells[npcIdx][39]);
        data.glowColor = EditorUtils.ParseRoseColorInt(stb.Cells[npcIdx][40]);
        data.localizationID = EditorUtils.ParseSTBInt(stb.Cells[npcIdx][41]);
        data.eventTriggerDeath = stb.Cells[npcIdx][42];

        npc.monsterData = data;
        npc.skeleton = ImportSkeleton(chr.SkeletonFiles[chrObj.ID], context);

        var zsc = new ZSCImportHelper(Path.Combine(DataPath, "3DDATA/NPC/PART_NPC.ZSC"), context);

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

            var anim = ImportAnimation(chr.MotionFiles[zscMotion.Animation], npc.skeleton, context);

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
    /// Bake the animator controller for the NPC.
    /// </summary>
    /// <param name="npc">NPC.</param>
    /// <param name="path">Path.</param>
    /// <returns>Animator controller.</returns>
    private static AnimatorController BakeAnimatorController(EntitySO npc, string path)
    {
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != null)
        {
            AssetDatabase.DeleteAsset(path);
        }

        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);

        var stateMachine = controller.layers[0].stateMachine;

        AnimatorState defaultState = null;

        for (int i = 0; i < npc.animations.Count; i++)
        {
            var animation = npc.animations[i];

            if (animation == null)
            {
                continue;
            }

            var state = stateMachine.AddState($"Animation_{i}"); // TODO : A better way to name the states, an enum or something (also we should check if the animations are always in the same order)
           
            state.motion = animation;

            if (defaultState == null)
            {
                defaultState = state;
            }
        }

        if (defaultState != null)
        {
            stateMachine.defaultState = defaultState;
        }

        var layers = controller.layers;
        layers[0].defaultWeight = 1f;
        controller.layers = layers;

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        return AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
    }

    /// <summary>
    /// Register NPC in internal database.
    /// </summary>
    /// <param name="prefab">Prefab.</param>
    /// <param name="npc">NPC.</param>
    private static void RegisterNpcInInternalDB(GameObject prefab, EntitySO npc)
    {
        string folder = GameDataPaths.Database.Root;

        if (!AssetDatabase.IsValidFolder(folder))
        {
            AssetDatabase.CreateFolder(GameDataPaths.Root, "Databases");
        }

        string path = $"{folder}/NpcDatabase.asset";

        var database = AssetDatabase.LoadAssetAtPath<NPCDatabase>(path);

        if (database == null)
        {
            database = ScriptableObject.CreateInstance<NPCDatabase>();

            AssetDatabase.CreateAsset(database, path);
        }

        EditorUtils.EnsureAddressable(path, nameof(NPCDatabase));

        var existing = database.entries.Find(x => x.id == npc.monsterData.id);

        if (existing != null)
        {
            existing.name = npc.monsterData.displayName;
            existing.prefab = prefab;
            existing.data = npc;
        }

        else
        {
            database.entries.Add(new NPCDatabaseEntry
            {
                id = npc.monsterData.id,
                name = npc.monsterData.displayName,
                prefab = prefab,
                data = npc
            });
        }

        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// An Helper for keeping some references, could be removed later.
    /// </summary>
    public class ZSCImportHelper
    {
        public readonly ZSC zsc;
        public readonly string sourcePath;
        private readonly ImportContext context;

        public ZSCImportHelper(string rosePath, ImportContext context)
        {
            sourcePath = rosePath;
            zsc = new ZSC(rosePath);
            this.context = context;
        }

        public RoseCharPartData ImportPart(int partIdx)
        {
            var partPath = Path.Combine(context.Root, "Parts", $"NPC_PART_{partIdx}.asset");

            var existing = AssetDatabase.LoadAssetAtPath<RoseCharPartData>(partPath);

            if (existing != null)
                return existing;

            Directory.CreateDirectory(Path.GetDirectoryName(partPath));

            var zscObj = zsc.Objects[partIdx];

            var mdl = ScriptableObject.CreateInstance<RoseCharPartData>();

            foreach (var part in zscObj.Models)
            {
                var zmsPath = zsc.Models[part.ModelID];

                var fullZmsPath = Path.Combine(DataPath, zmsPath);

                var zms = new ZMS(fullZmsPath);

                var model = new Model
                {
                    mesh = ImportMesh(zmsPath, context),
                    material = ImportEquipmentMaterial(part.TextureID, zsc, sourcePath, context),
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
}
