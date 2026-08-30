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
    private static void ImportEffect(EffectDatabase database, int id, string dataPath, Shader shader)
    {

        var stb = ResourceManager.Instance.fileEffectSTB;

        string file = Path.Combine(dataPath, stb.Cells[id][2]);

        try
        {
            EFT eft = LoadEFT(file);

            if (eft == null)
            {
                return;
            }

            GameObject prefab = BuildEffect(eft, dataPath, shader);

            string path = $"{GameDataPaths.Effects.Prefabs}/{Path.GetFileName(file)}.prefab";

            PrefabUtility.SaveAsPrefabAsset(prefab, path);

            GameObject.DestroyImmediate(prefab);

            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null)
            {
                Debug.LogWarning($"Failed to load effect prefab: {path}");
                return;
            }

            var entry = new EffectDatabaseEntry()
            {
                id = id,
                prefab = prefab
            };

            int index = database.entries.FindIndex(x => x.id == id);

            if (index >= 0)
            {
                database.entries[index] = entry;
            }
            else
            {
                database.entries.Add(entry);
            }

            EditorUtility.SetDirty(database);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to import effect {file}: {ex.Message}\n{ex.StackTrace}");
        }
    }

    public static void ImportEffectByID(int id, string dataPath, Shader shader)
    {
        CheckEffectFolders();

        var database = RoseImporter.GetOrCreateDatabase<EffectDatabase>();

        ImportEffect(database, id, dataPath, shader);
    }

    public static void ImportEffects(string dataPath, Shader shader)
    {
        var database = RoseImporter.GetOrCreateDatabase<EffectDatabase>();
        var stb = ResourceManager.Instance.fileEffectSTB;

        CheckEffectFolders();

        // AssetDatabase.StartAssetEditing();

        int effectCount = 1200;

        try
        {
            for (int i = 1; i < effectCount; i++)
            {
                if (string.IsNullOrEmpty(stb.Cells[i][1]))
                {
                    continue;
                }

                ImportEffect(database, i, dataPath, shader);
            }
        }
        finally
        {
            // AssetDatabase.StopAssetEditing();
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

    private static void CheckEffectFolders()
    {
        EditorUtils.EnsureFolder(Path.Combine(GameDataPaths.Effects.Prefabs, "dummy.asset"));
        EditorUtils.EnsureFolder(Path.Combine(GameDataPaths.Effects.Materials, "dummy.asset"));
        EditorUtils.EnsureFolder(Path.Combine(GameDataPaths.Effects.Textures, "dummy.asset"));
        EditorUtils.EnsureFolder(Path.Combine(GameDataPaths.Effects.Meshes, "dummy.asset"));
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
                GameObject obj = BuildEmitter(emitter, dataPath, shader);

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
    private static GameObject BuildEmitter(PTL.Emitter emitter, string dataPath, Shader shader)
    {
        GameObject obj = new(emitter.Name);

        ParticleSystem particle = obj.AddComponent<ParticleSystem>();
        ParticleSystemRenderer renderer = obj.GetComponent<ParticleSystemRenderer>();

        var main = particle.main;

        main.maxParticles = (int)emitter.ParticleNumber;
        main.loop = emitter.LoopCount == 0;
        main.startLifetime = new ParticleSystem.MinMaxCurve(emitter.LifeTime.X, emitter.LifeTime.Y);
        main.startSpeed = 0;
        main.simulationSpeed = 2F; // Animations are hardcoded on a 30 FPS time frame, so double up

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
        emission.rateOverTime = 100f; // This is for a perfect loop, but it doesn't matter since this is only 1 particle max

        var shape = particleSystem.shape;
        shape.enabled = false;

        particleRenderer.renderMode = ParticleSystemRenderMode.Mesh;
        particleRenderer.alignment = ParticleSystemRenderSpace.World;
        particleRenderer.mesh = mesh;
        particleRenderer.material = material;

        BuildAnimationCurves(particleSystem, zmo, mesh);

        return effect;
    }

    private static void BuildAnimationCurves(ParticleSystem particleSystem, ZMO zmo, Mesh mesh)
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
                        BuildPositionCurve(particleSystem, zmo, duration);
                        break;
                    }

                case ZMO.ChannelType.Rotation:
                    {
                        BuildRotationCurve(particleSystem, zmo, i, duration);
                        break;
                    }

                case ZMO.ChannelType.Scale:
                    {
                        //    BuildScaleCurve(particleSystem, zmo, i, duration);
                        break;
                    }

                case ZMO.ChannelType.Normal:
                    {
                        BuildInitialNormal(particleSystem, zmo, i);
                        break;
                    }

                case ZMO.ChannelType.Alpha:
                    {
                        BuildAlphaCurve(particleSystem, zmo, duration);
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

    private static void BuildInitialNormal(ParticleSystem particleSystem, ZMO zmo, int channelIndex)
    {
        if (zmo.Frames == null || zmo.Frames.Length == 0)
        {
            return;
        }

        Vector3 normal = zmo.Frames[0].Channels[channelIndex].Normal;

        if (normal.sqrMagnitude < 0.0001f)
        {
            return;
        }

        particleSystem.transform.localRotation = Quaternion.FromToRotation(Vector3.forward, normal.normalized);
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

    private static void BuildAlphaCurve(ParticleSystem particleSystem, ZMO zmo, float duration)
    {
        if (zmo.Frames == null || zmo.Frames.Length == 0 || zmo.Channels == null)
        {
            return;
        }

        var alphaChannels = new List<int>();

        for (int i = 0; i < zmo.Channels.Length; i++)
        {
            if (zmo.Channels[i].Type == ZMO.ChannelType.Alpha)
            {
                alphaChannels.Add(i);
            }
        }

        if (alphaChannels.Count == 0)
        {
            return;
        }

        var color = particleSystem.colorOverLifetime;
        color.enabled = true;

        int keyCount = Mathf.Min(zmo.Frames.Length, 8);
        var alphaKeys = new GradientAlphaKey[keyCount];

        for (int i = 0; i < keyCount; i++)
        {
            int frameIndex = keyCount == 1
                ? 0
                : Mathf.RoundToInt(
                    i * (zmo.Frames.Length - 1f) / (keyCount - 1f));

            float time = zmo.Frames.Length > 1
                ? (float)frameIndex / (zmo.Frames.Length - 1)
                : 0f;

            float alpha = 0f;

            for (int j = 0; j < alphaChannels.Count; j++)
            {
                alpha += zmo.Frames[frameIndex]
                    .Channels[alphaChannels[j]]
                    .Alpha;
            }

            alpha /= alphaChannels.Count;

            alphaKeys[i] = new GradientAlphaKey(
                Mathf.Clamp01(alpha),
                time);
        }

        var gradient = new Gradient();

        gradient.SetKeys(
            new[]
            {
            new GradientColorKey(Color.white, 0f),
            new GradientColorKey(Color.white, 1f)
            },
            alphaKeys);

        color.color = new ParticleSystem.MinMaxGradient(gradient);
    }

    private static void BuildPositionCurve(ParticleSystem particleSystem, ZMO zmo, float duration)
    {
        if (zmo.Frames == null || zmo.Frames.Length < 2 || zmo.Channels == null)
        {
            return;
        }

        var positionChannels = new List<int>();

        for (int i = 0; i < zmo.Channels.Length; i++)
        {
            if (zmo.Channels[i].Type == ZMO.ChannelType.Position)
            {
                positionChannels.Add(i);
            }
        }

        if (positionChannels.Count == 0)
        {
            return;
        }

        var rotation = particleSystem.rotationOverLifetime;
        rotation.enabled = true;
        rotation.separateAxes = true;

        var velocity = particleSystem.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;

        var rotationX = new AnimationCurve();
        var rotationY = new AnimationCurve();
        var rotationZ = new AnimationCurve();

        var velocityX = new AnimationCurve();
        var velocityY = new AnimationCurve();
        var velocityZ = new AnimationCurve();

        float deltaTime = 1f / zmo.FPS;

        Vector3[] centers = new Vector3[zmo.Frames.Length];

        for (int frame = 0; frame < zmo.Frames.Length; frame++)
        {
            Vector3 center = Vector3.zero;

            for (int i = 0; i < positionChannels.Count; i++)
            {
                center += zmo.Frames[frame].Channels[positionChannels[i]].Position;
            }

            centers[frame] = center / positionChannels.Count;
        }

        // Rotation is intentionally calculated from the raw position of one vertex.
        // Do not subtract the center here: doing so changes the rotation signal.
        int rotationChannel = positionChannels[0];

        float previousAngle = 0f;

        for (int frame = 0; frame < zmo.Frames.Length; frame++)
        {
            float time = duration > 0f
                ? (float)frame / (zmo.Frames.Length - 1)
                : 0f;

            Vector3 position =
                zmo.Frames[frame].Channels[rotationChannel].Position;

            float angle = Mathf.Atan2(position.z, position.x);

            float angularVelocity = 0f;

            if (frame > 0)
            {
                float deltaAngle = Mathf.DeltaAngle(
                    previousAngle * Mathf.Rad2Deg,
                    angle * Mathf.Rad2Deg);

                angularVelocity = deltaAngle * Mathf.Deg2Rad / deltaTime;
            }

            previousAngle = angle;

            rotationX.AddKey(time, 0f);
            rotationY.AddKey(time, angularVelocity);
            rotationZ.AddKey(time, 0f);

            Vector3 centerVelocity;

            if (frame == 0)
            {
                centerVelocity =
                    (centers[1] - centers[0]) / deltaTime;
            }
            else if (frame == zmo.Frames.Length - 1)
            {
                centerVelocity =
                    (centers[frame] - centers[frame - 1]) / deltaTime;
            }
            else
            {
                centerVelocity =
                    (centers[frame + 1] - centers[frame - 1]) /
                    (deltaTime * 2f);
            }

            velocityX.AddKey(time, centerVelocity.x);
            velocityY.AddKey(time, centerVelocity.y);
            velocityZ.AddKey(time, centerVelocity.z);
        }

        rotation.x = new ParticleSystem.MinMaxCurve(1f, rotationX);
        rotation.y = new ParticleSystem.MinMaxCurve(1f, rotationY);
        rotation.z = new ParticleSystem.MinMaxCurve(1f, rotationZ);

        velocity.x = new ParticleSystem.MinMaxCurve(1f, velocityX);
        velocity.y = new ParticleSystem.MinMaxCurve(1f, velocityY);
        velocity.z = new ParticleSystem.MinMaxCurve(1f, velocityZ);
    }
    private static void BuildRotationCurve(ParticleSystem particleSystem, ZMO zmo, int channelIndex, float duration)
    {
        Debug.Log($"BUILD ROTATIOn - channel {channelIndex}");

        if (zmo.Frames.Length < 2 || zmo.FPS <= 0)
        {
            return;
        }

        var rotation = particleSystem.rotationOverLifetime;
        rotation.enabled = true;
        rotation.separateAxes = true;

        var x = new AnimationCurve();
        var y = new AnimationCurve();
        var z = new AnimationCurve();

        float deltaTime = 1f / zmo.FPS;

        for (int i = 0; i < zmo.Frames.Length; i++)
        {
            float time = duration > 0f ? (float)i / (zmo.Frames.Length - 1) : 0f;

            Vector3 current = zmo.GetContinuousEulerRotationAt(channelIndex, i);
            Vector3 angularVelocity;

            if (i == 0)
            {
                Vector3 next = zmo.GetContinuousEulerRotationAt(channelIndex, i + 1);

                angularVelocity = (next - current) / deltaTime;
            }
            else
            {
                Vector3 previous = zmo.GetContinuousEulerRotationAt(channelIndex, i - 1);

                angularVelocity = (current - previous) / deltaTime;
            }

            angularVelocity *= Mathf.Deg2Rad;

            x.AddKey(time, angularVelocity.x);
            y.AddKey(time, angularVelocity.y);
            z.AddKey(time, angularVelocity.z);
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

        float minSize = float.MaxValue;
        float maxSize = float.MinValue;

        foreach (var info in infos)
        {
            float time = duration > 0f ? info.TimeRange.X / duration : 0f;
            float value = initialSize != 0f ? info.SizeMinimum.X / initialSize : 0f;

            curve.AddKey(time, value);

            //  minSize = Mathf.Min(minSize, info.SizeMinimum.X);
            minSize = Mathf.Min(minSize, info.SizeMinimum.X) / 10F;
            maxSize = Mathf.Max(maxSize, info.SizeMaximum.X) * 10F;  // Puting the * 10F as a temp solution because
        }

        size.size = new ParticleSystem.MinMaxCurve(1f, curve);

        var renderer = particle.GetComponent<ParticleSystemRenderer>();

        renderer.minParticleSize = minSize;
        renderer.maxParticleSize = maxSize;
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

        int keyCount = Mathf.Min(infos.Count, 8);

        var colorKeys = new GradientColorKey[keyCount];
        var alphaKeys = new GradientAlphaKey[keyCount];

        float duration = infos[infos.Count - 1].TimeRange.X;

        for (int i = 0; i < keyCount; i++)
        {
            int index = keyCount == 1
                ? 0
                : Mathf.RoundToInt(i * (infos.Count - 1f) / (keyCount - 1f));

            var info = infos[index];

            float time = duration > 0f ? info.TimeRange.X / duration : 0f;

            colorKeys[i] = new GradientColorKey(ToUnityColor(info.ColorMinimum), time);
            alphaKeys[i] = new GradientAlphaKey(info.ColorMinimum.A, time);
        }

        Gradient gradient = new Gradient();

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
