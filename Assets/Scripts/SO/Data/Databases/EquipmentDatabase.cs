using System;
using System.Collections.Generic;
using UnityEngine;
using RevolutionShared.Rose.Data;
using RevolutionShared.Rose.Data.Equipment;

/// <summary>
/// Equipment database.
/// </summary>
public class EquipmentDatabase : ScriptableObject
{
    public WeaponDatabase weaponDatabase;
    public ArmorDatabase backDatabase;
    public HeadgearDatabase headgearDatabase;
    public FootwearDatabase footwearDatabase;
    public ArmorDatabase bodyDatabase;
    public ArmorDatabase armDatabase;
    public ArmorDatabase faceItemDatabase;
    public AppearenceDatabase appearenceDatabase;

    private Dictionary<BodyPartType, IItemDatabase> databases;


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
            { BodyPartType.FACEITEM, faceItemDatabase }
        };
    }


    public ItemDatabaseEntry<T> GetItem<T>(BodyPartType slot, int id, GenderType gender)
        where T : EquipmentData
    {
        if (!databases.TryGetValue(slot, out var database))
            return null;

        return (database as ItemDatabase<T>)?.Get(id, gender);
    }


    public GameObject GetPrefab(BodyPartType slot, int id, GenderType gender)
    {
        switch (slot)
        {
            case BodyPartType.FACE:
            case BodyPartType.HAIR:
                return appearenceDatabase.Get(slot, id, gender)?.prefab;

            default:
                return databases.TryGetValue(slot, out var database)
                    ? database.Get(id, gender)?.prefab
                    : null;
        }
    }
}


/// <summary>
/// Base item database interface.
/// </summary>
public interface IItemDatabase
{
    ItemDatabaseEntryBase Get(int id, GenderType gender);
}


/// <summary>
/// Base item database entry.
/// </summary>
[Serializable]
public abstract class ItemDatabaseEntryBase
{
    public int id;
    public GenderType gender;
    public GameObject prefab;
}


/// <summary>
/// Typed item database.
/// </summary>
public abstract class ItemDatabase<T> : ScriptableObject, IItemDatabase
    where T : EquipmentData
{
    public List<ItemDatabaseEntry<T>> entries = new();


    public ItemDatabaseEntry<T> Get(int id, GenderType gender)
    {
        foreach (var entry in entries)
        {
            if (entry.id != id)
                continue;

            if (entry.gender != GenderType.NONE && entry.gender != gender)
                continue;

            return entry;
        }

        return null;
    }


    ItemDatabaseEntryBase IItemDatabase.Get(int id, GenderType gender)
    {
        return Get(id, gender);
    }
}


/// <summary>
/// Typed item database entry.
/// </summary>
[Serializable]
public class ItemDatabaseEntry<T> : ItemDatabaseEntryBase
    where T : EquipmentData
{
    public T item;
}