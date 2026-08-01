using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SkyboxDatabase : ScriptableObject
{
    public List<SkyboxData> entries;

    public SkyboxData Get(int id)
    {
        return entries.FirstOrDefault(e => e.Id == id);
    }
}