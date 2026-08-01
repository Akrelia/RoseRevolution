using System.Collections.Generic;
using System;
using UnityEngine;

[CreateAssetMenu(menuName = "ROSE/Spawn Database")]
public class MonsterSpawnDatabase : ScriptableObject
{
    public List<EnemySpawnSO> maps = new();
}