using RevolutionShared.Rose.Data;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Appearance database.
/// </summary>
public class AppearenceDatabase : ScriptableObject
{
    public List<AppearenceEntry> faces = new();
    public List<AppearenceEntry> hairs = new();

    public AppearenceEntry Get(BodyPartType type, int id, GenderType gender)
    {
        var list = type switch
        {
            BodyPartType.FACE => faces,
            BodyPartType.HAIR => hairs,
            _ => null
        };

        if (list == null)
        {
            return null;
        }

        bool isUnisexSlot = type is BodyPartType.BACK or BodyPartType.FACEITEM or BodyPartType.WEAPON or BodyPartType.SUBWEAPON;

        return isUnisexSlot ? list.Find(e => e.id == id) : list.Find(e => e.id == id && e.gender == gender);
    }
}

/// <summary>
/// Appearence entry.
/// </summary>
[Serializable]
public class AppearenceEntry
{
    public int id;

    public GameObject prefab;

    public GenderType gender;
}