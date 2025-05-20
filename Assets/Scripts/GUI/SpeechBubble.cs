using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Speech bubble.
/// </summary>
public class SpeechBubble : MonoBehaviour
{
    [Header("Components")]
    public TextMeshProUGUI label;
    
    /// <summary>
    /// Show a message.
    /// </summary>
    /// <param name="message">Message.</param>
    public void ShowMessage(string message)
    {
        gameObject.SetActive(true);

        label.text = message;

        StartCoroutine(DisableAfterSeconds(4f));
    }

    IEnumerator DisableAfterSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        gameObject.SetActive(false);
    }
}
