using RevolutionShared.Rose.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityRose.Game;

/// <summary>
/// Rose map database.
/// </summary>
public class MapDatabase : ScriptableObject
{
    public List<RoseMapEntry> maps = new();

    /// <summary>
    /// Get a map by its id.
    /// </summary>
    /// <param name="id">ID map.</param>
    /// <returns>Map.</returns>
    public RoseMapEntry GetMapById(int id)
    {
        var map = maps.FirstOrDefault(m => m.id == id);

        if (map == null)
        {
            Debug.LogWarning($"Map with ID {id} not found in the database.");
        }

        return map;
    }
}

/// <summary>
/// Rose map entry.
/// </summary>
[Serializable]
public class RoseMapEntry
{
    public int id;
    public MapData data;
    public GameObject prefab;
}
