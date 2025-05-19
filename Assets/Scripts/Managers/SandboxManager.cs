using RevolutionShared.Networking.Packets;
using RevolutionShared.Packets;
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
    [Header("Components")]
    public Transform spawnPosition;
    public CameraController cameraController;
    public WorldManager worldManager;

    /// <summary>
    /// Awake.
    /// </summary>
    private void Awake()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

        NetworkEvents.Subscribe(ServerCommands.SandboxConnectionResponse, Connected);
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
        worldManager.SpawnMainPlayer(gender, hair, face, spawnPosition.position);
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
