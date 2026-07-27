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
    public List<DropTableSO> entries;

    public DropTableSO GetEntry(int id)
    {
        return entries.FirstOrDefault(x => x.id == id);
    }
}
