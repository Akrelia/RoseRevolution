using RevolutionShared.Data;
using UnityEngine;

/// <summary>
/// Entity behavior.
/// </summary>
public class EntityBehavior : MonoBehaviour
{
    public EntityInfos infos;
    public EntityModelBehavior model;

    /// <summary>
    /// Load the infos.
    /// </summary>
    /// <param name="infos">Infos.</param>
    public void LoadBasicInfos(EntityInfos infos)
    {
        this.infos = infos;
    }
}
