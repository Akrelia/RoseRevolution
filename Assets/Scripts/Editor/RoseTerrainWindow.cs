
#if UNITY_EDITOR

// Importer for rose maps and player
using UnityEditor;
using UnityEngine;
using UnityRose.Formats;
using System;
using System.IO;
using System.Collections.Generic;
using UnityRose.Game;
using UnityRose;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

public class RoseTerrainWindow : EditorWindow
{
    string m_inputDir = "";
    BodyPartType bodyPart;
    int objID;
    RosePlayer player;
    Transform transform;
    GameObject playerObject;

    // Add menu named "My Window" to the Window menu
    static void Init()
    {
        // Get existing open window or if none, make a new one:
        RoseTerrainWindow window = (RoseTerrainWindow)EditorWindow.GetWindow(typeof(RoseTerrainWindow), true, "ROSE Character Creator");
    }

    private struct PatchNormalIndex
    {
        public int patchID;
        public int normalID;
        public PatchNormalIndex(int patchID, int normalID)
        {
            this.patchID = patchID;
            this.normalID = normalID;
        }
    }

    public static void ImportMap(int mapID)
    {
        Debug.Log("Importing map ID " + mapID + "...");
        bool success = true;

        var stb = ResourceManager.Instance.stb_zone;
        var stl = ResourceManager.Instance.stl_zone_list;

        var mapDirectory = Path.Combine(ROSEImport.GetDataPath(), Utils.FixPath(Path.GetDirectoryName(stb.Cells[mapID][2].ToString()))); // Get the ZON path to determine the whole path

        Debug.Log(mapDirectory);

        DirectoryInfo dirs = new DirectoryInfo(mapDirectory);

        GameObject map = new GameObject();
        map.name = stb.Cells[mapID][1].ToString();

        var roseMap = map.AddComponent<RoseMap>();

        GameObject terrain = new GameObject();
        terrain.name = "Ground";
        terrain.transform.parent = map.transform;
        terrain.layer = LayerMask.NameToLayer("Floor");

        GameObject terrainObjects = new GameObject();
        terrainObjects.name = "Objects";
        terrainObjects.transform.parent = map.transform;
        terrainObjects.layer = LayerMask.NameToLayer("MapObjects");

        List<RosePatch> patches = new List<RosePatch>();
        Dictionary<string, Rect> atlasRectHash = new Dictionary<string, Rect>();
        Dictionary<string, Texture2D> atlasTexHash = new Dictionary<string, Texture2D>();
        List<Texture2D> textures = new List<Texture2D>();


        // Instantiate all patches
        foreach (DirectoryInfo dir in dirs.GetDirectories())
        {
            if (!dir.Name.Contains("."))
            {
                RosePatch patch = new RosePatch(dir);
                patch.Load(mapID);
                patch.UpdateAtlas(ref atlasRectHash, ref atlasTexHash, ref textures);
                patches.Add(patch);
            }
        }
        // Create a texture atlas from the textures of all patches and populate the rectangles in the hash

        // Figure out the required size of the atlas from the number of textures in the atlas
        int height, width;  // these must be powers of 2 to be compatible with iPhone
        if (atlasRectHash.Count <= 16) width = height = 4 * 256;
        else if (atlasRectHash.Count <= 32) { width = 8 * 256; height = 4 * 256; }
        else if (atlasRectHash.Count <= 64) { width = 8 * 256; height = 8 * 256; }
        else if (atlasRectHash.Count <= 128) { width = 16 * 256; height = 8 * 256; }
        else if (atlasRectHash.Count <= 256) { width = 16 * 256; height = 16 * 256; }
        else throw new Exception("Number of tiles in terrain is larger than supported by terrain atlas");


        Texture2D atlas = new Texture2D(width, height);


        // Pack the textures into one texture atlas
        Rect[] rects = atlas.PackTextures(textures.ToArray(), 0, Math.Max(width, height));
        atlas.anisoLevel = 11;

        Texture2D myAtlas = new Texture2D(width, height);
        myAtlas.SetPixels32(atlas.GetPixels32(0), 0);

        string atlasPath = "Assets/Terrain/Textures/" + mapID + "_atlas.png";

        if (!Directory.Exists("Assets/Terrain/Textures/"))
        {
            Directory.CreateDirectory("Assets/Terrain/Textures/");
        }


        if (!File.Exists(atlasPath))
        {
            FileStream fs = new FileStream(atlasPath, FileMode.Create);
            BinaryWriter bw = new BinaryWriter(fs);
            bw.Write(myAtlas.EncodeToPNG());
            bw.Close();
            fs.Close();


            AssetDatabase.Refresh();
        }

        myAtlas = Utils.loadTex(ref atlasPath);

        // copy rects back to hash (should update rect refs in Tile objects
        int rectID = 0;
        foreach (string key in atlasTexHash.Keys)
            atlasRectHash[key] = rects[rectID++];

        // Generate the patches
        foreach (RosePatch patch in patches)
            patch.Import(terrain.transform, terrainObjects.transform, myAtlas, myAtlas, atlasRectHash);


        //blend vertex normals at the seams between patches
        Dictionary<string, List<PatchNormalIndex>> patchNormalLookup = new Dictionary<string, List<PatchNormalIndex>>();
        int patchID = 0;
        // combine all normal lookups into one big lookup containing patch ID and normal ID
        foreach (RosePatch patch in patches)
        {

            // go through the lookup of this patch and append all normal id's to big lookup
            foreach (string vertex in patch.edgeVertexLookup.Keys)
            {
                List<PatchNormalIndex> ids = new List<PatchNormalIndex>();
                foreach (int id in patch.edgeVertexLookup[vertex])
                    ids.Add(new PatchNormalIndex(patchID, id));

                if (!patchNormalLookup.ContainsKey(vertex))
                    patchNormalLookup.Add(vertex, ids);
                else
                    patchNormalLookup[vertex].AddRange(ids);

            }

            patchID++;
        }

        // go through each enttry in the big lookup and calculate avg normal, then assign to corresponding patches
        foreach (string vertex in patchNormalLookup.Keys)
        {
            Vector3 avg = Vector3.zero;
            // First pass: calculate average normal
            foreach (PatchNormalIndex entry in patchNormalLookup[vertex])
                avg += patches[entry.patchID].m_mesh.normals[entry.normalID];

            avg.Normalize();

            // Second pass: assign new normal to corresponding patches
            foreach (PatchNormalIndex entry in patchNormalLookup[vertex])
                patches[entry.patchID].m_mesh.normals[entry.normalID] = avg;

        }

        terrainObjects.transform.localScale = new Vector3(1.0f, 1.0f, -1.0f);
        terrainObjects.transform.Rotate(0.0f, -90.0f, 0.0f);
        terrainObjects.transform.position = new Vector3(5200.0f, 0.0f, 5200.0f);


        var worldManager = FindAnyObjectByType<SandboxManager>(); // Akima : this will be removed when everything will be off editor scripts

        GameObject spawns = new GameObject();
        spawns.name = "Spawn Points";
        spawns.transform.position = new Vector3(5200 * 2F, 0, 0); // Akima : took me almost an hour to find the correct offset, I guess its the origin of Rose Map + The -1 scale of every object
        spawns.transform.Rotate(0, -90F, 0);
        spawns.transform.SetParent(map.transform);

        for (int i = 0; i < patches[0].m_ZON.SpawnPoints.Count; i++)
        {
            var spawnPoint = patches[0].m_ZON.SpawnPoints[i];

            GameObject spawn = new GameObject();

            spawn.name = spawnPoint.Name;

            spawn.transform.parent = spawns.transform;
            spawn.transform.localPosition = Utils.r2uScale((spawnPoint.Position));
            spawn.transform.rotation = Quaternion.identity;

            if (spawnPoint.Name == "start")
            {
                worldManager.spawnPosition = spawn.transform.position;
            }
        }

        GameObject npcs = new GameObject();
        npcs.name = "NPCs";
        npcs.transform.SetParent(map.transform);
        npcs.transform.localScale = new Vector3(1.0f, 1.0f, -1.0f);
        npcs.transform.Rotate(0, -90F, 0);
        npcs.transform.position = new Vector3(5200, 0, 5200);

        for (int i = 0; i < patches.Count; i++)
        {
            var ifo = patches[i].m_IFO;

            for (int j = 0; j < ifo.NPCs.Count; j++)
            {
                ROSEImport.ImportNPC(ifo.NPCs[j].ObjectID);

                GameObject npc = new GameObject();

                npc.name = "NPC_" + ifo.NPCs[j].ObjectID;

                npc.transform.parent = npcs.transform;

                npc.transform.localPosition = ifo.NPCs[j].Position / 100F;
                npc.transform.rotation = Quaternion.identity; ;

                var roseNpc = npc.AddComponent<RoseNpc>();

                roseNpc.data = LoadNPCAssetStartingWith<RoseNpcData>($"[{ifo.NPCs[j].ObjectID}]");
            }
        }
        GameObject monsters = new GameObject();
        monsters.name = "Monsters";
        monsters.transform.SetParent(map.transform);
        monsters.transform.localScale = new Vector3(1.0f, 1.0f, -1.0f);
        monsters.transform.Rotate(0, -90F, 0);
        monsters.transform.position = new Vector3(5200, 0, 5200);

        worldManager.worldManager.mobSpawner = monsters;

        roseMap.patches.AddRange(patches);
        roseMap.mapName = map.name;

        if (success)
            Debug.Log("Map Import Complete");
        else
            Debug.Log("!Map Import Failed");



        // worldManager.spawnPosition = Utils.r2uScale(patches[0].m_ZON.SpawnPoints.FirstOrDefault(sp => sp.Name == "start").Position); // Akima : since ZON file is the same for every patch, just take the ref of the first one
    }

    public static void ExportSpawns(int mapID)
    {
        var stb = ResourceManager.Instance.stb_zone;
        var stbNPC = ResourceManager.Instance.stb_npc_list;

        string path = EditorUtility.SaveFilePanel("Export Spawns", Application.dataPath, $"{stb.Cells[mapID][1].ToString()}", "json");

        if (!string.IsNullOrEmpty(path))
        {
            Debug.Log("Exporting spawns for map ID " + mapID + "...");

            var mapDirectory = Path.Combine(ROSEImport.GetDataPath(), (Path.GetDirectoryName(Utils.FixPath(stb.Cells[mapID][2].ToString()))));

            DirectoryInfo dirs = new DirectoryInfo(mapDirectory);

            List<RosePatch> patches = new List<RosePatch>();

            foreach (DirectoryInfo dir in dirs.GetDirectories())
            {
                if (!dir.Name.Contains("."))
                {
                    RosePatch patch = new RosePatch(dir);
                    patch.Load(mapID);
                    patches.Add(patch);
                }
            }

            JObject root = new JObject();

            JArray monstersArray = new JArray();

            root["Spawns"] = monstersArray;
            root["MapID"] = mapID;
            root["MapName"] = stb.Cells[mapID][1];

            for (int i = 0; i < patches.Count; i++)
            {
                JObject patchObj = new JObject();

                var monsters = patches[i].m_IFO.Monsters;

                for (int j = 0; j < monsters.Count; j++)
                {
                    JObject monsterObj = new JObject();

                    JArray basicArray = new JArray();

                    JObject spawnSettings = new JObject
                    {
                        ["Name"] = monsters[j].Name,
                        ["MapX"] = monsters[j].MapPosition.x,
                        ["MapY"] = monsters[j].MapPosition.y,
                        ["ID"] = monsters[j].ObjectID,
                        ["WorldX"] = (monsters[j].Position.x + 520000.0f) / 100F,
                        ["WorldY"] = (monsters[j].Position.y + 520000.0f) / 100F,
                        ["WorldZ"] = monsters[j].Position.z / -10000F,
                        ["Interval"] = monsters[j].Interval,
                        ["LimitCount"] = monsters[j].Limit,
                        ["Range"] = monsters[j].Range,
                        ["TacticPoints"] = monsters[j].TacticPoints,
                    };

                    foreach (var b in monsters[j].Basic)
                    {
                        JObject basicObj = new JObject
                        {
                            ["ID"] = b.ID,
                            ["Count"] = b.Count,
                            ["Description"] = stbNPC.Cells[b.ID][1]
                        };

                        basicArray.Add(basicObj);
                    }

                    JArray tacticArray = new JArray();

                    foreach (var t in monsters[j].Tactic)
                    {
                        JObject tacticObj = new JObject
                        {
                            ["ID"] = t.ID,
                            ["Count"] = t.Count,
                            ["Description"] = stbNPC.Cells[t.ID][1]
                        };
                        tacticArray.Add(tacticObj);
                    }

                    monsterObj["Settings"] = spawnSettings;
                    monsterObj["Basic"] = basicArray;
                    monsterObj["Tactic"] = tacticArray;

                    monstersArray.Add(monsterObj);
                }

                patchObj["Spawns"] = monstersArray;

                if (monstersArray.Count != 0)
                {
                  //  patchesArray.Add(patchObj);
                }
            }

            string jsonString = root.ToString(Formatting.Indented);



            File.WriteAllText(path, jsonString, Encoding.Unicode);

        }
        else
        {
            Debug.Log("Save operation cancelled.");
        }
    }

    public static T LoadNPCAssetStartingWith<T>(string prefix) where T : ScriptableObject
    {
        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { "Assets/Npcs" });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string filename = System.IO.Path.GetFileNameWithoutExtension(path);

            if (filename.StartsWith(prefix))
            {
                return AssetDatabase.LoadAssetAtPath<T>(path);
            }
        }

        return null;
    }

    /*
	private RosePatch ImportPatch(string inputDir, Transform terrainParent, Transform objectsParent)
	{
		// Patch consists of the following elements:
		//	- .HIM file specifying the heighmap of the terrain: 65x65 image of floats 
		//	- .TIL file specifying the texture tileset: 16x16 tiles, containing ID's that index into .ZON.Tiles, which returns index into .ZON.Textures
		//  - TODO: add other fileTypes here after researching
		
		
		bool success = true;
		
		Debug.Log ("Importing patch from " + inputDir + "...");
		
		RosePatch patch = new RosePatch(new DirectoryInfo(inputDir));
		
		success &= patch.Load();
		success &= patch.Import(terrainParent, objectsParent);
		
		
		if(success)
			Debug.Log ("Patch Import complete");
		else
			Debug.Log ("!Patch Import failed");
		
		return patch;
	}
	*/

    void OnGUI()
    {
        // ======================== OBJECT ==========================
        EditorGUILayout.BeginToggleGroup("Characters", true);
        objID = EditorGUILayout.IntField("ID: ", objID);
        bodyPart = (BodyPartType)EditorGUILayout.EnumPopup("Body Part: ", bodyPart);
        transform = EditorGUILayout.ObjectField("Transform: ", transform, typeof(Transform), true) as Transform;
        playerObject = EditorGUILayout.ObjectField("Player Game Object: ", playerObject, typeof(GameObject), true) as GameObject;
        if (GUILayout.Button("Create Player"))
        {
            if (transform != null)
                player = new RosePlayer(transform.position); // Note: Player reference is lost after hitting play.  Must create new after that.
            else
                player = new RosePlayer();
        }

        if (GUILayout.Button("Create Player (Selection)"))
        {
            CharModel model = new CharModel();
            model.rig = RigType.CHARSELECT;
            model.state = States.HOVERING;

            if (transform != null)
                model.pos = transform.position;

            player = new RosePlayer(model); // Note: Player reference is lost after hitting play.  Must create new after that.

        }

        if (GUILayout.Button("Equip to Character"))
        {
            if (playerObject != null)
            {
                var playerController = playerObject.GetComponent<PlayerController>();

                playerController.rosePlayer.equip((BodyPartType)bodyPart, objID);
            }

            else
            {
                Debug.Log("Please set a player in the window");
            }
        }

        if (GUILayout.Button("Generate Player Animations"))
        {
            //RosePlayer player = new RosePlayer(GenderType.MALE, WeaponType.THSWORD);
            //ResourceManager.Instance.GenerateAnimationAsset(GenderType.MALE, WeaponType.EMPTY);
            //ResourceManager.Instance.GenerateAnimationAssets();
            GenerateCharSelectAnimations();
        }

        EditorGUILayout.EndToggleGroup();
    } // OnGui()


    void GenerateCharSelectAnimations()
    {
        foreach (GenderType gender in Enum.GetValues(typeof(GenderType)))
        {
            bool m = (gender == GenderType.MALE);
            Dictionary<String, String> clips = new Dictionary<String, String>();
            clips.Add("standup", "3ddata/motion/avatar/empty_stand_" + (m ? "m" : "f") + "1.zmo");
            clips.Add("standing", "3ddata/motion/avatar/empty_stop1_" + (m ? "m" : "f") + "1.zmo");
            clips.Add("sit", "3ddata/motion/avatar/empty_sit_" + (m ? "m" : "f") + "1.zmo");
            clips.Add("sitting", "3ddata/motion/avatar/empty_siting_" + (m ? "m" : "f") + "1.zmo");

            clips.Add("hovering", "3ddata/motion/avatar/event_creat_m1.zmo");
            clips.Add("select", "3ddata/motion/avatar/event_select_m1.zmo");

            ResourceManager.Instance.GenerateAnimationAsset(gender, RigType.CHARSELECT, clips);
        }
    }
}

#endif