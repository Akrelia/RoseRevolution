using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Skybox data.
/// </summary>
public class SkyboxData : ScriptableObject
{
    public int Id;

    public Mesh Mesh;

    public Material Material;

    public Color BackgroundColor1;
    public Color BackgroundColor2;
    public Color BackgroundColor3;
    public Color BackgroundColor4;
    public Color AmbientCharacter1;
    public Color DiffuseCharacter1;
    public Color AmbientCharacter2;
    public Color DiffuseCharacter2;
    public Color AmbientCharacter3;
    public Color DiffuseCharacter3;
    public Color AmbientCharacter4;
    public Color DiffuseCharacter4;
}