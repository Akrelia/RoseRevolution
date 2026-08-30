using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Progress Bar Controller.
/// </summary>
public class ProgressBarController : MonoBehaviour
{
    [Header("Values")]
    public bool applyGradient;
    public Gradient gradient;
    [Header("Elements")]
    public Image backgroundImage;
    public Image foregroundImage;
    public RectTransform mask;
    public TextMeshProUGUI valueLabel;

    /// <summary>
    /// Start.
    /// </summary>
    public void Start()
    {
        UpdateValue(0f);
    }

    /// <summary>
    /// Update the progress bar value.
    /// </summary>
    /// <param name="value">Value.</param>
    public void UpdateValue(float value)
    {
        value = Mathf.Clamp01(value);

        mask.localScale = new Vector3(value, 1f, 1f);

        if (applyGradient)
        {
            foregroundImage.color = gradient.Evaluate(value);
        }

        if (valueLabel != null)
        {
            valueLabel.text = $"{Mathf.RoundToInt(value * 100)}%";
        }
    }
}
