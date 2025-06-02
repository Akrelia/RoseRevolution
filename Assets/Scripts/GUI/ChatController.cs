using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Chat controller.
/// </summary>
public class ChatController : MonoBehaviour
{
    [Header("Colors")]
    public Color playerMessageColor;
    public Color shoutMessageColor;
    public Color announcementMessageColor;
    public Color whisperMessageColor;
    public Color systemMessageColor;
    [Header("Components")]
    public TextMeshProUGUI chatText;
    public TextMeshProUGUI logText;
    public ScrollRect scrollRect;
    public TMP_InputField input;

    /// <summary>
    /// Start.
    /// </summary>
    private void Start()
    {
        input.onSubmit.AddListener(OnSubmit);
    }

    /// <summary>
    /// When submit the input.
    /// </summary>
    /// <param name="text">Text.</param>
    private void OnSubmit(string text)
    {
        if (!string.IsNullOrEmpty(text))
        {
            Client.Instance.SendPacket(Packets.SendChatMessage(input.text));

            input.text = "";
        }

        input.Select();
        input.ActivateInputField();
    }

    /// <summary>
    /// Add a message from a player.
    /// </summary>
    /// <param name="playerName">Player's name.</param>
    /// <param name="message">Message.</param>
    public void AddPlayerMessage(string playerName, string message)
    {
        AppendText(ColorizeText($"{playerName}> {message}", playerMessageColor));
    }

    /// <summary>
    /// Add a system message.
    /// </summary>
    /// <param name="message">Message.</param>
    public void AddSystemMessage(string message)
    {
        AppendText(ColorizeText(message, announcementMessageColor));
    }

    /// <summary>
    /// Append text in the chat.
    /// </summary>
    /// <param name="message">Message.</param>
    private void AppendText(string message)
    {
        int previousLineCount = chatText.textInfo.lineCount;

        chatText.text += message + Environment.NewLine;

        StartCoroutine(ScrollByNewLines(previousLineCount));
    }

    /// <summary>
    /// Makes the chat scroll by x line(s)
    /// </summary>
    /// <param name="oldLineCount">Old line count.</param>
    /// <returns>Coroutine.</returns>
    private IEnumerator ScrollByNewLines(int oldLineCount)
    {
        yield return new WaitForEndOfFrame();

        int newLineCount = chatText.textInfo.lineCount;
        int addedLines = Mathf.Max(1, newLineCount - oldLineCount);

        float lineHeight = chatText.textInfo.lineInfo[Mathf.Max(0, newLineCount - 1)].lineHeight;
        float scrollStep = lineHeight * addedLines / (scrollRect.content.rect.height - scrollRect.viewport.rect.height);

        scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition - scrollStep);
    }

    /// <summary>
    /// Colorize a text with a color.
    /// </summary>
    /// <param name="text">Text.</param>
    /// <param name="color">Color.</param>
    /// <returns>Colored text.</returns>
    static public string ColorizeText(string text, Color color)
    {
        var colorCode = ColorUtility.ToHtmlStringRGB(color);

        return $"<color=#{colorCode}>{text}</color>";
    }
}
