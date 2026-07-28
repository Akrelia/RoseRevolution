using UnityEngine;
using System.Collections.Generic;
using System;
using RevolutionShared.Rose.Data.NPC;

/// <summary>
/// Rose NPC Infos.
/// </summary>
public class EntitySO : ScriptableObject
{
    public EnemyData monsterData; // TODO : Minor change but don't put the data here, the data in the DB entry is already suffisant
    public RoseSkeletonData skeleton;
    public List<RoseCharPartData> parts = new List<RoseCharPartData>();
    public List<AnimationClip> animations = new List<AnimationClip>();
}