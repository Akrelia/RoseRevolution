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
    Task updateTask; 
    CancellationTokenSource cancellationTokenSource;

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

    private async Task OnPing(TcpClient client)
    {
        await packetHandler.PongServer(client);
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

        Debug.Log("Connecting ...");

        if (tcpClient.Connected)
        {
            cancellationTokenSource = new CancellationTokenSource();
            updateTask = UpdateAsync(cancellationTokenSource.Token);

            Debug.Log("Connected to server!");
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
            Debug.LogError($"Client crashed : {ex.Message}{ex.StackTrace}");
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

        if (tcpClient.Connected)
            tcpClient.Close();

        cancellationTokenSource.Dispose();

        Debug.Log("Client is now closed");
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
