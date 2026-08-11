using UnityEditor;
using UnityEngine;
using UnityRose.Formats;
using System.IO;
using System.Collections.Generic;
using System;

/// 
/// STB Viewer Window for Unity Editor.
/// 
public class STBViewerWindow : EditorWindow
{
    private STB stb;
    private Vector2 scroll;
    private string loadedFilePath = "";

    private const float ColumnWidth = 120f;
    private const float RowIndexWidth = 40f;
    private const int RowsPerPage = 50;

    private int currentPage = 0;

    /// <summary>
    /// Open the STB Window.
    /// </summary>
    [MenuItem("ROSE Online/Tools/STB Viewer")]
    public static void OpenWindow()
    {
        GetWindow<STBViewerWindow>("STB Viewer");
    }

    /// <summary>
    /// Draws the GUI for the STB Viewer Window.
    /// </summary>
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
                    currentPage = 0;
                }

                catch (Exception e)
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

        GUILayout.Space(10);

        int pageCount = Mathf.CeilToInt((float)stb.Cells.Count / RowsPerPage);

        EditorGUILayout.BeginHorizontal();

        EditorGUI.BeginDisabledGroup(currentPage <= 0);

        if (GUILayout.Button("Previous", GUILayout.Width(100)))
        {
            currentPage--;

            scroll = Vector2.zero;
        }

        EditorGUI.EndDisabledGroup();

        GUILayout.FlexibleSpace();

        GUILayout.Label($"Page {currentPage + 1} / {pageCount}");

        GUILayout.FlexibleSpace();

        EditorGUI.BeginDisabledGroup(currentPage >= pageCount - 1);

        if (GUILayout.Button("Next", GUILayout.Width(100)))
        {
            currentPage++;

            scroll = Vector2.zero;
        }

        EditorGUI.EndDisabledGroup();

        EditorGUILayout.EndHorizontal();

        GUILayout.Space(5);

        scroll = EditorGUILayout.BeginScrollView(scroll);

        DrawHeader();

        int startIndex = currentPage * RowsPerPage;
        int endIndex = Mathf.Min(startIndex + RowsPerPage, stb.Cells.Count);

        for (int i = startIndex; i < endIndex; i++)
        {
            DrawRow(i, stb.Cells[i]);
        }

        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// Draw the header.
    /// </summary>
    private void DrawHeader()
    {
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField("#", EditorStyles.boldLabel, GUILayout.Width(RowIndexWidth));

        for (int i = 0; i < stb.ColumnNames.Count; i++)
        {
            string name = $"[{i}] {stb.ColumnNames[i]}";

            EditorGUILayout.LabelField(name, EditorStyles.boldLabel, GUILayout.Width(ColumnWidth));
        }

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// Draw the row.
    /// </summary>
    /// <param name="index">Index.</param>
    /// <param name="cells">Cells.</param>
    private void DrawRow(int index, List<string> cells)
    {
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField(index.ToString(), GUILayout.Width(RowIndexWidth));

        for (int i = 0; i < stb.ColumnNames.Count; i++)
        {
            string value = i < cells.Count ? cells[i] : "";

            EditorGUILayout.TextField(value, GUILayout.Width(ColumnWidth));
        }

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// Exports the loaded STB data to a JSON file.
    /// </summary>
    /// <param name="path">Path of the JSON.</param>
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

    /// <summary>
    /// Quick JSON class.
    /// </summary>
    [Serializable]
    private class STBJsonExport
    {
        public string file;
        public List<string> columns;
        public List<List<string>> rows;
    }
}