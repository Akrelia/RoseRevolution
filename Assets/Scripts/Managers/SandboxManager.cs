using RevolutionShared.Networking.Packets;
using RevolutionShared.Packets;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityRose;

/// <summary>
/// Sandbox manager.
/// </summary>
public class SandboxManager : MonoBehaviour
{
    [Header("Server")]
    public string address;
    public short port;
    [Header("Player")]
    public string playerName;
    public GenderType gender;
    public byte hair;
    public byte face;
    public int back;
    public int body;
    public int gloves;
    public int shoes;
    public int mask;
    public int hat;
    [Header("Components")]
    public Transform spawnPosition;
    public CameraController cameraController;
    public WorldManager worldManager;
    public GUIController guiController;

    Dictionary<Guid, RosePlayer> players;

    /// <summary>
    /// Awake.
    /// </summary>
    private void Awake()
    {
        players = new Dictionary<Guid, RosePlayer>();

        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

        NetworkEvents.Subscribe(ServerCommands.SandboxConnectionResponse, Connected);
        NetworkEvents.Subscribe(ServerCommands.MessageReceived, MessageReceived);
    }

    /// <summary>
    /// Start.
    /// </summary>
    public async void Start()
    {
        await Client.ConnectAsync(address, port);

        Client.SendPacket(Packets.ConnectSandbox(playerName));
    }

    /// <summary>
    /// When connected to the server.
    /// </summary>
    /// <param name="client">Client.</param>
    /// <param name="packet">Packet.</param>
    private void Connected(Client client, PacketIn packet)
    {
        var mainPlayer = worldManager.SpawnMainPlayer(gender,playerName, hair, face, back, body, gloves, shoes, mask, hat, spawnPosition.position);

        var guid = new Guid(packet.GetBytes(16));
        var name = packet.GetString();

        mainPlayer.charModel.name = name;

        players.Add(guid, mainPlayer);

        Debug.Log("Main Character added");
    }

    private void MessageReceived(Client client, PacketIn packet)
    {
        var guid = new Guid(packet.GetBytes(16));
        var message = packet.GetString();

        if (players.ContainsKey(guid))
        {
            var author = players[guid];

            guiController.chatController.AddPlayerMessage(author.charModel.name, message);

            author.player.GetComponentInChildren<EntityGUIController>().bubble.ShowMessage(message);
        }

        else
        {
            Debug.LogWarning("Received a message from a missing player !");
        }
    }

    /// <summary>
    /// Stop the client when exiting play mode.
    /// </summary>
    /// <param name="state">State</param>
    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            Client.SendPacket(Packets.DisconnectSandbox());

            _ = Client.CloseAsync();
        }
    }

    /// <summary>
    /// Get the client.
    /// </summary>
    public Client Client
    {
        get { return Client.Instance; }
    }
}
