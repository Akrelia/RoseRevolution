using RevolutionShared.Rose.Data.NPC.Drops;
using UnityEngine;

public class DropTableSO : ScriptableObject // TODO : Make a system for all database, they share Entries / ID / Get(), etc ...
{
    public int id;
    public DropTableData table;
}