using UnityEngine;

/// <summary>
/// Input manager.
/// </summary>
public class InputManager : MonoBehaviour
{
    [Header("Shortcuts")]
    public KeyCode toggleConsoleKey;
    public KeyCode toggleGMPanel;
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

        if (Input.GetKeyDown(toggleGMPanel))
        {
            guiController.gmPanel.gameObject.SetActive(!guiController.gmPanel.gameObject.activeSelf);
        }
    }
}
