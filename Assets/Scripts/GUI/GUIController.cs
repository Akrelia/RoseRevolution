using NUnit.Framework;
using RevolutionShared.Networking.Packets;
using RevolutionShared.Packets;
using System;
using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// GUI Controller.
/// </summary>
public class GUIController : MonoBehaviour
{
    [Header("Windows")]
    public List<GameObject> windowPrefabs;
    [Header("Components")]
    public ChatController chatController;
    public CharacterPreview characterPreview;
    public Transform windowsParent;

    Dictionary<Type, BaseWindow> windows;

    /// <summary>
    /// Awake.
    /// </summary>
    public void Awake()
    {
        windows = new Dictionary<Type, BaseWindow>();
    }

    /// <summary>
    /// Create a window.
    /// </summary>
    /// <param name="prefab">Prefab.</param>
    public T CreateWindow<T>() where T : BaseWindow
    {
        var type = typeof(T);

        if (windows.ContainsKey(type))
        {
            var existingWindow = windows[type];

            if (existingWindow.window.isUnique)
            {
                existingWindow.window.Show();
            }

            return existingWindow.GetComponent<T>();
        }

        else
        {
            var prefab = windowPrefabs.FirstOrDefault(w => w.GetComponent<T>());

            if (prefab)
            {
                var window = Instantiate(prefab, windowsParent).GetComponent<T>();

                windows.Add(typeof(T), window);

                return window;
            }

            else
            {
                RoseDebug.LogWarning("Trying to create a missing window");

                return null;
            }
        }
    }
}
