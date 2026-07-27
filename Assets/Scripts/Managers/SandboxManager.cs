using RevolutionCore.Utils;
using RevolutionShared.Data;
using RevolutionShared.Networking.Packets;
using RevolutionShared.Packets;
using RevolutionShared.Rose.Data;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityRose;

/// <summary>
/// Sandbox manager.
/// </summary>
public class SandboxManager : MonoBehaviour
{
    [Tooltip("Use this only if you want to use something else that the Generated Databases")]
    [Header("Data Override")]
    public MapDatabase mapDatabase;
    public EquipmentDatabase equipmentDatabase;
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

    CharacterAppearance appearance;
    Dictionary<long, RosePlayer> players;
    Dictionary<int, NPCEntityBehavior> entities;

    /// <summary>
    /// Awake.
    /// </summary>
    private void Awake()
    {
        players = new Dictionary<long, RosePlayer>();
        entities = new Dictionary<int, NPCEntityBehavior>();

#if UNITY_EDITOR
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif
        try
        {
            if (mapDatabase == null)
                Addressables.LoadAssetAsync<MapDatabase>(nameof(MapDatabase)).Completed += OnDatabaseLoaded;

            if (equipmentDatabase == null)
                Addressables.LoadAssetAsync<EquipmentDatabase>(nameof(EquipmentDatabase)).Completed += OnDatabaseLoaded;
        }

        catch (Exception ex)
        {
            Debug.Log("Coudln't load Map database : " + ex.Message);
        }
    }

    /// <summary>
    /// When the map database is loaded.
    /// </summary>
    /// <param name="handle">Handle.</param>
    private void OnDatabaseLoaded(AsyncOperationHandle<MapDatabase> handle)
    {
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            mapDatabase = handle.Result;

            Debug.Log($"Loaded {mapDatabase.maps.Count} Maps");
        }
    }

    private void OnDatabaseLoaded(AsyncOperationHandle<EquipmentDatabase> handle)
    {
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            equipmentDatabase = handle.Result;

            Debug.Log($"Loaded Equipment Databases");
        }
    }

    /// <summary>
    /// Start.
    /// </summary>
    public async void Start()
    {
        await Client.ConnectAsync(address, port);

        appearance = new CharacterAppearance(gender, hair, face, back, body, gloves, shoes, mask, hat, weapon, shield);

        Client.SendPacket(Packets.ConnectSandbox(playerName, appearance));
    }

    /// <summary>
    /// When connected to the server.
    /// </summary>
    /// <param name="client">Client.</param>
    /// <param name="packet">Packet.</param>
    [PacketEvent(ServerCommands.SandboxConnectionResponse)]
    public void Connected(Client client, PacketIn packet)
    {
        try
        {
            var id = packet.GetLong();
            var name = packet.GetString();
            var mapID = packet.GetInt();

            var x = packet.GetFloat();
            var y = packet.GetFloat();
            var z = packet.GetFloat();

            var mapSpawn = new Vector3(x, y, z);

            var map = mapDatabase.GetMapById(mapID);

            if (map != null)
            {
                Instantiate(map.prefab);

                RoseDebug.Log($"{map.name} has been loaded");
            }

            var mainPlayer = worldManager.SpawnPlayer(true, playerName, appearance, WorldManager.RoseToUnity(mapSpawn));

            guiController.characterPreview.SetCharacterInformations(playerName, 856, 950, 350, 350, 15, "Visitor");

            mainPlayer.charModel.name = name;

            players.Add(id, mainPlayer);

            RoseDebug.Log("Character for the player has been added");
        }

        catch (Exception ex)
        {
            RoseDebug.LogException(ex);
        }
    }

    /// <summary>
    /// When message received.
    /// </summary>
    /// <param name="client">Client.</param>
    /// <param name="packet">Packet.</param>
    [PacketEvent(ServerCommands.MessageReceived)]
    public void MessageReceived(Client client, PacketIn packet)
    {
        var id = packet.GetLong();
        var message = packet.GetString();

        if (players.ContainsKey(id))
        {
            var author = players[id];

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
    public void WorldReceived(Client client, PacketIn packet)
    {
        var motd = packet.GetString();

        var playerCount = packet.GetInt();

        for (int i = 0; i < playerCount; i++)
        {
            var id = packet.GetLong();
            var playerName = packet.GetString();

            if (!players.ContainsKey(id))
            {
                var appearence = packet.Get<CharacterAppearance>();

                var x = packet.GetFloat();
                var y = packet.GetFloat();
                var z = packet.GetFloat();

                var position = new Vector3(x, y, z);

                var player = worldManager.SpawnPlayer(false, playerName, appearence, position);

                players.Add(id, player);
            }
        }
    }

    /// <summary>
    /// When a player is connected.
    /// </summary>
    /// <param name="client">Client.</param>
    /// <param name="packet">Packet.</param>
    [PacketEvent(ServerCommands.PlayerConnected)]
    public void PlayerConnected(Client client, PacketIn packet)
    {
        var id = packet.GetLong();
        var playerName = packet.GetString();

        if (!players.ContainsKey(id))
        {
            var appearence = packet.Get<CharacterAppearance>();

            var player = worldManager.SpawnPlayer(false, playerName, appearence, spawnPosition);

            players.Add(id, player);
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
    public void PlayerDisconnected(Client client, PacketIn packet)
    {
        var id = packet.GetLong();

        var player = GetRosePlayer(id);

        if (player != null)
        {
            players.Remove(id);

            Destroy(player.player);
        }
    }

    /// <summary>
    /// When a player moved.
    /// </summary>
    /// <param name="client">Client.</param>
    /// <param name="packet">Packet.</param>
    [PacketEvent(ServerCommands.PlayerMoved)]
    public void PlayerMoved(Client client, PacketIn packet)
    {
        var id = packet.GetLong();

        var x = packet.GetFloat();
        var y = packet.GetFloat();
        var z = packet.GetFloat();

        Vector3 position = new Vector3(x, y, z);

        var player = GetRosePlayer(id);

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
    public void SurroundingsReceived(Client client, PacketIn packet)
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
    /// <param name="id">ID.</param>
    /// <returns>Rose Player.</returns>
    public RosePlayer GetRosePlayer(long id)
    {
        if (players.ContainsKey(id))
        {
            return players[id];
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


namespace System.Runtime.CompilerServices
{
    public class IsExternalInit { } // Hack to use some C# 9 features in .NET Framework 4.8 (which is keyword Record here)
}
