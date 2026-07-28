using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "ROSE/NPC Database")]
public class NPCDatabase : ScriptableObject
{
    public List<NPCDatabaseEntry> entries = new();

    public NPCDatabaseEntry GetEntry(int id)
    {
        return entries.FirstOrDefault(x => x.id == id);
    }
}

[Serializable]
public class NPCDatabaseEntry
{
    public int id;
    public string name; // Just in case to retrieve what was the NPC 
    public EntitySO data; 
    public GameObject prefab;
}