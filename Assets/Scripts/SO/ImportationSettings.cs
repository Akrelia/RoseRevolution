using System.Collections.Generic;
using UnityEngine;
using UnityRose;
using static RevolutionShared.Rose.Data.RoseEnums;

[CreateAssetMenu(menuName = "SO/Importation Settings")]
public class ImportationSettings : ScriptableObject
{
    public List<STBConfiguration> filesToImport = new List<STBConfiguration>();
}

[System.Serializable]
public class STBConfiguration
{
    public string stbID;
    public int iconColumn;
    public int categoryColumn;
    [Tooltip("This is the column that you want to check for filter empty / uncomplete items")]
    public int nullCheckColumn;
    public ItemType type;
    public string stbPath;
    public string stlPath;
    public string outputFolder;
    public string groupName;
    public string subgroupName;
}