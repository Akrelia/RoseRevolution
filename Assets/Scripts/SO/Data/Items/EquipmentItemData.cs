using UnityEngine;
using UnityRose;

/// <summary>
/// Back Item Data.
/// </summary>
[CreateAssetMenu(fileName = "BackItem", menuName = "Data/Items/Equipment")]
public class EquipmentItemData : ItemData
{
    public short jobRequired;
    public short baseStatType1;
    public short baseStatValue1;
    public short baseStatType2;
    public short baseStatValue2;
    public short durability;
    public short defense;
    public short magicDefense;
    public short prefix;
}
