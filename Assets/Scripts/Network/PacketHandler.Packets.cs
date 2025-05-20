using RevolutionShared.Attributes;
using RevolutionShared.Networking.Packets;
using RevolutionShared.Packets;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Packets for Packet Handler.
/// </summary>
public partial class PacketHandler
{
    /// <summary>
    /// Action - Ping.
    /// </summary>
    /// <param name="user">User.</param>
    /// <param name="packet">Packet.</param>
    /// <returns>Task.</returns>
    [PacketCommand(ServerCommands.Ping)]
    public async Task Ping(Client client, PacketIn packet)
    {
        client.SendPacket(Packets.Pong());

        await Task.CompletedTask;
    }

    /// <summary>
    /// Action - Pong.
    /// </summary>
    /// <param name="user">User.</param>
    /// <param name="packet">Packet.</param>
    /// <returns>Task.</returns>
    [PacketCommand(ServerCommands.Pong)]
    public async Task Pong(Client client, PacketIn packet)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Action - Sandobox connected.
    /// </summary>
    /// <param name="user">User.</param>
    /// <param name="packet">Packet.</param>
    /// <returns>Task.</returns>
    [PacketCommand(ServerCommands.SandboxConnectionResponse)]
    public async Task Connected(Client client, PacketIn packet)
    {
        await SendPacket(client, Packets.GetWorld());
    }

    /// <summary>
    /// Action - Message Received.
    /// </summary>
    /// <param name="client">Client.</param>
    /// <param name="packet">Packet.</param>
    /// <returns>Task.</returns>
    [PacketCommand(ServerCommands.MessageReceived)]
    public async Task ChatMessageReceived(Client client, PacketIn packet)
    {
        await Task.CompletedTask;
    }
}

/// <summary>
/// All packets.
/// </summary>
public static class Packets
{
    /// <summary>
    /// Ping Packet.
    /// </summary>
    /// <returns>Packet.</returns>
    public static PacketOut Ping()
    {
        PacketOut packet = new PacketOut(ClientCommands.Ping);

        return packet;
    }

    /// <summary>
    /// Pong Packet.
    /// </summary>
    /// <returns>Packet.</returns>
    public static PacketOut Pong()
    {
        PacketOut packet = new PacketOut(ClientCommands.Pong);

        return packet;
    }

    /// <summary>
    /// Packet - Connect to the Sandbox server.
    /// </summary>
    /// <param name="username">Username.</param>
    /// <returns>Packet.</returns>
    public static PacketOut ConnectSandbox(string username)
    {
        PacketOut packet = new PacketOut(ClientCommands.ConnectSandbox);

        packet.Add(username);

        return packet;
    }

    /// <summary>
    /// Packet - Disconnect from the Sandbox server.
    /// </summary>
    /// <returns>Packet.</returns>
    public static PacketOut DisconnectSandbox()
    {
        PacketOut packet = new PacketOut(ClientCommands.DisconnectSandbox);

        return packet;
    }

    /// <summary>
    /// Packet - Send chat Message.
    /// </summary>
    /// <param name="message">Message.</param>
    /// <returns>Packet.</returns>
    public static PacketOut SendChatMessage(string message)
    {
        PacketOut packet = new PacketOut(ClientCommands.SendNormalChat);

        packet.Add(message);

        return packet;
    }

    /// <summary>
    /// Packet - Get world.
    /// </summary>
    /// <returns></returns>
    public static PacketOut GetWorld()
    {
        PacketOut packet = new PacketOut(ClientCommands.GetWorld);

        return packet;
    }
}
