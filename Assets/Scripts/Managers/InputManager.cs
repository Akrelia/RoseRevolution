using UnityEngine;

/// <summary>
/// Input manager.
/// </summary>
public class InputManager : MonoBehaviour
{
    [Header("Shortcuts")]
    public KeyCode toggleConsoleKey;
    [Header("Components")]
    public GUIController guiController;

    /// <summary>
    /// Update.
    /// </summary>
    private void Update()
    {
        if (Input.GetKeyDown(toggleConsoleKey))
        {
            guiController.debugConsole.gameObject.SetActive(!guiController.debugConsole.gameObject.activeSelf);
        }
    }
}
