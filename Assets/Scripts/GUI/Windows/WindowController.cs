using UnityEngine;
using TMPro;
using UnityEngine.Events;

/// <summary>
/// Window controller.
/// </summary>
public class WindowController : MonoBehaviour
{
    [Header("Windows Settings")]
    public bool hideOnStartup;
    public CloseButtonAction action;
    [Header("Windows Components")]
    public TextMeshProUGUI titleLabel;

    public UnityEvent showEvent = new UnityEvent();
    public UnityEvent hideEvent = new UnityEvent();
    public UnityEvent<WindowController> closeEvent = new UnityEvent<WindowController>();

    /// <summary>
    /// Awake.
    /// </summary>
    public void Awake()
    {
        if (hideOnStartup)
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Set the window title.
    /// </summary>
    /// <param name="title">Title.</param>
    public void SetTitle(string title)
    {
        titleLabel.text = title;
    }

    /// <summary>
    /// When the close button is clicked.
    /// </summary>
    public void CloseButtonClick()
    {
        if (action == CloseButtonAction.Close)
        {
            Close();
        }

        else if (action == CloseButtonAction.Hide)
        {
            Hide();
        }
    }

    /// <summary>
    /// Close the window.
    /// </summary>
    public void Close()
    {
        closeEvent?.Invoke(this);

        Destroy(gameObject);
    }

    /// <summary>
    /// Hide the window.
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);

        hideEvent?.Invoke();
    }

    /// <summary>
    /// Toggle the window visibility.
    /// </summary>
    public void Toggle()
    {
        if (gameObject.activeSelf)
        {
            Hide();
        }

        else
        {
            Show();
        }
    }

    /// <summary>
    /// Show the window.
    /// </summary>
    public void Show()
    {
        gameObject.SetActive(true);

        showEvent?.Invoke();
    }
}

/// <summary>
/// Close button action.
/// </summary>
public enum CloseButtonAction
{
    Close = 1,
    Hide = 2,
}