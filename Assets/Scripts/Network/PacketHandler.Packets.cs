using RevolutionShared.Attributes;
using RevolutionShared.Data;
using RevolutionCore.Utils;
using RevolutionShared.Networking.Packets;
using RevolutionShared.Packets;
using RevolutionShared.Rose.Data;
using System.Threading.Tasks;
using UnityEngine;
using UnityRose;
using System;

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
    /// Action - Send World.
    /// </summary>
    /// <param name="client">Client.</param>
    /// <param name="packet">Packet.</param>
    /// <returns>Task.</returns>
    [PacketCommand(ServerCommands.SendWorld)]
    public async Task WorldReceived(Client client, PacketIn packet)
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
    public static PacketOut ConnectSandbox(string username, CharacterAppearance appearence)
    {
        PacketOut packet = new PacketOut(ClientCommands.ConnectSandbox);

        packet.Add(username);

        packet.Add(appearence);

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

    /// <summary>
    /// Packet - Move.
    /// </summary>
    /// <param name="position">Position.</param>
    /// <returns>Packet.</returns>
    public static PacketOut Move(Vector3 position)
    {
        PacketOut packet = new PacketOut(ClientCommands.Move);

        WorldPosition worldposition = position.ToWorldPosition();

        packet.Add(worldposition);

        return packet;
    }

    /// <summary>
    /// Packet - GM Command Spawn
    /// </summary>
    /// <param name="enemyID">Enemy ID.</param>
    /// <param name="amount">Amount.</param>
    /// <returns>Packet.</returns>
    public static PacketOut GMCommandSpawn(int enemyID, int amount)
    {
        PacketOut packet = new PacketOut(ClientCommands.GMCommandSpawnMonster);

        packet.Add(enemyID);
        packet.Add(amount);

        return packet;
    }
}
