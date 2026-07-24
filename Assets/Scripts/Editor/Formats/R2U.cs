#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using System.IO;
using UnityEditor;

namespace UnityRose.Formats
{
    public class R2U
    {
        public static AnimationClip GetClip(string zmoPath, ZMD skeleton, string name)
        {
            DirectoryInfo zmoDir = new DirectoryInfo(zmoPath);
            string unityPath = zmoDir.FullName.Replace(zmoDir.Name, name) + ".anim";

            AnimationClip clip = (AnimationClip)Utils.LoadAsset(unityPath, ".anim");

            if (clip != null && clip.legacy)
            {
                Debug.Log($"Deleting legacy clip: {clip.name}");

                AssetDatabase.DeleteAsset(unityPath);
                clip = null;
            }

            if (clip == null)
            {
                clip = new ZMO(zmoPath).buildAnimationClip(skeleton);
                clip.name = name;
                clip.legacy = false;

                Debug.Log($"Before save: {clip.name} legacy={clip.legacy}");

                clip = (AnimationClip)Utils.SaveReloadAsset(clip, unityPath, ".anim");
            }

            return clip;
        }

        public static Mesh GetMesh(string zmsPath)
        {
            Mesh mesh = (Mesh)Utils.LoadAsset(zmsPath);
            if (mesh == null)
            {
                mesh = new ZMS(zmsPath).getMesh();
                mesh = (Mesh)Utils.SaveReloadAsset(mesh, zmsPath);
            }

            return mesh;
        }

    }
}

#endif