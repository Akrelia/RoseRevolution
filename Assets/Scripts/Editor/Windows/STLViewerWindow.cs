using UnityEditor;
using UnityEngine;
using UnityRose.Formats;
using System.IO;
using System;

/// <summary>
/// STL Viewer Window for Unity Editor.
/// </summary>
public class STLViewerWindow : EditorWindow
{
    private STL stl;
    private Vector2 scroll;
    private string loadedFilePath = "";

    private const float ColumnWidth = 250f;
    private const float RowIndexWidth = 40f;
    private const int RowsPerPage = 50;

    private int currentPage = 0;

    /// <summary>
    /// Open the window.
    /// </summary>
    [MenuItem("ROSE Online/Tools/STL Viewer")]
    public static void OpenWindow()
    {
        GetWindow<STLViewerWindow>("STL Viewer");
    }

    /// <summary>
    /// Draws the GUI for the STL Viewer Window.
    /// </summary>
    private void OnGUI()
    {
        GUILayout.Label("STL File Viewer", EditorStyles.boldLabel);

        if (GUILayout.Button("Open STL File"))
        {
            string path = EditorUtility.OpenFilePanel("Open STL File", Application.dataPath, "stl");

            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    stl = new STL(path);
                    loadedFilePath = path;
                    currentPage = 0;
                }

                catch (Exception e)
                {
                    Debug.LogError($"Failed to load STL file: {e.Message}");
                }
            }
        }

        if (stl == null)
        {
            EditorGUILayout.HelpBox("No STL file loaded.", MessageType.Info);

            return;
        }

        GUILayout.Space(10);

        GUILayout.Label($"Loaded File: {Path.GetFileName(loadedFilePath)}", EditorStyles.miniLabel);

        GUILayout.Label($"Type: {stl.Type}", EditorStyles.miniLabel);

        GUILayout.Space(10);

        int pageCount = Mathf.CeilToInt((float)stl.Entries.Count / RowsPerPage);

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
        int endIndex = Mathf.Min(startIndex + RowsPerPage, stl.Entries.Count);

        for (int i = startIndex; i < endIndex; i++)
        {
            DrawRow(i);
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

        for (int i = 0; i < stl.Rows.Count; i++)
        {
            string name = i < stl.Entries.Count ? $"{stl.Entries[i].ID}: {stl.Entries[i].StringID}" : i.ToString();

            EditorGUILayout.LabelField(name, EditorStyles.boldLabel, GUILayout.Width(ColumnWidth));
        }

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// Draw the row.
    /// </summary>
    /// <param name="index">Index of the row.</param>
    private void DrawRow(int index)
    {
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField(index.ToString(), GUILayout.Width(RowIndexWidth));

        for (int i = 0; i < stl.Rows.Count; i++)
        {
            string value = index < stl.Rows[i].Count ? stl.Rows[i][index].Text : "";

            EditorGUILayout.TextField(value, GUILayout.Width(ColumnWidth));
        }

        EditorGUILayout.EndHorizontal();
    }
}