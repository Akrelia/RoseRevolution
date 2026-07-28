using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary>
/// Bootstrap manager - Will setup every system needed.
/// </summary>
public class BootstrapManager : MonoBehaviour
{
    [Header("Shortcuts")]
    public KeyCode toggleConsoleKey = KeyCode.F1;

    /// <summary>
    /// Singleton.
    /// </summary>
    public static BootstrapManager Instance { get; private set; }
    public const string ConfigurationName = "Bootstrap Configuration";

    public GUIBootstrap gui;
    public DebugConsole debugConsole;

    /// <summary>
    /// Initialize the bootstrap.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static async void Initialize()
    {
        if (Instance != null) return;

        var go = new GameObject("[Bootstrap]");

        Instance = go.AddComponent<BootstrapManager>();

        DontDestroyOnLoad(go);

        var configuration = await Addressables.LoadAssetAsync<BootstrapConfiguration>(ConfigurationName).Task; // This is different of the Editor script since await here is safe

        if (configuration != null)
        {
            Instance.SpawnSystems(configuration);

            Debug.Log("Bootstrap loaded !");
        }

        else
        {
            Debug.LogError($"Bootstrap configuration file : '{ConfigurationName}' not found ! This shouldn't happen, but if it does, go to the tools and generate one");
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleConsoleKey))
        {
        }
    }

    /// <summary>
    /// Spawn the systems.
    /// </summary>
    /// <param name="configuration">Configuration.</param>
    private void SpawnSystems(BootstrapConfiguration configuration)
    {
        gui = SpawnSystem<GUIBootstrap>(configuration.guiPrefab);
        debugConsole = SpawnSystem<DebugConsole>(configuration.debugConsolePrefab, gui.transform);

        gui.debugConsole = debugConsole;
    }

    /// <summary>
    /// Spawn a system.
    /// </summary>
    /// <typeparam name="T">T component.</typeparam>
    /// <param name="prefab">Prefab.</param>
    /// <returns>Component.</returns>
    /// <param name="transform">Optionnal transform.</param>
    private T SpawnSystem<T>(GameObject prefab, Transform transform = null) where T : Component
    {
        if (prefab != null)
        {
            var instance = Instantiate(prefab, transform != null ? transform : this.transform);

            var component = instance.GetComponent<T>();

            return component;
        }

        else
        {
            Debug.LogError($"Can't find the {typeof(T)} prefab in the configuration");
        }

        return null;
    }
}
