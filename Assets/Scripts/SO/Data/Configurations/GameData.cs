using RevolutionShared.Rose.Data;
using RevolutionShared.Rose.Data.Clan;
using UnityEngine;

/// <summary>
/// Game Data.
/// </summary>
[CreateAssetMenu(menuName = "ROSE Data/Game Data")]
public class GameData : ScriptableObject
{
    public FakeDictionary<string, Color> prefixNameColors;
    public FakeDictionary<int, ClanGradeData> clanGrades;
    public FakeDictionary<NameColor, Color> nameColors;

    /// <summary>
    /// Get name color.
    /// </summary>
    /// <param name="nameColor">Name color.</param>
    /// <returns>Name color.</returns>
    public Color GetNameColor(NameColor nameColor)
    {
        if (nameColors.ContainsKey(nameColor))
        {
            return nameColors[nameColor];
        }

        return Color.white;
    }

    /// <summary>
    /// Get prefix name color.
    /// </summary>
    /// <param name="playerName">Player's name.</param>
    /// <returns>Prefix color.</returns>
    public Color GetPrefixNameColor(string playerName)
    {
        foreach (var prefix in prefixNameColors)
        {
            if (playerName.StartsWith(prefix.Key))
            {
                return prefix.Value;
            }
        }

        return Color.white;
    }
}

