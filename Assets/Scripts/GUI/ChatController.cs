using System;
using TMPro;
using UnityEngine;

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
    public TMP_InputField input;

    /// <summary>
    /// Start.
    /// </summary>
    private void Start()
    {
        input.onSubmit.AddListener(OnSubmit);
    }

    /// <summary>
    /// Append text in the chat.
    /// </summary>
    /// <param name="message">Message.</param>
    private void AppendText(string message)
    {
        chatText.text += message + Environment.NewLine;
    }

    /// <summary>
    /// When submit the input.
    /// </summary>
    /// <param name="text">Text.</param>
    private void OnSubmit(string text)
    {
        Client.Instance.SendPacket(Packets.SendChatMessage(input.text));

        input.text = "";
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
