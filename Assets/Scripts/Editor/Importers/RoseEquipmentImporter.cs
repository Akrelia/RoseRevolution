using System;
using UnityEditor;
using UnityEngine;
using UnityRose.Formats;
using RevolutionShared.Rose.Data;
using RevolutionShared.Rose.Data.Equipment;
using System.Collections.Generic;
using UnityRose.Import;

namespace UnityRose.ImportEditor
{
    /// <summary>
    /// Rose Equipment Importer.
    /// </summary>
    public class RoseEquipmentImporter
    {
        private static readonly GameDataPaths.EquipmentImportContext context = new GameDataPaths.EquipmentImportContext();

        /// <summary>
        /// Creates a database asset of type T with the specified name if it doesn't already exist, and returns the instance of the database.
        /// </summary>
        /// <typeparam name="T">Type of database.</typeparam>
        /// <param name="name">Name.</param>
        /// <returns>Database.</returns>
        private static T CreateSubDatabase<T>(string name) where T : ScriptableObject
        {
            string path = $"{GameDataPaths.Database.Root}/{name}.asset";

            var database = AssetDatabase.LoadAssetAtPath<T>(path);

            if (database == null)
            {
                database = ScriptableObject.CreateInstance<T>();

                AssetDatabase.CreateAsset(database, path);
            }

            return database;
        }

        /// <summary>
        /// Import all equipment.
        /// </summary>
        public static void ImportAllEquipment(int maxSlots)
        {
            string dbFolder = "Assets/GameData/Databases";

            if (!AssetDatabase.IsValidFolder(dbFolder))
                EditorUtils.EnsureFolder(dbFolder);

            string dbPath = $"{dbFolder}/EquipmentDatabase.asset";

            var database = AssetDatabase.LoadAssetAtPath<EquipmentDatabase>(dbPath);

            if (database == null)
            {
                database = ScriptableObject.CreateInstance<EquipmentDatabase>();
                AssetDatabase.CreateAsset(database, dbPath);
            }

            if (database.weaponDatabase == null)
                database.weaponDatabase = CreateSubDatabase<WeaponDatabase>("WeaponDatabase");

            if (database.subWeaponDatabase == null)
                database.subWeaponDatabase = CreateSubDatabase<ArmorDatabase>("SubWeaponDatabase");

            if (database.bodyDatabase == null)
                database.bodyDatabase = CreateSubDatabase<ArmorDatabase>("BodyDatabase");

            if (database.armDatabase == null)
                database.armDatabase = CreateSubDatabase<ArmorDatabase>("ArmDatabase");

            if (database.backDatabase == null)
                database.backDatabase = CreateSubDatabase<ArmorDatabase>("BackDatabase");

            if (database.headgearDatabase == null)
                database.headgearDatabase = CreateSubDatabase<HeadgearDatabase>("HeadgearDatabase");

            if (database.footwearDatabase == null)
                database.footwearDatabase = CreateSubDatabase<FootwearDatabase>("FootwearDatabase");

            if (database.faceItemDatabase == null)
                database.faceItemDatabase = CreateSubDatabase<ArmorDatabase>("FaceItemDatabase");

            if (database.appearenceDatabase == null)
                database.appearenceDatabase = CreateSubDatabase<AppearenceDatabase>("AppearenceDatabase");

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();

            database.bodyDatabase.entries.Clear();
            database.armDatabase.entries.Clear();
            database.backDatabase.entries.Clear();
            database.weaponDatabase.entries.Clear();
            database.headgearDatabase.entries.Clear();
            database.footwearDatabase.entries.Clear();

            database.appearenceDatabase.faces.Clear();
            database.appearenceDatabase.hairs.Clear();


            var rm = ResourceManager.Instance;
            var baker = new RoseEquipmentImporter();

            //        AssetDatabase.StartAssetEditing();

            try
            {
                CreateSlot<ArmorData, ArmorDataImporter>(baker, database.bodyDatabase, "Body_M", rm.maleBodyZSC, "3DDATA/AVATAR/BODY/BODY_M.ZSC", BodyPartType.BODY, GenderType.MALE, maxSlots, ResourceManager.Instance.armorSTB);
                CreateSlot<ArmorData, ArmorDataImporter>(baker, database.bodyDatabase, "Body_F", rm.femaleBodyZSC, "3DDATA/AVATAR/BODY/BODY_F.ZSC", BodyPartType.BODY, GenderType.FEMALE, maxSlots, ResourceManager.Instance.armorSTB);

                CreateSlot<ArmorData, ArmorDataImporter>(baker, database.armDatabase, "Arms_M", rm.maleArmsZSC, "3DDATA/AVATAR/ARMS/ARMS_M.ZSC", BodyPartType.ARMS, GenderType.MALE, maxSlots, ResourceManager.Instance.armSTB);
                CreateSlot<ArmorData, ArmorDataImporter>(baker, database.armDatabase, "Arms_F", rm.femaleArmsZSC, "3DDATA/AVATAR/ARMS/ARMS_F.ZSC", BodyPartType.ARMS, GenderType.FEMALE, maxSlots, ResourceManager.Instance.armSTB);

                CreateSlot<FootwearData, FootwearDataImporter>(baker, database.footwearDatabase, "Foot_M", rm.maleFootZSC, "3DDATA/AVATAR/FOOT/FOOT_M.ZSC", BodyPartType.FOOT, GenderType.MALE, maxSlots, ResourceManager.Instance.footSTB);
                CreateSlot<FootwearData, FootwearDataImporter>(baker, database.footwearDatabase, "Foot_F", rm.femaleFootZSC, "3DDATA/AVATAR/FOOT/FOOT_F.ZSC", BodyPartType.FOOT, GenderType.FEMALE, maxSlots, ResourceManager.Instance.footSTB);

                CreateAppearenceSlot(baker, database.appearenceDatabase.faces, "Face_M", rm.maleFaceZSC, "3DDATA/AVATAR/FACE/FACE_M.ZSC", BodyPartType.FACE, GenderType.MALE, maxSlots);
                CreateAppearenceSlot(baker, database.appearenceDatabase.faces, "Face_F", rm.femaleFaceZSC, "3DDATA/AVATAR/FACE/FACE_F.ZSC", BodyPartType.FACE, GenderType.FEMALE, maxSlots);

                CreateAppearenceSlot(baker, database.appearenceDatabase.hairs, "Hair_M", rm.maleHairZSC, "3DDATA/AVATAR/HAIR/HAIR_M.ZSC", BodyPartType.HAIR, GenderType.MALE, maxSlots);
                CreateAppearenceSlot(baker, database.appearenceDatabase.hairs, "Hair_F", rm.femaleHairZSC, "3DDATA/AVATAR/HAIR/HAIR_F.ZSC", BodyPartType.HAIR, GenderType.FEMALE, maxSlots);

                CreateSlot<HeadgearData, HeadgearDataImporter>(baker, database.headgearDatabase, "Cap_M", rm.maleCapZSC, "3DDATA/AVATAR/CAP/CAP_M.ZSC", BodyPartType.CAP, GenderType.MALE, maxSlots, ResourceManager.Instance.capSTB);
                CreateSlot<HeadgearData, HeadgearDataImporter>(baker, database.headgearDatabase, "Cap_F", rm.femaleCapZSC, "3DDATA/AVATAR/CAP/CAP_F.ZSC", BodyPartType.CAP, GenderType.FEMALE, maxSlots, ResourceManager.Instance.capSTB);

                CreateSlot<WeaponData, WeaponDataImporter>(baker, database.weaponDatabase, "Weapon", rm.weaponZSC, "3DDATA/WEAPON/LIST_WEAPON.ZSC", BodyPartType.WEAPON, GenderType.NONE, maxSlots, ResourceManager.Instance.weaponSTB);

                CreateSlot<ArmorData, ArmorDataImporter>(baker, database.subWeaponDatabase, "Sub_Weapon", rm.subWeaponZSC, "3DDATA/WEAPON/LIST_SUBWPN.ZSC", BodyPartType.SUBWEAPON, GenderType.NONE, maxSlots, ResourceManager.Instance.subWeaponSTB);

                CreateSlot<ArmorData, ArmorDataImporter>(baker, database.faceItemDatabase, "FaceItem", rm.faceItemZSC, "3DDATA/AVATAR/FACEITEM/FACEITEM.ZSC", BodyPartType.FACEITEM, GenderType.NONE, maxSlots, ResourceManager.Instance.faceItemSTB);

                CreateSlot<ArmorData, ArmorDataImporter>(baker, database.backDatabase, "Back", rm.backZSC, "3DDATA/AVATAR/BACK/BACK.ZSC", BodyPartType.BACK, GenderType.NONE, maxSlots, ResourceManager.Instance.backSTB);

                //   BakeSlot<ArmorData, PATDataImporter>(baker, database.backDatabase, "PAT", rm.patZSC, "3DDATA/PAT/PAT.ZSC", BodyPartType.BODY, GenderType.NONE, maxIdsPerSlot, ResourceManager.Instance.patSTB);

            }

            catch (Exception ex)
            {
                Debug.LogError("Error while importing base equipment: " + ex.Message + "\n" + ex.StackTrace);
            }

            finally
            {
                //       AssetDatabase.StopAssetEditing();
            }


            EditorUtility.SetDirty(database.weaponDatabase);
            EditorUtility.SetDirty(database.subWeaponDatabase);
            EditorUtility.SetDirty(database.bodyDatabase);
            EditorUtility.SetDirty(database.armDatabase);
            EditorUtility.SetDirty(database.backDatabase);
            EditorUtility.SetDirty(database.headgearDatabase);
            EditorUtility.SetDirty(database.footwearDatabase);
            EditorUtility.SetDirty(database.faceItemDatabase);
            EditorUtility.SetDirty(database.appearenceDatabase);

            EditorUtility.SetDirty(database);

            AssetDatabase.SaveAssets();

            RegisterEquipmentDatabaseAddressables(database, dbPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Base equipment import done ({maxSlots} ids/slot max");
        }

        /// <summary>
        /// Bake a slot of equipment.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <param name="baker"></param>
        /// <param name="database"></param>
        /// <param name="namePrefix"></param>
        /// <param name="zsc"></param>
        /// <param name="zscPath"></param>
        /// <param name="bodyPart"></param>
        /// <param name="gender"></param>
        /// <param name="maxIds"></param>
        /// <param name="stb"></param>
        private static void CreateSlot<T, U>(RoseEquipmentImporter baker, ItemDatabase<T> database, string namePrefix, ZSC zsc, string zscPath, BodyPartType bodyPart, GenderType gender, int maxIds, STB stb) where T : EquipmentData where U : EquipmentDataImporter<T>, new()
        {
            if (zsc == null)
            {
                Debug.LogWarning($"BakeSlot: ZSC null for {namePrefix}");

                return;
            }

            var importer = new U();

            int baked = 0;
            int id = 0;

            while (baked < maxIds && id < zsc.Objects.Count && id < stb.Cells.Count)
            {
                var name = stb.Cells[id][1];

                if (EditorUtils.IsInvalidSTBEntry(name, bodyPart, id))
                {
                    id++;
                    continue;
                }

                GameObject prefab;

                try
                {
                    prefab = CreateEquipment($"{namePrefix}_{id}", bodyPart, zsc, zscPath, id);
                }

                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed baking {namePrefix}_{id}: {ex.Message}");

                    id++;
                    continue;
                }

                if (prefab == null)
                {
                    id++;
                    continue;
                }

                database.entries.Add(new ItemDatabaseEntry<T>
                {
                    id = id,
                    prefab = prefab,
                    gender = gender,
                    item = importer.Import(id, stb)
                });

                baked++;
                id++;
            }

            Debug.Log($"{namePrefix}: baked {baked}/{maxIds} items");
        }

        /// <summary>
        /// Bake an appearance slot (face, hair, etc).
        /// </summary>
        /// <param name="baker"></param>
        /// <param name="target"></param>
        /// <param name="namePrefix"></param>
        /// <param name="zsc"></param>
        /// <param name="zscPath"></param>
        /// <param name="bodyPart"></param>
        /// <param name="gender"></param>
        /// <param name="maxIds"></param>
        private static void CreateAppearenceSlot(RoseEquipmentImporter baker, List<AppearenceEntry> target, string namePrefix, ZSC zsc, string zscPath, BodyPartType bodyPart, GenderType gender, int maxIds)
        {
            if (zsc == null)
            {
                Debug.LogWarning($"BakeAppearenceSlot: ZSC is null for {namePrefix} ({bodyPart}), skipping.");
                return;
            }

            int count = Mathf.Min(maxIds, zsc.Objects.Count);

            for (int id = 0; id < count; id++)
            {
                GameObject prefab;

                try
                {
                    prefab = CreateEquipment($"{namePrefix}_{id}", bodyPart, zsc, zscPath, id);
                }

                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to bake {namePrefix}_{id}: {ex.Message}");

                    continue;
                }

                if (prefab == null)
                    continue;

                target.Add(new AppearenceEntry
                {
                    id = id,
                    gender = gender,
                    prefab = prefab
                });
            }
        }

        /// <summary>
        /// Register the equipment database and its subdatabases as addressables.
        /// </summary>
        /// <param name="database"></param>
        /// <param name="path"></param>
        private static void RegisterEquipmentDatabaseAddressables(EquipmentDatabase database, string path)
        {
            EditorUtils.EnsureAddressable(path, nameof(EquipmentDatabase));

            var subDatabases = new ScriptableObject[]
            {
        database.weaponDatabase,
        database.subWeaponDatabase,
        database.bodyDatabase,
        database.armDatabase,
        database.backDatabase,
        database.headgearDatabase,
        database.footwearDatabase,
        database.faceItemDatabase,
        database.appearenceDatabase
            };

            foreach (var subDatabase in subDatabases)
            {
                if (subDatabase == null)
                    continue;

                var subPath = AssetDatabase.GetAssetPath(subDatabase);

                if (string.IsNullOrEmpty(subPath))
                {
                    Debug.LogWarning($"Cannot find asset path for {subDatabase.name}");
                    continue;
                }

                EditorUtils.EnsureAddressable(subPath, subDatabase.GetType().Name);
            }
        }

        /// <summary>
        /// Bake equipment for a specific ID in the ZSC file.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="bodyPart"></param>
        /// <param name="zsc"></param>
        /// <param name="zscPath"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static GameObject CreateEquipment(string name, BodyPartType bodyPart, ZSC zsc, string zscPath, int id)
        {
            if (id < 0 || id >= zsc.Objects.Count)
                return null;

            var zscObject = zsc.Objects[id];

            if (zscObject.Models == null || zscObject.Models.Count == 0)
                return null;

            GameObject root = new GameObject(name);

            int builtParts = 0;

            try
            {

                foreach (var model in zscObject.Models)
                {
                    if (BuildPart(root.transform, bodyPart, model.DummyIndex, model.ModelID, model.TextureID, zsc, zscPath))
                        builtParts++;
                }

                if (builtParts == 0)
                {
                    GameObject.DestroyImmediate(root);

                    return null;
                }

                var prefab = SavePrefab(root, name);

                GameObject.DestroyImmediate(root);

                return prefab;
            }

            catch (Exception ex)
            {
                Debug.Log("Error while baking equipment: " + ex.Message);

                if (root)
                {
                    GameObject.DestroyImmediate(root);
                }

                return null;
            }
        }

        /// <summary>
        /// Build a part of the equipment.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="bodyPart"></param>
        /// <param name="dummy"></param>
        /// <param name="modelID"></param>
        /// <param name="textureID"></param>
        /// <param name="zsc"></param>
        /// <param name="zscPath"></param>
        /// <returns></returns>
        private static bool BuildPart(Transform parent, BodyPartType bodyPart, ZSC.DummyType dummy, int modelID, int textureID, ZSC zsc, string zscPath)
        {
            if (modelID < 0 || modelID >= zsc.Models.Count)
                return false;

            string zmsPath = zsc.Models[modelID];

            var mesh = ROSEEditorBaker.ImportMesh(zmsPath, context);

            if (mesh == null || mesh.vertexCount == 0)
                return false;

            var material = ROSEEditorBaker.ImportEquipmentMaterial(textureID, zsc, zscPath, context);

            if (material == null)
                return false;


            GameObject obj = new GameObject(bodyPart.ToString());
            obj.transform.SetParent(parent, false);


            var attachment = obj.AddComponent<RoseAttachment>();
            //   attachment.dummy = dummy;

            if (mesh.boneWeights != null && mesh.boneWeights.Length > 0)
            {
                var renderer = obj.AddComponent<SkinnedMeshRenderer>();

                renderer.sharedMesh = mesh;
                renderer.sharedMaterial = material;

                renderer.updateWhenOffscreen = true;
            }
            else
            {
                var filter = obj.AddComponent<MeshFilter>();
                filter.sharedMesh = mesh;

                var renderer = obj.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
            }


            return true;
        }

        /// <summary>
        /// Save the prefab.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private static GameObject SavePrefab(GameObject obj, string name)
        {
            string path = $"{context.Prefab}/{name}.prefab";

            EditorUtils.EnsureFolder(path);

            return PrefabUtility.SaveAsPrefabAsset(obj, path);
        }
    }

    /// <summary>
    /// Base class for importing equipment data from STB files.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class EquipmentDataImporter<T> where T : EquipmentData
    {
        public T Import(int id, STB stb)
        {
            var data = CreateInstance();

            ReadBaseFields(data, stb, id);
            ReadFields(data, stb, id);

            return data;
        }

        protected abstract T CreateInstance();

        protected virtual void ReadBaseFields(T data, STB stb, int id)
        {
            data.id = id;
            data.name = stb.Cells[id][1];
            data.price = EditorUtils.ParseSTBInt(stb.Cells[id][6]);
            data.iconID = (short)EditorUtils.ParseSTBInt(stb.Cells[id][10]);
        }

        protected abstract void ReadFields(T data, STB stb, int id);
    }

    /// <summary>
    /// Importer for PAT data.
    /// </summary>
    public class PATDataImporter : EquipmentDataImporter<ArmorData>
    {
        protected override ArmorData CreateInstance() => new ArmorData();

        protected override void ReadFields(ArmorData data, STB stb, int id) => ReadArmorFields(data, stb, id);

        public static void ReadArmorFields(ArmorData data, STB stb, int id)
        {
        }
    }

    /// <summary>
    /// Importer for armor data.
    /// </summary>
    public class ArmorDataImporter : EquipmentDataImporter<ArmorData>
    {
        protected override ArmorData CreateInstance() => new ArmorData();

        protected override void ReadFields(ArmorData data, STB stb, int id) => ReadArmorFields(data, stb, id);

        public static void ReadArmorFields(ArmorData data, STB stb, int id)
        {
        }
    }

    /// <summary>
    /// Importer for footwear data.
    /// </summary>
    public class FootwearDataImporter : EquipmentDataImporter<FootwearData>
    {
        protected override FootwearData CreateInstance() => new FootwearData();

        protected override void ReadFields(FootwearData data, STB stb, int id)
        {
            ArmorDataImporter.ReadArmorFields(data, stb, id);
        }
    }

    /// <summary>
    /// Importer for weapon data.
    /// </summary>
    public class WeaponDataImporter : EquipmentDataImporter<WeaponData>
    {
        protected override WeaponData CreateInstance() => new WeaponData();

        protected override void ReadFields(WeaponData data, STB stb, int id)
        {
            data.weaponType = (WeaponType)EditorUtils.ParseSTBInt(stb.Cells[id][5]);
        }
    }

    /// <summary>
    /// Importer for headgear data.
    /// </summary>
    public class HeadgearDataImporter : EquipmentDataImporter<HeadgearData>
    {
        protected override HeadgearData CreateInstance() => new HeadgearData();

        protected override void ReadFields(HeadgearData data, STB stb, int id)
        {
            ArmorDataImporter.ReadArmorFields(data, stb, id);

            data.hair = (byte)EditorUtils.ParseSTBInt(stb.Cells[id][34]);
        }
    }
}