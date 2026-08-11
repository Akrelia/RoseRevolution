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
    /// <summary>
    /// This is a resource manager for the editor only, so we don't reload constantly the STB / ZSC files.
    /// </summary>
    public class ResourceManager
    {
        /// <summary>
        /// Private instance.
        /// </summary>
        private static ResourceManager instance;

        /// <summary>
        /// Singleton.
        /// </summary>
        public static ResourceManager Instance => instance ??= new ResourceManager();

        public ZSC maleBodyZSC;
        public ZSC maleArmsZSC;
        public ZSC maleFootZSC;
        public ZSC maleFaceZSC;
        public ZSC maleHairZSC;
        public ZSC maleCapZSC;

        public ZSC femaleBodyZSC;
        public ZSC femaleArmsZSC;
        public ZSC femaleFootZSC;
        public ZSC femaleFaceZSC;
        public ZSC femaleHairZSC;
        public ZSC femaleCapZSC;

        public ZSC backZSC;
        public ZSC faceItemZSC;
        public ZSC weaponZSC;
        public ZSC subWeaponZSC;
        public ZSC patZSC;

        public ZMD maleZMD;
        public ZMD femaleZMD;

        public STB animationSTB;
        public STB animationTypeSTB;
        public STB weaponSTB;
        public STB subWeaponSTB;
        public STB capSTB;
        public STB armSTB;
        public STB armorSTB;
        public STB footSTB;
        public STB backSTB;
        public STB dropSTB;
        public STB faceItemSTB;
        public STB hairSTB;
        public STB npcSTB;
        public STB skySTB;
        public STB zoneSTB;
        public STB patSTB;

        public STL zoneSTL;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ResourceManager()
        {
            maleBodyZSC = LoadFile<ZSC>("3DData/Avatar/LIST_MBODY");
            maleArmsZSC = LoadFile<ZSC>("3DData/Avatar/LIST_MARMS");
            maleFootZSC = LoadFile<ZSC>("3DData/Avatar/LIST_MFOOT");
            maleFaceZSC = LoadFile<ZSC>("3DData/Avatar/LIST_MFACE");
            maleHairZSC = LoadFile<ZSC>("3DData/Avatar/LIST_MHAIR");
            maleCapZSC = LoadFile<ZSC>("3DData/Avatar/LIST_MCAP");

            femaleBodyZSC = LoadFile<ZSC>("3DData/Avatar/LIST_WBODY");
            femaleArmsZSC = LoadFile<ZSC>("3DData/Avatar/LIST_WARMS");
            femaleFootZSC = LoadFile<ZSC>("3DData/Avatar/LIST_WFOOT");
            femaleFaceZSC = LoadFile<ZSC>("3DData/Avatar/LIST_WFACE");
            femaleHairZSC = LoadFile<ZSC>("3DData/Avatar/LIST_WHAIR");
            femaleCapZSC = LoadFile<ZSC>("3DData/Avatar/LIST_WCAP");

            backZSC = LoadFile<ZSC>("3DData/Avatar/LIST_BACK");
            faceItemZSC = LoadFile<ZSC>("3DData/Avatar/LIST_FACEIEM");

            patZSC = LoadFile<ZSC>("3DData/PAT/LIST_PAT");

            weaponZSC = LoadFile<ZSC>("3DData/Weapon/LIST_WEAPON");
            subWeaponZSC = LoadFile<ZSC>("3DData/Weapon/LIST_SUBWPN");

            animationSTB = LoadFile<STB>("3DData/STB/FILE_MOTION");
            animationTypeSTB = LoadFile<STB>("3DData/STB/TYPE_MOTION");

            weaponSTB = LoadFile<STB>("3DData/STB/LIST_WEAPON");
            subWeaponSTB = LoadFile<STB>("3DData/STB/LIST_SUBWPN");
            capSTB = LoadFile<STB>("3DData/STB/LIST_CAP");
            armorSTB = LoadFile<STB>("3DData/STB/LIST_BODY");
            backSTB = LoadFile<STB>("3DData/STB/LIST_BACK");
            footSTB = LoadFile<STB>("3DData/STB/LIST_FOOT");
            armSTB = LoadFile<STB>("3DData/STB/LIST_ARMS");
            faceItemSTB = LoadFile<STB>("3DData/STB/LIST_FACEITEM");
            hairSTB = LoadFile<STB>("3DData/STB/LIST_HAIR");
            npcSTB = LoadFile<STB>("3DData/STB/LIST_NPC");
            dropSTB = LoadFile<STB>("3DData/STB/ITEM_DROP");
            zoneSTB = LoadFile<STB>("3DData/STB/LIST_ZONE");
            skySTB = LoadFile<STB>("3DData/STB/LIST_SKY");

            patSTB = LoadFile<STB>("3DData/STB/LIST_PAT");

            zoneSTL = LoadFile<STL>("3DData/STB/LIST_ZONE_S");
        }

        /// <summary>
        /// Load a ROSE Format file.
        /// </summary>
        /// <typeparam name="T">Rose Format file.</typeparam>
        /// <param name="path">Path.</param>
        /// <returns>Rose file.</returns>
        public T LoadFile<T>(string path) where T : class, IRoseFileFormat, new()
        {
            path = Path.Combine(RoseDataSource.DataPath, path);

            T resource = new T();

            resource.Load($"{path}.{resource.FormatName}");

            return resource;
        }

        /// <summary>
        /// Get Animation ZMO File path
        /// </summary>
        public string GetZMOPath(WeaponType WeaponType, ActionType Action, GenderType Gender)
        {
            int actionIdx = (int)Action;

            if (!WeaponAnimationColumn.TryGetValue(WeaponType, out int weaponIdx))
            {
                Debug.LogWarning($"GetZMOPath: no animation column mapping for {WeaponType}.");

                return "";
            }

            if (actionIdx < 0 || actionIdx >= animationTypeSTB.Cells.Count)
            {
                Debug.LogWarning($"GetZMOPath: ActionType {Action} ({actionIdx}) is out of range for stb_animation_type ({animationTypeSTB.Cells.Count} rows).");

                return "";
            }

            var row = animationTypeSTB.Cells[actionIdx];

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

            if (motionTypeIdx < 0 || motionTypeIdx >= animationSTB.Cells.Count)
            {
                Debug.LogWarning($"GetZMOPath: motion type index {motionTypeIdx} out of range for stb_animation_list ({animationSTB.Cells.Count} rows).");

                return "";
            }

            var animRow = animationSTB.Cells[motionTypeIdx];

            int genderIdx = (int)Gender;

            string filePath = (genderIdx >= 0 && genderIdx < animRow.Count) ? animRow[genderIdx] : "";

            if (filePath == "")
            {
                int maleIdx = (int)GenderType.MALE;

                filePath = (maleIdx >= 0 && maleIdx < animRow.Count) ? animRow[maleIdx] : "";
            }

            return filePath;
        }

        /// <summary>
        /// Get the weapon type from the weapon STB file based on the weapon ID.
        /// </summary>
        /// <param name="weaponID">Weapon ID.</param>
        /// <returns>Weapon Type.</returns>
        public WeaponType GetWeaponTypeFromSTB(int weaponID)
        {
            int typeID = 0;

            WeaponType type = WeaponType.EMPTY;

            try
            {
                typeID = int.Parse(weaponSTB.Cells[weaponID][5]);
                type = WeaponTypeLookUp[typeID];
            }

            catch (Exception ex)
            {
                Debug.Log(ex.Message);

                type = WeaponType.EMPTY;
            }

            return type;
        }

        /// <summary>
        /// Get the weapon animation column index based on the weapon type.
        /// </summary>
        private static readonly Dictionary<WeaponType, int> WeaponAnimationColumn = new()
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
        /// Get the weapon type from the weapon STB file based on the weapon type ID.
        /// </summary>
        public static Dictionary<int, WeaponType> WeaponTypeLookUp = new Dictionary<int, WeaponType>() {
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

    }
}