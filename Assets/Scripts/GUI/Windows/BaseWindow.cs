using UnityEngine;

/// <summary>
/// Base Window.
/// </summary>
[RequireComponent(typeof(WindowController))]
public class BaseWindow : MonoBehaviour
{
    [HideInInspector]
    public WindowController window;

    /// <summary>
    /// Awake.
    /// </summary>
    public void Awake()
    {
        window = GetComponent<WindowController>();
    }
}
