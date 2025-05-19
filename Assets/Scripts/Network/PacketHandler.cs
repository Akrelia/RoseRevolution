using RevolutionShared.Attributes;
using RevolutionShared.Networking.Packets;
using RevolutionShared.Packets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Packet handler.
/// </summary>
public partial class PacketHandler
{
    Dictionary<int, Func<Client, PacketIn, Task>> actions;

    /// <summary>
    /// Constructor.
    /// </summary>
    public PacketHandler()
    {
        this.actions = new Dictionary<int, Func<Client, PacketIn, Task>>();

        LoadAsyncActions();
    }

    /// <summary>
    /// Get packet from a context.
    /// </summary>
    /// <param name="context">Context.</param>
    /// <returns>Packet.</returns>
    public async Task<PacketIn> GetPacketAsync(TcpClient client, CancellationToken token)
    {
        var header = new byte[6];
        var stream = client.GetStream();

        if (stream.DataAvailable)
        {
            int bytesRead = await stream.ReadAsync(header, 0, PacketIn.HeaderLength, token);

            if (bytesRead != 0)
            {
                var size = BitConverter.ToInt32(header, 0);

                byte[] buffer = new byte[size + PacketIn.HeaderLength];

                buffer[0] = header[0];
                buffer[1] = header[1];
                buffer[2] = header[2];
                buffer[3] = header[3];
                buffer[4] = header[4];
                buffer[5] = header[5];

                if (size != 0)
                {
                    await stream.ReadAsync(buffer, PacketIn.HeaderLength, size, token);
                }

                PacketIn packet = new PacketIn(buffer);

                Debug.Log("PACKET IN : " + packet.StringFormat);

                return packet;
            }
        }

        return null;
    }

    /// <summary>
    /// Load async actions.
    /// </summary>
    public void LoadAsyncActions()
    {
        var type = this.GetType();

        foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
        {
            PacketCommand attribute = method.GetCustomAttribute<PacketCommand>();

            if (attribute != null)
            {
                if (method.ReturnType == typeof(Task))
                {
                    ParameterInfo[] parameters = method.GetParameters();

                    if (parameters.Length == 2 && parameters[1].ParameterType == typeof(PacketIn) && parameters[0].ParameterType == typeof(Client))
                    {
                        var instance = this;

                        Func<Client, PacketIn, Task> action = async (Client client, PacketIn packet) =>
                        {
                            await (Task)method.Invoke(instance, new object[] { client, packet });

                            NetworkEvents.Raise((ServerCommands)attribute.Value, client, packet);
                        };

                        if (!actions.ContainsKey(attribute.Value))
                        {
                            actions[attribute.Value] = action; // Call the action

                            NetworkEvents.RegisterEvent((ServerCommands)attribute.Value); // Register the event

                        }

                        else
                        {
                            Console.WriteLine($"Warning: Duplicate key {attribute.Value}. Method {method.Name} was skipped.");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Method {method.Name} skipped: Incorrect parameter type or count.");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Handle the packet.
    /// </summary>
    /// <param name="packet">Packet.</param>
    /// <param name="client">Client.</param>
    /// <returns>Task.</returns>
    public async Task HandlePacket(PacketIn packet, Client client)
    {
        if (actions.ContainsKey(packet.Command))
        {
            await actions[packet.Command](client, packet);
        }

        else
        {
            Debug.LogWarning($"There is no packet action for the following command : {packet.Command} ({packet.CommandString})");
        }
    }

    /// <summary>
    /// Send a packet.
    /// </summary>
    /// <param name="stream">Stream.</param>
    /// <param name="packet">Packet.</param>
    /// <returns>Task.</returns>
    public virtual async Task SendPacket(Stream stream, PacketOut packet)
    {
        Debug.Log("PACKET OUT " + packet.StringFormat);

        await stream.WriteAsync(packet.Buffer, 0, packet.Buffer.Length);

        await stream.FlushAsync();
    }

    /// <summary>
    /// Send a packet.
    /// </summary>
    /// <param name="client">Client.</param>
    /// <param name="packet">Packet.</param>
    /// <returns>Task.</returns>
    public async Task SendPacket(Client client, PacketOut packet)
    {
        await SendPacket(client.TcpClient.GetStream(), packet);
    }

    /// <summary>
    /// Send a packet.
    /// </summary>
    /// <param name="tcpClient">TCP Client.</param>
    /// <param name="packet">Packet.</param>
    /// <returns>Task.</returns>
    public async Task SendPacket(TcpClient tcpClient, PacketOut packet)
    {
        await SendPacket(tcpClient.GetStream(), packet);
    }

    /// <summary>
    /// Ping the client.
    /// </summary>
    /// <param name="client">Client.</param>
    /// <returns>Task.</returns>
    public async Task PingServer(Client client)
    {
        await SendPacket(client, Packets.Ping());
    }

    /// <summary>
    /// Pong the client.
    /// </summary>
    /// <param name="client">Client.</param>
    /// <returns>Task.</returns>
    public async Task PongServer(TcpClient client)
    {
        await SendPacket(client, Packets.Pong());
    }

}
