using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Skybox database.
/// </summary>
public class SkyboxDatabase : ScriptableObject
{
    public List<SkyboxData> entries;

    public SkyboxData Get(int id)
    {
        return entries.FirstOrDefault(e => e.Id == id);
    }
}