using System.IO;
using UnityEngine;
using UnityRose.RoseFile;
using System.Collections.Generic;
using UnityEditor;

namespace UnityRose.Formats
{
    /// <summary>
    /// ZMO class.
    /// </summary>
    public class ZMO
    {
        /// <summary>
        /// Channel type.
        /// </summary>
        public enum ChannelType
        {
            /// <summary>
            /// Position.
            /// </summary>
            Position = 1 << 1,

            /// <summary>
            /// Rotation.
            /// </summary>
            Rotation = 1 << 2,

            /// <summary>
            /// Normal.
            /// </summary>
            Normal = 1 << 3,

            /// <summary>
            /// Alpha.
            /// </summary>
            Alpha = 1 << 4,

            /// <summary>
            /// Texture coordinate.
            /// </summary>
            UV0 = 1 << 5,

            /// <summary>
            /// Texture coordinate.
            /// </summary>
            UV1 = 1 << 6,

            /// <summary>
            /// Texture coordinate.
            /// </summary>
            UV2 = 1 << 7,

            /// <summary>
            /// Texture coordinate.
            /// </summary>
            UV3 = 1 << 8,

            /// <summary>
            /// Texture animation.
            /// </summary>
            TextureAnimation = 1 << 9,

            /// <summary>
            /// Scale.
            /// </summary>
            Scale = 1 << 10
        }

        /// <summary>
        /// Channel structure.
        /// </summary>
        public struct Channel
        {
            #region Member Declarations

            /// <summary>
            /// Gets or sets the type.
            /// </summary>
            /// <value>The type.</value>
            public ChannelType Type { get; set; }

            /// <summary>
            /// Gets or sets the ID.
            /// </summary>
            /// <value>The ID.</value>
            public int ID { get; set; }

            #endregion
        };

        /// <summary>
        /// Frame structure.
        /// </summary>
        public struct Frame
        {
            /// <summary>
            /// Channel structure.
            /// </summary>
            public struct Channel
            {
                #region Member Declarations

                /// <summary>
                /// Gets or sets the position.
                /// </summary>
                /// <value>The position.</value>
                public Vector3 Position { get; set; }

                /// <summary>
                /// Gets or sets the rotation.
                /// </summary>
                /// <value>The rotation.</value>
                public Quaternion Rotation { get; set; }

                /// <summary>
                /// Gets or sets the normal.
                /// </summary>
                /// <value>The normal.</value>
                public Vector3 Normal { get; set; }

                /// <summary>
                /// Gets or sets the alpha.
                /// </summary>
                /// <value>The alpha.</value>
                public float Alpha { get; set; }

                /// <summary>
                /// Gets or sets the texture coordinate.
                /// </summary>
                /// <value>The texture coordinate.</value>
                public Vector2 UV0 { get; set; }

                /// <summary>
                /// Gets or sets the texture coordinate.
                /// </summary>
                /// <value>The texture coordinate.</value>
                public Vector2 UV1 { get; set; }

                /// <summary>
                /// Gets or sets the texture coordinate.
                /// </summary>
                /// <value>The texture coordinate.</value>
                public Vector2 UV2 { get; set; }

                /// <summary>
                /// Gets or sets the texture coordinate.
                /// </summary>
                /// <value>The texture coordinate.</value>
                public Vector2 UV3 { get; set; }

                /// <summary>
                /// Gets or sets the texture animation.
                /// </summary>
                /// <value>The texture animation.</value>
                public float TextureAnimation { get; set; }

                /// <summary>
                /// Gets or sets the scale.
                /// </summary>
                /// <value>The scale.</value>
                public float Scale { get; set; }

                #endregion
            };

            #region Member Declarations

            /// <summary>
            /// Gets or sets the channels.
            /// </summary>
            /// <value>The channels.</value>
            public Channel[] Channels { get; set; }

            #endregion
        };


        #region Member Declarations

        public AnimationClip clip { get; set; }
        /// <summary>
        /// Gets or sets the FPS.
        /// </summary>
        /// <value>The FPS.</value>
        public int FPS { get; set; }

        /// <summary>
        /// Gets or sets the channels.
        /// </summary>
        /// <value>The channels.</value>
        public Channel[] Channels { get; set; }

        /// <summary>
        /// Gets or sets the frames.
        /// </summary>
        /// <value>The frames.</value>
        public Frame[] Frames { get; set; }

        private int channelCount;
        private int frameCount;

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="ZMO"/> class.
        /// </summary>
        public ZMO()
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ZMO"/> class.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="camera">if set to <c>true</c> [camera].</param>
        /// <param name="divide">if set to <c>true</c> [divide].</param>
        public ZMO(string filePath, bool camera = false, bool divide = true)
        {
            Load(filePath, camera, divide);
        }

        private Vector3 GetContinuousEulerRotation(int channelIndex, int frameIndex)
        {
            Vector3 current = Frames[frameIndex].Channels[channelIndex].Rotation.eulerAngles;

            current.x = Mathf.DeltaAngle(0f, current.x);
            current.y = Mathf.DeltaAngle(0f, current.y);
            current.z = Mathf.DeltaAngle(0f, current.z);

            if (frameIndex > 0)
            {
                Vector3 previous = GetContinuousEulerRotation(channelIndex, frameIndex - 1);

                current.x = previous.x + Mathf.DeltaAngle(previous.x, current.x);
                current.y = previous.y + Mathf.DeltaAngle(previous.y, current.y);
                current.z = previous.z + Mathf.DeltaAngle(previous.z, current.z);
            }

            return current;
        }

        public AnimationClip buildAnimationClip(ZMD skeleton)
        {
            var clip = new AnimationClip();

            for (var i = 0; i < channelCount; ++i)
            {
                if (Channels[i].ID < 0 || Channels[i].ID >= skeleton.nBones)
                {
                    Debug.LogWarning("Found invalid channel index.");
                    continue;
                }

                string cbn = skeleton.bones[Channels[i].ID].Path;

                if (Channels[i].Type == ChannelType.Rotation)
                {
                    var curvex = new AnimationCurve();
                    var curvey = new AnimationCurve();
                    var curvez = new AnimationCurve();

                    for (var j = 0; j < frameCount; ++j)
                    {
                        float time = (float)j / FPS;
                        Vector3 rotation = GetContinuousEulerRotation(i, j);

                        curvex.AddKey(time, rotation.x);
                        curvey.AddKey(time, rotation.y);
                        curvez.AddKey(time, rotation.z);
                    }

                    clip.SetCurve(cbn, typeof(Transform), "localEulerAnglesRaw.x", curvex);
                    clip.SetCurve(cbn, typeof(Transform), "localEulerAnglesRaw.y", curvey);
                    clip.SetCurve(cbn, typeof(Transform), "localEulerAnglesRaw.z", curvez);
                }
                else if (Channels[i].Type == ChannelType.Position)
                {
                    var curvex = new AnimationCurve();
                    var curvey = new AnimationCurve();
                    var curvez = new AnimationCurve();

                    for (var j = 0; j < frameCount; ++j)
                    {
                        float time = (float)j / FPS;
                        Frame.Channel chan = Frames[j].Channels[i];

                        curvex.AddKey(time, chan.Position.x);
                        curvey.AddKey(time, chan.Position.y);
                        curvez.AddKey(time, chan.Position.z);
                    }

                    clip.SetCurve(cbn, typeof(Transform), "localPosition.x", curvex);
                    clip.SetCurve(cbn, typeof(Transform), "localPosition.y", curvey);
                    clip.SetCurve(cbn, typeof(Transform), "localPosition.z", curvez);
                }
            }

            return clip;
        }

        public AnimationClip BuildSkeletonAnimationClip(RoseSkeletonData skeleton)
        {
            var clip = new AnimationClip();
            clip.legacy = false;

            for (var i = 0; i < channelCount; ++i)
            {
                var boneId = Channels[i].ID;

                if (skeleton == null || boneId < 0 || boneId >= skeleton.bones.Count)
                {
                    Debug.LogWarning("Found invalid channel index or null skeleton.");
                    continue;
                }

                var cbn = skeleton.bones[boneId].name;
                int curBoneIdx = boneId;

                while (true)
                {
                    if (curBoneIdx == 0)
                    {
                        break;
                    }

                    curBoneIdx = skeleton.bones[curBoneIdx].parent;

                    if (curBoneIdx < 0)
                    {
                        break;
                    }

                    cbn = skeleton.bones[curBoneIdx].name + "/" + cbn;
                }

                if (Channels[i].Type == ChannelType.Rotation)
                {
                    var curvex = new AnimationCurve();
                    var curvey = new AnimationCurve();
                    var curvez = new AnimationCurve();

                    for (var j = 0; j < frameCount; ++j)
                    {
                        float time = (float)j / FPS;
                        Vector3 rotation = GetContinuousEulerRotation(i, j);

                        curvex.AddKey(time, rotation.x);
                        curvey.AddKey(time, rotation.y);
                        curvez.AddKey(time, rotation.z);
                    }

                    clip.SetCurve(cbn, typeof(Transform), "localEulerAnglesRaw.x", curvex);
                    clip.SetCurve(cbn, typeof(Transform), "localEulerAnglesRaw.y", curvey);
                    clip.SetCurve(cbn, typeof(Transform), "localEulerAnglesRaw.z", curvez);
                }
                else if (Channels[i].Type == ChannelType.Position)
                {
                    var curvex = new AnimationCurve();
                    var curvey = new AnimationCurve();
                    var curvez = new AnimationCurve();

                    for (var j = 0; j < frameCount; ++j)
                    {
                        float time = (float)j / FPS;
                        Frame.Channel chan = Frames[j].Channels[i];

                        curvex.AddKey(time, chan.Position.x);
                        curvey.AddKey(time, chan.Position.y);
                        curvez.AddKey(time, chan.Position.z);
                    }

                    clip.SetCurve(cbn, typeof(Transform), "localPosition.x", curvex);
                    clip.SetCurve(cbn, typeof(Transform), "localPosition.y", curvey);
                    clip.SetCurve(cbn, typeof(Transform), "localPosition.z", curvez);
                }
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            return clip;
        }

        public AnimationClip buildAnimationClip(string objectName, AnimationClip clip)
        {
            for (var i = 0; i < channelCount; ++i)
            {
                if (Channels[i].ID < 0)
                {
                    Debug.LogWarning("Found invalid channel index.");
                    continue;
                }

                string cbn = objectName;

                if (Channels[i].Type == ChannelType.Rotation)
                {
                    var curvex = new AnimationCurve();
                    var curvey = new AnimationCurve();
                    var curvez = new AnimationCurve();

                    for (var j = 0; j < frameCount; ++j)
                    {
                        float time = (float)j / FPS;
                        Vector3 rotation = GetContinuousEulerRotation(i, j);

                        curvex.AddKey(time, rotation.x);
                        curvey.AddKey(time, rotation.y);
                        curvez.AddKey(time, rotation.z);
                    }

                    clip.SetCurve(cbn, typeof(Transform), "localEulerAnglesRaw.x", curvex);
                    clip.SetCurve(cbn, typeof(Transform), "localEulerAnglesRaw.y", curvey);
                    clip.SetCurve(cbn, typeof(Transform), "localEulerAnglesRaw.z", curvez);
                }
                else if (Channels[i].Type == ChannelType.Position)
                {
                    var curvex = new AnimationCurve();
                    var curvey = new AnimationCurve();
                    var curvez = new AnimationCurve();

                    for (var j = 0; j < frameCount; ++j)
                    {
                        float time = (float)j / FPS;
                        Frame.Channel chan = Frames[j].Channels[i];

                        curvex.AddKey(time, chan.Position.x);
                        curvey.AddKey(time, chan.Position.y);
                        curvez.AddKey(time, chan.Position.z);
                    }

                    clip.SetCurve(cbn, typeof(Transform), "localPosition.x", curvex);
                    clip.SetCurve(cbn, typeof(Transform), "localPosition.y", curvey);
                    clip.SetCurve(cbn, typeof(Transform), "localPosition.z", curvez);
                }
            }

            return clip;
        }



        /// <summary>
        /// Loads the specified file.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="camera">if set to <c>true</c> [camera].</param>
        /// <param name="divide">if set to <c>true</c> [divide].</param>
        public void Load(string filePath, bool camera, bool divide)
        {
            FileHandler fh = new FileHandler(filePath, FileHandler.FileOpenMode.Reading, null);

            fh.Seek(8, SeekOrigin.Begin);

            FPS = fh.Read<int>();
            frameCount = fh.Read<int>();
            channelCount = fh.Read<int>();

            Channels = new Channel[channelCount];


            for (int i = 0; i < channelCount; i++)
            {
                Channels[i] = new Channel()
                {
                    Type = (ChannelType)fh.Read<int>(),
                    ID = fh.Read<int>()
                };

            }


            Frames = new Frame[frameCount];

            for (int i = 0; i < frameCount; i++)
            {
                Frames[i] = new Frame()
                {
                    Channels = new Frame.Channel[channelCount]
                };

                for (int j = 0; j < channelCount; j++)
                {
                    switch (Channels[j].Type)
                    {
                        case ChannelType.Position:
                            {
                                Frames[i].Channels[j].Position = Utils.RoseToUnityPosition(fh.Read<Vector3>());

                                if (camera && (j == 0 || j == 1))
                                    Frames[i].Channels[j].Position = Utils.RoseToUnityPosition(fh.Read<Vector3>() + new Vector3(52000.0f, 52000.0f, 0.0f)) / 100.0f;
                                else if (divide)
                                    Frames[i].Channels[j].Position /= 100.0f;
                            }
                            break;
                        case ChannelType.Rotation:
                            {
                                Frames[i].Channels[j].Rotation = Utils.R2URotation2(new Quaternion()
                                {
                                    w = fh.Read<float>(),
                                    x = fh.Read<float>(),
                                    y = fh.Read<float>(),
                                    z = fh.Read<float>()
                                });
                            }
                            break;
                        case ChannelType.Normal:
                            Frames[i].Channels[j].Normal = Utils.RoseToUnityVector(fh.Read<Vector3>());
                            break;
                        case ChannelType.Alpha:
                            Frames[i].Channels[j].Alpha = fh.Read<float>();
                            break;
                        case ChannelType.UV0:
                            Frames[i].Channels[j].UV0 = fh.Read<Vector2>();
                            break;
                        case ChannelType.UV1:
                            Frames[i].Channels[j].UV1 = fh.Read<Vector2>();
                            break;
                        case ChannelType.UV2:
                            Frames[i].Channels[j].UV2 = fh.Read<Vector2>();
                            break;
                        case ChannelType.UV3:
                            Frames[i].Channels[j].UV3 = fh.Read<Vector2>();
                            break;
                        case ChannelType.TextureAnimation:
                            Frames[i].Channels[j].TextureAnimation = fh.Read<float>();
                            break;
                        case ChannelType.Scale:
                            Frames[i].Channels[j].Scale = fh.Read<float>();
                            break;
                    }
                }
            }

            fh.Close();
        }
    }
}