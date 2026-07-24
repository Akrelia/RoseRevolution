#if UNITY_EDITOR

using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityRose.Formats;
using UnityRose.Import;
using RevolutionShared.Rose.Data;
using System.Linq;

namespace UnityRose.ImportEditor
{
    public class RoseEquipmentBaker
    {
        private readonly ImportPaths.EquipmentImportContext context;

        public RoseEquipmentBaker()
        {
            context = new ImportPaths.EquipmentImportContext();
        }

        public GameObject BakeEquipment(string name, BodyPartType bodyPart, ZSC zsc, string zscPath, int id)
        {
            if (id < 0 || id >= zsc.Objects.Count)
                return null;

            var zscObject = zsc.Objects[id];

            if (zscObject.Models == null || zscObject.Models.Count == 0)
                return null;

            GameObject root = new GameObject(name);

            int builtParts = 0;

            foreach (var model in zscObject.Models)
            {
                if (BuildPart(root.transform, bodyPart, model.DummyIndex, model.ModelID, model.TextureID, zsc, zscPath))
                    builtParts++;
            }

            if (builtParts == 0)
            {
                Object.DestroyImmediate(root);
                return null;
            }

            var prefab = SavePrefab(root, name);

            Object.DestroyImmediate(root);

            return prefab;
        }

        private bool BuildPart(Transform parent, BodyPartType bodyPart, ZSC.DummyType dummy, int modelID, int textureID, ZSC zsc, string zscPath)
        {
            if (modelID < 0 || modelID >= zsc.Models.Count)
                return false;

            string zmsPath = zsc.Models[modelID];

            var mesh = ROSEEditorBaker.BakeMesh(zmsPath, context);

            if (mesh == null || mesh.vertexCount == 0)
                return false;

            var material = ROSEEditorBaker.BakeMaterial(textureID, zsc, zscPath, context);

            if (material == null)
                return false;


            GameObject obj = new GameObject(bodyPart.ToString());
            obj.transform.SetParent(parent, false);


            var attachment = obj.AddComponent<RoseAttachment>();
         //   attachment.dummy = dummy;

            if (mesh.boneWeights != null && mesh.boneWeights.Length > 0)
            {
                var renderer = obj.AddComponent<SkinnedMeshRenderer>();

                renderer.sharedMesh = mesh;
                renderer.sharedMaterial = material;

                renderer.updateWhenOffscreen = true;
            }
            else
            {
                var filter = obj.AddComponent<MeshFilter>();
                filter.sharedMesh = mesh;

                var renderer = obj.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
            }


            return true;
        }

        private GameObject SavePrefab(GameObject obj, string name)
        {
            string path = $"{context.Prefab}/{name}.prefab";

            Utils.EnsureFolder(path);

            return PrefabUtility.SaveAsPrefabAsset(obj, path);
        }
    }
}

#endif