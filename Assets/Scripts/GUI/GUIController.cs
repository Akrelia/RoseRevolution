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
    public ChatController chatController;
    public DebugConsole debugConsole;

    /// <summary>
    /// When connected to the server.
    /// </summary>
    /// <param name="client">Client.</param>
    /// <param name="packet">Packet.</param>
    [PacketEvent(ServerCommands.SendWorld)]
    private void WorldReceived(Client client, PacketIn packet)
    {
        var motd = packet.GetString();

        chatController.AddSystemMessage(motd);
    }
}
