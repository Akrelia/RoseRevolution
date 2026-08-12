using RevolutionShared.Rose.Data;
using System.Collections.Generic;
using System;
using UnityEditor;
using UnityEngine;
using UnityRose.Formats;
using UnityRose.ImportEditor;
using UnityRose;
using System.IO;
using UnityRose.Import;

/// <summary>
/// Rose character importer.
/// </summary>
public static class RoseCharacterImporter
{
    /// <summary>
    /// Generate animation assets.
    /// </summary>
    public static void ImportSkeletons()
    {
        foreach (GenderType gender in Enum.GetValues(typeof(GenderType)))
        {
            if (gender == GenderType.NONE) continue;

            foreach (WeaponType weapon in Enum.GetValues(typeof(WeaponType)))
            {
                GenerateSkeleton(gender, weapon);
            }
        }
    }

    /// <summary>
    /// Generate the skeleton.
    /// </summary>
    /// <param name="gender"></param>
    /// <param name="weapon"></param>
    public static void GenerateSkeleton(GenderType gender, WeaponType weapon)
    {
        GameObject skeleton = new GameObject("skeleton");
        bool male = gender == GenderType.MALE;
        ZMD zmd = new ZMD(male ? ROSEEditorBaker.DataPath + "/3DData/Avatar/MALE.ZMD" : ROSEEditorBaker.DataPath + "/3DData/Avatar/FEMALE.ZMD");
        zmd.buildSkeleton(skeleton);

        BindPoses poses = ScriptableObject.CreateInstance<BindPoses>();
        poses.bindPoses = zmd.bindposes;
        poses.boneNames = EditorUtils.GetBoneNames(zmd.boneTransforms);
        poses.boneTransforms = zmd.boneTransforms;

        LoadClips(skeleton, zmd, weapon, gender);

        string path = "Assets/GameData/Player/Animations/" + gender + "/" + weapon + "/skeleton.prefab";
        EditorUtils.EnsureFolder(path);

        AssetDatabase.CreateAsset(poses, path.Replace("skeleton.prefab", "bindPoses.asset"));
        AssetDatabase.SaveAssets();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(skeleton, path);

        RegisterSkeleton(gender, weapon, prefab, poses);

        GameObject.DestroyImmediate(skeleton);
    }

    /// <summary>
    /// Register the skeleton in the database.
    /// </summary>
    /// <param name="gender"></param>
    /// <param name="weapon"></param>
    /// <param name="prefab"></param>
    /// <param name="poses"></param>
    private static void RegisterSkeleton(GenderType gender, WeaponType weapon, GameObject prefab, BindPoses poses)
    {
        string folder = GameDataPaths.Database.Root;

        if (!AssetDatabase.IsValidFolder(folder))
        {
            AssetDatabase.CreateFolder(GameDataPaths.Root, "Databases");
        }

        string path = $"{folder}/CharacterDatabase.asset";

        var skeletonDatabase = AssetDatabase.LoadAssetAtPath<CharacterDatabase>(path);

        if (skeletonDatabase == null)
        {
            skeletonDatabase = ScriptableObject.CreateInstance<CharacterDatabase>();

            AssetDatabase.CreateAsset(skeletonDatabase, path);
        }

        EditorUtils.EnsureAddressable(path, nameof(CharacterDatabase));

        var genderEntry = skeletonDatabase.genders.Find(x => x.gender == gender);

        if (genderEntry == null)
        {
            genderEntry = new CharacterDatabase.GenderEntry
            {
                gender = gender,
                weapons = new List<CharacterDatabase.SkeletonEntry>()
            };

            skeletonDatabase.genders.Add(genderEntry);
        }

        var weaponEntry = genderEntry.weapons.Find(x => x.weapon == weapon);

        if (weaponEntry == null)
        {
            weaponEntry = new CharacterDatabase.SkeletonEntry
            {
                weapon = weapon,
                prefab = prefab,
                bindPoses = poses
            };

            genderEntry.weapons.Add(weaponEntry);
        }

        EditorUtility.SetDirty(skeletonDatabase);
        AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// Load clips animation.
    /// </summary>
    /// <param name="skeleton"></param>
    /// <param name="zmd"></param>
    /// <param name="weapon"></param>
    /// <param name="gender"></param>
    public static void LoadClips(GameObject skeleton, ZMD zmd, WeaponType weapon, GenderType gender)
    {
        List<AnimationClip> clips = new List<AnimationClip>();

        foreach (ActionType action in Enum.GetValues(typeof(ActionType)))
        {
            string zmoRelativePath = ResourceManager.Instance.GetZMOPath(weapon, action, gender);

            if (string.IsNullOrWhiteSpace(zmoRelativePath))
            {
                Debug.LogWarning($"Skipping {gender}/{weapon}/{action}: no ZMO path found.");
                continue;
            }

            string zmoPath = ROSEEditorBaker.DataPath + "/" + EditorUtils.FixPath(zmoRelativePath);

            if (!File.Exists(zmoPath))
            {
                Debug.LogWarning($"Skipping missing ZMO for {gender}/{weapon}/{action}: {zmoPath}");
                continue;
            }

            string unityPath = "Assets/GameData/Player/Animations/" + gender.ToString() + "/" + weapon.ToString() + "/clips/" + action.ToString() + ".anim";
            EditorUtils.EnsureFolder(unityPath);

            var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(unityPath);

            AnimationClip clip;

            if (existing == null)
            {
                clip = new ZMO(zmoPath).buildAnimationClip(zmd);
                clip.name = action.ToString().ToLowerInvariant();
                clip.legacy = true;

                AssetDatabase.CreateAsset(clip, unityPath);
                AssetDatabase.SaveAssets();

                clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(unityPath);
            }
            else
            {
                clip = existing;
            }

            clips.Add(clip);
        }

        Animation animation = skeleton.AddComponent<Animation>();
        AnimationUtility.SetAnimationClips(animation, clips.ToArray());
    }

    //public void LoadAnimations(GameObject player, ZMD skeleton, WeaponType weapon, GenderType gender)
    //{
    //    List<AnimationClip> clips = new List<AnimationClip>();

    //    foreach (ActionType action in Enum.GetValues(typeof(ActionType)))
    //    {
    //        string zmoPath = EditorUtils.FixPath(ResourceManager.Instance.GetZMOPath(weapon, action, gender));
    //        AnimationClip clip = R2U.GetClip(zmoPath, skeleton, action.ToString());
    //        clip.legacy = true;
    //        clips.Add(clip);
    //    }

    //    Animation animation = player.GetComponent<Animation>();
    //    AnimationUtility.SetAnimationClips(animation, clips.ToArray());
    //}
}
