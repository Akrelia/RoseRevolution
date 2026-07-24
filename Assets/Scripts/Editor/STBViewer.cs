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

    private const float ColumnWidth = 120f;
    private const float RowIndexWidth = 40f;

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

        GUILayout.Label(
            $"Loaded File: {Path.GetFileName(loadedFilePath)}",
            EditorStyles.miniLabel);

        if (GUILayout.Button("JSON Export"))
        {
            string jsonPath = EditorUtility.SaveFilePanel(
                "Export to JSON",
                Application.dataPath,
                Path.GetFileNameWithoutExtension(loadedFilePath),
                "json");

            if (!string.IsNullOrEmpty(jsonPath))
            {
                ExportSTBToJson(jsonPath);
            }
        }

        GUILayout.Space(10);

        scroll = EditorGUILayout.BeginScrollView(scroll);

        DrawHeader();

        for (int i = 0; i < stb.Cells.Count; i++)
        {
            DrawRow(i, stb.Cells[i]);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawHeader()
    {
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField(
            "#",
            EditorStyles.boldLabel,
            GUILayout.Width(RowIndexWidth));

        for (int i = 0; i < stb.ColumnNames.Count; i++)
        {
            string name = $"{i}: {stb.ColumnNames[i]}";

            EditorGUILayout.LabelField(
                name,
                EditorStyles.boldLabel,
                GUILayout.Width(ColumnWidth));
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawRow(int index, List<string> cells)
    {
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField(
            index.ToString(),
            GUILayout.Width(RowIndexWidth));

        for (int i = 0; i < stb.ColumnNames.Count; i++)
        {
            string value = i < cells.Count
                ? cells[i]
                : "";

            EditorGUILayout.TextField(
                value,
                GUILayout.Width(ColumnWidth));
        }

        EditorGUILayout.EndHorizontal();
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