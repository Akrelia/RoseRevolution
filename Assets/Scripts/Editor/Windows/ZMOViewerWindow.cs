using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityRose.Formats;

public class ZMOViewerWindow : EditorWindow
{
    private ZMO zmo;
    private string filePath;
    private int selectedFrame;

    private Vector2 channelScroll;
    private Vector2 frameScroll;

    [MenuItem("ROSE Online/Tools/ZMO Viewer")]
    public static void ShowWindow()
    {
        GetWindow<ZMOViewerWindow>("ZMO Viewer");
    }

    private void OnGUI()
    {
        DrawToolbar();

        if (zmo == null)
        {
            EditorGUILayout.HelpBox("No ZMO loaded.", MessageType.Info);
            return;
        }

        EditorGUILayout.Space(5);

        DrawInfo();
        EditorGUILayout.Space(10);
        DrawChannels();
        EditorGUILayout.Space(10);
        DrawFrameSelector();
        EditorGUILayout.Space(5);
        DrawFrame();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Button("Open ZMO", EditorStyles.toolbarButton, GUILayout.Width(100)))
        {
            string path = EditorUtility.OpenFilePanel("Open ZMO", "", "zmo");

            if (!string.IsNullOrEmpty(path))
            {
                LoadZMO(path);
            }
        }

        GUI.enabled = zmo != null;

        if (GUILayout.Button("Export JSON", EditorStyles.toolbarButton, GUILayout.Width(100)))
        {
            ExportJson();
        }

        GUI.enabled = true;

        GUILayout.FlexibleSpace();

        if (!string.IsNullOrEmpty(filePath))
        {
            GUILayout.Label(filePath, EditorStyles.miniLabel);
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawInfo()
    {
        EditorGUILayout.LabelField("ZMO Information", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField("FPS", zmo.FPS.ToString());
        EditorGUILayout.LabelField("Frames", zmo.Frames?.Length.ToString() ?? "0");
        EditorGUILayout.LabelField("Channels", zmo.Channels?.Length.ToString() ?? "0");

        if (zmo.FPS > 0 && zmo.Frames != null)
        {
            float duration = zmo.Frames.Length / (float)zmo.FPS;
            EditorGUILayout.LabelField("Duration", $"{duration:F3}s");
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawChannels()
    {
        EditorGUILayout.LabelField("Channels", EditorStyles.boldLabel);

        if (zmo.Channels == null || zmo.Channels.Length == 0)
        {
            EditorGUILayout.HelpBox("No channels.", MessageType.Info);
            return;
        }

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("#", EditorStyles.boldLabel, GUILayout.Width(40));
        EditorGUILayout.LabelField("Type", EditorStyles.boldLabel, GUILayout.Width(180));
        EditorGUILayout.LabelField("ID", EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();

        channelScroll = EditorGUILayout.BeginScrollView(channelScroll, GUILayout.MaxHeight(180));

        for (int i = 0; i < zmo.Channels.Length; i++)
        {
            var channel = zmo.Channels[i];

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(i.ToString(), GUILayout.Width(40));
            EditorGUILayout.LabelField(channel.Type.ToString(), GUILayout.Width(180));
            EditorGUILayout.LabelField(channel.ID.ToString());

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.EndVertical();
    }

    private void DrawFrameSelector()
    {
        int frameCount = zmo.Frames?.Length ?? 0;

        if (frameCount == 0)
        {
            return;
        }

        EditorGUILayout.LabelField("Frame", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        GUI.enabled = selectedFrame > 0;

        if (GUILayout.Button("<", GUILayout.Width(40)))
        {
            selectedFrame--;
        }

        GUI.enabled = true;

        selectedFrame = EditorGUILayout.IntSlider(selectedFrame, 0, frameCount - 1);

        GUI.enabled = selectedFrame < frameCount - 1;

        if (GUILayout.Button(">", GUILayout.Width(40)))
        {
            selectedFrame++;
        }

        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField(
            $"Frame {selectedFrame} / {frameCount - 1}",
            EditorStyles.centeredGreyMiniLabel);
    }

    private void DrawFrame()
    {
        if (zmo.Frames == null || zmo.Frames.Length == 0)
        {
            return;
        }

        var frame = zmo.Frames[selectedFrame];

        EditorGUILayout.LabelField(
            $"Frame {selectedFrame}",
            EditorStyles.boldLabel);

        frameScroll = EditorGUILayout.BeginScrollView(
            frameScroll,
            EditorStyles.helpBox);

        for (int i = 0; i < zmo.Channels.Length; i++)
        {
            var channel = zmo.Channels[i];
            var data = frame.Channels[i];

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField(
                $"Channel {i} — {channel.Type} (ID {channel.ID})",
                EditorStyles.boldLabel);

            switch (channel.Type)
            {
                case ZMO.ChannelType.Position:
                    EditorGUILayout.Vector3Field("Position", data.Position);
                    break;

                case ZMO.ChannelType.Rotation:
                    EditorGUILayout.Vector3Field(
                        "Euler",
                        zmo.GetContinuousEulerRotationAt(i, selectedFrame));

                    EditorGUILayout.Vector4Field(
                        "Quaternion",
                        new Vector4(
                            data.Rotation.x,
                            data.Rotation.y,
                            data.Rotation.z,
                            data.Rotation.w));
                    break;

                case ZMO.ChannelType.Normal:
                    EditorGUILayout.Vector3Field("Normal", data.Normal);
                    break;

                case ZMO.ChannelType.Alpha:
                    EditorGUILayout.FloatField("Alpha", data.Alpha);
                    break;

                case ZMO.ChannelType.UV0:
                    EditorGUILayout.Vector2Field("UV0", data.UV0);
                    break;

                case ZMO.ChannelType.UV1:
                    EditorGUILayout.Vector2Field("UV1", data.UV1);
                    break;

                case ZMO.ChannelType.UV2:
                    EditorGUILayout.Vector2Field("UV2", data.UV2);
                    break;

                case ZMO.ChannelType.UV3:
                    EditorGUILayout.Vector2Field("UV3", data.UV3);
                    break;

                case ZMO.ChannelType.TextureAnimation:
                    EditorGUILayout.FloatField(
                        "Texture Animation",
                        data.TextureAnimation);
                    break;

                case ZMO.ChannelType.Scale:
                    EditorGUILayout.FloatField("Scale", data.Scale);
                    break;
            }

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndScrollView();
    }

    private void LoadZMO(string path)
    {
        try
        {
            zmo = new ZMO(path, false, false);
            filePath = path;
            selectedFrame = 0;

            Repaint();
        }
        catch (Exception ex)
        {
            zmo = null;
            filePath = null;

            Debug.LogError($"Failed loading ZMO '{path}': {ex}");
        }
    }

    private void ExportJson()
    {
        if (zmo == null)
        {
            return;
        }

        string defaultName = Path.GetFileNameWithoutExtension(filePath) + ".json";

        string outputPath = EditorUtility.SaveFilePanel(
            "Export ZMO JSON",
            Path.GetDirectoryName(filePath),
            defaultName,
            "json");

        if (string.IsNullOrEmpty(outputPath))
        {
            return;
        }

        var export = new ZMOExport
        {
            FPS = zmo.FPS,
            FrameCount = zmo.Frames?.Length ?? 0,
            ChannelCount = zmo.Channels?.Length ?? 0,
            Channels = new List<ZMOChannelExport>(),
            Frames = new List<ZMOFrameExport>()
        };

        if (zmo.Channels != null)
        {
            for (int i = 0; i < zmo.Channels.Length; i++)
            {
                var channel = zmo.Channels[i];

                export.Channels.Add(new ZMOChannelExport
                {
                    Index = i,
                    Type = channel.Type.ToString(),
                    TypeValue = (int)channel.Type,
                    ID = channel.ID
                });
            }
        }

        if (zmo.Frames != null)
        {
            for (int frameIndex = 0; frameIndex < zmo.Frames.Length; frameIndex++)
            {
                var frame = zmo.Frames[frameIndex];

                var frameExport = new ZMOFrameExport
                {
                    Index = frameIndex,
                    Channels = new List<ZMOValueExport>()
                };

                for (int channelIndex = 0; channelIndex < zmo.Channels.Length; channelIndex++)
                {
                    var channel = zmo.Channels[channelIndex];
                    var data = frame.Channels[channelIndex];

                    frameExport.Channels.Add(new ZMOValueExport
                    {
                        Index = channelIndex,
                        Value = GetChannelValue(channel.Type, data)
                    });
                }

                export.Frames.Add(frameExport);
            }
        }

        string json = JsonConvert.SerializeObject(
            export,
            Formatting.Indented);

        File.WriteAllText(outputPath, json);

        AssetDatabase.Refresh();

        Debug.Log($"ZMO exported to: {outputPath}");
    }

    private static object GetChannelValue(ZMO.ChannelType type, ZMO.Frame.Channel data)
    {
        switch (type)
        {
            case ZMO.ChannelType.Position:
                return new
                {
                    x = data.Position.x,
                    y = data.Position.y,
                    z = data.Position.z
                };

            case ZMO.ChannelType.Rotation:
                return new
                {
                    x = data.Rotation.x,
                    y = data.Rotation.y,
                    z = data.Rotation.z,
                    w = data.Rotation.w
                };

            case ZMO.ChannelType.Normal:
                return new
                {
                    x = data.Normal.x,
                    y = data.Normal.y,
                    z = data.Normal.z
                };

            case ZMO.ChannelType.Alpha:
                return data.Alpha;

            case ZMO.ChannelType.UV0:
                return new
                {
                    x = data.UV0.x,
                    y = data.UV0.y
                };

            case ZMO.ChannelType.UV1:
                return new
                {
                    x = data.UV1.x,
                    y = data.UV1.y
                };

            case ZMO.ChannelType.UV2:
                return new
                {
                    x = data.UV2.x,
                    y = data.UV2.y
                };

            case ZMO.ChannelType.UV3:
                return new
                {
                    x = data.UV3.x,
                    y = data.UV3.y
                };

            case ZMO.ChannelType.TextureAnimation:
                return data.TextureAnimation;

            case ZMO.ChannelType.Scale:
                return data.Scale;

            default:
                return null;
        }
    }


    [Serializable]
    private class ZMOExport
    {
        public int FPS;
        public int FrameCount;
        public int ChannelCount;
        public List<ZMOChannelExport> Channels;
        public List<ZMOFrameExport> Frames;
    }

    [Serializable]
    private class ZMOChannelExport
    {
        public int Index;
        public string Type;
        public int TypeValue;
        public int ID;
    }

    [Serializable]
    private class ZMOFrameExport
    {
        public int Index;
        public List<ZMOValueExport> Channels;
    }

    [Serializable]
    private class ZMOValueExport
    {
        public int Index;
        public object Value;
    }
}
