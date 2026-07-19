using UnityEngine;
using System.Collections.Generic;
using System;
using RevolutionShared.Rose.Data.NPC;

public class RoseNPCInfos : ScriptableObject
{
    public int id;
    public string npcName;
    public MonsterData monsterData;
    public RoseSkeletonData skeleton;
    public List<RoseCharPartData> parts = new List<RoseCharPartData>();
    public List<AnimationClip> animations = new List<AnimationClip>();
}