using System;
using UnityEditor;
using UnityEngine;
using UnityRose.Formats;
using RevolutionShared.Rose.Data;
using System.Collections.Generic;
using UnityRose.Import;
using System.Linq;
using System.Reflection;
using System.Globalization;
using RevolutionShared.Rose.Data.Items.Equipment;
using RevolutionShared.Rose.Data.Items;

namespace UnityRose.ImportEditor
{
    /// <summary>
    /// Rose Equipment Importer.
    /// </summary>
    public class RoseEquipmentImporter
    {
        private static readonly GameDataPaths.EquipmentImportContext context = new GameDataPaths.EquipmentImportContext();

        private static T CreateOrGetDatabase<T>() where T : ScriptableObject
        {
            return CreateOrGetDatabase<T>(typeof(T).Name);
        }

        /// <summary>
        /// Creates a database asset of type T with the specified name if it doesn't already exist, and returns the instance of the database.
        /// </summary>
        /// <typeparam name="T">Type of database.</typeparam>
        /// <param name="name">Name.</param>
        /// <returns>Database.</returns>
        private static T CreateOrGetDatabase<T>(string name) where T : ScriptableObject
        {
            string path = $"{GameDataPaths.Database.Root}/{name}.asset";

            EditorUtils.EnsureFolder(path);

            var database = AssetDatabase.LoadAssetAtPath<T>(path);

            if (database == null)
            {
                database = ScriptableObject.CreateInstance<T>();

                AssetDatabase.CreateAsset(database, path);
            }

            EditorUtils.EnsureAddressable(path, name);

            return database;
        }

        public static void ImportEquipment(int itemID, BodyPartType bodyPart, Shader entityShader)
        {
            var database = LoadMasterDatabase();

            ImportEquipment(new[] { itemID }, bodyPart, entityShader, database);

            AssetDatabase.SaveAssets();
        }

        private static void ImportEquipment(IEnumerable<int> itemIDs, BodyPartType bodyPart, Shader entityShader, EquipmentDatabase database)
        {
            var subDatabase = database.Databases[bodyPart];
            var stb = ResourceManager.Instance.stbEquipmentFiles[bodyPart];

            var zscFiles = ResourceManager.Instance.zscFiles.Where(x => x.Key.part == bodyPart).Select(x => (x.Key.gender, x.Value)).ToList();

            foreach (var itemID in itemIDs)
            {
                try
                {
                    var data = STBImporter.Read(bodyPart, stb, itemID);

                    foreach (var zscFile in zscFiles)
                    {
                        var zsc = zscFile.Value;
                        string namePrefix = zsc.fileName.Replace("LIST_", "").Replace(".zsc", "");

                        var prefab = CreateEquipment($"{namePrefix}_{itemID}", bodyPart, zsc, itemID, entityShader);

                        if (prefab == null)
                        {
                            continue;
                        }

                        var existing = subDatabase.Get(itemID, zscFile.gender);

                        if (existing != null)
                        {
                            subDatabase.Remove(itemID);
                        }

                        subDatabase.Add(itemID, zscFile.gender, prefab, (EquipmentData)data); // TODO : remove data, it is useless now
                    }
                }

                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed baking {bodyPart}_{itemID}: {ex.Message}");
                }
            }

          //  EditorUtility.SetDirty(subDatabase.Asset);
        }

        /// <summary>
        /// Import all equipment.
        /// </summary>
        /// <param name="maxSlots">Max slots.</param>
        /// <param name="shader">Shader.</param>
        public static void ImportAllEquipment(int maxSlots, Shader shader)
        {
            var database = LoadMasterDatabase();

            foreach (var bodyPart in new[]
            {
                BodyPartType.BODY,
                BodyPartType.ARMS,
                BodyPartType.BACK,
                BodyPartType.WEAPON,
                BodyPartType.CAP,
                BodyPartType.FOOT,
                BodyPartType.SUBWEAPON,
                BodyPartType.FACEITEM
                })
            {
                var stb = ResourceManager.Instance.stbEquipmentFiles[bodyPart];
                var validItems = new List<int>();

                for (int i = 0; i < stb.Cells.Count && validItems.Count < maxSlots; i++)
                {
                    if (stb.Cells[i].Count <= 10 || string.IsNullOrEmpty(stb.Cells[i][10]) || string.IsNullOrEmpty(stb.Cells[i][2]))
                    {
                        continue;
                    }

                    validItems.Add(i);
                }

                ImportEquipment(validItems, bodyPart, shader, database);
            }

            SaveDatabase(database);

            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Load the master database.
        /// </summary>
        /// <returns></returns>
        public static EquipmentDatabase LoadMasterDatabase()
        {
            var database = CreateOrGetDatabase<EquipmentDatabase>();

            database.weaponDatabase = CreateOrGetDatabase<WeaponDatabase>();
            database.subWeaponDatabase = CreateOrGetDatabase<ArmorDatabase>("SubweaponDatabase");
            database.bodyDatabase = CreateOrGetDatabase<ArmorDatabase>("BodyDatabase");
            database.armDatabase = CreateOrGetDatabase<ArmorDatabase>("ArmDatabase");
            database.backDatabase = CreateOrGetDatabase<ArmorDatabase>("BackDatabase");
            database.headgearDatabase = CreateOrGetDatabase<HeadgearDatabase>();
            database.footwearDatabase = CreateOrGetDatabase<FootwearDatabase>();
            database.faceItemDatabase = CreateOrGetDatabase<ArmorDatabase>("FaceItemDatabase");
            database.appearenceDatabase = CreateOrGetDatabase<AppearenceDatabase>();

            return database;
        }

        private static void SaveDatabase(EquipmentDatabase database)
        {
            EditorUtility.SetDirty(database);
            EditorUtility.SetDirty(database.weaponDatabase);
            EditorUtility.SetDirty(database.subWeaponDatabase);
            EditorUtility.SetDirty(database.bodyDatabase);
            EditorUtility.SetDirty(database.armDatabase);
            EditorUtility.SetDirty(database.backDatabase);
            EditorUtility.SetDirty(database.headgearDatabase);
            EditorUtility.SetDirty(database.footwearDatabase);
            EditorUtility.SetDirty(database.faceItemDatabase);
            EditorUtility.SetDirty(database.appearenceDatabase);

            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Import all appearences.
        /// </summary>
        /// <param name="shader">Shader.</param>
        public static void ImportAllAppearences(Shader shader)
        {
            var rm = ResourceManager.Instance;

            var database = LoadMasterDatabase();

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();

            database.appearenceDatabase.faces.Clear();
            database.appearenceDatabase.hairs.Clear();

            CreateAppearenceSlot(database.appearenceDatabase.faces, "Face_M", rm.maleFaceZSC, BodyPartType.FACE, GenderType.MALE, shader);
            CreateAppearenceSlot(database.appearenceDatabase.faces, "Face_F", rm.femaleFaceZSC, BodyPartType.FACE, GenderType.FEMALE, shader);
            CreateAppearenceSlot(database.appearenceDatabase.hairs, "Hair_M", rm.maleHairZSC, BodyPartType.HAIR, GenderType.MALE, shader);
            CreateAppearenceSlot(database.appearenceDatabase.hairs, "Hair_F", rm.femaleHairZSC, BodyPartType.HAIR, GenderType.FEMALE, shader);

            EditorUtility.SetDirty(database.appearenceDatabase);
            EditorUtility.SetDirty(database);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Appearence imported");
        }

        /// <summary>
        /// Import all equipment.
        /// </summary>
        public static void OldImportAllEquipment(int maxSlots, Shader shader)
        {
            // This is unused now and shall be removed when confirmed there is no problem

            var database = LoadMasterDatabase();

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();

            database.bodyDatabase.entries.Clear();
            database.armDatabase.entries.Clear();
            database.backDatabase.entries.Clear();
            database.weaponDatabase.entries.Clear();
            database.headgearDatabase.entries.Clear();
            database.footwearDatabase.entries.Clear();

            var rm = ResourceManager.Instance;

            try
            {
                // TURN THIS INTO A SIMPLE LOOP NEXT TIME

                //CreateSlot<ArmorData, ArmorDataImporter>(database.bodyDatabase, "Body_M", rm.maleBodyZSC, BodyPartType.BODY, GenderType.MALE, maxSlots, ResourceManager.Instance.armorSTB, shader);
                //CreateSlot<ArmorData, ArmorDataImporter>(database.bodyDatabase, "Body_F", rm.femaleBodyZSC, BodyPartType.BODY, GenderType.FEMALE, maxSlots, ResourceManager.Instance.armorSTB, shader);

                //CreateSlot<ArmorData, ArmorDataImporter>(database.armDatabase, "Arms_M", rm.maleArmsZSC, BodyPartType.ARMS, GenderType.MALE, maxSlots, ResourceManager.Instance.armSTB, shader);
                //CreateSlot<ArmorData, ArmorDataImporter>(database.armDatabase, "Arms_F", rm.femaleArmsZSC, BodyPartType.ARMS, GenderType.FEMALE, maxSlots, ResourceManager.Instance.armSTB, shader);

                //CreateSlot<FootwearData, FootwearDataImporter>(database.footwearDatabase, "Foot_M", rm.maleFootZSC, BodyPartType.FOOT, GenderType.MALE, maxSlots, ResourceManager.Instance.footSTB, shader);
                //CreateSlot<FootwearData, FootwearDataImporter>(database.footwearDatabase, "Foot_F", rm.femaleFootZSC, BodyPartType.FOOT, GenderType.FEMALE, maxSlots, ResourceManager.Instance.footSTB, shader);

                //CreateSlot<HeadgearData, HeadgearDataImporter>(database.headgearDatabase, "Cap_M", rm.maleCapZSC, BodyPartType.CAP, GenderType.MALE, maxSlots, ResourceManager.Instance.capSTB, shader);
                //CreateSlot<HeadgearData, HeadgearDataImporter>(database.headgearDatabase, "Cap_F", rm.femaleCapZSC, BodyPartType.CAP, GenderType.FEMALE, maxSlots, ResourceManager.Instance.capSTB, shader);

                //CreateSlot<WeaponData, WeaponDataImporter>(database.weaponDatabase, "Weapon", rm.weaponZSC, BodyPartType.WEAPON, GenderType.NONE, maxSlots, ResourceManager.Instance.weaponSTB, shader);
                //CreateSlot<ArmorData, ArmorDataImporter>(database.subWeaponDatabase, "Sub_Weapon", rm.subWeaponZSC, BodyPartType.SUBWEAPON, GenderType.NONE, maxSlots, ResourceManager.Instance.subWeaponSTB, shader);
                //CreateSlot<ArmorData, ArmorDataImporter>(database.faceItemDatabase, "FaceItem", rm.faceItemZSC, BodyPartType.FACEITEM, GenderType.NONE, maxSlots, ResourceManager.Instance.faceItemSTB, shader);
                //CreateSlot<ArmorData, ArmorDataImporter>(database.backDatabase, "Back", rm.backZSC, BodyPartType.BACK, GenderType.NONE, maxSlots, ResourceManager.Instance.backSTB, shader);


            }

            catch (Exception ex)
            {
                Debug.LogError("Error while importing base equipment: " + ex.Message + "\n" + ex.StackTrace);
            }

            EditorUtility.SetDirty(database.weaponDatabase);
            EditorUtility.SetDirty(database.subWeaponDatabase);
            EditorUtility.SetDirty(database.bodyDatabase);
            EditorUtility.SetDirty(database.armDatabase);
            EditorUtility.SetDirty(database.backDatabase);
            EditorUtility.SetDirty(database.headgearDatabase);
            EditorUtility.SetDirty(database.footwearDatabase);
            EditorUtility.SetDirty(database.faceItemDatabase);

            EditorUtility.SetDirty(database);

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
        private static void CreateSlot<T, U>(PartDatabase<T> database, string namePrefix, ZSC zsc, BodyPartType bodyPart, GenderType gender, int maxIds, STB stb, Shader shader) where T : EquipmentData
        {
            //if (zsc == null)
            //{
            //    Debug.LogWarning($"BakeSlot: ZSC null for {namePrefix}");

            //    return;
            //}

            //var importer = new U();

            //int baked = 0;
            //int id = 0;

            //while (baked < maxIds && id < zsc.Objects.Count && id < stb.Cells.Count)
            //{
            //    var name = stb.Cells[id][1];

            //    if (EditorUtils.IsInvalidSTBEntry(name, bodyPart, id))
            //    {
            //        id++;

            //        continue;
            //    }

            //    GameObject prefab;

            //    try
            //    {
            //        prefab = CreateEquipment($"{namePrefix}_{id}", bodyPart, zsc, id, shader);
            //    }

            //    catch (Exception ex)
            //    {
            //        Debug.LogWarning($"Failed baking {namePrefix}_{id}: {ex.Message}");

            //        id++;

            //        continue;
            //    }

            //    if (prefab == null)
            //    {
            //        id++;

            //        continue;
            //    }

            //    database.entries.Add(new ItemDatabaseEntry<T>
            //    {
            //        id = id,
            //        prefab = prefab,
            //        gender = gender,
            //        item = importer.Import(id, stb)
            //    });

            //    baked++;
            //    id++;
            //}
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
        private static void CreateAppearenceSlot(List<AppearenceEntry> target, string namePrefix, ZSC zsc, BodyPartType bodyPart, GenderType gender, Shader shader)
        {
            if (zsc == null)
            {
                Debug.LogWarning($"BakeAppearenceSlot: ZSC is null for {namePrefix} ({bodyPart}), skipping.");

                return;
            }

            int count = zsc.Objects.Count;

            for (int id = 0; id < count; id++)
            {
                GameObject prefab;

                try
                {
                    prefab = CreateEquipment($"{namePrefix}_{id}", bodyPart, zsc, id, shader);
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
        /// Bake equipment for a specific ID in the ZSC file.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="bodyPart"></param>
        /// <param name="zsc"></param>
        /// <param name="zscPath"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static GameObject CreateEquipment(string name, BodyPartType bodyPart, ZSC zsc, int id, Shader shader)
        {
            if (id < 0 || id >= zsc.Objects.Count)
                return null;

            var zscObject = zsc.Objects[id];

            if (zscObject.Models == null || zscObject.Models.Count == 0)
                return null;

            GameObject root = new GameObject(name);

            for (int i = 0; i < zscObject.Effects.Count;i++)
            {
                GameObject obj = new GameObject("Effect " + i);

                obj.transform.SetParent(root.transform);

                obj.transform.position = zscObject.Effects[i].Position;
            }

            int builtParts = 0;

            try
            {
                for (int i = 0; i < (bodyPart != BodyPartType.HAIR ? zscObject.Models.Count : 1); i++) // Akima : this is a fire because Male Hair 10 have an extra ugly mesh. So we only import one mesh, whatever
                {
                    var model = zscObject.Models[i];

                    if (BuildPart(root.transform, bodyPart, model.DummyIndex, model.ModelID, model.TextureID, zsc, shader, i))
                    {
                        builtParts++;
                    }
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
        private static bool BuildPart(Transform parent, BodyPartType bodyPart, ZSC.DummyType dummy, int modelID, int textureID, ZSC zsc, Shader shader, int partIndex = 0)
        {
            if (modelID < 0 || modelID >= zsc.Models.Count)
                return false;

            string zmsPath = zsc.Models[modelID];

            var mesh = ROSEEditorBaker.ImportMesh(zmsPath, context, partIndex);

            if (mesh == null || mesh.vertexCount == 0)
                return false;

            var material = ROSEEditorBaker.ImportEquipmentMaterial(textureID, zsc, shader, context);

            if (material == null)
                return false;


            GameObject obj = new GameObject(bodyPart.ToString());
            obj.transform.SetParent(parent, false);


            if (bodyPart == BodyPartType.WEAPON)
            {
                var attachment = obj.AddComponent<RoseAttach>();
                attachment.dummy = (RoseAttachPoint)dummy;
            }

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

    public static class STBImporter
    {
        private static readonly Dictionary<Type, Action<ItemData, STB, int>> readers = BuildReaders();
        private static readonly Dictionary<BodyPartType, Func<STB, int, ItemData>> dataReaders = BuildDataReaders();

        public static ItemData Read(BodyPartType part, STB stb, int id)
        {
            if (!dataReaders.TryGetValue(part, out var reader))
            {
                throw new InvalidOperationException($"No STB reader registered for {part}.");
            }

            return reader(stb, id);
        }

        public static T Read<T>(STB stb, int id) where T : ItemData, new()
        {
            var data = new T();

            ReadBaseFields(data, stb, id);

            if (readers.TryGetValue(typeof(T), out var reader))
            {
                reader(data, stb, id);
            }

            return data;
        }

        private static void ReadBaseFields(ItemData data, STB stb, int id)
        {
            data.id = id;
            data.name = ReadString(stb, id, 1);
            data.price = ReadInt(stb, id, 6);
            data.iconID = ReadShort(stb, id, 10);
        }

        private static Dictionary<Type, Action<ItemData, STB, int>> BuildReaders()
        {
            var result = new Dictionary<Type, Action<ItemData, STB, int>>();

            var methods = typeof(STBImporter)
                .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                .Select(x => new
                {
                    Method = x,
                    Attribute = x.GetCustomAttribute<STBReaderAttribute>()
                })
                .Where(x => x.Attribute != null);

            foreach (var entry in methods)
            {
                var method = entry.Method;
                var dataType = entry.Attribute.DataType;
                var parameters = method.GetParameters();

                if (method.ReturnType != typeof(void) ||
                    parameters.Length != 3 ||
                    parameters[0].ParameterType != dataType ||
                    parameters[1].ParameterType != typeof(STB) ||
                    parameters[2].ParameterType != typeof(int))
                {
                    throw new InvalidOperationException($"Invalid STB reader signature: {method.Name}");
                }

                result.Add(dataType, CreateReader(method, dataType));
            }

            return result;
        }

        private static Action<ItemData, STB, int> CreateReader(MethodInfo method, Type dataType)
        {
            var actionType = typeof(Action<,,>).MakeGenericType(dataType, typeof(STB), typeof(int));
            var action = Delegate.CreateDelegate(actionType, method);

            return (data, stb, id) => action.DynamicInvoke(data, stb, id);
        }

        private static Dictionary<BodyPartType, Func<STB, int, ItemData>> BuildDataReaders()
        {
            var result = new Dictionary<BodyPartType, Func<STB, int, ItemData>>();

            var readMethod = typeof(STBImporter)
                .GetMethods(BindingFlags.Static | BindingFlags.Public)
                .First(x =>
                    x.Name == nameof(Read) &&
                    x.IsGenericMethodDefinition &&
                    x.GetGenericArguments().Length == 1);

            foreach (var pair in RevolutionShared.Utils.Tools.GetEquipmentDataTypes())
            {
                var bodyPart = pair.Key;
                var dataType = pair.Value;

                var genericMethod = readMethod.MakeGenericMethod(dataType);
                var delegateType = typeof(Func<,,>).MakeGenericType(typeof(STB), typeof(int), dataType);
                var delegateInstance = Delegate.CreateDelegate(delegateType, genericMethod);

                result.Add(bodyPart, (stb, id) => (ItemData)delegateInstance.DynamicInvoke(stb, id));
            }

            return result;
        }

        private static string ReadString(STB stb, int id, int column)
        {
            return stb.Cells[id][column];
        }

        private static int ReadInt(STB stb, int id, int column)
        {
            return EditorUtils.ParseSTBInt(stb.Cells[id][column]);
        }

        private static short ReadShort(STB stb, int id, int column)
        {
            return (short)ReadInt(stb, id, column);
        }

        private static byte ReadByte(STB stb, int id, int column)
        {
            return (byte)ReadInt(stb, id, column);
        }

        private static float ReadFloat(STB stb, int id, int column)
        {
            return float.Parse(stb.Cells[id][column], CultureInfo.InvariantCulture);
        }

        private static T ReadEnum<T>(STB stb, int id, int column) where T : Enum
        {
            return (T)Enum.ToObject(typeof(T), ReadInt(stb, id, column));
        }

        [STBReader(typeof(WeaponData))]
        private static void ReadWeapon(WeaponData data, STB stb, int id)
        {
            data.weaponType = ReadEnum<WeaponType>(stb, id, 5);
        }

        [STBReader(typeof(HeadgearData))]
        private static void ReadHeadgear(HeadgearData data, STB stb, int id)
        {
            data.hair = ReadByte(stb, id, 34);
        }
    }
}