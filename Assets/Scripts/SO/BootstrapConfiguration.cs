using UnityEngine;

/// <summary>
/// Boostrap Configuration SO.
/// </summary>
[CreateAssetMenu(fileName = BootstrapManager.ConfigurationName, menuName = "Data/Bootstrap")]
public class BootstrapConfiguration : ScriptableObject
{
    [Header("Systems")]
    public GameObject guiPrefab;
    public GameObject debugConsolePrefab;
}
