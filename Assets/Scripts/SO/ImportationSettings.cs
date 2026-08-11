using UnityEngine;

/// <summary>
/// Importation settings.
/// </summary>
[CreateAssetMenu(menuName = "SO/Importation Settings")]
public class ImportationSettings : ScriptableObject
{
    [Header("Values")]
    [Range(0, 16)]
    public int anisotropyLevel = 8;
    [Header("Shaders")]
    public Shader terrainShader;
    public Shader objectShader; 
    public Shader entityShader;
    public Shader playerShader;
}