using System;
using System.Collections.Generic;
using UnityEngine;
using RevolutionShared.Rose.Data;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Linq;

/// <summary>
/// Utils.
/// </summary>
public class Utils
{
    public static bool isEmail(string email)
    {
        return email.Contains("@") && email.Contains(".");
    }

    public static Quaternion RoseToUnityRotation(Quaternion q)
    {
        return new Quaternion(q.x, q.z, -q.y, q.w);
    }

    public static Vector3 RoseToUnityPosition(Vector3 v)
    {
        return new Vector3(v.x, v.z, -v.y); // OP
    }

    public static Vector3 RoseToUnityVector(Vector3 v)
    {
        return new Vector3(v.x, v.z, -v.y); // OP
    }

    public static Vector3 RoseToUnityScale(Vector3 v)
    {
        return new Vector3(v.x, v.z, v.y);
    }

    public static Transform findChild(GameObject go, string name)
    {
        Transform[] children = go.GetComponentsInChildren<Transform>();
        foreach (Transform child in children)
            if (child.name == name)
                return child;
        return null;
    }

    public static List<Transform> findChildren(GameObject go, string name)
    {
        Transform[] children = go.GetComponentsInChildren<Transform>();
        List<Transform> result = new List<Transform>();
        foreach (Transform child in children)
            if (child.name == name)
                result.Add(child);
        return result;
    }

    public static bool IsOneHanded(WeaponType weapon)
    {
        return weapon == WeaponType.THAXE ||
                weapon == WeaponType.OHMACE ||
                weapon == WeaponType.OHSWORD ||
                weapon == WeaponType.OHTOOL ||
                weapon == WeaponType.WAND ||
                weapon == WeaponType.XBOX ||
                weapon == WeaponType.EMPTY;

    }

    /// <summary>
    /// Safe mod.
    /// </summary>
    /// <param name="value">Value.</param>
    /// <param name="modulo">Modulo.</param>
    /// <returns>Int.</returns>
    public static int Modulo(int value, int modulo)
    {
        return ((value % modulo) + modulo) % modulo;
    }

    public static Bounds GetMaxBounds(GameObject g)
    {
        var b = new Bounds(g.transform.position, Vector3.zero);

        foreach (Renderer r in g.GetComponentsInChildren<Renderer>())
        {
            b.Encapsulate(r.bounds);
        }

        return b;
    }

    public static AsyncOperationHandle<T> LoadAddressableAsync<T>(string address, Action<T> onLoaded, Action onFailed = null) where T : ScriptableObject // ScriptableObject because we should load SO as addresasbles because its very fast and its used mostly in startup, prefabs are always instancied afterward
    {
        var handle = Addressables.LoadAssetAsync<T>(address);

        handle.Completed += op =>
        {
            if (op.Status == AsyncOperationStatus.Succeeded && op.Result != null)
            {
                onLoaded?.Invoke(op.Result);
            }

            else
            {
                Debug.LogError($"Failed to load '{address}' as {typeof(T).Name} (status: {op.Status}).");

                onFailed?.Invoke();
            }
        };

        return handle;
    }

    public static T LoadDatabase<T>() where T : UnityEngine.Object
    {
        string address = typeof(T).Name;

        foreach (var locator in Addressables.ResourceLocators)
        {
            if (!locator.Keys.Contains(address))
                continue;

            var handle = Addressables.LoadAssetAsync<T>(address);

            if (!handle.IsValid())
                return null;

            handle.WaitForCompletion();

            if (handle.Status != AsyncOperationStatus.Succeeded)
                return null;

            return handle.Result;
        }

        return null;
    }

    public static bool IsMac()
    {
        return Application.platform == RuntimePlatform.OSXEditor;
    }

    public static string Colorize(string text, Color color)
    {
        return $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{text}</color>";
    }

    public static string Colorize(int value, Color color)
    {
        return Colorize(value.ToString(), color);
    }

    public static string Colorize(float value, Color color)
    {
        return Colorize(value.ToString(), color);
    }

    public static string Colorize(byte value, Color color)
    {
        return Colorize(value.ToString(), color);
    }

    public static string Colorize<T>(T value, Color color) where T : Enum
    {
        return Colorize(value.ToString(), color);
    }

    public static Sprite BytesToSprite(byte[] data)
    {
        if (data == null || data.Length == 0)
        {
            return null;
        }

        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

        if (!texture.LoadImage(data))
        {
            UnityEngine.Object.Destroy(texture);

            return null;
        }

        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }

    /// Destroy every children of a game object.
    /// </summary>
    /// <param name="gameObject">Game object.</param>
    static public void DestroyChildren(GameObject gameObject)
    {
        foreach (Transform child in gameObject.transform)
        {
            GameObject.Destroy(child.gameObject);
        }
    }
}