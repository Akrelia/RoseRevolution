using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityRose.ImportEditor;
using System;
using static UnityRose.Import.GameDataPaths;
using RevolutionShared.Rose.Data.Game;
using System.IO;
using RevolutionShared.Rose.Data;
using RevolutionShared.Rose.Data.Items.Equipment.Armor;
using RevolutionShared.Rose.Data.Items.Equipment;
using UnityRose.Formats;
using RevolutionShared.Rose.Data.Items;
using UnityEditor.VersionControl;

namespace UnityRose.Import
{
    /// <summary>
    /// Rose Import core. For small imports, big ones got their own class.
    /// </summary>
    public class RoseImporter
    {
        private const string DataPathKey = "ROSE_DataPath";

        /// <summary>
        /// Import the skyboxes.
        /// </summary>
        public static void ImportSkyboxes()
        {
            var stb = ResourceManager.Instance.skySTB;

            var database = ScriptableObject.CreateInstance<SkyboxDatabase>();

            database.entries = new List<SkyboxData>();

            var context = new SkyboxImportContext();

            for (int i = 0; i < stb.Cells.Count; i++)
            {
                var skybox = ScriptableObject.CreateInstance<SkyboxData>();

                skybox.Id = i;

                context.index = i;

                var zmsPath = stb.Cells[i][1];
                var textureDayPath = stb.Cells[i][2];
                var textureNightPath = stb.Cells[i][3];

                skybox.BackgroundColor1 = EditorUtils.ParseSTBColor(stb.Cells[i][7]);
                skybox.BackgroundColor2 = EditorUtils.ParseSTBColor(stb.Cells[i][8]);
                skybox.BackgroundColor3 = EditorUtils.ParseSTBColor(stb.Cells[i][9]);
                skybox.BackgroundColor4 = EditorUtils.ParseSTBColor(stb.Cells[i][10]);

                skybox.AmbientCharacter1 = EditorUtils.ParseSTBColor(stb.Cells[i][11]);
                skybox.DiffuseCharacter1 = EditorUtils.ParseSTBColor(stb.Cells[i][12]);

                skybox.AmbientCharacter2 = EditorUtils.ParseSTBColor(stb.Cells[i][13]);
                skybox.DiffuseCharacter2 = EditorUtils.ParseSTBColor(stb.Cells[i][14]);

                skybox.AmbientCharacter3 = EditorUtils.ParseSTBColor(stb.Cells[i][15]);
                skybox.DiffuseCharacter3 = EditorUtils.ParseSTBColor(stb.Cells[i][16]);

                skybox.AmbientCharacter4 = EditorUtils.ParseSTBColor(stb.Cells[i][17]);
                skybox.DiffuseCharacter4 = EditorUtils.ParseSTBColor(stb.Cells[i][18]);

                skybox.Mesh = ROSEEditorBaker.ImportMesh(zmsPath, context); // Akima : I think every mesh are the same, even if they are multiple files
                skybox.Material = ROSEEditorBaker.ImportSkyboxMaterial(textureDayPath, textureNightPath, $"SkyMaterial{i}.mat.asset", context);

                var assetPath = $"{context.Root}/Skybox_{i}.asset";

                AssetDatabase.CreateAsset(skybox, assetPath);

                EditorUtils.EnsureFolder(assetPath);

                database.entries.Add(skybox);
            }

            var databasePath = $"{GameDataPaths.Database.Root}/{nameof(SkyboxDatabase)}.asset";

            CreateAddressableAsset(database, databasePath);
        }

        /// <summary>
        /// Import the icons.
        /// </summary>
        public static void ImportIcons()
        {
            var database = ScriptableObject.CreateInstance<IconDatabase>();

            database.entries = new List<IconAtlasData>();

            var context = new IconImportContext();

            int index = 1;

            while (index < 200) // Fake big limit to avoid any infinite loop
            {
                var fileName = $"icon{index:00}.dds";
                var rosePath = $"3DDATA/CONTROL/RES/{fileName}";

                var texture = ROSEEditorBaker.ImportTexture(rosePath, context, true);

                if (texture == null)
                {
                    break;
                }

                database.entries.Add(new IconAtlasData
                {
                    name = fileName,
                    texture = texture
                });

                index++;
            }

            var databasePath = $"{GameDataPaths.Database.Root}/IconDatabase.asset";

            CreateAddressableAsset(database, databasePath);
        }

        /// <summary>
        /// Import loadings.
        /// </summary>
        public static void ImportLoadings(string dataPath)
        {
            var database = GetOrCreateDatabase<LoadingDatabase>();

            database.mapLoadings = new FakeDictionary<int, Sprite>();
            database.planetloadings = new FakeDictionary<int, List<Sprite>>();

            var stb = ResourceManager.Instance.loadingSTB;

            for (int i = 0; i < stb.Cells.Count; i++)
            {
                var path = stb.Cells[i][1];
                var id = EditorUtils.ParseSTBInt(stb.Cells[i][2]);

                var sprite = EditorUtils.ImportImageAsJPG(Path.Combine(dataPath, path), GameDataPaths.Loadings.Root);

                if (id < 500) // Map Loading
                {
                    database.mapLoadings.Add(id, sprite);
                }

                else // Planet loading
                {
                    var planetID = id - 500;

                    if (!database.planetloadings.ContainsKey(planetID))
                    {
                        database.planetloadings.Add(planetID, new List<Sprite>());
                    }

                    database.planetloadings[planetID].Add(sprite);
                }
            }

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Import the Rose Data.
        /// </summary>
        public static void ImportRoseData()
        {
            var database = GetOrCreateDatabase<RoseDataDatabase>();

            database.itemGrades = new FakeDictionary<int, ItemGradeData>();

            var gradeSTB = ResourceManager.Instance.gradeSTB;

            for (int i = 1; i < gradeSTB.Cells.Count; i++)
            {
                var gradeLevel = EditorUtils.ParseSTBInt(gradeSTB.Cells[i][0]);
                var attackBonus = EditorUtils.ParseSTBInt(gradeSTB.Cells[i][1]);
                var accuracyBonus = EditorUtils.ParseSTBInt(gradeSTB.Cells[i][2]);
                var defenseBonus = EditorUtils.ParseSTBInt(gradeSTB.Cells[i][3]);
                var magicDefenseBonus = EditorUtils.ParseSTBInt(gradeSTB.Cells[i][4]);
                var dodgeRateBonus = EditorUtils.ParseSTBInt(gradeSTB.Cells[i][5]);
                var glowColor = EditorUtils.ParseRoseColorInt(gradeSTB.Cells[i][6]);

                var grade = new ItemGradeData(gradeLevel, attackBonus, accuracyBonus, defenseBonus, magicDefenseBonus, dodgeRateBonus, glowColor);

                database.itemGrades.Add(gradeLevel, grade);
            }
        }

        /// <summary>
        /// Import the drop tables.
        /// </summary>
        public static void ImportDropTables()
        {
            string path = $"{GameDataPaths.Database.Root}/DropTableDatabase.asset";

            var database = AssetDatabase.LoadAssetAtPath<DropTableDatabase>(path);

            if (database == null)
            {
                database = ScriptableObject.CreateInstance<DropTableDatabase>();
            }

            EditorUtils.EnsureFolder($"{GameDataPaths.DropTables.Root}/dummy.asset"); // Do this instead of doing it in the loop

            var stb = ResourceManager.Instance.dropSTB;

            AssetDatabase.StartAssetEditing();

            try
            {
                for (int i = 0; i < stb.Cells.Count; i++)
                {
                    if (!string.IsNullOrEmpty(stb.Cells[i][1]))
                    {
                        var table = RoseExport.ExportDropTable(stb, i);

                        var entry = ScriptableObject.CreateInstance<DropTableSO>();

                        entry.id = i;
                        entry.table = table;

                        AssetDatabase.CreateAsset(entry, $"{GameDataPaths.DropTables.Root}/{i}.asset");

                        var index = database.entries.FindIndex(x => x.id == entry.id);

                        if (index >= 0)
                        {
                            database.entries[index] = entry;
                        }

                        else
                        {
                            database.entries.Add(entry);
                        }

                        EditorUtility.SetDirty(entry);
                    }
                }

                CreateAddressableAsset(database, path);
            }

            catch (Exception ex)
            {
                Debug.LogError($"Error importing drop tables: {ex.Message}\n{ex.StackTrace}");
            }

            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }

        /// <summary>
        /// Import all items.
        /// </summary>
        /// <param name="amount">Amount.</param>
        public static void ImportAllItems()
        {
            var database = GetOrCreateDatabase<ItemDatabase>();

            database.Initialize();

            var stbList = ResourceManager.Instance.stbItems;

            database.faceItems = ImportItemsData<FaceItemData>(stbList[ItemType.FACEITEM]);
            database.caps = ImportItemsData<HeadgearData>(stbList[ItemType.HAT]);
            database.bodies = ImportItemsData<BodyArmorData>(stbList[ItemType.BODY]);
            database.arms = ImportItemsData<HandwearData>(stbList[ItemType.GLOVES]);
            database.boots = ImportItemsData<FootwearData>(stbList[ItemType.BOOTS]);
            database.backs = ImportItemsData<BackData>(stbList[ItemType.BACK]);
            database.jewels = ImportItemsData<JewelData>(stbList[ItemType.JEWEL]);
            database.weapons = ImportItemsData<WeaponData>(stbList[ItemType.WEAPON]);
            database.subweapons = ImportItemsData<SubWeaponData>(stbList[ItemType.SUBWEAPON]);
            database.consumables = ImportItemsData<ConsumableData>(stbList[ItemType.USEITEM]);
            database.gems = ImportItemsData<GemData>(stbList[ItemType.GEMS]);
            database.materials = ImportItemsData<MaterialData>(stbList[ItemType.MATERIAL]);

            EditorUtility.SetDirty(database);

            Debug.Log("Imported all items data");
        }

        /// <summary>
        /// Import items data.
        /// </summary>
        /// <typeparam name="T">Type.</typeparam>
        /// <param name="stb">STB.</param>
        /// <returns>Items data.</returns>
        private static FakeDictionary<int, T> ImportItemsData<T>(STB stb) where T : ItemData, new()
        {
            FakeDictionary<int, T> datas = new FakeDictionary<int, T>();

            for (int i = 0; i < stb.Cells.Count; i++)
            {
                var data = STBImporter.Read<T>(stb, i);

                datas.Add(i, data);
            }

            return datas;
        }

        /// <summary>
        /// Gets or creates a database of type T. If the database does not exist, it will be created and saved as an asset in the specified path.
        /// </summary>
        /// <typeparam name="T">Type of database.</typeparam>
        public static T GetOrCreateDatabase<T>() where T : ScriptableObject
        {
            string name = typeof(T).Name;
            string databasePath = $"{GameDataPaths.Database.Root}/{name}.asset";

            var database = AssetDatabase.LoadAssetAtPath<T>(databasePath);

            if (database == null)
            {
                database = ScriptableObject.CreateInstance<T>();

                CreateAddressableAsset(database, databasePath);
            }

            return database;
        }

        /// <summary>
        /// Creates an addressable asset at the specified path.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="asset"></param>
        /// <param name="path"></param>
        public static void CreateAddressableAsset<T>(T asset, string path) where T : UnityEngine.Object
        {
            EditorUtils.EnsureFolder(path);
            AssetDatabase.CreateAsset(asset, path);
            EditorUtility.SetDirty(asset);
            EditorUtils.EnsureAddressable(path, typeof(T).Name);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Get the DataPath from EditorPrefs. This is the path to the uncompressed ROSE VFS folder.
        /// </summary>
        public static string DataPath
        {
            get => EditorPrefs.GetString(DataPathKey, "");
            set => EditorPrefs.SetString(DataPathKey, value);
        }
    }

    /// <summary>
    /// Contains constants for game data paths and import contexts.
    /// </summary>
    public static class GameDataPaths
    {
        public const string Root = "Assets/GameData";

        public static class Database
        {
            public const string Root = GameDataPaths.Root + "/Databases";
        }

        public static class NPC
        {
            public const string Root = GameDataPaths.Root + "/NPC";
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
            public const string Root = GameDataPaths.Root + "/Player";
            public const string Prefabs = Player.Root + "/Prefabs";
            public const string Meshes = Player.Root + "/Meshes";
            public const string Materials = Player.Root + "/Materials";
            public const string Textures = Player.Root + "/Textures";
            public const string Animation = Player.Root + "/Animations";
        }

        public static class Maps
        {
            public const string Root = GameDataPaths.Root + "/Maps";
            public const string Patches = Maps.Root + "/Patches";
            public const string Prefabs = Maps.Root + "/Prefabs";
            public const string Meshes = Maps.Root + "/Meshes";
            public const string Chunks = Maps.Root + "/Chunks";
            public const string Animations = Maps.Root + "/Animations";
            public const string Materials = Maps.Root + "/Materials";
            public const string Shared = Maps.Root + "/Shared";
            public const string SharedMeshes = Maps.Shared + "/Meshes";
            public const string SharedMaterials = Maps.Shared + "/Materials";
            public const string Atlas = Maps.Root + "/Atlas";
        }

        public static class Items
        {
            public const string Root = GameDataPaths.Root + "/Items";
            public const string Data = Items.Root + "/Data";
            public const string Prefabs = Items.Root + "/Prefabs";
        }

        public static class Icons
        {
            public const string Root = GameDataPaths.Root + "/Icons";
        }

        public static class Skyboxes
        {
            public const string Root = GameDataPaths.Root + "/Skyboxes";
        }

        public static class DropTables
        {
            public const string Root = GameDataPaths.Root + "/Drops";
        }

        public static class Loadings
        {
            public const string Root = GameDataPaths.Root + "/Loadings";
        }

        public static class Effects
        {
            public const string Root = GameDataPaths.Root + "/Effects";
            public const string Prefabs = Effects.Root + "/Prefabs";
            public const string Materials = Effects.Root + "/Materials";
            public const string Textures = Effects.Root + "/Textures";
            public const string Meshes = Effects.Root + "/Meshes";
        }

        /// <summary>
        /// Context for importing game data.
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

        /// <summary>
        /// Context for importing NPCs, including paths for root, data, prefabs, meshes, materials, textures, motions, animations, avatars, and controllers.
        /// </summary>
        public class NPCImportContext : ImportContext
        {
            public NPCImportContext(int id, string category, string name)
            {
                Id = id;
                Name = name;

                var folderName = $"[{id}] {name}";

                Root = $"{GameDataPaths.NPC.Root}/{category}/{folderName}";

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
        /// Context for importing equipment, including paths for root, prefabs, meshes, materials, and textures.
        /// </summary>
        public class EquipmentImportContext : ImportContext
        {
            public EquipmentImportContext()
            {
                Root = GameDataPaths.Player.Root;
                Prefab = GameDataPaths.Player.Prefabs;
                Meshes = GameDataPaths.Player.Meshes;
                Materials = GameDataPaths.Player.Materials;
                Textures = GameDataPaths.Player.Textures;
            }
        }

        /// <summary>
        /// Context for importing skyboxes, including paths for root, meshes, materials, and textures.
        /// </summary>
        public class SkyboxImportContext : ImportContext
        {
            public int index = 0;

            public SkyboxImportContext()
            {
                Root = GameDataPaths.Skyboxes.Root;
                Meshes = Root;
                Materials = Root;
                Textures = Root;
            }

            public string CurrentFolder
            {
                get { return Path.Combine(Root, index.ToString()); }
            }
        }

        /// <summary>
        /// Context for importing icons, including paths for root and textures.
        /// </summary>
        public class IconImportContext : ImportContext
        {
            public IconImportContext()
            {
                Root = GameDataPaths.Icons.Root;
                Textures = Root;
            }
        }

        /// <summary>
        /// Context for importing effects, including paths for root, textures, and materials.
        /// </summary>
        public class EffectImportContext : ImportContext
        {
            public EffectImportContext()
            {
                Root = GameDataPaths.Effects.Root;
                Textures = GameDataPaths.Effects.Textures;
                Materials = GameDataPaths.Effects.Materials;
                Meshes = GameDataPaths.Effects.Meshes;
                Prefab = GameDataPaths.Effects.Prefabs;
            }
        }
    }
}