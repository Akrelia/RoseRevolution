using UnityEngine;
using TMPro;

/// <summary>
/// Character preview.
/// </summary>
public class CharacterPreview : MonoBehaviour
{
    [Header("Components")]
    public TextMeshProUGUI hpLabel;
    public TextMeshProUGUI mpLabel;
    public TextMeshProUGUI levelLabel;
    public TextMeshProUGUI nameLabel;
    public TextMeshProUGUI jobLabel;

    /// <summary>
    /// Start.
    /// </summary>
    public void SetCharacterInformations(string playerName, int maxHP, int currentHP, int maxMP, int currentMP, byte level, string job)
    {
        nameLabel.text = playerName;
        hpLabel.text = $"{currentHP} / {maxHP}";
        mpLabel.text = $"{currentMP} / {maxMP}";
        levelLabel.text = level.ToString();
        jobLabel.text = job;
    }
}
