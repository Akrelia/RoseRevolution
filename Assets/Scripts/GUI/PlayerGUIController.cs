using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Player GUI Controller.
/// </summary>
public class PlayerGUIController : MonoBehaviour
{
    [Header("Components")]
    public SpeechBubble bubble;
    public Image clanImage;
    public GameObject clanPanel;
    public TextMeshProUGUI nameLabel;
    public TextMeshProUGUI clanLabel;

    /// <summary>
    /// Set the name of the player.
    /// </summary>
    /// <param name="name">Name.</param>
    public void SetName(string name)
    {
        nameLabel.text = name;
    }

    /// <summary>
    /// Set the clan infos.
    /// </summary>
    /// <param name="name">Name.</param>
    /// <param name="color">Color.</param>
    /// <param name="icon">Icon.</param>
    public void SetClan(string name, Color color, Sprite icon)
    {
        clanPanel.SetActive(true);

        clanLabel.text = name;
        clanLabel.color = color;
        clanImage.sprite = icon;

        LayoutRebuilder.ForceRebuildLayoutImmediate(clanPanel.GetComponent<RectTransform>());
    }

    /// <summary>
    /// Disable the clan.
    /// </summary>
    public void DisableClan()
    {
        clanPanel.SetActive(false);
    }
}
