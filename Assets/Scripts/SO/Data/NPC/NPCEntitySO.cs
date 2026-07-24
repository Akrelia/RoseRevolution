using UnityEngine;
using System.Collections.Generic;
using System;
using RevolutionShared.Rose.Data.NPC;

/// <summary>
/// Rose NPC Infos.
/// </summary>
public class NPCEntitySO : ScriptableObject
{
    public MonsterData monsterData;
    public RoseSkeletonData skeleton;
    public List<RoseCharPartData> parts = new List<RoseCharPartData>();
    public List<AnimationClip> animations = new List<AnimationClip>();
}