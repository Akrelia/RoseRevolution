using UnityEditor;
using UnityEngine;
using UnityRose.Formats;
using System.IO;
using System.Collections.Generic;

public class STBViewer : EditorWindow
{
    private STB stb;
    private Vector2 scroll;
    private string loadedFilePath = "";

    [MenuItem("ROSE Online/Tools/STB Viewer")]
    public static void OpenWindow()
    {
        GetWindow<STBViewer>("STB Viewer");
    }

    private void OnGUI()
    {
        GUILayout.Label("STB File Viewer", EditorStyles.boldLabel);

        if (GUILayout.Button("Open STB File"))
        {
            string path = EditorUtility.OpenFilePanel("Open STB File", Application.dataPath, "stb");
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    stb = new STB(path);
                    loadedFilePath = path;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to load STB file: {e.Message}");
                }
            }
        }

        if (stb == null)
        {
            EditorGUILayout.HelpBox("No STB file loaded.", MessageType.Info);
            return;
        }

        GUILayout.Space(10);
        GUILayout.Label($"Loaded File: {Path.GetFileName(loadedFilePath)}", EditorStyles.miniLabel);

        if (GUILayout.Button("JSON Export"))
        {
            string jsonPath = EditorUtility.SaveFilePanel("Export to JSON", Application.dataPath, Path.GetFileNameWithoutExtension(loadedFilePath), "json");
            if (!string.IsNullOrEmpty(jsonPath))
            {
                ExportSTBToJson(jsonPath);
            }
        }

        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.BeginHorizontal();
        foreach (var col in stb.ColumnNames)
        {
            EditorGUILayout.LabelField(col, EditorStyles.boldLabel, GUILayout.MinWidth(80));
        }
        EditorGUILayout.EndHorizontal();

        for (int i = 0; i < stb.Cells.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            for (int j = 0; j < stb.Cells[i].Count; j++)
            {
                EditorGUILayout.TextField(stb.Cells[i][j], GUILayout.MinWidth(80));
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }

    private void ExportSTBToJson(string path)
    {
        var export = new STBJsonExport
        {
            file = Path.GetFileName(loadedFilePath),
            columns = stb.ColumnNames,
            rows = stb.Cells
        };

        string json = JsonUtility.ToJson(export, true);
        File.WriteAllText(path, json);
        Debug.Log($"STB exported to JSON : {path}");
    }

    [System.Serializable]
    private class STBJsonExport
    {
        public string file;
        public List<string> columns;
        public List<List<string>> rows;
    }
}
