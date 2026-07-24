// ResourceManager.cs
//
// Restored to keep every resource that's used elsewhere in the codebase (RosePatch reads
// stb_zone directly, for instance) - only two things were actually removed from the
// original:
//
//   1. getZSC(gender, bodyPart) + LoadPart(...): the player-equipment pipeline that
//      parsed ZSC/ZMS/DDS at runtime to build a body part on the fly. RosePlayer no
//      longer needs this - it instantiates pre-baked prefabs from RoseAvatarDatabase
//      instead. Nothing else in the codebase called these two methods.
//
//   2. GenerateAnimationAsset(s) / LoadClips(...): Editor-only baking (AssetDatabase,
//      PrefabUtility) that doesn't belong in a class with no UnityEditor dependency.
//      That logic should live in ROSEEditorBaker instead, not here.
//
// Everything else - every ZSC/STB/STL/ZMD field, loadResource, cachedLoad, getWeaponType,
// GetZMOPath - is unchanged from the original, still runtime-safe (no UnityEditor usings
// were ever needed for any of this), just no longer routed through the old ROSEImport
// static path (uses RoseDataSource.DataPath, same as before).

using RevolutionShared.Rose.Data;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityRose.Formats;
using UnityRose.Import;

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
            string filePath = stb_animation_list.Cells[int.Parse(stb_animation_type.Cells[(int)Action][(int)WeaponType])][(int)Gender];

            //if no female animation then use male one
            if (filePath == "")
                filePath = stb_animation_list.Cells[int.Parse(stb_animation_type.Cells[(int)Action][(int)WeaponType])][(int)GenderType.MALE];

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

        // NOTE: getZSC(GenderType, BodyPartType) and LoadPart(...) were removed here -
        // that was the runtime ZSC/ZMS/texture parsing pipeline for building a player
        // body part on the fly, now fully replaced by RosePlayer instantiating baked
        // prefabs via RoseAvatarDatabase. If something else in the codebase still calls
        // ResourceManager.Instance.getZSC(...) directly, let me know and I'll restore it
        // as a standalone read accessor (it can still safely return e.g. zsc_body_male
        // etc. for inspection) without reintroducing LoadPart's runtime-building logic.

        // NOTE: GenerateAnimationAsset(s)/LoadClips(...) were removed here too - that's
        // Editor-only baking (AssetDatabase.CreateAsset, PrefabUtility) and belongs in
        // ROSEEditorBaker, not in a class with no UnityEditor dependency. Let me know if
        // you want them ported over there instead of left out.
    }
}