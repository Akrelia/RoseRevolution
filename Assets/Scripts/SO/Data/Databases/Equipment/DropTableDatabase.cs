using NUnit.Framework;
using RevolutionShared.Rose.Data.NPC.Drops;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Drop table database.
/// </summary>
public class DropTableDatabase : ScriptableObject
{
    public List<DropTableSO> entries = new List<DropTableSO>();

    /// <summary>
    /// Get entry by its id.
    /// </summary>
    /// <param name="id">ID.</param>
    /// <returns>Entry.</returns>
    public DropTableSO GetEntry(int id)
    {
        return entries.FirstOrDefault(x => x.id == id);
    }
}
