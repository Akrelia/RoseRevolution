using RevolutionShared.Rose.Data;
using System.Collections.Generic;
using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Data/Database/Avatar")]
public class RoseAvatarDatabase : ScriptableObject
{
    public List<RoseAvatarEntry> bodies = new();
    public List<RoseAvatarEntry> arms = new();
    public List<RoseAvatarEntry> feet = new();
    public List<RoseAvatarEntry> faces = new();
    public List<RoseAvatarEntry> hairs = new();
    public List<RoseAvatarEntry> caps = new();
    public List<RoseAvatarEntry> backs = new();
    public List<RoseAvatarEntry> faceItems = new();
    public List<RoseAvatarEntry> weapons = new();
    public List<RoseAvatarEntry> subWeapons = new();


    public RoseAvatarEntry Get(BodyPartType type, int id)
    {
        return type switch
        {
            BodyPartType.BODY => bodies[id],
            BodyPartType.ARMS => arms[id],
            BodyPartType.FOOT => feet[id],
            BodyPartType.FACE => faces[id],
            BodyPartType.HAIR => hairs[id],
            BodyPartType.CAP => caps[id],
            BodyPartType.BACK => backs[id],
            BodyPartType.FACEITEM => faceItems[id],
            BodyPartType.WEAPON => weapons[id],
            BodyPartType.SUBWEAPON => subWeapons[id],
            _ => null
        };
    }
}


[Serializable]
public class RoseAvatarEntry
{
    public int id;
    public RoseCharPartData part;
}