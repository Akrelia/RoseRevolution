using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "IDDatabase", menuName = "SO/Data/IdDatabase")]
public class IDDatabase : ScriptableObject, IAddressableAsset
{
    public FakeDictionary<int, DataID> ids = new FakeDictionary<int, DataID>();
    public int ID { get; set; }
    public string DisplayName { get; set; }
    public List<string> Labels { get; set; } = new List<string>();
}

[Serializable]
public class DataID
{
    public int id;
    public string dataName;
    public string meta;

    public DataID(int id, string dataName, string meta)
    {
        this.id = id;
        this.dataName = dataName;
        this.meta = meta;
    }
}