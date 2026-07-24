using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ROSE/NPC Database")]
public class NPCDatabase : ScriptableObject
{
    public List<NPCDatabaseEntry> npcs = new();
}

[Serializable]
public class NPCDatabaseEntry
{
    public int id;
    public string name; // Just in case to retrieve what was the NPC 
    public NPCEntitySO data; 
    public GameObject prefab;
}