using RevolutionShared.Data;
using UnityEngine;

public interface IEntityMod
{
    void LoadMod(EntitySubInfos infos);
}

public interface IEntityMod<T> : IEntityMod where T : EntitySubInfos
{
    void LoadMod(T infos);
    void IEntityMod.LoadMod(EntitySubInfos infos) => LoadMod((T)infos);
}

public abstract class EntityMod<T> : MonoBehaviour, IEntityMod<T> where T : EntitySubInfos
{
    public abstract void LoadMod(T infos);

    public void LoadMod(EntitySubInfos infos)
    {
        LoadMod((T)infos); // One cast to have a complete polymorphic entity system ! (but I want to get rid of anyway)
    }
}