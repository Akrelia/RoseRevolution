using System;
using System.Collections.Generic;
using UnityEngine;
using RevolutionShared.Rose.Data.Equipment;
using RevolutionShared.Rose.Data;
using UnityRose;

[CreateAssetMenu(menuName = "ROSE/Character Database")]
public class CharacterDatabase : ScriptableObject
{
    [Serializable]
    public class GenderEntry
    {
        public GenderType gender;
        public List<SkeletonEntry> weapons = new List<SkeletonEntry>();
    }

    [Serializable]
    public class SkeletonEntry
    {
        public WeaponType weapon;
        public GameObject prefab;
        public BindPoses bindPoses;
    }

    public List<GenderEntry> genders = new List<GenderEntry>();

    public SkeletonEntry GetEntry(GenderType gender, WeaponType weapon)
    {
        var genderEntry = genders.Find(x => x.gender == gender);

        if (genderEntry == null)
            return null;

        var skeletonEntry = genderEntry.weapons.Find(x => x.weapon == weapon);

        return skeletonEntry;
    }
}