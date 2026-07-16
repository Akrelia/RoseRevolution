using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Addressable index.
/// </summary>
[CreateAssetMenu(fileName = "AddressableIndex", menuName = "SO/AddressableIndex")]
public class AddressableIndex : ScriptableObject
{
    public List<GroupEntry> groups = new();

    [Serializable]
    public class GroupEntry
    {
        public string groupName;
        public List<ItemIndexEntry> addresses = new();
    }

    [Serializable]
    public class ItemIndexEntry
    {
        public string address;
        public string displayName;

        public ItemIndexEntry(string address, string displayName)
        {
            this.address = address;
            this.displayName = displayName;
        }
    }
}
