using System.IO;
using System;
using UnityEditor;
using UnityEngine;
using UnityRose.ImportEditor;
using UnityRose.Import;
using static UnityRose.Import.GameDataPaths;
using UnityRose;
using static PlasticGui.WorkspaceWindow.Merge.MergeInProgress;
using UnityRose.Formats;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// Rose effect importer.
/// </summary>
public class RoseEffectImporter : MonoBehaviour
{
    /// <summary>
    /// Import all effects.
    /// </summary>
    /// <param name="dataPath">Data path.</param>
    public static void ImportEffects(string dataPath, Shader shader)
    {
        var database = RoseImporter.GetOrCreateDatabase<EffectDatabase>();
        var stb = ResourceManager.Instance.fileEffectSTB;

        EditorUtils.EnsureFolder(Path.Combine(GameDataPaths.Effects.Prefabs, "dummy.asset"));
        EditorUtils.EnsureFolder(Path.Combine(GameDataPaths.Effects.Materials, "dummy.asset"));
        EditorUtils.EnsureFolder(Path.Combine(GameDataPaths.Effects.Textures, "dummy.asset"));
        EditorUtils.EnsureFolder(Path.Combine(GameDataPaths.Effects.Meshes, "dummy.asset"));

        AssetDatabase.StartAssetEditing();

        int effectCount = 1200;

        try
        {
            for (int i = 1; i < effectCount; i++)
            {
                if (string.IsNullOrEmpty(stb.Cells[i][1]))
                {
                    continue;
                }

                var file = Path.Combine(dataPath, stb.Cells[i][2]);

                try
                {
                    if (i == 1177)
                    {
                        Debug.Log("jsuioada");
                    }

                    EFT eft = LoadEFT(file);

                    if (eft == null)
                    {
                        continue;
                    }

                    GameObject prefab = BuildEffect(eft, dataPath, shader);

                    string path = $"{GameDataPaths.Effects.Prefabs}/{Path.GetFileName(file)}.prefab";

                    PrefabUtility.SaveAsPrefabAsset(prefab, path);

                    GameObject.DestroyImmediate(prefab);
                }

                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to import effect {file}: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        for (int i = 1; i < effectCount; i++)
        {
            if (string.IsNullOrEmpty(stb.Cells[i][1]))
            {
                continue;
            }

            string file = Path.Combine(dataPath, stb.Cells[i][2]);
            string path = $"{GameDataPaths.Effects.Prefabs}/{Path.GetFileName(file)}.prefab";

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null)
            {
                Debug.LogWarning($"Failed to load effect prefab: {path}");

                continue;
            }

            var entry = new EffectDatabaseEntry()
            {
                id = i,
                prefab = prefab
            };

            int index = database.entries.FindIndex(x => x.id == entry.id);

            if (index >= 0)
            {
                database.entries[index] = entry;
            }

            else
            {
                database.entries.Add(entry);
            }
        }

        EditorUtility.SetDirty(database);

        AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// Build the effect as a collection of Particle System.
    /// </summary>
    /// <param name="eft">Effect file.</param>
    /// <param name="dataPath">Data path.</param>
    /// <returns>Particle system.</returns>
    private static GameObject BuildEffect(EFT eft, string dataPath, Shader shader)
    {
        EffectImportContext context = new EffectImportContext();

        GameObject root = new(eft.Name);

        foreach (var system in eft.Systems)
        {
            string ptlPath = Path.Combine(dataPath, system.PtlFile);

            PTL ptl = LoadPTL(ptlPath);

            if (ptl == null)
            {
                continue;
            }

            foreach (var emitter in ptl.Emitters)
            {
                GameObject obj = BuildEmitter(emitter, dataPath);

                obj.transform.SetParent(root.transform);
                obj.transform.localPosition = new Vector3(emitter.MaxEmitRadius.X, emitter.MaxEmitRadius.Z, emitter.MaxEmitRadius.Y) / 100F;
                obj.transform.localRotation = system.Rotation;

            }
        }

        foreach (var animation in eft.Animations)
        {
            GameObject obj = BuildAnimation(animation, context, shader, dataPath);

            obj.transform.SetParent(root.transform);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
        }

        return root;
    }

    /// <summary>
    /// Build a single emitter as a Particle System.
    /// </summary>
    /// <param name="emitter">Emitter.</param>
    /// <param name="dataPath">Data path.</param>
    /// <returns>Particle system.</returns>
    private static GameObject BuildEmitter(PTL.Emitter emitter, string dataPath)
    {
        GameObject obj = new(emitter.Name);

        ParticleSystem particle = obj.AddComponent<ParticleSystem>();
        ParticleSystemRenderer renderer = obj.GetComponent<ParticleSystemRenderer>();

        var main = particle.main;

        main.maxParticles = (int)emitter.ParticleNumber;
        main.loop = emitter.LoopCount == 0;
        main.startLifetime = new ParticleSystem.MinMaxCurve(emitter.LifeTime.X, emitter.LifeTime.Y);
        main.startSpeed = 0;

        main.startRotationX = new ParticleSystem.MinMaxCurve(emitter.MinSpawnDir.X, emitter.MaxSpawnDir.X);
        main.startRotationY = new ParticleSystem.MinMaxCurve(emitter.MinSpawnDir.Y, emitter.MaxSpawnDir.Y);
        main.startRotationZ = new ParticleSystem.MinMaxCurve(emitter.MinSpawnDir.Z, emitter.MaxSpawnDir.Z);

        var emission = particle.emission;

        emission.rateOverTime = new ParticleSystem.MinMaxCurve(emitter.EmitRate.X, emitter.EmitRate.Y);

        var shape = particle.shape;

        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.001F;

        particle.gameObject.transform.position = new Vector3(emitter.MaxEmitRadius.X, emitter.MaxEmitRadius.Z, emitter.MaxEmitRadius.Y) / 100F;

        var velocityModule = particle.velocityOverLifetime;

        velocityModule.enabled = true;

        velocityModule.x = new ParticleSystem.MinMaxCurve(emitter.MinGravity.X, emitter.MaxGravity.X);
        velocityModule.y = new ParticleSystem.MinMaxCurve(emitter.MinGravity.Y, emitter.MaxGravity.Y);
        velocityModule.z = new ParticleSystem.MinMaxCurve(emitter.MinGravity.Z, emitter.MaxGravity.Z);

        var textureSheetAnimation = particle.textureSheetAnimation;

        textureSheetAnimation.enabled = true;
        textureSheetAnimation.mode = ParticleSystemAnimationMode.Grid;
        textureSheetAnimation.numTilesX = (int)emitter.TextureWidth;
        textureSheetAnimation.numTilesY = (int)emitter.TextureHeight;

        var infoGroups = emitter.Infos.GroupBy(x => x.Type).ToDictionary(x => x.Key, x => x.OrderBy(y => y.TimeRange.X).ToList());

        foreach (var group in infoGroups)
        {
            var type = group.Key;
            var info = group.Value;

            switch (type)
            {
                case PTL.AnimType.SIZE:
                    {
                        BuildSize(particle, info);
                        break;
                    }

                case PTL.AnimType.COLOR:
                    {
                        BuildColor(particle, info);
                        break;
                    }

                case PTL.AnimType.RED:
                    {
                        BuildRed(particle, info);
                        break;
                    }

                case PTL.AnimType.GREEN:
                    {
                        BuildGreen(particle, info);
                        break;
                    }

                case PTL.AnimType.BLUE:
                    {
                        BuildBlue(particle, info);
                        break;
                    }

                case PTL.AnimType.ALPHA:
                    {
                        BuildAlpha(particle, info);
                        break;
                    }

                case PTL.AnimType.VELOCITY:
                    {
                        BuildVelocity(particle, info);
                        break;
                    }

                case PTL.AnimType.VELOCITYX:
                    {
                        BuildVelocityX(particle, info);
                        break;
                    }

                case PTL.AnimType.VELOCITYY:
                    {
                        BuildVelocityY(particle, info);
                        break;
                    }

                case PTL.AnimType.VELOCITYZ:
                    {
                        BuildVelocityZ(particle, info);
                        break;
                    }

                case PTL.AnimType.TEXTUREINDEX:
                    {
                        BuildTextureIndex(particle, info);
                        break;
                    }

                case PTL.AnimType.EVENTTIMER:
                    {
                        BuildEventTimer(particle, info);
                        break;
                    }

                case PTL.AnimType.NONE:
                    {
                        BuildNone(particle, info);
                        break;
                    }

                case PTL.AnimType.ROTATION:
                    {
                        BuildRotation(particle, info);
                        break;
                    }

                default:
                    {
                        Debug.LogWarning($"Unknown PTL animation type: {type}");
                        break;
                    }
            }
        }

        if (!string.IsNullOrEmpty(emitter.Texture))
        {
            var context = new EffectImportContext();

            Texture2D texture = ROSEEditorBaker.ImportTexture(dataPath + "/" + emitter.Texture, context, true);

            if (texture != null)
            {
                var shader = Shader.Find("Particles/Standard Unlit");

                Material material = ROSEEditorBaker.ImportMaterial(dataPath + "/" + emitter.Texture, texture, shader, context);

                renderer.sharedMaterial = material;
            }
        }

        return obj;
    }

    // ===============================================================================
    // Effect Mesh
    // ===============================================================================

    private static GameObject BuildAnimation(EFT.AnimationEntry entry, EffectImportContext context, Shader shader, string dataPath)
    {
        var mesh = ROSEEditorBaker.ImportMesh(entry.ZmsFile, context);
        var texture = ROSEEditorBaker.ImportTexture(Path.Combine(dataPath, entry.DdsFile), context, true);
        var material = ROSEEditorBaker.ImportMaterial(entry.DdsFile, texture, shader, context);
        var zmo = new ZMO(Path.Combine(dataPath, entry.ZmoFile));

        var effect = new GameObject(Path.GetFileNameWithoutExtension(entry.ZmsFile));

        var particleSystem = effect.AddComponent<ParticleSystem>();
        var particleRenderer = effect.GetComponent<ParticleSystemRenderer>();

        material.SetInt("_Cull", entry.TwoSided == 1 ? (int)UnityEngine.Rendering.CullMode.Off : (int)UnityEngine.Rendering.CullMode.Back);

        var main = particleSystem.main;
        main.loop = false;
        main.playOnAwake = true;
        main.duration = (float)(zmo.Frames.Length - 1) / zmo.FPS;
        main.startLifetime = main.duration;
        main.startSpeed = 0f;
        main.startSize = 1f;
        effect.transform.position = entry.Position;
        effect.transform.rotation = entry.Rotation;
        main.loop = true;
        main.maxParticles = 1;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emission = particleSystem.emission;
        emission.enabled = true;
        emission.rateOverTime = 10f;

        var shape = particleSystem.shape;
        shape.enabled = false;

        particleRenderer.renderMode = ParticleSystemRenderMode.Mesh;
        particleRenderer.alignment = ParticleSystemRenderSpace.World;
        particleRenderer.mesh = mesh;
        particleRenderer.material = material;

        BuildAnimationCurves(particleSystem, zmo);

        return effect;
    }

    private static void BuildAnimationCurves(ParticleSystem particleSystem, ZMO zmo)
    {
        if (zmo.Frames == null || zmo.Frames.Length == 0 || zmo.Channels == null)
        {
            return;
        }

        float duration = (float)(zmo.Frames.Length - 1) / zmo.FPS;

        for (int i = 0; i < zmo.Channels.Length; i++)
        {
            var channel = zmo.Channels[i];

            switch (channel.Type)
            {
                case ZMO.ChannelType.Position:
                    {
                        BuildPositionCurve(particleSystem, zmo, i, duration);
                        break;
                    }

                case ZMO.ChannelType.Rotation:
                    {
                     //   BuildRotationCurve(particleSystem, zmo, i, duration);
                        break;
                    }

                case ZMO.ChannelType.Scale:
                    {
                        BuildScaleCurve(particleSystem, zmo, i, duration);
                        break;
                    }

                case ZMO.ChannelType.Alpha:
                    {
                        BuildAlphaCurve(particleSystem, zmo, i, duration);
                        break;
                    }

                case ZMO.ChannelType.UV0:
                case ZMO.ChannelType.UV1:
                case ZMO.ChannelType.UV2:
                case ZMO.ChannelType.UV3:
                case ZMO.ChannelType.TextureAnimation:
                    {
                        break;
                    }
            }
        }
    }

    private static void BuildScaleCurve(ParticleSystem particleSystem, ZMO zmo, int channelIndex, float duration)
    {
        var size = particleSystem.sizeOverLifetime;

        size.enabled = true;

        var curve = new AnimationCurve();

        for (int i = 0; i < zmo.Frames.Length; i++)
        {
            float time = duration > 0f ? (float)i / (zmo.Frames.Length - 1) : 0f;
            float value = zmo.Frames[i].Channels[channelIndex].Scale;

            curve.AddKey(time, value);
        }

        size.size = new ParticleSystem.MinMaxCurve(1f, curve);
    }

    private static void BuildAlphaCurve(ParticleSystem particleSystem, ZMO zmo, int channelIndex, float duration)
    {
        var color = particleSystem.colorOverLifetime;

        color.enabled = true;

        var alpha = new GradientAlphaKey[zmo.Frames.Length];

        for (int i = 0; i < zmo.Frames.Length; i++)
        {
            float time = duration > 0f ? (float)i / (zmo.Frames.Length - 1) : 0f;
            float value = zmo.Frames[i].Channels[channelIndex].Alpha;

            alpha[i] = new GradientAlphaKey(value, time);
        }

        Gradient gradient = new Gradient();

        gradient.SetKeys(
            new[]
            {
            new GradientColorKey(Color.white, 0f),
            new GradientColorKey(Color.white, 1f)
            },
            alpha);

        color.color = new ParticleSystem.MinMaxGradient(gradient);
    }

    private static void BuildPositionCurve(ParticleSystem particleSystem, ZMO zmo, int channelIndex, float duration)
    {
        if (zmo.Frames.Length < 2)
            return;

        var velocity = particleSystem.rotationOverLifetime;
        velocity.enabled = true;
        velocity.separateAxes = true;

        var x = new AnimationCurve();
        var y = new AnimationCurve();
        var z = new AnimationCurve();

        float deltaTime = 1f / zmo.FPS;

        for (int i = 0; i < zmo.Frames.Length; i++)
        {
            float time = duration > 0f ? (float)i / (zmo.Frames.Length - 1) : 0f;

            Vector3 current = zmo.Frames[i].Channels[channelIndex].Position;

            Vector3 velocityValue;

            if (i == 0)
            {
                Vector3 next = zmo.Frames[i + 1].Channels[channelIndex].Position;
                velocityValue = (next - current) / deltaTime;
            }
            else
            {
                Vector3 previous = zmo.Frames[i - 1].Channels[channelIndex].Position;
                velocityValue = (current - previous) / deltaTime;
            }

            x.AddKey(time, velocityValue.x * Mathf.PI);
            y.AddKey(time, velocityValue.y * Mathf.PI);
            z.AddKey(time, velocityValue.z * Mathf.PI);
        }

        velocity.x = new ParticleSystem.MinMaxCurve(1f, x);
        velocity.y = new ParticleSystem.MinMaxCurve(1f, y);
        velocity.z = new ParticleSystem.MinMaxCurve(1f, z);
    }

    private static void BuildRotationCurve(ParticleSystem particleSystem, ZMO zmo, int channelIndex, float duration)
    {
        var rotation = particleSystem.rotationOverLifetime;
        rotation.enabled = true;
        rotation.separateAxes = true;

        var x = new AnimationCurve();
        var y = new AnimationCurve();
        var z = new AnimationCurve();

        for (int i = 0; i < zmo.Frames.Length; i++)
        {
            float time = duration > 0f ? (float)i / (zmo.Frames.Length - 1) : 0f;

            Vector3 value = zmo.GetContinuousEulerRotationAt(channelIndex, i);

            x.AddKey(time, value.x * Mathf.PI);
            y.AddKey(time, value.y * Mathf.PI);
            z.AddKey(time, value.z * Mathf.PI);
        }

        rotation.x = new ParticleSystem.MinMaxCurve(1f, x);
        rotation.y = new ParticleSystem.MinMaxCurve(1f, y);
        rotation.z = new ParticleSystem.MinMaxCurve(1f, z);
    }

    // ===============================================================================
    // Particles
    // ===============================================================================

    private static void BuildSize(ParticleSystem particle, List<PTL.ParticleInfo> infos)
    {
        if (infos == null || infos.Count == 0)
        {
            return;
        }

        var initial = infos[0];

        var main = particle.main;

        main.startSize = initial.SizeMinimum.X;

        if (infos.Count < 2)
        {
            return;
        }

        var size = particle.sizeOverLifetime;

        size.enabled = true;

        var curve = new AnimationCurve();

        float duration = infos[infos.Count - 1].TimeRange.X;
        float initialSize = initial.SizeMinimum.X;

        foreach (var info in infos)
        {
            float time = duration > 0f ? info.TimeRange.X / duration : 0f;
            float value = initialSize != 0f ? info.SizeMinimum.X / initialSize : 0f;

            curve.AddKey(time, value);
        }

        size.size = new ParticleSystem.MinMaxCurve(1f, curve);
    }

    private static void BuildColor(ParticleSystem particle, List<PTL.ParticleInfo> infos)
    {
        if (infos == null || infos.Count == 0)
        {
            return;
        }

        var initial = infos[0];

        var main = particle.main;

        main.startColor = ToUnityColor(initial.ColorMinimum);

        if (infos.Count < 2)
        {
            return;
        }

        var color = particle.colorOverLifetime;

        color.enabled = true;

        var colorKeys = new GradientColorKey[infos.Count];
        var alphaKeys = new GradientAlphaKey[infos.Count];

        float duration = infos[infos.Count - 1].TimeRange.X;

        for (int i = 0; i < infos.Count; i++)
        {
            var info = infos[i];

            float time = duration > 0f ? info.TimeRange.X / duration : 0f;

            colorKeys[i] = new GradientColorKey(ToUnityColor(info.ColorMinimum), time);
            alphaKeys[i] = new GradientAlphaKey(info.ColorMinimum.A, time);
        }

        Gradient gradient = new();

        gradient.SetKeys(colorKeys, alphaKeys);

        color.color = new ParticleSystem.MinMaxGradient(gradient);
    }

    private static void BuildRed(ParticleSystem particle, List<PTL.ParticleInfo> infos)
    {
    }

    private static void BuildGreen(ParticleSystem particle, List<PTL.ParticleInfo> infos)
    {
    }

    private static void BuildBlue(ParticleSystem particle, List<PTL.ParticleInfo> infos)
    {
    }

    private static void BuildAlpha(ParticleSystem particle, List<PTL.ParticleInfo> infos)
    {
        if (infos == null || infos.Count == 0)
        {
            return;
        }

        var initial = infos[0];

        var startColor = particle.main.startColor.color;
        startColor.a = initial.ValueMinimum;

        var main = particle.main;
        
        main.startColor = startColor;

        if (infos.Count < 2)
        {
            return;
        }

        var color = particle.colorOverLifetime;

        color.enabled = true;

        var alphaKeys = new GradientAlphaKey[infos.Count];

        float duration = infos[infos.Count - 1].TimeRange.X;

        for (int i = 0; i < infos.Count; i++)
        {
            var info = infos[i];

            float time = duration > 0f ? info.TimeRange.X / duration : 0f;

            alphaKeys[i] = new GradientAlphaKey(info.ValueMinimum, time);
        }

        Gradient gradient = new Gradient();

        gradient.SetKeys(
            new[]
            {
            new GradientColorKey(Color.white, 0f),
            new GradientColorKey(Color.white, 1f)
            },
            alphaKeys);

        color.color = new ParticleSystem.MinMaxGradient(gradient);
    }
    private static void BuildVelocity(ParticleSystem particle, List<PTL.ParticleInfo> infos)
    {
        if (infos == null || infos.Count == 0)
        {
            return;
        }

        var initial = infos[0];

        var velocity = particle.velocityOverLifetime;
        velocity.enabled = true;

        velocity.x = new ParticleSystem.MinMaxCurve(initial.VelocityMinimum.X, initial.VelocityMaximum.X);
        velocity.y = new ParticleSystem.MinMaxCurve(initial.VelocityMinimum.Y, initial.VelocityMaximum.Y);
        velocity.z = new ParticleSystem.MinMaxCurve(initial.VelocityMinimum.Z, initial.VelocityMaximum.Z);

        if (infos.Count < 2)
        {
            return;
        }

        float duration = infos[infos.Count - 1].TimeRange.X;

        var x = new AnimationCurve();
        var y = new AnimationCurve();
        var z = new AnimationCurve();

        foreach (var info in infos)
        {
            float time = duration > 0f ? info.TimeRange.X / duration : 0f;

            x.AddKey(time, info.VelocityMinimum.X);
            y.AddKey(time, info.VelocityMinimum.Y);
            z.AddKey(time, info.VelocityMinimum.Z);
        }

        velocity.x = new ParticleSystem.MinMaxCurve(1f, x);
        velocity.y = new ParticleSystem.MinMaxCurve(1f, y);
        velocity.z = new ParticleSystem.MinMaxCurve(1f, z);
    }

    private static void BuildVelocityX(ParticleSystem particle, List<PTL.ParticleInfo> infos)
    {
        if (infos == null || infos.Count == 0)
        {
            return;
        }

        var initial = infos[0];

        var velocity = particle.velocityOverLifetime;
        velocity.enabled = true;
        velocity.x = new ParticleSystem.MinMaxCurve(initial.ValueMinimum, initial.ValueMaximum);

        if (infos.Count < 2)
        {
            return;
        }

        float duration = infos[infos.Count - 1].TimeRange.X;

        var curve = new AnimationCurve();

        foreach (var info in infos)
        {
            float time = duration > 0f ? info.TimeRange.X / duration : 0f;
            curve.AddKey(time, info.ValueMinimum);
        }

        velocity.x = new ParticleSystem.MinMaxCurve(1f, curve);
    }

    private static void BuildVelocityY(ParticleSystem particle, List<PTL.ParticleInfo> infos)
    {
        if (infos == null || infos.Count == 0)
        {
            return;
        }

        var initial = infos[0];

        var velocity = particle.velocityOverLifetime;
        velocity.enabled = true;
        velocity.y = new ParticleSystem.MinMaxCurve(initial.ValueMinimum, initial.ValueMaximum);

        if (infos.Count < 2)
        {
            return;
        }

        float duration = infos[infos.Count - 1].TimeRange.X;

        var curve = new AnimationCurve();

        foreach (var info in infos)
        {
            float time = duration > 0f ? info.TimeRange.X / duration : 0f;
            curve.AddKey(time, info.ValueMinimum);
        }

        velocity.y = new ParticleSystem.MinMaxCurve(1f, curve);
    }

    private static void BuildVelocityZ(ParticleSystem particle, List<PTL.ParticleInfo> infos)
    {
        if (infos == null || infos.Count == 0)
        {
            return;
        }

        var initial = infos[0];

        var velocity = particle.velocityOverLifetime;
        velocity.enabled = true;
        velocity.z = new ParticleSystem.MinMaxCurve(initial.ValueMinimum, initial.ValueMaximum);

        if (infos.Count < 2)
        {
            return;
        }

        float duration = infos[infos.Count - 1].TimeRange.X;

        var curve = new AnimationCurve();

        foreach (var info in infos)
        {
            float time = duration > 0f ? info.TimeRange.X / duration : 0f;
            curve.AddKey(time, info.ValueMinimum);
        }

        velocity.z = new ParticleSystem.MinMaxCurve(1f, curve);
    }

    private static void BuildTextureIndex(ParticleSystem particle, List<PTL.ParticleInfo> infos)
    {
    }

    private static void BuildEventTimer(ParticleSystem particle, List<PTL.ParticleInfo> infos)
    {
    }

    private static void BuildNone(ParticleSystem particle, List<PTL.ParticleInfo> infos)
    {
    }

    private static void BuildRotation(ParticleSystem particle, List<PTL.ParticleInfo> infos)
    {
        if (infos == null || infos.Count == 0)
        {
            return;
        }

        var initial = infos[0];

        var main = particle.main;

        main.startRotation = new ParticleSystem.MinMaxCurve(initial.ValueMinimum, initial.ValueMaximum);

        if (infos.Count < 2)
        {
            return;
        }

        var rotation = particle.rotationOverLifetime;

        rotation.enabled = true;

        var curve = new AnimationCurve();

        float duration = infos[infos.Count - 1].TimeRange.X;

        foreach (var info in infos)
        {
            float time = duration > 0f ? info.TimeRange.X / duration : 0f;

            curve.AddKey(time, info.ValueMinimum);
        }

        rotation.z = new ParticleSystem.MinMaxCurve(1f, curve);
    }

    /// <summary>
    /// Load an EFT file from the given path.
    /// </summary>
    /// <param name="path">Path.</param>
    /// <returns>EFT file.</returns>
    private static EFT LoadEFT(string path)
    {
        EFT eft = new();

        using (var stream = File.OpenRead(path))

        using (var reader = new BinaryReader(stream))
        {
            eft.Read(reader);
        }

        return eft;
    }

    /// <summary>
    /// Load a PTL file from the given path.
    /// </summary>
    /// <param name="path">Path.</param>
    /// <returns>PTL file.</returns>
    private static PTL LoadPTL(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogWarning($"Missing PTL: {path}");

            return null;
        }

        PTL ptl = new();

        using (var stream = File.OpenRead(path))
        using (var reader = new BinaryReader(stream))
        {
            ptl.Read(reader);
        }

        return ptl;
    }

    /// <summary>
    /// Converts a PTL.Color to a UnityEngine.Color
    /// </summary>
    /// <param name="color">Color.</param>
    /// <returns>Color converted.</returns>
    private static Color ToUnityColor(PTL.Color color)
    {
        return new Color(color.R, color.G, color.B, color.A);
    }
}
