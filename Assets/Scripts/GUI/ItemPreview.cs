using RevolutionShared.Rose.Data.Equipment;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Item preview.
/// </summary>
public class ItemPreview : MonoBehaviour
{
    [Header("GUI")]
    public Image iconImage;
    public TextMeshProUGUI rateLabel;

    EquipmentData data;

    public void SetIcon(EquipmentData data, float chance, Sprite icon)
    {
        this.data = data;

        iconImage.sprite = icon;

        rateLabel.text = $"{chance:0.##}%";
    }

    public void SetIcon(float chance)
    {
        rateLabel.text = $"{chance:0.##}%";
    }
}
