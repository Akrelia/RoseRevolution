using UnityEngine;

/// <summary>
/// GUI Bootstrap.
/// </summary>
public class GUIBootstrap : MonoBehaviour
{
    [Header("GUI")]
    public DebugConsole debugConsole;
    public ObjectViewerWindow effectWindow;
    [Header("Shortcuts")]
    public KeyCode toggleConsoleKey;
    public KeyCode effectViewerKey;

    private void Awake()
    {
    }

    /// <summary>
    /// Update. 
    /// </summary>
    private void Update()
    {
        if (Input.GetKeyDown(toggleConsoleKey))
        {
            debugConsole.ToggleDisplay();
        }

        if (Input.GetKeyDown(effectViewerKey))
        {
            effectWindow.window.Toggle();
        }
    }
}
