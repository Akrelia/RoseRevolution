using RevolutionShared.Rose.Data;
using RevolutionShared.Rose.Data.Items;
using RevolutionShared.Rose.Data.Items.Equipment;
using RevolutionShared.Rose.Data.Items.Equipment.Armor;
using UnityEngine;

/// <summary>
/// Item Database
/// </summary>
public class ItemDatabase : ScriptableObject
{
    public FakeDictionary<int, WeaponData> weapons;
    public FakeDictionary<int, SubWeaponData> subweapons;
    public FakeDictionary<int, BackData> backs;
    public FakeDictionary<int, BodyArmorData> bodies;
    public FakeDictionary<int, HeadgearData> caps;
    public FakeDictionary<int, HandwearData> arms;
    public FakeDictionary<int, FootwearData> boots;
    public FakeDictionary<int, FaceItemData> faceItems;
    public FakeDictionary<int, ConsumableData> consumables;
    public FakeDictionary<int, GemData> gems;
    public FakeDictionary<int, MaterialData> materials;
    public FakeDictionary<int, MaterialData> projectiles;
    public FakeDictionary<int, JewelData> jewels;

    public FakeDictionary<ItemType, FakeDictionary<int, ItemData>> items;

    public void Initialize()
    {
        weapons = new FakeDictionary<int, WeaponData>();
        subweapons = new FakeDictionary<int, SubWeaponData>();
        backs = new FakeDictionary<int, BackData>();
        bodies = new FakeDictionary<int, BodyArmorData>();
        caps = new FakeDictionary<int, HeadgearData>();
        arms = new FakeDictionary<int, HandwearData>();
        boots = new FakeDictionary<int, FootwearData>();
        faceItems = new FakeDictionary<int, FaceItemData>();
        consumables = new FakeDictionary<int, ConsumableData>();
        gems = new FakeDictionary<int, GemData>();
        materials = new FakeDictionary<int, MaterialData>();
        projectiles = new FakeDictionary<int, MaterialData>();
        jewels = new FakeDictionary<int, JewelData>();

        items = new FakeDictionary<ItemType, FakeDictionary<int, ItemData>>();
    }

    public ItemData Get(int id, ItemType type)
    {
        switch (type)
        {
            case ItemType.FACEITEM: return faceItems[id];
            case ItemType.HAT: return caps[id];
            case ItemType.BODY: return bodies[id];
            case ItemType.GLOVES: return arms[id];
            case ItemType.BOOTS: return boots[id];
            case ItemType.BACK: return backs[id];
            case ItemType.JEWEL: return jewels[id];
            case ItemType.WEAPON: return weapons[id];
            case ItemType.SUBWEAPON: return subweapons[id];
            case ItemType.USEITEM: return consumables[id];
            case ItemType.GEMS: return gems[id];
            case ItemType.MATERIAL: return materials[id];
            default: return null;
        }
    }

    public T Get<T>(int id, ItemType type) where T : ItemData
    {
        return Get(id, type) as T;
    }
}
