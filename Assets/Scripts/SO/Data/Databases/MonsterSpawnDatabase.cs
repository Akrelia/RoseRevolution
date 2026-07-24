using System.Collections.Generic;
using System;
using UnityEngine;

[CreateAssetMenu(menuName = "ROSE/Spawn Database")]
public class MonsterSpawnDatabase : ScriptableObject
{
    public List<MapSpawnData> maps = new();
}

[Serializable]
public class MapSpawnData
{
    public int MapID;
    public string MapName;
    public List<MonsterSpawnData> Spawns;
}

[Serializable]
public class MonsterSpawnData
{
    public string Name;
    public float MapX;
    public float MapY;
    public int ID;
    public float WorldX;
    public float WorldY;
    public float WorldZ;
    public int Interval;
    public int LimitCount;
    public float Range;
    public int TacticPoints;
    public List<MonsterEntryData> Basic;
    public List<MonsterEntryData> Tactic;
}

[Serializable]
public class MonsterEntryData
{
    public int ID;
    public int Count;
    public string Description;
}