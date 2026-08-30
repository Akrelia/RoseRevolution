using RevolutionShared.Rose.Data.Game;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Rose Data Database (Weird name, I know). This is for the small data structures imported from Rose.
/// </summary>
[CreateAssetMenu(menuName = "ROSE Data/Databases/Rose Data Database")]
public class RoseDataDatabase : ScriptableObject
{
    public FakeDictionary<int, ItemGradeData> itemGrades;
    public FakeDictionary<int, string> itemType;
}
