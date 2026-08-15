using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Effect database.
/// </summary>
public class EffectDatabase : ScriptableObject
{
    public List<EffectDatabaseEntry> entries;

    /// <summary>
    /// Get effect database entry by id.
    /// </summary>
    /// <param name="id"></param>
    /// <returns>Entry.</returns>
    public EffectDatabaseEntry Get(int id)
    {
        return entries.FirstOrDefault(e => e.id == id);
    }
}

/// <summary>
/// Effect database entry.
/// </summary>
[Serializable]
public class EffectDatabaseEntry
{
    public int id;
    public GameObject prefab;
}
