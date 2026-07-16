using System.Collections.Generic;
using UnityEngine;
using UnityRose;

/// <summary>
/// Item Data.
/// </summary>
[CreateAssetMenu(fileName = "ItemData", menuName = "Data/Items/Item")] // Akima : this could be useless, better check when all STB are done
public class ItemData : ScriptableObject, IAddressableAsset
{
    public int id;
    public string itemName;
    public string description;
    public int price;
    public ItemType type;
    public short category;
    public short weight;
    public byte quality;
    public short iconID;
    public short fieldItem;
    public short sfx;
    public short craftNumber;
    public byte craftSkillLevel;
    public short craftProductNumber;
    public short craftDifficulty;
    public short conditionType;
    public short conditionValue;

    public int ID => id;
    public string DisplayName => itemName;
    public List<string> Labels { get; set; } = new List<string>();
}
