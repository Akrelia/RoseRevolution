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
    public int weapon;
    public int shield;
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

#if UNITY_EDITOR
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif
        //NetworkEvents.Subscribe(ServerCommands.SandboxConnectionResponse, Connected);
        //NetworkEvents.Subscribe(ServerCommands.MessageReceived, MessageReceived);
        //NetworkEvents.Subscribe(ServerCommands.PlayerConnected, PlayerConnected);
    }

    /// <summary>
    /// Start.
    /// </summary>
    public async void Start()
    {
        await Client.ConnectAsync(address, port);

        Client.SendPacket(Packets.ConnectSandbox(playerName, gender, hair, face, back, body, gloves, shoes, mask, hat, weapon, shield));
    }

    /// <summary>
    /// When connected to the server.
    /// </summary>
    /// <param name="client">Client.</param>
    /// <param name="packet">Packet.</param>
    [PacketEvent(ServerCommands.SandboxConnectionResponse)]
    private void Connected(Client client, PacketIn packet)
    {
        var mainPlayer = worldManager.SpawnPlayer(true, gender, playerName, hair, face, back, body, gloves, shoes, mask, hat, weapon, shield, spawnPosition.position);

        var guid = new Guid(packet.GetBytes(16));
        var name = packet.GetString();

        mainPlayer.charModel.name = name;

        players.Add(guid, mainPlayer);

        RoseDebug.Log("Main Character added");
    }

    /// <summary>
    /// When message received.
    /// </summary>
    /// <param name="client">Client.</param>
    /// <param name="packet">Packet.</param>
    [PacketEvent(ServerCommands.MessageReceived)]
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
            RoseDebug.LogWarning("Received a message from a missing player !");
        }
    }

    /// <summary>
    /// When a player is connected.
    /// </summary>
    /// <param name="client">Client.</param>
    /// <param name="packet">Packet.</param>
    [PacketEvent(ServerCommands.PlayerConnected)]
    private void PlayerConnected(Client client, PacketIn packet)
    {
        var guid = new Guid(packet.GetBytes(16));
        var playerName = packet.GetString();

        if (!players.ContainsKey(guid))
        {
            var playerGender = (GenderType)packet.GetByte();
            var playerHair = packet.GetByte();
            var playerFace = packet.GetByte();
            var playerBack = packet.GetInt();
            var playerBody = packet.GetInt();
            var playerGloves = packet.GetInt();
            var playerShoes = packet.GetInt();
            var playerMask = packet.GetInt();
            var playerHat = packet.GetInt();
            var playerWeapon = packet.GetInt();
            var playerSubweapon = packet.GetInt();

            var player = worldManager.SpawnPlayer(false, playerGender, playerName, playerHair, playerFace, playerBack, playerBody, playerGloves, playerShoes, playerMask, playerHat,playerWeapon,playerSubweapon, spawnPosition.position);

            players.Add(guid, player);
        }

        else
        {
            RoseDebug.LogWarning("Trying to add a player that's already exists !");
        }
    }

#if UNITY_EDITOR
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
#endif
    /// <summary>
    /// Get the client.
    /// </summary>
    public Client Client
    {
        get { return Client.Instance; }
    }
}
