using UnityEngine;
using UnityEditor;
using UnityRose.Formats;
using System.Collections;
using UnityRose;

public class ROSEImportWindow : EditorWindow
{

    private const string DataPathKey = "ROSE_DataPath";
    private const string NPCShaderKey = "ROSE_NPCShader";
    private const string MapShaderKey = "ROSE_MapShader";

    private bool wasEditing = false;
    private string dataPath = "";
    private Shader npcShader;
    private Shader mapShader;

    [MenuItem("ROSE Online/Models Importer")]
    static void Init()
    {
        var window = GetWindow<ROSEImportWindow>(true, "ROSE NPC/Map Importer");
        window.Show();
    }

    private Vector2 mapListScrollPosition;
    private bool mapListShowUnnamed = false;
    void OnGUI()
    {
        GUILayout.Label("Settings", EditorStyles.boldLabel);
        dataPath = EditorGUILayout.TextField("3DData Path", dataPath);
        npcShader = (Shader)EditorGUILayout.ObjectField("Shader for NPC", npcShader, typeof(Shader), false);
        mapShader = (Shader)EditorGUILayout.ObjectField("Shader for Map", mapShader, typeof(Shader), false);

        GUILayout.Label("Importing", EditorStyles.boldLabel);
        GUILayout.Label("Current Path: " + ROSEImport.GetCurrentPath());
        if (GUILayout.Button("Spawn 30 NPC"))
            ROSEImport.ImportNPC(30);
        if (GUILayout.Button("Import ALL NPC"))
        {
            bool confirm = EditorUtility.DisplayDialog("Confirmation", "This will take a lot of time, are you sure ?", "Yes", "No");

            if (confirm)
            {
                ROSEImport.ImportAllNPC();
            }
        }

        if (GUILayout.Button("Clear Generated ROSE Data"))
            ROSEImport.ClearData();

        if (GUILayout.Button("Generate Character Player Animations"))
            ResourceManager.Instance.GenerateAnimationAssets();

        if (GUILayout.Button("Import PTL / EFT"))
            ROSEImport.ImportParticles();


        GUILayout.Label("Maps", EditorStyles.boldLabel);

        mapListShowUnnamed = GUILayout.Toggle(mapListShowUnnamed, "Show Unnamed Maps");

        var mapData = ROSEImport.GetMapListData();
        if (mapData != null)
        {
            mapListScrollPosition = GUILayout.BeginScrollView(mapListScrollPosition, GUILayout.Height(400));
            for (var i = 0; i < mapData.stb.Cells.Count; ++i)
            {
                string mapName = mapData.stl.GetString(mapData.stb.Cells[i][27], STL.Language.English);
                if (mapName != null || mapListShowUnnamed)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("[" + i.ToString() + "] " + mapName);
                    if (GUILayout.Button("Import", GUILayout.Width(100)))
                    {
                        RoseTerrainWindow.ImportMap(i);
                    }
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndScrollView();
        }


        if (EditorGUIUtility.editingTextField)
        {
            wasEditing = true;
        }
        else
        {
            if (wasEditing)
            {
                wasEditing = false;
                EditorPrefs.SetString(DataPathKey, dataPath);
                ROSEImport.MaybeUpdate();
            }
        }
    }

    void OnFocus()
    {
        if (EditorPrefs.HasKey(DataPathKey))
        {
            dataPath = EditorPrefs.GetString(DataPathKey);
        }

        if (EditorPrefs.HasKey(NPCShaderKey))
        {
            //  npcShader = GetNPCShader();
        }

        ROSEImport.MaybeUpdate();
    }

    void OnLostFocus()
    {
        EditorPrefs.SetString(DataPathKey, dataPath);
        EditorPrefs.SetString(NPCShaderKey, AssetDatabase.GetAssetPath(npcShader));
        ROSEImport.MaybeUpdate();
    }

    void OnDestroy()
    {
        EditorPrefs.SetString(DataPathKey, dataPath);
        EditorPrefs.SetString(NPCShaderKey, AssetDatabase.GetAssetPath(npcShader));
        ROSEImport.MaybeUpdate();
    }

    public static string GetDataPath()
    {
        return EditorPrefs.GetString(DataPathKey);
    }

    public static Shader GetNPCShader()
    {
        return AssetDatabase.LoadAssetAtPath<Shader>(EditorPrefs.GetString(NPCShaderKey));
    }

}