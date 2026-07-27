using UnityEngine;
using UnityEditor;
using UnityRose.Formats;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityRose;
using UnityRose.Import;
using System.IO;
using System;
using System.Collections.Generic;
using static AddressableIndex;
using System.Text;
using UnityRose.Game;
using System.Linq;
using static UnityRose.Formats.ZON;
using RevolutionShared.Rose.Data;
using RevolutionShared.Rose.Data.Equipment;
using RevolutionShared.Rose.Data.NPC.Drops;
using Unity.VisualScripting;

namespace UnityRose.ImportEditor
{
    /// <summary>
    /// Rose Import Window.
    /// </summary>
    public class ROSEImportWindow : EditorWindow
    {
        private bool wasEditing = false;
        private int indexNPC = 0;
        private string dataPath = "";
        private Vector2 mapListScrollPosition;
        private bool mapListShowUnnamed = false;
        private ImportationSettings settings;

        [MenuItem("ROSE Online/Data Importer")]
        static void Init()
        {
            var window = GetWindow<ROSEImportWindow>(true, "ROSE Data Import");

            window.Show();
        }

        /// <summary>
        /// Import all maps.
        /// </summary>
        public void ImportAllMaps()
        {
            var mapData = ROSEMapListCache.Get();

            if (mapData == null)
                return;

            string folder = "Assets/Data/Maps";

            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder("Assets/Data", "Maps");
            }

            string dbPath = $"{folder}/MapDatabase.asset";

            var database = AssetDatabase.LoadAssetAtPath<MapDatabase>(dbPath);

            if (database == null)
            {
                database = CreateInstance<MapDatabase>();

                AssetDatabase.CreateAsset(database, dbPath);
            }

            database.maps.Clear();

            AssetDatabase.StartAssetEditing();

            try
            {
                for (int i = 0; i < mapData.stb.Cells.Count; i++)
                {
                    string name = mapData.stl.GetText(mapData.stb.Cells[i][27], STL.Language.English);

                    if (string.IsNullOrEmpty(name))
                    {
                        name = "Map_" + i;
                    }

                    Debug.Log($"Importing map {i} : {name}");

                    RoseMapImporter.ImportMap(i);

                    string prefabPath = $"{ImportPaths.Maps.Prefabs}/{name}.prefab";

                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

                    if (prefab == null)
                    {
                        Debug.LogWarning($"Prefab not found for {name}");

                        continue;
                    }

                    database.maps.Add(new RoseMapEntry
                    {
                        id = i,
                        name = name,
                        prefab = prefab
                    });
                }
            }

            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private T CreateDatabase<T>(string name) where T : ScriptableObject
        {
            string path = $"Assets/GameData/Databases/{name}.asset";

            var database = AssetDatabase.LoadAssetAtPath<T>(path);

            if (database == null)
            {
                database = CreateInstance<T>();
                AssetDatabase.CreateAsset(database, path);
            }

            return database;
        }

        public void ImportAllEquipment()
        {
            const int maxIdsPerSlot = 15;

            string dbFolder = "Assets/GameData/Databases";

            if (!AssetDatabase.IsValidFolder(dbFolder))
                Utils.EnsureFolder(dbFolder);

            string dbPath = $"{dbFolder}/EquipmentDatabase.asset";

            var database = AssetDatabase.LoadAssetAtPath<EquipmentDatabase>(dbPath);

            if (database == null)
            {
                database = CreateInstance<EquipmentDatabase>();
                AssetDatabase.CreateAsset(database, dbPath);
            }

            if (database.weaponDatabase == null)
                database.weaponDatabase = CreateDatabase<WeaponDatabase>("WeaponDatabase");

            if (database.bodyDatabase == null)
                database.bodyDatabase = CreateDatabase<ArmorDatabase>("BodyDatabase");

            if (database.armDatabase == null)
                database.armDatabase = CreateDatabase<ArmorDatabase>("ArmDatabase");

            if (database.backDatabase == null)
                database.backDatabase = CreateDatabase<ArmorDatabase>("BackDatabase");

            if (database.headgearDatabase == null)
                database.headgearDatabase = CreateDatabase<HeadgearDatabase>("HeadgearDatabase");

            if (database.footwearDatabase == null)
                database.footwearDatabase = CreateDatabase<FootwearDatabase>("FootwearDatabase");

            if (database.faceItemDatabase == null)
                database.faceItemDatabase = CreateDatabase<ArmorDatabase>("FaceItemDatabase");

            if (database.appearenceDatabase == null)
                database.appearenceDatabase = CreateDatabase<AppearenceDatabase>("AppearenceDatabase");

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
            var baker = new RoseEquipmentBaker();

            //        AssetDatabase.StartAssetEditing();

            try
            {
                BakeSlot<ArmorData, ArmorDataImporter>(baker, database.bodyDatabase, "Body_M", rm.zsc_body_male, "3DDATA/AVATAR/BODY/BODY_M.ZSC", BodyPartType.BODY, GenderType.MALE, maxIdsPerSlot, ResourceManager.Instance.stb_armor_list);
                BakeSlot<ArmorData, ArmorDataImporter>(baker, database.bodyDatabase, "Body_F", rm.zsc_body_female, "3DDATA/AVATAR/BODY/BODY_F.ZSC", BodyPartType.BODY, GenderType.FEMALE, maxIdsPerSlot, ResourceManager.Instance.stb_armor_list);

                BakeSlot<ArmorData, ArmorDataImporter>(baker, database.armDatabase, "Arms_M", rm.zsc_arms_male, "3DDATA/AVATAR/ARMS/ARMS_M.ZSC", BodyPartType.ARMS, GenderType.MALE, maxIdsPerSlot, ResourceManager.Instance.stb_arms_list);
                BakeSlot<ArmorData, ArmorDataImporter>(baker, database.armDatabase, "Arms_F", rm.zsc_arms_female, "3DDATA/AVATAR/ARMS/ARMS_F.ZSC", BodyPartType.ARMS, GenderType.FEMALE, maxIdsPerSlot, ResourceManager.Instance.stb_arms_list);

                BakeSlot<FootwearData, FootwearDataImporter>(baker, database.footwearDatabase, "Foot_M", rm.zsc_foot_male, "3DDATA/AVATAR/FOOT/FOOT_M.ZSC", BodyPartType.FOOT, GenderType.MALE, maxIdsPerSlot, ResourceManager.Instance.stb_foot_list);
                BakeSlot<FootwearData, FootwearDataImporter>(baker, database.footwearDatabase, "Foot_F", rm.zsc_foot_female, "3DDATA/AVATAR/FOOT/FOOT_F.ZSC", BodyPartType.FOOT, GenderType.FEMALE, maxIdsPerSlot, ResourceManager.Instance.stb_foot_list);

                BakeAppearenceSlot(baker, database.appearenceDatabase.faces, "Face_M", rm.zsc_face_male, "3DDATA/AVATAR/FACE/FACE_M.ZSC", BodyPartType.FACE, GenderType.MALE, maxIdsPerSlot);
                BakeAppearenceSlot(baker, database.appearenceDatabase.faces, "Face_F", rm.zsc_face_female, "3DDATA/AVATAR/FACE/FACE_F.ZSC", BodyPartType.FACE, GenderType.FEMALE, maxIdsPerSlot);

                BakeAppearenceSlot(baker, database.appearenceDatabase.hairs, "Hair_M", rm.zsc_hair_male, "3DDATA/AVATAR/HAIR/HAIR_M.ZSC", BodyPartType.HAIR, GenderType.MALE, maxIdsPerSlot);
                BakeAppearenceSlot(baker, database.appearenceDatabase.hairs, "Hair_F", rm.zsc_hair_female, "3DDATA/AVATAR/HAIR/HAIR_F.ZSC", BodyPartType.HAIR, GenderType.FEMALE, maxIdsPerSlot);

                BakeSlot<HeadgearData, HeadgearDataImporter>(baker, database.headgearDatabase, "Cap_M", rm.zsc_cap_male, "3DDATA/AVATAR/CAP/CAP_M.ZSC", BodyPartType.CAP, GenderType.MALE, maxIdsPerSlot, ResourceManager.Instance.stb_cap_list);
                BakeSlot<HeadgearData, HeadgearDataImporter>(baker, database.headgearDatabase, "Cap_F", rm.zsc_cap_female, "3DDATA/AVATAR/CAP/CAP_F.ZSC", BodyPartType.CAP, GenderType.FEMALE, maxIdsPerSlot, ResourceManager.Instance.stb_cap_list);

                BakeSlot<WeaponData, WeaponDataImporter>(baker, database.weaponDatabase, "Weapon", rm.zsc_weapon, "3DDATA/WEAPON/LIST_WEAPON.ZSC", BodyPartType.WEAPON, GenderType.NONE, maxIdsPerSlot, ResourceManager.Instance.stb_weapon_list);

                BakeSlot<ArmorData, ArmorDataImporter>(baker, database.faceItemDatabase, "FaceItem", rm.zsc_faceItem, "3DDATA/AVATAR/FACEITEM/FACEITEM.ZSC", BodyPartType.FACEITEM, GenderType.NONE, maxIdsPerSlot, ResourceManager.Instance.stb_faceitem_list);

                BakeSlot<ArmorData, ArmorDataImporter>(baker, database.backDatabase, "Back", rm.zsc_back, "3DDATA/AVATAR/BACK/BACK.ZSC", BodyPartType.BACK, GenderType.NONE, maxIdsPerSlot, ResourceManager.Instance.stb_back_list);
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

            Debug.Log($"Base equipment import done ({maxIdsPerSlot} ids/slot max");
        }

        public void ImportAllDropTables()
        {
            var roothPath = "Assets/GameData/Drops";

            Utils.EnsureFolder($"{roothPath}/dummy.asset");

            var stb = ResourceManager.Instance.stb_drops_list;

            AssetDatabase.StartAssetEditing();

            try
            {
                for (int i = 0; i < 50; i++)
                {
                    if (!string.IsNullOrEmpty(stb.Cells[i][1]))
                    {
                        var table = RoseExport.ExportDropTable(stb, i);

                        var entry = ScriptableObject.CreateInstance<DropTableSO>();

                        entry.id = i;
                        entry.table = table;

                        AssetDatabase.CreateAsset(entry, $"{roothPath}/{i}.asset");

                        RegisterDropTableInInternalDB(entry);

                        EditorUtility.SetDirty(entry);
                    }
                }
            }

            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static bool IsInvalidSTBEntry(string name, BodyPartType bodyPart, int id)
        {
            if (string.IsNullOrWhiteSpace(name))
                return true;

            if (name.Contains("???") || string.IsNullOrEmpty(name)) // Easy way to filter invalid or empty entries, but still
            {
                if (bodyPart == BodyPartType.BODY && id <= 1)
                    return false;

                return true;
            }

            return false;
        }

        private void BakeSlot<T, U>(RoseEquipmentBaker baker, ItemDatabase<T> database, string namePrefix, ZSC zsc, string zscPath, BodyPartType bodyPart, GenderType gender, int maxIds, STB stb) where T : EquipmentData where U : EquipmentDataImporter<T>, new()
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

                if (IsInvalidSTBEntry(name, bodyPart, id))
                {
                    id++;
                    continue;
                }

                GameObject prefab;

                try
                {
                    prefab = baker.BakeEquipment($"{namePrefix}_{id}", bodyPart, zsc, zscPath, id);
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

        private void BakeAppearenceSlot(RoseEquipmentBaker baker, List<AppearenceEntry> target, string namePrefix, ZSC zsc, string zscPath, BodyPartType bodyPart, GenderType gender, int maxIds)
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
                    prefab = baker.BakeEquipment($"{namePrefix}_{id}", bodyPart, zsc, zscPath, id);
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
        /// Register a map in the internal database.
        /// </summary>
        /// <param name="id">ID.</param>
        private void RegisterMapInInternalDB(int id)
        {
            var mapData = ROSEMapListCache.Get();

            string folder = ImportPaths.Database.Root;

            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder(ImportPaths.Root, "Databases");
            }

            string path = $"{folder}/MapDatabase.asset";

            var database = AssetDatabase.LoadAssetAtPath<MapDatabase>(path);

            if (database == null)
            {
                database = CreateInstance<MapDatabase>();

                AssetDatabase.CreateAsset(database, path);
            }

            AddressableUtils.EnsureAddressable(path, nameof(MapDatabase));

            string displayName = mapData.stl.GetText(mapData.stb.Cells[id][27], STL.Language.English);

            string prefabName = mapData.stb.Cells[id][1];

            if (string.IsNullOrEmpty(name))
            {
                name = "Map_" + id;
            }

            string prefabPath = $"{ImportPaths.Maps.Prefabs}/{prefabName}.prefab";

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (prefab == null)
            {
                Debug.LogWarning($"Prefab not found for map {id} ({name})");

                return;
            }

            var roseMap = prefab.GetComponent<RoseMap>();

            var existing = database.maps.Find(x => x.id == id);

            if (existing != null)
            {
                existing.name = displayName;
                existing.prefab = prefab;
                existing.spawnPoints = roseMap != null ? roseMap.spawns : new List<SpawnData>();
            }

            else
            {
                database.maps.Add(new RoseMapEntry
                {
                    id = id,
                    name = displayName,
                    prefab = prefab,
                    spawnPoints = roseMap != null ? roseMap.spawns : new List<SpawnData>()
                });
            }

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
        }

        private void RegisterSpawnInInternalDB(int mapID)
        {
            var database = LoadMonsterSpawnDatabase();

            if (database == null)
            {
                database = CreateInstance<MonsterSpawnDatabase>();

                Utils.EnsureFolder(Path.GetDirectoryName(ROSEDatabaseWindow.MonsterSpawnDatabasePath));

                AssetDatabase.CreateAsset(database, ROSEDatabaseWindow.MonsterSpawnDatabasePath);
            }

            AddressableUtils.EnsureAddressable(ROSEDatabaseWindow.MonsterSpawnDatabasePath, nameof(MonsterSpawnDatabase)); // Shouldn't be useful but just in case

            database.maps.RemoveAll(x => x.MapID == mapID);

            MapSpawnData spawnData = new MapSpawnData();

            database.maps.Add(BuildSpawnData(mapID));

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
        }

        private void RegisterNpcInInternalDB(GameObject prefab, NPCEntitySO npc)
        {
            string folder = ImportPaths.Database.Root;

            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder(ImportPaths.Root, "Databases");
            }

            string path = $"{folder}/NpcDatabase.asset";

            var database = AssetDatabase.LoadAssetAtPath<NPCDatabase>(path);

            if (database == null)
            {
                database = CreateInstance<NPCDatabase>();

                AssetDatabase.CreateAsset(database, path);
            }

            AddressableUtils.EnsureAddressable(path, nameof(NPCDatabase));

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

        private void RegisterDropTableInInternalDB(DropTableSO entry)
        {
            string folder = ImportPaths.Database.Root;

            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder(ImportPaths.Root, "Databases");
            }

            string path = $"{folder}/DropTableDatabase.asset";

            var database = AssetDatabase.LoadAssetAtPath<DropTableDatabase>(path);

            if (database == null)
            {
                database = CreateInstance<DropTableDatabase>();

                AssetDatabase.CreateAsset(database, path);
            }

            AddressableUtils.EnsureAddressable(path, nameof(DropTableDatabase));

            var existing = database.entries.Find(x => x.id == entry.id);

            if (existing != null)
            {
                existing = entry;
            }

            else
            {
                database.entries.Add(entry);
            }

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
        }

        public static MapSpawnData BuildSpawnData(int mapID)
        {
            var stb = ResourceManager.Instance.stb_zone;
            var stbNPC = ResourceManager.Instance.stb_npc_list;

            var mapDirectory = Path.Combine(RoseDataSource.DataPath, Path.GetDirectoryName(Utils.FixPath(stb.Cells[mapID][2].ToString())));

            var dirs = new DirectoryInfo(mapDirectory);
            var patches = new List<RosePatch>();

            foreach (var dir in dirs.GetDirectories())
            {
                if (dir.Name.Contains(".")) continue;

                var patch = new RosePatch(dir);
                var valid = patch.Load(mapID);

                if (valid)
                    patches.Add(patch);
            }


            var data = new MapSpawnData
            {
                MapID = mapID,
                MapName = stb.Cells[mapID][1].ToString(),
                Spawns = new List<MonsterSpawnData>()
            };

            foreach (var patch in patches)
            {
                foreach (var monster in patch.m_IFO.Monsters)
                {
                    var spawn = new MonsterSpawnData
                    {
                        Name = monster.Name,
                        MapX = monster.MapPosition.x,
                        MapY = monster.MapPosition.y,
                        ID = monster.ObjectID,
                        WorldX = (monster.Position.x + 520000.0f) / 100F,
                        WorldY = (monster.Position.y + 520000.0f) / 100F,
                        WorldZ = monster.Position.z / -10000F,
                        Interval = monster.Interval,
                        LimitCount = monster.Limit,
                        Range = monster.Range,
                        TacticPoints = monster.TacticPoints,
                        Basic = new List<MonsterEntryData>(),
                        Tactic = new List<MonsterEntryData>()
                    };

                    foreach (var b in monster.Basic)
                    {
                        spawn.Basic.Add(new MonsterEntryData
                        {
                            ID = b.ID,
                            Count = b.Count,
                            Description = stbNPC.Cells[b.ID][1].ToString()
                        });
                    }

                    foreach (var t in monster.Tactic)
                    {
                        spawn.Tactic.Add(new MonsterEntryData
                        {
                            ID = t.ID,
                            Count = t.Count,
                            Description = stbNPC.Cells[t.ID][1].ToString()
                        });
                    }

                    data.Spawns.Add(spawn);
                }
            }

            return data;
        }

        /// <summary>
        /// Exports the spawns for the specified map ID to a JSON file.
        /// </summary>
        /// <param name="mapID">Map ID.</param>
        public static void ExportSpawns(int mapID)
        {
            var stb = ResourceManager.Instance.stb_zone;

            var path = EditorUtility.SaveFilePanel("Export Spawns", Application.dataPath, stb.Cells[mapID][1].ToString(), "json");

            if (!string.IsNullOrEmpty(path))
            {
                Debug.Log("Exporting spawns for map ID " + mapID + "...");

                var json = RoseMapImporter.BuildSpawnExportJson(mapID);

                System.IO.File.WriteAllText(path, json, Encoding.Unicode);

                Debug.Log("Export spawns for map ID " + mapID + " done!");
            }
        }

        /// <summary>
        /// Loads the RoseMapDatabase asset.
        /// </summary>
        /// <returns>Map database.</returns>
        private MapDatabase LoadMapDatabase()
        {
            return AssetDatabase.LoadAssetAtPath<MapDatabase>(ROSEDatabaseWindow.MapDatabasePath);
        }

        /// <summary>
        /// Loads the RoseMapDatabase asset.
        /// </summary>
        /// <returns>Map database.</returns>
        private MonsterSpawnDatabase LoadMonsterSpawnDatabase()
        {
            return AssetDatabase.LoadAssetAtPath<MonsterSpawnDatabase>(ROSEDatabaseWindow.MonsterSpawnDatabasePath);
        }

        private bool MapPrefabExists(int mapID)
        {
            var mapData = ROSEMapListCache.Get();

            string mapName = Path.GetFileNameWithoutExtension(Utils.FixPath(mapData.stb.Cells[mapID][1].ToString()));

            string prefabPath = $"{ImportPaths.Maps.Prefabs}/{mapName}.prefab";

            return AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null;
        }

        private void RegisterEquipmentDatabaseAddressables(EquipmentDatabase database, string path)
        {
            AddressableUtils.EnsureAddressable(path, nameof(EquipmentDatabase));

            var subDatabases = new ScriptableObject[]
            {
        database.weaponDatabase,
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

                AddressableUtils.EnsureAddressable(subPath, subDatabase.GetType().Name);
            }
        }

        public static class AddressableUtils
        {
            public static void EnsureAddressable(string assetPath, string address)
            {
                var settings = AddressableAssetSettingsDefaultObject.Settings;

                if (settings == null)
                    return;

                string guid = AssetDatabase.AssetPathToGUID(assetPath);

                var entry = settings.FindAssetEntry(guid) ?? settings.CreateOrMoveEntry(guid, settings.DefaultGroup);

                entry.address = address;
            }
        }

        /// <summary>
        /// Draws the GUI for the ROSE Import Window.
        /// </summary>
        private void OnGUI()
        {
            var centeredStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };

            var mapDatabase = LoadMapDatabase();
            var spawnDatabase = LoadMonsterSpawnDatabase();

            GUILayout.Label("Settings", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();

            dataPath = EditorGUILayout.TextField("Uncompressed VFS folder path", dataPath);

            if (GUILayout.Button("Browse", GUILayout.Width(80)))
            {
                string path = EditorUtility.OpenFolderPanel("Select ROSE VFS folder", dataPath, "");

                if (!string.IsNullOrEmpty(path))
                {
                    dataPath = path;
                    ROSEEditorBaker.DataPath = dataPath;
                    ROSEMapListCache.MaybeUpdate();
                }
            }

            GUILayout.EndHorizontal();

            GUILayout.Label("Importing", EditorStyles.boldLabel);
            GUILayout.Label("Current Path: " + ROSEEditorBaker.DataPath);

            if (GUILayout.Button("Import Characters"))
            {
                if (EditorUtility.DisplayDialog("Confirmation", "This will import every assets for Characters (animations, meshs)", "Yes", "No"))
                {
                    ResourceManager.Instance.GenerateAnimationAssets();
                }
            }

            if (GUILayout.Button("Import ALL Equipment"))
            {
                if (EditorUtility.DisplayDialog("Import ALL Equipment", "This will bake every body/armor/weapon/etc. ZSC into prefabs and rebuild the Avatar database.", "Yes", "No"))
                {
                    ImportAllEquipment();
                }
            }

            if (GUILayout.Button("Import ALL Maps"))
            {
                if (EditorUtility.DisplayDialog("Import ALL Maps", "This will import every ROSE map and rebuild the map database.", "Yes", "No"))
                {
                    ImportAllMaps();
                }
            }

            if (GUILayout.Button("Import Drop Tables"))
            {
                if (EditorUtility.DisplayDialog("Import Drop Tables", "This will import every drop tables.", "Yes", "No"))
                {
                    ImportAllDropTables();
                }
            }

            GUILayout.BeginHorizontal();

            indexNPC = EditorGUILayout.IntField("NPC ID", indexNPC);

            if (GUILayout.Button("Import NPC"))
            {
                var npc = ROSEEditorBaker.ImportNPC(indexNPC);

                if (npc)
                {
                    RegisterNpcInInternalDB(npc, npc.GetComponent<NPCEntityBehavior>().data);
                }

                else
                {
                    Debug.Log($"Failed to import NPC with ID {indexNPC}");
                }
            }

            GUILayout.EndHorizontal();

            if (GUILayout.Button("Clear ROSE Data"))
            {
                ROSEEditorBaker.ClearData();
            }

            GUILayout.Label("Maps", EditorStyles.boldLabel);

            mapListShowUnnamed = GUILayout.Toggle(mapListShowUnnamed, "Show Unnamed Maps");

            var mapData = ROSEMapListCache.Get();

            GUILayout.BeginHorizontal();

            if (mapData != null)
            {

                GUILayout.Label("Name", GUILayout.ExpandWidth(true));
                GUILayout.Label("Prefab", centeredStyle, GUILayout.Width(50));
                GUILayout.Label("Data", centeredStyle, GUILayout.Width(50));
                GUILayout.Label("Spawns", centeredStyle, GUILayout.Width(80));
                GUILayout.Label("Action", GUILayout.Width(100));
                GUILayout.EndHorizontal();

                mapListScrollPosition = GUILayout.BeginScrollView(mapListScrollPosition, GUILayout.Height(400));

                for (var i = 0; i < mapData.stb.Cells.Count; ++i)
                {
                    string mapName = mapData.stl.GetText(mapData.stb.Cells[i][27], STL.Language.English);

                    if (mapName == null && !mapListShowUnnamed)
                        continue;

                    bool mapDataExist = mapDatabase != null && mapDatabase.maps.Any(x => x.id == i);
                    bool spawnsExist = spawnDatabase != null && spawnDatabase.maps.Any(x => x.MapID == i);

                    GUILayout.BeginHorizontal();

                    GUILayout.Label($"[{i}] {mapName}", GUILayout.ExpandWidth(true));

                    GUILayout.Label(MapPrefabExists(i) ? "✔️" : "", centeredStyle, GUILayout.Width(50));

                    GUILayout.Label(mapDataExist ? "✔️" : "", centeredStyle, GUILayout.Width(50));

                    GUILayout.Label(spawnsExist ? "✔️" : "", centeredStyle, GUILayout.Width(80));

                    if (GUILayout.Button("Import", GUILayout.Width(100)))
                    {
                        var map = RoseMapImporter.ImportMap(i);

                        RegisterMapInInternalDB(i);
                        RegisterSpawnInInternalDB(i);
                    }

                    GUILayout.EndHorizontal();
                }

                GUILayout.EndScrollView();
            }

            if (EditorGUIUtility.editingTextField)
            {
                wasEditing = true;
            }

            else if (wasEditing)
            {
                wasEditing = false;

                ROSEEditorBaker.DataPath = dataPath;

                ROSEMapListCache.MaybeUpdate();
            }
        }

        private void OnEnable()
        {
            SyncDataPath();
        }

        private void OnFocus()
        {
            SyncDataPath();
            ROSEMapListCache.MaybeUpdate();
        }

        private void OnLostFocus()
        {
            ROSEEditorBaker.DataPath = dataPath;
            ROSEMapListCache.MaybeUpdate();
        }

        private void OnDestroy()
        {
            ROSEEditorBaker.DataPath = dataPath;
            ROSEMapListCache.MaybeUpdate();
        }

        private void SyncDataPath()
        {
            dataPath = EditorPrefs.GetString("ROSE_DataPath");
            ROSEEditorBaker.DataPath = dataPath;
        }
    }

    public static class ROSEMapListCache
    {
        public class MapListData
        {
            public STB stb;
            public STL stl;
        }

        private static MapListData _data;
        private static string _lastPath = "";

        public static MapListData Get() => _data;

        public static void MaybeUpdate()
        {
            var current = ROSEEditorBaker.DataPath;
            if (current == _lastPath) return;
            _lastPath = current;

            _data = new MapListData
            {
                stb = new STB(Utils.CombinePath(current, "3DDATA/STB/LIST_ZONE.STB")),
                stl = new STL(Utils.CombinePath(current, "3DDATA/STB/LIST_ZONE_S.STL"))
            };
        }
    }

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
            data.price = Utils.ParseInt(stb.Cells[id][6]);
        }

        protected abstract void ReadFields(T data, STB stb, int id);
    }

    public class ArmorDataImporter : EquipmentDataImporter<ArmorData>
    {
        protected override ArmorData CreateInstance() => new ArmorData();

        protected override void ReadFields(ArmorData data, STB stb, int id) => ReadArmorFields(data, stb, id);

        public static void ReadArmorFields(ArmorData data, STB stb, int id)
        {
        }
    }

    public class FootwearDataImporter : EquipmentDataImporter<FootwearData>
    {
        protected override FootwearData CreateInstance() => new FootwearData();

        protected override void ReadFields(FootwearData data, STB stb, int id)
        {
            ArmorDataImporter.ReadArmorFields(data, stb, id);
        }
    }

    public class WeaponDataImporter : EquipmentDataImporter<WeaponData>
    {
        protected override WeaponData CreateInstance() => new WeaponData();

        protected override void ReadFields(WeaponData data, STB stb, int id)
        {
            data.weaponType = (WeaponType)Utils.ParseInt(stb.Cells[id][5]);
        }
    }

    public class HeadgearDataImporter : EquipmentDataImporter<HeadgearData>
    {
        protected override HeadgearData CreateInstance() => new HeadgearData();

        protected override void ReadFields(HeadgearData data, STB stb, int id)
        {
            ArmorDataImporter.ReadArmorFields(data, stb, id);

            data.hair = (byte)Utils.ParseInt(stb.Cells[id][34]);
        }
    }
}

