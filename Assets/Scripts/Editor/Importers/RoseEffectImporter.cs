using System.IO;
using System;
using UnityEditor;
using UnityEngine;
using UnityRose.ImportEditor;
using UnityRose.Import;
using static UnityRose.Import.GameDataPaths;

/// <summary>
/// Rose effect importer.
/// </summary>
public class RoseEffectImporter : MonoBehaviour
{
    /// <summary>
    /// Import all effects.
    /// </summary>
    /// <param name="dataPath">Data path.</param>
    public static void ImportEffects(string dataPath)
    {
        // TODO : Add the database creation here when completing effects

        EditorUtils.EnsureFolder(GameDataPaths.Effects.Root + "/dummy.asset");

        var files = Directory.GetFiles(dataPath + "/3DDATA/Effect", "*.eft", SearchOption.AllDirectories);

        for (int i = 0; i < 150; i++)
        {
            var file = files[i];

            try
            {
                EFT eft = LoadEFT(file);

                if (eft == null)
                {
                    continue;
                }

                GameObject prefab = BuildEffect(eft, dataPath);

                string path = $"{GameDataPaths.Effects.Prefabs}/{Path.GetFileName(file)}.prefab";

                EditorUtils.EnsureFolder(path);

                PrefabUtility.SaveAsPrefabAsset(prefab, path);

                GameObject.DestroyImmediate(prefab);
            }

            catch (Exception ex)
            {
                Debug.LogError($"Failed to import effect {file}: {ex.Message}\n{ex.StackTrace}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
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
    /// Build the effect as a collection of Particle System.
    /// </summary>
    /// <param name="eft">Effect file.</param>
    /// <param name="dataPath">Data path.</param>
    /// <returns>Particle system.</returns>
    private static GameObject BuildEffect(EFT eft, string dataPath)
    {
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

        var velocity = particle.velocityOverLifetime;

        velocity.enabled = true;

        velocity.x = new ParticleSystem.MinMaxCurve(emitter.MinGravity.X, emitter.MaxGravity.X);
        velocity.y = new ParticleSystem.MinMaxCurve(emitter.MinGravity.Y, emitter.MaxGravity.Y);
        velocity.z = new ParticleSystem.MinMaxCurve(emitter.MinGravity.Z, emitter.MaxGravity.Z);

        var textureSheetAnimation = particle.textureSheetAnimation;

        foreach (var info in emitter.Infos)
        {
            switch (info.Type)
            {
                case PTL.AnimType.SIZE:
                    {
                        var size = particle.sizeOverLifetime;

                        size.enabled = true;
                        size.size = new ParticleSystem.MinMaxCurve(info.SizeMinimum.X, info.SizeMaximum.X);

                        break;
                    }

                case PTL.AnimType.COLOR:
                    {
                        var color = particle.colorOverLifetime;

                        color.enabled = true;

                        Gradient gradient = new();

                        gradient.SetKeys(
                            new[]
                            {
                                    new GradientColorKey(ToUnityColor(info.ColorMinimum), 0f),
                                    new GradientColorKey(ToUnityColor(info.ColorMaximum), 1f)
                            },
                            new[]
                            {
                                    new GradientAlphaKey(info.ColorMinimum.A, 0f),
                                    new GradientAlphaKey(info.ColorMaximum.A, 1f)
                            });

                        color.color = new ParticleSystem.MinMaxGradient(gradient);

                        break;
                    }

                case PTL.AnimType.RED:
                case PTL.AnimType.GREEN:
                case PTL.AnimType.BLUE:
                case PTL.AnimType.ALPHA:
                    {

                        break;
                    }

                case PTL.AnimType.VELOCITY:
                    {
                        var velo1 = particle.velocityOverLifetime;

                        velo1.enabled = true;

                        velo1.x = new ParticleSystem.MinMaxCurve(info.VelocityMinimum.X, info.VelocityMaximum.X);
                        velo1.y = new ParticleSystem.MinMaxCurve(info.VelocityMinimum.Y, info.VelocityMaximum.Y);
                        velo1.z = new ParticleSystem.MinMaxCurve(info.VelocityMinimum.Z, info.VelocityMaximum.Z);

                        break;
                    }

                case PTL.AnimType.VELOCITYX:
                    {
                        var velo2 = particle.velocityOverLifetime;

                        velo2.enabled = true;
                        velo2.x = new ParticleSystem.MinMaxCurve(info.ValueMinimum, info.ValueMaximum);

                        break;
                    }

                case PTL.AnimType.VELOCITYY:
                    {
                        var velo3 = particle.velocityOverLifetime;

                        velo3.enabled = true;
                        velo3.y = new ParticleSystem.MinMaxCurve(info.ValueMinimum, info.ValueMaximum);

                        break;
                    }

                case PTL.AnimType.VELOCITYZ:
                    {
                        var velo3 = particle.velocityOverLifetime;

                        velo3.enabled = true;
                        velo3.z = new ParticleSystem.MinMaxCurve(info.ValueMinimum, info.ValueMaximum);

                        break;
                    }

                case PTL.AnimType.TEXTUREINDEX:
                    {
                        textureSheetAnimation.enabled = true;
                        textureSheetAnimation.mode = ParticleSystemAnimationMode.Grid;
                        textureSheetAnimation.numTilesX = (int)info.TextureIndex.X - 1;
                        textureSheetAnimation.numTilesY = (int)info.TextureIndex.Y - 1;

                        break;
                    }

                case PTL.AnimType.EVENTTIMER:
                    {
                        break;
                    }

                case PTL.AnimType.NONE:
                    {
                        break;
                    }

                case PTL.AnimType.ROTATION:
                    {
                        var rotation = particle.rotationOverLifetime;

                        rotation.enabled = true;
                        rotation.z = new ParticleSystem.MinMaxCurve(info.ValueMinimum, info.ValueMaximum);

                        break;
                    }

                default:
                    {
                        Debug.LogWarning($"Unknown PTL animation type: {info.Type}");

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
