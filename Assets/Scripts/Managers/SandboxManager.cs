using RevolutionCore.Utils;
using RevolutionShared.Data;
using RevolutionShared.Networking.Packets;
using RevolutionShared.Packets;
using RevolutionShared.Rose.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UIElements;
using UnityRose;
using UnityRose.Game;

/// <summary>
/// Sandbox manager.
/// </summary>
public class SandboxManager : MonoBehaviour
{
    [Tooltip("Use this only if you want to use something else that the Generated Databases")]
    [Header("Data Override")]
    public MapDatabase mapDatabase;
    public EquipmentDatabase equipmentDatabase;
    public CharacterDatabase characterDatabase;
    public EffectDatabase effectDatabase;
    public NPCDatabase npcDatabase;
    public SkyboxDatabase skyboxDatabase;
    [Header("Server")]
    public bool useLocal;
    public string localAddress;
    public short localPort;
    public string address;
    public short port;
    [Header("Player")]
    public string playerName;
    public string clanName;
    public Sprite clanSprite;
    [Range(1, 7)]
    public byte clanGrade;
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
    public CameraController cameraController;
    public WorldManager worldManager;
    public GUIController guiController;
    public WorldGUIController worldGUIController;

    RoseMap currentMap;
    Dictionary<long, RosePlayer> players;
    Dictionary<int, EntityBehavior> entities;

    public static string _playerName;
    public static string _clanName;
    public static Sprite _clanSprite;
    public static byte _clanGrade;
    public static CharacterAppearance appearance;

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
            mapDatabase = Utils.LoadDatabase<MapDatabase>();
            equipmentDatabase = Utils.LoadDatabase<EquipmentDatabase>();
            npcDatabase = Utils.LoadDatabase<NPCDatabase>();
            skyboxDatabase = Utils.LoadDatabase<SkyboxDatabase>();
            characterDatabase = Utils.LoadDatabase<CharacterDatabase>();
            effectDatabase = Utils.LoadDatabase<EffectDatabase>();

            ConnectToServer();
        }

        catch (Exception ex)
        {
            Debug.Log("Coudln't load databases : " + ex.Message);
        }

        if (!string.IsNullOrEmpty(_playerName))
            playerName = _playerName;

        if (!string.IsNullOrEmpty(_clanName))
            clanName = _clanName;

        if (_clanSprite != null)
            clanSprite = _clanSprite;

        if (_clanGrade != 0)
            clanGrade = _clanGrade;
    }

    /// <summary>
    /// Start.
    /// </summary>
    public async void ConnectToServer()
    {
        if (!useLocal)
        {
            await Client.ConnectAsync(address, port);
        }

        else
        {
            await Client.ConnectAsync(localAddress, localPort);
        }

        if (appearance == null)
        {
            appearance = new CharacterAppearance(gender, hair, face, back, body, gloves, shoes, mask, hat, weapon, shield);
        }

        Client.SendPacket(Packets.ConnectSandbox(playerName, clanName, clanSprite ? clanSprite.texture.EncodeToPNG() : new byte[0], clanGrade, appearance));
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
            var playerOwnID = packet.GetLong();
            var playerInfos = packet.GetNew<CharacterInfos>();

            var mapID = packet.GetInt();
            var npcCount = packet.GetInt();

            currentMap = worldManager.SpawnMap(mapID);

            for (int i = 0; i < npcCount; i++)
            {
                var entityInfos = packet.GetNew<EntityInfos>();
                var subInfos = packet.GetNew<NPCInfos>();

                worldManager.SpawnNPC(entityInfos, subInfos, entityInfos.position.ToVector3());
            }

            var motd = packet.GetString();

            var mapSpawn = packet.GetReadable<WorldPosition>();

            var mainPlayer = worldManager.SpawnPlayer(true, playerInfos, _clanSprite != null ? _clanSprite : Utils.BytesToSprite(playerInfos.clanIcon), appearance, mapSpawn.ToVector3());

            guiController.characterPreview.SetCharacterInformations(playerName, 950, 950, 350, 350, 15, "Visitor");

            mainPlayer.charModel.name = playerName;

            players.Add(playerOwnID, mainPlayer);

            EntitiesReceived(client, packet);

            guiController.chatController.AddSystemMessage(motd);

            guiController.chatController.AddSystemMessage($"{playerInfos.name} has connected");

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

            if (!players.ContainsKey(id))
            {
                var appearence = packet.GetNew<CharacterAppearance>();
                var infos = packet.GetNew<CharacterInfos>();
                var position = packet.GetReadable<WorldPosition>().ToVector3();

                var player = worldManager.SpawnPlayer(false, infos, Utils.BytesToSprite(infos.clanIcon), appearence, position);

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

        if (!players.ContainsKey(id))
        {
            var appearence = packet.GetNew<CharacterAppearance>();
            var infos = packet.GetNew<CharacterInfos>();
            var position = packet.GetReadable<WorldPosition>().ToVector3();

            var player = worldManager.SpawnPlayer(false, infos, Utils.BytesToSprite(infos.clanIcon), appearence, position);

            guiController.chatController.AddSystemMessage($"{infos.name} has connected");

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

        guiController.chatController.AddSystemMessage($"{player.player.name} just left");

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
            var subInfos = packet.GetNew<EnemyInfos>();

            var entity = worldManager.SpawnEntity(entityInfos, subInfos, entityInfos.position.ToVector3(), false);

            if (entity != null)
            {
                entities.Add(entityInfos.id, entity);
            }
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

    [PacketEvent(ServerCommands.EntityDeath)]
    public void EntityDeath(Client client, PacketIn packet)
    {
        var entityID = packet.GetInt();

        if (entities.ContainsKey(entityID))
        {
            var entity = entities[entityID];

            entity.Die();
        }

        else
        {
            RoseDebug.LogWarning($"Received an death event for an entity that doesn't exist : {entityID}");
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

    /// <summary>
    /// Get the current map.
    /// </summary>
    public RoseMap CurrentMap
    {
        get { return currentMap; }
    }
}


namespace System.Runtime.CompilerServices
{
    public class IsExternalInit { } // Hack to use some C# 9 features in .NET Framework 4.8 (which is keyword Record here)
}