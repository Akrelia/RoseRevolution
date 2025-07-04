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
    public Vector3 spawnPosition;
    public CameraController cameraController;
    public WorldManager worldManager;
    public GUIController guiController;

    Dictionary<Guid, RosePlayer> players;
    Dictionary<int, RoseNpc> entities;

    /// <summary>
    /// Awake.
    /// </summary>
    private void Awake()
    {
        players = new Dictionary<Guid, RosePlayer>();
        entities = new Dictionary<int, RoseNpc>();

#if UNITY_EDITOR
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif
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
        var mainPlayer = worldManager.SpawnPlayer(true, gender, playerName, hair, face, back, body, gloves, shoes, mask, hat, weapon, shield, spawnPosition);

        guiController.characterPreview.SetCharacterInformations(playerName, 1200, 1200, 960, 960, 1, "Visitor");

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
    /// When the world is received.
    /// </summary>
    /// <param name="client">Client.</param>
    /// <param name="packet">Packet.</param>
    [PacketEvent(ServerCommands.SendWorld)]
    private void WorldReceived(Client client, PacketIn packet)
    {
        var motd = packet.GetString();

        var playerCount = packet.GetInt();

        for (int i = 0; i < playerCount; i++)
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

                var x = packet.GetFloat();
                var y = packet.GetFloat();
                var z = packet.GetFloat();

                var position = new Vector3(x, y, z);

                var player = worldManager.SpawnPlayer(false, playerGender, playerName, playerHair, playerFace, playerBack, playerBody, playerGloves, playerShoes, playerMask, playerHat, playerWeapon, playerSubweapon, position);

                players.Add(guid, player);
            }
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

            var player = worldManager.SpawnPlayer(false, playerGender, playerName, playerHair, playerFace, playerBack, playerBody, playerGloves, playerShoes, playerMask, playerHat, playerWeapon, playerSubweapon, spawnPosition);

            players.Add(guid, player);
        }

        else
        {
            RoseDebug.LogWarning("Trying to add a player that's already exists !");
        }
    }

    /// <summary>
    /// When a player is disconnected.
    /// </summary>
    /// <param name="client">Client.</param>
    /// <param name="packet">Packet.</param>
    [PacketEvent(ServerCommands.PlayerDisconnected)]
    private void PlayerDisonnected(Client client, PacketIn packet)
    {
        var guid = new Guid(packet.GetBytes(16));

        var player = GetRosePlayer(guid);

        if (player != null)
        {
            players.Remove(guid);

            Destroy(player.player);
        }
    }

    /// <summary>
    /// When a player moved.
    /// </summary>
    /// <param name="client">Client.</param>
    /// <param name="packet">Packet.</param>
    [PacketEvent(ServerCommands.PlayerMoved)]
    private void PlayerMoved(Client client, PacketIn packet)
    {
        var guid = new Guid(packet.GetBytes(16));

        var x = packet.GetFloat();
        var y = packet.GetFloat();
        var z = packet.GetFloat();

        Vector3 position = new Vector3(x, y, z);

        var player = GetRosePlayer(guid);

        if (player != null)
        {
            player.player.GetComponent<PlayerController>().destinationPosition = position;
        }
    }

    /// <summary>
    /// When you received the surroundings.
    /// </summary>
    /// <param name="client">Client.</param>
    /// <param name="packet">Packet.</param>
    [PacketEvent(ServerCommands.AddEntities)]
    private void SurroundingsReceived(Client client, PacketIn packet)
    {
        var count = packet.GetInt();

        for (int i = 0; i < count; i++)
        {
            var id = packet.GetInt();
            var dataId = packet.GetInt();
            var x = packet.GetFloat();
            var y = packet.GetFloat();
            var z = packet.GetFloat();

            var position = new Vector3(x, y, z);

            var entity = worldManager.SpawnEntity(id, dataId, position);

            entities.Add(id, entity);
        }
    }

    /// <summary>
    /// Get the rose player using its id.
    /// </summary>
    /// <param name="guid">GUID.</param>
    /// <returns>Rose Player.</returns>
    public RosePlayer GetRosePlayer(Guid guid)
    {
        if (players.ContainsKey(guid))
        {
            return players[guid];
        }

        return null;
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
