using RevolutionShared.Data;
using UnityEngine;

public class EnemyMod : EntityMod<EnemyInfos>
{
    public override void LoadMod(EnemyInfos infos)
    {
        Debug.Log("YOUPIIIIIIIIII : " + infos.health);
    }
}