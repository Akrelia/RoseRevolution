using System;
using System.Collections.Generic;
using UnityEngine;
using RevolutionShared.Rose.Data;
using System.Linq;
using RevolutionShared.Rose.Data.Items.Equipment;

/// <summary>
/// Equipment database.
/// </summary>
public class EquipmentDatabase : ScriptableObject
{
    public WeaponDatabase weaponDatabase;
    public ArmorDatabase subWeaponDatabase;
    public ArmorDatabase backDatabase;
    public HeadgearDatabase headgearDatabase;
    public FootwearDatabase footwearDatabase;
    public ArmorDatabase bodyDatabase;
    public ArmorDatabase armDatabase;
    public ArmorDatabase faceItemDatabase;
    public AppearenceDatabase appearenceDatabase;

    private Dictionary<BodyPartType, IPartDatabase> databases;

    private void OnEnable()
    {
        databases = new()
        {
            { BodyPartType.WEAPON, weaponDatabase },
            { BodyPartType.BACK, backDatabase },
            { BodyPartType.CAP, headgearDatabase },
            { BodyPartType.FOOT, footwearDatabase },
            { BodyPartType.BODY, bodyDatabase },
            { BodyPartType.ARMS, armDatabase },
            { BodyPartType.FACEITEM, faceItemDatabase },
            { BodyPartType.SUBWEAPON, subWeaponDatabase },
        };
    }

    public EquipmentData GetItem(BodyPartType slot, int id, GenderType gender)
    {
        if (!databases.TryGetValue(slot, out var database))
        {
            return null;
        }

        return database.Get(id, gender)?.Item;
    }

    public ItemPartDatabaseEntry<T> GetItem<T>(BodyPartType slot, int id, GenderType gender) where T : EquipmentData
    {
        if (!databases.TryGetValue(slot, out var database))
            return null;

        return (database as PartDatabase<T>)?.Get(id, gender);
    }

    public GameObject GetPrefab(BodyPartType slot, int id, GenderType gender)
    {
        switch (slot)
        {
            case BodyPartType.FACE:
            case BodyPartType.HAIR:
                return appearenceDatabase.Get(slot, id, gender)?.prefab;

            default:
                return databases.TryGetValue(slot, out var database) ? database.Get(id, gender)?.prefab : null;
        }
    }

    public Dictionary<BodyPartType, IPartDatabase> Databases
    {
        get { return databases; }
    }
}

public interface IPartDatabase
{
    PartDatabaseEntryBase Get(int id, GenderType gender);

    void Add(int id, GenderType gender, GameObject prefab, EquipmentData item);

    void Remove(int id);

    ScriptableObject Asset { get; }
}

/// <summary>
/// Base item database entry.
/// </summary>
[Serializable]
public abstract class PartDatabaseEntryBase
{
    public int id;
    public GenderType gender;
    public GameObject prefab;
    public abstract EquipmentData Item { get; }
}

/// <summary>
/// Typed item database.
/// </summary>
public abstract class PartDatabase<T> : ScriptableObject, IPartDatabase where T : EquipmentData
{
    public List<ItemPartDatabaseEntry<T>> entries = new();
    public ScriptableObject Asset => this;

    public ItemPartDatabaseEntry<T> Get(int id, GenderType gender)
    {
        foreach (var entry in entries)
        {
            if (entry.id != id) continue;
            if (entry.gender != GenderType.NONE && entry.gender != gender) continue;

            return entry;
        }

        return null;
    }

    /// <summary>
    /// Add an entry.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="gender"></param>
    /// <param name="prefab"></param>
    /// <param name="item"></param>
    /// <exception cref="ArgumentException"></exception>
    public void Add(int id, GenderType gender, GameObject prefab, EquipmentData item)
    {
        if (item is not T typedItem)
            throw new ArgumentException($"Expected {typeof(T).Name} but got {item?.GetType().Name ?? "null"}.");

        var entry = new ItemPartDatabaseEntry<T> { id = id, gender = gender, prefab = prefab, item = typedItem };

        int index = entries.FindIndex(x => x.id == id && x.gender == gender);

        if (index >= 0) entries[index] = entry;
        else entries.Add(entry);
    }

    PartDatabaseEntryBase IPartDatabase.Get(int id, GenderType gender) => Get(id, gender);

    public void Remove(int id)
    {
        var entry = entries.FirstOrDefault(x => x.id == id);

        if (entry != null)
        {
            entries.Remove(entry);
        }

        else
        {
            Debug.Log("Can't find the entry to remove : " + id);
        }
    }
}


/// <summary>
/// Typed item database entry.
/// </summary>
[Serializable]
public class ItemPartDatabaseEntry<T> : PartDatabaseEntryBase where T : EquipmentData
{
    public T item;

    public override EquipmentData Item
    {
        get { return item; }
    }
}