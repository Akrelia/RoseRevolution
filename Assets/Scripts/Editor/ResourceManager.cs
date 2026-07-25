using RevolutionShared.Rose.Data;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityRose.Formats;
using UnityRose.Import;
using UnityRose.ImportEditor;

namespace UnityRose
{
    public class ResourceManager
    {
        public static Dictionary<int, WeaponType> weapon_type_lookup = new Dictionary<int, WeaponType>() {
            { 211, WeaponType.OHSWORD},
            { 212, WeaponType.OHMACE},
            { 271, WeaponType.XBOX},
            { 221, WeaponType.THSWORD},
            { 223, WeaponType.THBLUNT},
            { 222, WeaponType.THSPEAR},
            { 231, WeaponType.BOW},
            { 232, WeaponType.GUN},
            { 233, WeaponType.CANNON},
            { 241, WeaponType.STAFF},
            { 242, WeaponType.WAND},
            { 251, WeaponType.KATAR},
            { 252, WeaponType.DSW},
        };

        // Male ZSC's (equipment model links) - still loaded and available for anything
        // that reads model/texture data directly (e.g. tools, inspection), even though
        // RosePlayer itself no longer builds body parts from these at runtime.
        public ZSC zsc_body_male;
        public ZSC zsc_arms_male;
        public ZSC zsc_foot_male;
        public ZSC zsc_face_male;
        public ZSC zsc_hair_male;
        public ZSC zsc_cap_male;

        // Female ZSC's
        public ZSC zsc_body_female;
        public ZSC zsc_arms_female;
        public ZSC zsc_foot_female;
        public ZSC zsc_face_female;
        public ZSC zsc_hair_female;
        public ZSC zsc_cap_female;

        // Unisex ZSC's
        public ZSC zsc_back;
        public ZSC zsc_faceItem;
        public ZSC zsc_weapon;
        public ZSC zsc_subweapon;

        // ZMD's (skeleton)
        public ZMD zmd_male;
        public ZMD zmd_female;

        // STB's
        public STB stb_animation_list;
        public STB stb_animation_type;
        public STB stb_weapon_list;
        public STB stb_cap_list;
        public STB stb_arms_list;
        public STB stb_armor_list;
        public STB stb_foot_list;
        public STB stb_back_list; // Turn into Dictio
        public STB stb_faceitem_list;
        public STB stb_hair_list;
        public STB stb_npc_list;
        public STB stb_zone;

        // STL
        public STL stl_zone_list;

        // TODO: add any other common persistent resources here

        // TODO: determine optimal cache size
        // Possibly have a different cache for each resource type ?
        private const int CACHE_SIZE = 250;
        private Cache cache;

        private static ResourceManager instance;

        public static ResourceManager Instance => instance ??= new ResourceManager();

        private ResourceManager()
        {
            zsc_body_male = (ZSC)loadResource("3DDATA/AVATAR/LIST_MBODY.ZSC");
            zsc_arms_male = (ZSC)loadResource("3DData/Avatar/LIST_MARMS.zsc");
            zsc_foot_male = (ZSC)loadResource("3DData/Avatar/LIST_MFOOT.zsc");
            zsc_face_male = (ZSC)loadResource("3DData/Avatar/LIST_MFACE.zsc");
            zsc_hair_male = (ZSC)loadResource("3DData/Avatar/LIST_MHAIR.zsc");
            zsc_cap_male = (ZSC)loadResource("3DData/Avatar/LIST_MCAP.zsc");

            zsc_body_female = (ZSC)loadResource("3DData/Avatar/LIST_WBODY.zsc");
            zsc_arms_female = (ZSC)loadResource("3DData/Avatar/LIST_WARMS.zsc");
            zsc_foot_female = (ZSC)loadResource("3DData/Avatar/LIST_WFOOT.zsc");
            zsc_face_female = (ZSC)loadResource("3DData/Avatar/LIST_WFACE.zsc");
            zsc_hair_female = (ZSC)loadResource("3DData/Avatar/LIST_WHAIR.zsc");
            zsc_cap_female = (ZSC)loadResource("3DData/Avatar/LIST_WCAP.zsc");

            zsc_back = (ZSC)loadResource("3DData/Avatar/LIST_BACK.zsc");
            zsc_faceItem = (ZSC)loadResource("3DData/Avatar/LIST_FACEIEM.zsc");

            zsc_weapon = (ZSC)loadResource("3DDATA/WEAPON/LIST_WEAPON.zsc");
            zsc_subweapon = (ZSC)loadResource("3DDATA/WEAPON/LIST_SUBWPN.zsc");

            stb_animation_list = (STB)loadResource("3Ddata/STB/FILE_MOTION.STB");
            stb_animation_type = (STB)loadResource("3DDATA/STB/TYPE_MOTION.STB");

            stb_weapon_list = (STB)loadResource("3DDATA/STB/LIST_WEAPON.STB");
            stb_cap_list = (STB)loadResource("3DDATA/STB/LIST_CAP.STB");
            stb_armor_list = (STB)loadResource("3DDATA/STB/LIST_BODY.STB");
            stb_back_list = (STB)loadResource("3DDATA/STB/LIST_BACK.STB");
            stb_foot_list = (STB)loadResource("3DDATA/STB/LIST_FOOT.STB");
            stb_arms_list = (STB)loadResource("3DDATA/STB/LIST_ARMS.STB");
            stb_faceitem_list = (STB)loadResource("3DDATA/STB/LIST_FACEITEM.STB");
            stb_hair_list = (STB)loadResource("3DDATA/STB/LIST_HAIR.STB");
            stb_npc_list = (STB)loadResource("3DDATA/STB/LIST_NPC.STB");
            stb_zone = (STB)loadResource("3DDATA/STB/LIST_ZONE.STB");

            stl_zone_list = (STL)loadResource("3DDATA/STB/LIST_ZONE_S.STL");

            cache = new Cache(this, CACHE_SIZE);
        }

        /// <summary>
        /// Load a Rose Asset from text asset resource file to memory. Not cached.
        /// </summary>
        public object loadResource(string path)
        {
            path = Path.Combine(RoseDataSource.DataPath, path);
            var dir = new DirectoryInfo(path);

            switch (dir.Extension)
            {
                case ".zms":
                case ".ZMS":
                    return new ZMS(path);
                case ".zmd":
                case ".ZMD":
                    return new ZMD(path);
                case ".zsc":
                case ".ZSC":
                    return new ZSC(path);
                case ".stb":
                case ".STB":
                    return new STB(path);
                case ".zmo":
                case ".ZMO":
                    return new ZMO(path);
                // TODO: add all other rose formats here
                case ".png":
                case ".PNG":
                    return Resources.Load(path.Replace(dir.Extension, ""));
                default:
                    return null;
            }
        }

        public void unloadResource(UnityEngine.Object resource)
        {
            Resources.UnloadAsset(resource);
        }

        /// <summary>
        /// Checks the cache to see if the resource has already been loaded recently
        /// If found, returns the cached resource from memory (fast)
        /// If not found, loads the resource from file (slow) and caches the resource
        /// </summary>
        public object cachedLoad(string path)
        {
            return cache.request(path);
        }

        /// <summary>
        /// Get Animation ZMO File path
        /// </summary>
        public string GetZMOPath(WeaponType WeaponType, ActionType Action, GenderType Gender)
        {
            int actionIdx = (int)Action;

            if (!weaponAnimationColumn.TryGetValue(WeaponType, out int weaponIdx))
            {
                Debug.LogWarning($"GetZMOPath: no animation column mapping for {WeaponType}.");

                return "";
            }

            if (actionIdx < 0 || actionIdx >= stb_animation_type.Cells.Count)
            {
                Debug.LogWarning($"GetZMOPath: ActionType {Action} ({actionIdx}) is out of range for stb_animation_type ({stb_animation_type.Cells.Count} rows).");
                return "";
            }

            var row = stb_animation_type.Cells[actionIdx];

            if (weaponIdx < 0 || weaponIdx >= row.Count)
            {
                Debug.LogWarning($"GetZMOPath: WeaponType {WeaponType} ({weaponIdx}) is out of range for stb_animation_type row {actionIdx} ({row.Count} columns).");

                return "";
            }

            string motionTypeCell = row[weaponIdx];

            if (string.IsNullOrWhiteSpace(motionTypeCell) || !int.TryParse(motionTypeCell, out int motionTypeIdx))
            {
                Debug.LogWarning($"GetZMOPath: no valid motion type for {Gender}/{WeaponType}/{Action} (cell value: '{motionTypeCell}').");
               
                return "";
            }

            if (motionTypeIdx < 0 || motionTypeIdx >= stb_animation_list.Cells.Count)
            {
                Debug.LogWarning($"GetZMOPath: motion type index {motionTypeIdx} out of range for stb_animation_list ({stb_animation_list.Cells.Count} rows).");
           
                return "";
            }

            var animRow = stb_animation_list.Cells[motionTypeIdx];
            int genderIdx = (int)Gender;

            string filePath = (genderIdx >= 0 && genderIdx < animRow.Count) ? animRow[genderIdx] : "";

            if (filePath == "")
            {
                int maleIdx = (int)GenderType.MALE;
               
                filePath = (maleIdx >= 0 && maleIdx < animRow.Count) ? animRow[maleIdx] : "";
            }

            return filePath;
        }

        public WeaponType getWeaponType(int weaponID)
        {
            int typeID = 0;
            WeaponType type = WeaponType.EMPTY;
            try
            {
                typeID = int.Parse(stb_weapon_list.Cells[weaponID][5]); // TODO: create enums for the columns and use them to look things up
                type = weapon_type_lookup[typeID];
            }
            catch (Exception ex)
            {
                Debug.Log(ex.Message);
                type = WeaponType.EMPTY;
            }

            return type;
        }

        private static readonly Dictionary<WeaponType, int> weaponAnimationColumn = new()
{
    { WeaponType.EMPTY, 1 },
    { WeaponType.OHSWORD, 2 },
    { WeaponType.THAXE, 3 },
    { WeaponType.OHMACE, 4 },
    { WeaponType.OHTOOL, 5 },
    { WeaponType.THSWORD, 6 },
    { WeaponType.THSPEAR, 7 },
    { WeaponType.DSW, 8 },
    { WeaponType.THBLUNT, 9 },
    { WeaponType.CANNON, 10 },
    { WeaponType.BOW, 11 },
    { WeaponType.XBOX, 12 },
    { WeaponType.GUN, 13 },
    { WeaponType.STAFF, 14 },
    { WeaponType.WAND, 15 },
    { WeaponType.BOOK, 16 },
    { WeaponType.KATAR, 17 },
    { WeaponType.SHIELD, 18 }
};

        /// <summary>
        /// Loop through all weapon types for each gender and create an animation asset and all associated clips
        /// The animations and clips are placed in GameData/Animation
        /// </summary>      
        public void GenerateAnimationAssets()
        {
            foreach (GenderType gender in Enum.GetValues(typeof(GenderType)))
            {
                if (gender == GenderType.NONE) continue;

                foreach (WeaponType weapon in Enum.GetValues(typeof(WeaponType)))
                {
                    GenerateAnimationAsset(gender, weapon);
                }
            }
        }

        private string[] getBoneNames(Transform[] transforms)
        {
            List<string> names = new List<string>();
            foreach (Transform transform in transforms)
            {
                names.Add(transform.name);
            }

            return names.ToArray();
        }

        public void GenerateAnimationAsset(GenderType gender, WeaponType weapon)
        {
            GameObject skeleton = new GameObject("skeleton");
            bool male = (gender == GenderType.MALE);
            ZMD zmd = new ZMD(male ? ROSEEditorBaker.DataPath + "/3DData/Avatar/MALE.ZMD" : ROSEEditorBaker.DataPath + "/3DData/Avatar/FEMALE.ZMD");
            zmd.buildSkeleton(skeleton);

            BindPoses poses = ScriptableObject.CreateInstance<BindPoses>();
            poses.bindPoses = zmd.bindposes;
            poses.boneNames = getBoneNames(zmd.boneTransforms);
            poses.boneTransforms = zmd.boneTransforms;
            LoadClips(skeleton, zmd, weapon, gender);

            string path = "Assets/Resources/Animation/" + gender.ToString() + "/" + weapon.ToString() + "/skeleton.prefab";
            Utils.EnsureFolder(path); // manquait - CreateAsset exige que le dossier existe déjà

            AssetDatabase.CreateAsset(poses, path.Replace("skeleton.prefab", "bindPoses.asset"));
            AssetDatabase.SaveAssets();
            PrefabUtility.SaveAsPrefabAsset(skeleton, path);
        }

        public void GenerateAnimationAsset(GenderType gender, RigType rig, Dictionary<String, String> zmoPaths)
        {
            GameObject skeleton = new GameObject("skeleton");
            bool male = (gender == GenderType.MALE);
            ZMD zmd = new ZMD(male ? ROSEEditorBaker.DataPath + "/3DData/Avatar/MALE.ZMD" : ROSEEditorBaker.DataPath + "/3DData/Avatar/FEMALE.ZMD");
            zmd.buildSkeleton(skeleton);

            BindPoses poses = ScriptableObject.CreateInstance<BindPoses>();
            poses.bindPoses = zmd.bindposes;
            poses.boneNames = getBoneNames(zmd.boneTransforms);
            poses.boneTransforms = zmd.boneTransforms;
            LoadClips(skeleton, zmd, gender, rig, zmoPaths);
            string path = "Assets/Resources/Animation/" + gender.ToString() + "/" + rig.ToString() + "/skeleton.prefab";
            AssetDatabase.CreateAsset(poses, path.Replace("skeleton.prefab", "bindPoses.asset"));
            AssetDatabase.SaveAssets();
            PrefabUtility.SaveAsPrefabAsset(skeleton, path);
        }

        public void LoadClips(GameObject skeleton, ZMD zmd, WeaponType weapon, GenderType gender)
        {
            List<AnimationClip> clips = new List<AnimationClip>();

            foreach (ActionType action in Enum.GetValues(typeof(ActionType)))
            {
                string zmoRelativePath = GetZMOPath(weapon, action, gender);

                if (string.IsNullOrWhiteSpace(zmoRelativePath))
                {
                    Debug.LogWarning($"Skipping {gender}/{weapon}/{action}: no ZMO path found.");
                    continue;
                }

                string zmoPath = ROSEEditorBaker.DataPath + "/" + Utils.FixPath(zmoRelativePath);

                if (!File.Exists(zmoPath))
                {
                    Debug.LogWarning($"Skipping missing ZMO for {gender}/{weapon}/{action}: {zmoPath}");
                    continue;
                }

                string unityPath = "Assets/Resources/Animation/" + gender.ToString() + "/" + weapon.ToString() + "/clips/" + action.ToString() + ".anim";
                Utils.EnsureFolder(unityPath);

                // NOTE: not using Utils.SaveReloadAsset here - it internally reroutes the path
                // through r2uDir(), which expects a raw ROSE path and rewrites it into its own
                // Unity path, silently ignoring the exact unityPath we just prepared with
                // EnsureFolder above. That mismatch (create the folder at path A, then try to
                // write to path B) made CreateAsset fail silently inside SaveReloadAsset (no
                // try/catch, no log in there), so the clip vanished with nothing but the
                // ActionType/GetZMOPath warnings visible - hence "thousands of warnings, no
                // error, nothing saved". Writing directly here avoids that mismatch entirely.
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

        public void LoadClips(GameObject skeleton, ZMD zmd, GenderType gender, RigType rig, Dictionary<String, String> zmoPaths)
        {
            List<AnimationClip> clips = new List<AnimationClip>();

            foreach (KeyValuePair<String, String> motion in zmoPaths)
            {
                if (string.IsNullOrWhiteSpace(motion.Value))
                {
                    Debug.LogWarning($"Skipping motion '{motion.Key}' ({gender}/{rig}): empty ZMO path.");
                    continue;
                }

                string zmoPath = ROSEEditorBaker.DataPath + "/" + motion.Value;

                if (!File.Exists(zmoPath))
                {
                    Debug.LogWarning($"Skipping missing ZMO for motion '{motion.Key}' ({gender}/{rig}): {zmoPath}");
                    continue;
                }

                string unityPath = "Assets/Resources/Animation/" + gender.ToString() + "/" + rig.ToString() + "/clips/" + motion.Key + ".anim";
                Utils.EnsureFolder(unityPath);

                var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(unityPath);

                AnimationClip clip;
                if (existing == null)
                {
                    clip = new ZMO(zmoPath).buildAnimationClip(zmd);
                    clip.name = motion.Key;
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
        public void LoadAnimations(GameObject player, ZMD skeleton, WeaponType weapon, GenderType gender)
        {
            List<AnimationClip> clips = new List<AnimationClip>();

            foreach (ActionType action in Enum.GetValues(typeof(ActionType)))
            {
                string zmoPath = Utils.FixPath(ResourceManager.Instance.GetZMOPath(weapon, action, gender));
                AnimationClip clip = R2U.GetClip(zmoPath, skeleton, action.ToString());
                clip.legacy = true;
                clips.Add(clip);
            }

            Animation animation = player.GetComponent<Animation>();
            AnimationUtility.SetAnimationClips(animation, clips.ToArray());
        }
    }
}