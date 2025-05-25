using RevolutionShared.Networking.Packets;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class Client
{
    TcpClient tcpClient;
    PacketHandler packetHandler;
    CancellationTokenSource cancellationTokenSource;

    private Task updateTask;
    private static Client _instance;
    public static Client Instance => _instance ??= new Client();

    /// <summary>
    /// Constructor.
    /// </summary>
    public Client()
    {
        tcpClient = new TcpClient();

        packetHandler = new PacketHandler();
    }

    /// <summary>
    /// Connect the client.
    /// </summary>
    /// <param name="address">Address.</param>
    /// <param name="port">Port.</param>
    /// <returns>Task.</returns>
    public async Task ConnectAsync(string address, int port)
    {
        await tcpClient.ConnectAsync(address, port);

        RoseDebug.Log("Connecting ...");

        if (tcpClient.Connected)
        {
            cancellationTokenSource = new CancellationTokenSource();
            updateTask = UpdateAsync(cancellationTokenSource.Token);

            RoseDebug.Log($"Connected to server ! ({address}:{port})");
        }
    }

    /// <summary>
    /// Update the client.
    /// </summary>
    public async Task UpdateAsync(CancellationToken cancelToken)
    {
        try
        {
            while (!cancelToken.IsCancellationRequested)
            {
                var packet = await packetHandler.GetPacketAsync(tcpClient, cancelToken);

                if (packet != null)
                {
                    await packetHandler.HandlePacket(packet, this);
                }

                await Task.Delay(20);
            }
        }

        catch (Exception ex)
        {
            RoseDebug.LogError($"Client crashed : {ex.Message}{ex.StackTrace}");
        }

        finally
        {
            tcpClient.Close();
        }
    }

    /// <summary>
    /// Send a packet.
    /// </summary>
    /// <param name="packet">Packet.</param>
    public void SendPacket(PacketOut packet)
    {
        _ = packetHandler.SendPacket(tcpClient.GetStream(), packet);
    }

    /// <summary>
    /// Close the client connection.
    /// </summary>
    public async Task CloseAsync()
    {
        cancellationTokenSource.Cancel();

        if (updateTask != null)
            await updateTask;

        tcpClient?.Close();

        cancellationTokenSource.Dispose();

        RoseDebug.Log("Client is now closed");
    }

    /// <summary>
    /// Get the packet handler.
    /// </summary>
    public PacketHandler PacketHandler
    {
        get { return packetHandler; }
    }

    /// <summary>
    /// Get the TCP client.
    /// </summary>
    public TcpClient TcpClient
    {
        get { return tcpClient; }
    }
}
