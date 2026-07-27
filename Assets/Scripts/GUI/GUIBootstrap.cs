using UnityEngine;

/// <summary>
/// GUI Bootstrap.
/// </summary>
public class GUIBootstrap : MonoBehaviour
{
    [Header("GUI")]
    public DebugConsole debugConsole;
    [Header("Shortcuts")]
    public KeyCode toggleConsoleKey;

    /// <summary>
    /// Update. 
    /// </summary>
    private void Update()
    {
        if (Input.GetKeyDown(toggleConsoleKey))
        {

        }
    }
}
