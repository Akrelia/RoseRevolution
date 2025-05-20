using UnityEngine;
using TMPro;

/// <summary>
/// GUI Controller for an Entity.
/// </summary>
public class EntityGUIController : MonoBehaviour
{
    [Header("Components")]
    public SpeechBubble bubble;
    public TextMeshProUGUI nameLabel;

    public void SetName(string name)
    {
        nameLabel.text = name;
    }
}
