using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityRose.Formats;

namespace UnityRose.Import
{
    /// <summary>
    /// Where to find the raw ROSE 3DDATA folder. Set this once at startup.
    /// </summary>
    public static class RoseDataSource // TODO : Fuse this with the otehr one
    {
        private const string DataPathKey = "ROSE_DataPath";

        public static string DataPath
        {
            get => EditorPrefs.GetString(DataPathKey, "");
            set => EditorPrefs.SetString(DataPathKey, value);
        }
    }

    public static class RoseMeshImporter
    {
        private static readonly Dictionary<string, Mesh> _cache = new();

        public static Mesh Import(string rosePath)
        {
            var fullPath = Utils.CombinePath(RoseDataSource.DataPath, rosePath);

            if (!File.Exists(fullPath))
            {
                Debug.LogWarning("Could not find referenced mesh: " + fullPath);
                return null;
            }

            var mesh = new ZMS(fullPath).getMesh();
            mesh.name = Path.GetFileNameWithoutExtension(rosePath);

            return mesh;
        }

        public static void ClearCache() => _cache.Clear();
    }

    public static class RoseSkeletonImporter
    {
        private static readonly Dictionary<string, RoseSkeletonData> _cache = new();

        public static RoseSkeletonData Import(string rosePath)
        {
            if (_cache.TryGetValue(rosePath, out var cached) && cached != null)
                return cached;

            var fullPath = Utils.CombinePath(RoseDataSource.DataPath, rosePath);
            
            if (!File.Exists(fullPath))
            {
                Debug.LogWarning("Could not find referenced skeleton: " + fullPath);
            
                return null;
            }

            var zmd = new ZMD(fullPath);
            var skel = ScriptableObject.CreateInstance<RoseSkeletonData>();
            skel.name = Path.GetFileNameWithoutExtension(rosePath);

            foreach (var bone in zmd.bones)
            {
                skel.bones.Add(new RoseSkeletonData.Bone
                {
                    name = bone.Name,
                    parent = bone.ParentID,
                    translation = bone.Position,
                    rotation = bone.Rotation
                });
            }

            foreach (var dummy in zmd.dummies)
            {
                skel.dummies.Add(new RoseSkeletonData.Bone
                {
                    name = dummy.Name,
                    parent = dummy.ParentID,
                    translation = dummy.Position,
                    rotation = dummy.Rotation
                });
            }

            _cache[rosePath] = skel;
            return skel;
        }

        public static void ClearCache() => _cache.Clear();
    }

    public static class RoseAnimationImporter
    {
        public static AnimationClip Import(string rosePath, RoseSkeletonData skeleton)
        {
            var fullPath = Utils.CombinePath(RoseDataSource.DataPath, rosePath);
            if (!System.IO.File.Exists(fullPath))
            {
                Debug.LogWarning("Could not find referenced animation: " + fullPath);
                return null;
            }

            var clip = new ZMO(fullPath).BuildSkeletonAnimationClip(skeleton);
            clip.name = Path.GetFileNameWithoutExtension(rosePath);
            return clip;
        }
    }

    public static class RoseTextureImporter
    {
        private static readonly Dictionary<string, Texture2D> _cache = new();

        public static Texture2D Import(string rosePath)
        {
            if (_cache.TryGetValue(rosePath, out var cached) && cached != null)
                return cached;

            var fullPath = Utils.ResolvePathWithCorrectCase(RoseDataSource.DataPath, rosePath);
            if (!System.IO.File.Exists(fullPath))
            {
                Debug.LogWarning("Could not find referenced texture: " + fullPath);
                return null;
            }

            var tex = RoseDdsLoader.LoadFromFile(fullPath);
            if (tex != null)
            {
                tex.name = Path.GetFileNameWithoutExtension(rosePath);
                _cache[rosePath] = tex;
            }
            return tex;
        }

        public static void ClearCache() => _cache.Clear();
    }

    public static class RoseMaterialImporter
    {
        public static Material Build(string texturePath, string shaderName = "Universal Render Pipeline/Unlit")
        {
            var shader = Shader.Find(shaderName);
         
            if (shader == null)
            {
                Debug.LogError($"RoseMaterialImporter: shader '{shaderName}' not found. " + "Add it to Project Settings > Graphics > Always Included Shaders " + "or it will be stripped from builds and Shader.Find will fail there.");
            
                return null;
            }

            var mat = new Material(shader);
            var tex = RoseTextureImporter.Import(texturePath);
        
            if (tex != null)
                mat.SetTexture("_BaseMap", tex);

            return mat;
        }
    }

    public class RoseZscImporter
    {
        public readonly ZSC zsc;
        private readonly Dictionary<int, RoseCharPartData> _partCache = new();
        private readonly Dictionary<int, RoseMapObjectData> _objectCache = new();
        private readonly Dictionary<int, Material> _materialCache = new();

        public RoseZscImporter(string rosePath)
        {
            var fullPath = Utils.CombinePath(RoseDataSource.DataPath, rosePath);
            zsc = new ZSC(fullPath);
        }

        public Mesh ImportMesh(int meshIdx) => RoseMeshImporter.Import(zsc.Models[meshIdx]);

        public Material ImportMaterial(int materialIdx)
        {
            if (_materialCache.TryGetValue(materialIdx, out var cached))
                return cached;

            var mat = RoseMaterialImporter.Build(zsc.Textures[materialIdx].Path);
            _materialCache[materialIdx] = mat;
            return mat;
        }

        public RoseCharPartData ImportPart(int partIdx)
        {
            if (_partCache.TryGetValue(partIdx, out var cached))
                return cached;

            var zscObj = zsc.Objects[partIdx];
            var data = ScriptableObject.CreateInstance<RoseCharPartData>();

            foreach (var part in zscObj.Models)
            {
                data.models.Add(new Model
                {
                    mesh = ImportMesh(part.ModelID),
                    material = ImportMaterial(part.TextureID),
                    boneIndex = (short)part.BoneIndex   // Akima : was -1 hardcoded
                });
            }

            _partCache[partIdx] = data;
            return data;
        }

        public RoseMapObjectData ImportObject(int objectIdx)
        {
            if (_objectCache.TryGetValue(objectIdx, out var cached))
                return cached;

            var zscObj = zsc.Objects[objectIdx];
            var data = ScriptableObject.CreateInstance<RoseMapObjectData>();

            foreach (var part in zscObj.Models)
            {
                data.subObjects.Add(new RoseMapObjectData.SubObject
                {
                    mesh = ImportMesh(part.ModelID),
                    material = ImportMaterial(part.TextureID),
                    animation = null,
                    parent = part.Parent,
                    position = part.Position / 100,
                    rotation = part.Rotation,
                    scale = part.Scale
                });
            }

            _objectCache[objectIdx] = data;
            return data;
        }
    }

    public class RoseNpcImporter
    {
        public readonly CHR chr;
        private readonly RoseZscImporter zsc;
        private readonly Dictionary<int, NPCEntitySO> _cache = new();

        public RoseNpcImporter()
        {
            chr = new CHR(Utils.CombinePath(RoseDataSource.DataPath, "3DDATA/NPC/LIST_NPC.CHR"));
            zsc = new RoseZscImporter("3DDATA/NPC/PART_NPC.ZSC");
        }

        public NPCEntitySO ImportNpc(int npcIdx, string displayName = null)
        {
            if (!chr.Characters[npcIdx].IsEnabled)
                return null;

            if (_cache.TryGetValue(npcIdx, out var cached))
                return cached;

            var chrObj = chr.Characters[npcIdx];
            var npc = ScriptableObject.CreateInstance<NPCEntitySO>();
            npc.monsterData.id = npcIdx;
            npc.monsterData.displayName = displayName ?? npcIdx.ToString();
            npc.skeleton = RoseSkeletonImporter.Import(chr.SkeletonFiles[chrObj.ID]);

            foreach (var zscPart in chrObj.Objects)
                npc.parts.Add(zsc.ImportPart(zscPart.Object));

            foreach (var zscMotion in chrObj.Animations)
            {
                if (zscMotion.Animation < 0)
                    continue;

                var anim = RoseAnimationImporter.Import(chr.MotionFiles[zscMotion.Animation], npc.skeleton);
                while (npc.animations.Count <= (int)zscMotion.Type)
                    npc.animations.Add(null);
                npc.animations[(int)zscMotion.Type] = anim;
            }

            _cache[npcIdx] = npc;
            return npc;
        }

        public IEnumerable<NPCEntitySO> ImportAll()
        {
            for (var i = 0; i < chr.Characters.Count; ++i)
            {
                var npc = ImportNpc(i);
                if (npc != null)
                    yield return npc;
            }
        }
    }
}