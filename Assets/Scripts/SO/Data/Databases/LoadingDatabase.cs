using RevolutionCore.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Loading database.
/// </summary>
public class LoadingDatabase : ScriptableObject
{
    public Sprite defaultLoading;
    public FakeDictionary<int, Sprite> mapLoadings;
    public FakeDictionary<int, List<Sprite>> planetloadings;

    /// <summary>
    /// Get the loading of a specific map / planet.
    /// </summary>
    /// <param name="mapID">Map ID.</param>
    /// <param name="planetID">Planet ID.</param>
    /// <returns>Loading wallpaper.</returns>
    public Sprite GetLoading(int mapID, int planetID)
    {
        if (mapLoadings.ContainsKey(mapID))
        {
            return mapLoadings[mapID];
        }

        if (planetloadings.ContainsKey(planetID))
        {
            return planetloadings[planetID].PickUp();
        }

        RoseDebug.LogWarning($"Couldn't find a suitable loading for {mapID} / {planetID}");

        return defaultLoading;
    }
}
