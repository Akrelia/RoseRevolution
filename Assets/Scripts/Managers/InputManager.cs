using UnityEngine;

/// <summary>
/// Input manager.
/// </summary>
public class InputManager : MonoBehaviour
{
    [Header("Shortcuts")]
    public KeyCode toggleConsoleKey;
    public KeyCode toggleDebug;
    [Header("Components")]
    public GUIController guiController;

    /// <summary>
    /// Update. 
    /// </summary>
    private void Update()
    {
        if (Input.GetKeyDown(toggleDebug))
        {
            guiController.CreateWindow<GMPanel>();
        }
    }
}
