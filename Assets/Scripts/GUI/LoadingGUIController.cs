using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Loading GUI Controller.
/// </summary>
public class LoadingGUIController : MonoBehaviour
{
    [Header("Data")]
    public Sprite defaultWallpaper;
    [Header("Components")]
    public Image wallpaperImage;
    public ProgressBarController progressBarController;

    /// <summary>
    /// Set a random wallpaper.
    /// </summary>
    public void SetRandomWallpaper()
    {
        wallpaperImage.sprite = defaultWallpaper;
    }

    /// <summary>
    /// Initialize the loading screen with the scene name.
    /// </summary>
    /// <param name="sceneName">Scene name.</param>
    public void Initialize(string sceneName)
    {
        SetRandomWallpaper();

        progressBarController.UpdateValue(0f);
    }

    /// <summary>
    /// Set the current loading step.
    /// </summary>
    /// <param name="value">Value.</param>
    public void SetProgress(float value)
    {
        progressBarController.UpdateValue(value);
    }
}
