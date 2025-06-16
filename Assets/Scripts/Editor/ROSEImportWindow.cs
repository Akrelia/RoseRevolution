using UnityEngine;
using UnityEditor;
using UnityRose.Formats;
using System.Collections;
using UnityRose;

public class ROSEImportWindow : EditorWindow
{

    private const string DataPathKey = "ROSE_DataPath";

    private bool wasEditing = false;
    private int indexNPC = 0;
    private string dataPath = "";

    [MenuItem("ROSE Online/Data Importer")]
    static void Init()
    {
        var window = GetWindow<ROSEImportWindow>(true, "ROSE Data Import");
        window.Show();
    }

    private Vector2 mapListScrollPosition;
    private bool mapListShowUnnamed = false;

    void OnGUI()
    {
        GUILayout.Label("Settings", EditorStyles.boldLabel);
        dataPath = EditorGUILayout.TextField("Uncompressed VFS folder path", dataPath);

        GUILayout.Label("Importing", EditorStyles.boldLabel);
        GUILayout.Label("Current Path: " + ROSEImport.GetCurrentPath());

        if (GUILayout.Button("Initialize ROSE Data"))
            ResourceManager.Instance.GenerateAnimationAssets();

        if (GUILayout.Button("Import ALL NPC"))
        {
            bool confirm = EditorUtility.DisplayDialog("Confirmation", "This will take a lot of time, are you sure ?", "Yes", "No");

            if (confirm)
            {
                ROSEImport.ImportAllNPC();
            }
        }

        GUILayout.BeginHorizontal();

        indexNPC = EditorGUILayout.IntField("NPC ID", indexNPC);

        if (GUILayout.Button("Import NPC"))
        {
            ROSEImport.ImportNPC(indexNPC);
        }

        GUILayout.EndHorizontal();

        if (GUILayout.Button("Clear ROSE Data"))
            ROSEImport.ClearData();

        if (GUILayout.Button("Import PTL / EFT (Debug)"))
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

                    if (GUILayout.Button("Export Spawns", GUILayout.Width(100)))
                    {
                        RoseTerrainWindow.ExportSpawns(i);
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

        ROSEImport.MaybeUpdate();
    }

    void OnLostFocus()
    {
        EditorPrefs.SetString(DataPathKey, dataPath);
        ROSEImport.MaybeUpdate();
    }

    void OnDestroy()
    {
        EditorPrefs.SetString(DataPathKey, dataPath);
        ROSEImport.MaybeUpdate();
    }

    public static string GetDataPath()
    {
        return EditorPrefs.GetString(DataPathKey);
    }
}