using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class RoseDDSExportWindow : EditorWindow
{
    private List<Texture2D> textures = new List<Texture2D>();
    private Vector2 scrollPosition;
    private string outputFolder;

    [MenuItem("ROSE Online/Tools/Texture Exporter")]
    private static void ShowWindow()
    {
        GetWindow<RoseDDSExportWindow>("Texture Exporter");
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Textures", EditorStyles.boldLabel);

        if (GUILayout.Button("Add Selected"))
        {
            foreach (Object obj in Selection.objects)
            {
                Texture2D texture = obj as Texture2D;

                if (texture != null && !textures.Contains(texture))
                {
                    textures.Add(texture);
                }
            }
        }

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));

        for (int i = 0; i < textures.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();

            textures[i] = (Texture2D)EditorGUILayout.ObjectField(
                textures[i],
                typeof(Texture2D),
                false);

            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                textures.RemoveAt(i);
                i--;
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        if (GUILayout.Button("Add Texture"))
        {
            textures.Add(null);
        }

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();

        outputFolder = EditorGUILayout.TextField(outputFolder);

        if (GUILayout.Button("Select", GUILayout.Width(60)))
        {
            string folder = EditorUtility.OpenFolderPanel("Select Output Folder", "", "");

            if (!string.IsNullOrEmpty(folder))
            {
                outputFolder = folder;
            }
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        GUI.enabled = textures.Count > 0 && !string.IsNullOrEmpty(outputFolder);

        if (GUILayout.Button("Export PNG", GUILayout.Height(35)))
        {
            Export();
        }

        GUI.enabled = true;
    }

    private void Export()
    {
        int exported = 0;

        foreach (Texture2D texture in textures)
        {
            Debug.Log($"Texture: {texture.name} | Format: {texture.format} | Size: {texture.width}x{texture.height} | Linear: {texture.isDataSRGB}");

            if (texture == null)
            {
                continue;
            }

            try
            {

                Texture2D copy = new Texture2D(
                    texture.width,
                    texture.height,
                    TextureFormat.RGBA32,
                    false);

                if (copy == null)
                {
                    Debug.LogWarning($"Failed to copy texture: {texture.name}");
                    continue;
                }

                Color32[] pixels = texture.GetPixels32();

                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i].a = 255;
                }


                copy.SetPixels32(pixels);
                copy.Apply();

                File.WriteAllBytes(
                    Path.Combine(outputFolder, texture.name + ".png"),
                    copy.EncodeToPNG());

                DestroyImmediate(copy);

                exported++;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to export '{texture.name}': {ex}");
            }
        }

        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Texture Exporter",
            $"{exported} texture(s) exported.",
            "OK");
    }

    private Texture2D CopyTexture(Texture2D source)
    {
        Color32[] pixels = source.GetPixels32();

        Texture2D copy = new Texture2D(
            source.width,
            source.height,
            TextureFormat.RGBA32,
            false);

        copy.SetPixels32(pixels);
        copy.Apply();

        return copy;
    }
}