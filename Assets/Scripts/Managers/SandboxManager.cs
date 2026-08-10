using RevolutionCore.Utils;
using RevolutionShared.Data;
using RevolutionShared.Networking.Packets;
using RevolutionShared.Packets;
using RevolutionShared.Rose.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UIElements;
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
    public NPCDatabase npcDatabase;
    public SkyboxDatabase skyboxDatabase;
    [Header("Server")]
    public string address;
    public short port;
    [Header("Player")]
    public string playerName;
    public string clanName;
    public Sprite clanSprite;
    [Range(1, 7)]
    public int clanGrade;
    [Header("Appearence")]
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
    public WorldGUIController worldGUIController;

    CharacterAppearance appearance;
    Dictionary<long, RosePlayer> players;
    Dictionary<int, EntityBehavior> entities;

    /// <summary>
    /// Awake.
    /// </summary>
    private void Awake()
    {
        players = new Dictionary<long, RosePlayer>();
        entities = new Dictionary<int, EntityBehavior>();

#if UNITY_EDITOR
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged; // This is just for sending a proper packet like we will do in the standalone client, but for the Editor special case
#endif
        try
        {
            if (mapDatabase == null)
                Addressables.LoadAssetAsync<MapDatabase>(nameof(MapDatabase)).Completed += OnDatabaseLoaded;

            if (equipmentDatabase == null)
                Addressables.LoadAssetAsync<EquipmentDatabase>(nameof(EquipmentDatabase)).Completed += OnDatabaseLoaded;

            if (npcDatabase == null)
                Addressables.LoadAssetAsync<NPCDatabase>(nameof(NPCDatabase)).Completed += OnDatabaseLoaded;

            if (skyboxDatabase == null)
                Addressables.LoadAssetAsync<SkyboxDatabase>(nameof(SkyboxDatabase)).Completed += OnDatabaseLoaded;
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

    /// <summary>
    /// When the equipment database is loaded.
    /// </summary>
    /// <param name="handle">Handle.</param>
    private void OnDatabaseLoaded(AsyncOperationHandle<EquipmentDatabase> handle)
    {
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            equipmentDatabase = handle.Result;

            Debug.Log($"Loaded Equipment Databases");
        }
    }

    /// <summary>
    /// When the NPC database is loaded.
    /// </summary>
    /// <param name="handle">Handle.</param>
    private void OnDatabaseLoaded(AsyncOperationHandle<NPCDatabase> handle)
    {
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            npcDatabase = handle.Result;

            Debug.Log($"Loaded NPC Database");
        }
    }

    /// <summary>
    /// When the Skybox database is loaded.
    /// </summary>
    /// <param name="handle">Handle.</param>
    private void OnDatabaseLoaded(AsyncOperationHandle<SkyboxDatabase> handle)
    {
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            skyboxDatabase = handle.Result;

            Debug.Log($"Loaded Skybox Database");

            ConnectToServer();
        }
    }

    /// <summary>
    /// Start.
    /// </summary>
    public async void ConnectToServer()
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

            var motd = packet.GetString();

            var x = packet.GetFloat();
            var y = packet.GetFloat();
            var z = packet.GetFloat();

            var mapSpawn = new Vector3(x, y, z);

            var map = worldManager.SpawnMap(mapID);

            var mainPlayer = worldManager.SpawnPlayer(true, playerName, clanName, clanGrade, clanSprite, appearance, WorldManager.RoseToUnity(mapSpawn));

            guiController.characterPreview.SetCharacterInformations(playerName, 856, 950, 350, 350, 15, "Visitor");

            mainPlayer.charModel.name = name;

            players.Add(id, mainPlayer);

            EntitiesReceived(client, packet);

            guiController.chatController.AddSystemMessage(motd);

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

            author.player.GetComponentInChildren<PlayerGUIController>().bubble.ShowMessage(message);
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
            var clanName = packet.GetString();
            var clanGrade = packet.GetByte();

            if (!players.ContainsKey(id))
            {
                var appearence = packet.GetNew<CharacterAppearance>();

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
            var appearence = packet.GetNew<CharacterAppearance>();

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
    public void EntitiesReceived(Client client, PacketIn packet)
    {
        var count = packet.GetInt();

        for (int i = 0; i < count; i++)
        {
            var entityInfos = packet.GetNew<EntityInfos>();
            var subInfos = packet.GetNew<EntitySubInfos>();

            var entry = npcDatabase.GetEntry(entityInfos.dataID);
            var position = entityInfos.position.ToVector3();

            var entity = worldManager.SpawnEntity(entityInfos, subInfos, entry, position);

            entities.Add(entityInfos.id, entity);
        }
    }

    /// <summary>
    /// When a GM executed a command..
    /// </summary>
    /// <param name="client">Client.</param>
    /// <param name="packet">Packet.</param>
    [PacketEvent(ServerCommands.GMCommandExecuted)]
    public void GMCommandExecuted(Client client, PacketIn packet)
    {
        var username = packet.GetString();
        var message = packet.GetString();

        guiController.chatController.AddSystemMessage($"{username} issued the command {message}");
    }

    [PacketEvent(ServerCommands.EntityUpdate)]
    public void EntityUpdate(Client client, PacketIn packet)
    {
        var entityID = packet.GetInt();

        var position = packet.GetReadable<WorldPosition>();

        if (entities.ContainsKey(entityID))
        {
            var entity = entities[entityID];

            entity.SetDestination(position);
        }

        else
        {
            RoseDebug.LogWarning($"Received an update for an entity that doesn't exist : {entityID}");
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