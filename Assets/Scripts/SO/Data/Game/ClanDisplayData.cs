using System;
using System.Linq;
using UnityEngine;

/// <summary>
/// Display Clan Data
/// </summary>
[CreateAssetMenu(menuName = "Data/Clan Data")]
public class ClanDisplayData : ScriptableObject
{
    [SerializeField]
    FakeDictionary<int, ClanDisplayEntry> entries;

    public ClanDisplayEntry Get(int id)
    {
        if (entries.ContainsKey(id))
        {
            return entries[id];
        }

        return entries.entries.FirstOrDefault().value;
    }
}

/// <summary>
/// Clan display entry.?
/// </summary>
[Serializable]
public class ClanDisplayEntry
{
    public string name;
    public int cost;
    public int clanPointsRequired;
    public int maxClanMemberCount;
    public Color color;
}
