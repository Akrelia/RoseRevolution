using RevolutionShared.Networking.Packets;
using RevolutionShared.Packets;
using System;
using TMPro;
using UnityEngine;

/// <summary>
/// GUI Controller.
/// </summary>
public class GUIController : MonoBehaviour
{
    [Header("Components")]
    public TextMeshProUGUI debugText;
    public ChatController chatController;

    /// <summary>
    /// Awake.
    /// </summary>
    public void Awake()
    {
        Application.logMessageReceived += HandleLog;

        NetworkEvents.Subscribe(ServerCommands.SendWorld, WorldReceived);
        NetworkEvents.Subscribe(ServerCommands.MessageReceived, NormalChatReceived);
    }

    /// <summary>
    /// When connected to the server.
    /// </summary>
    /// <param name="client">Client.</param>
    /// <param name="packet">Packet.</param>
    private void WorldReceived(Client client, PacketIn packet)
    {
        var motd = packet.GetString();

        chatController.AddSystemMessage(motd);
    }

    /// <summary>
    /// When a normal chat message is received.
    /// </summary>
    /// <param name="client">Client.</param>
    /// <param name="packet">Packet.</param>
    private void NormalChatReceived(Client client, PacketIn packet)
    {
    }

    /// <summary>
    /// Hook the Unity logs to display them in-game.
    /// </summary>
    /// <param name="logString">Log string.</param>
    /// <param name="stackTrace">Stack trace.</param>
    /// <param name="type">Log's type.</param>
    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        debugText.text += ($"{logString}{Environment.NewLine}");
    }
}
