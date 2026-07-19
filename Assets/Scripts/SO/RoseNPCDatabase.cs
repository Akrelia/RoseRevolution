using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ROSE/NPC Database")]
public class RoseNPCDatabase : ScriptableObject
{
    public List<RoseNPCEntry> npcs = new();
}

[Serializable]
public class RoseNPCEntry
{
    public int id;
    public string name;
    public RoseNPCInfos data; 
    public GameObject prefab;
}