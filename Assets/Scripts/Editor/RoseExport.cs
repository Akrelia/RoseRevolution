using RevolutionShared.Rose.Data;
using RevolutionShared.Rose.Data.NPC.Drops;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityRose.Formats;

/// <summary>
/// Rose export.
/// </summary>
public class RoseExport : MonoBehaviour
{
    public static DropTableData ExportDropTable(STB stb, int row)
    {
        var table = new DropTableData();
        var drops = new Dictionary<int, DropData>();

        if (stb == null || row < 0 || row >= stb.Cells.Count)
        {
            return table;
        }

        int GetCell(int column)
        {
            if (column < 0 || column >= stb.Cells[row].Count)
            {
                return 0;
            }

            string value = stb.Cells[row][column];

            if (string.IsNullOrWhiteSpace(value))
            {
                return 0;
            }

            return int.TryParse(value, out int result) ? result : 0;
        }

        void AddDrop(int itemId, float chance)
        {
            if (itemId <= 0)
            {
                return;
            }

            ItemType type = RoseExport.GetItemType(itemId);

            // Remove the type prefix from the item ID.
            int cleanId = itemId % 1000;

            if (drops.TryGetValue(itemId, out var drop))
            {
                drop.dropChance += chance;
            }
            else
            {
                drops[itemId] = new DropData
                {
                    ID = cleanId,
                    dropChance = chance,
                    Type = type
                };
            }
        }

        float slotChance = 100f / 30f;

        for (int i = 0; i < 30; i++)
        {
            int value = GetCell(1 + i);

            if (value <= 0)
            {
                continue;
            }

            if (value > 4)
            {
                AddDrop(value, slotChance);
            }
            else
            {
                int startColumn = 26 + (value * 5);
                float redirectChance = slotChance / 5f;

                for (int j = 0; j < 5; j++)
                {
                    int itemId = GetCell(startColumn + j);

                    if (itemId > 0)
                    {
                        AddDrop(itemId, redirectChance);
                    }
                }
            }
        }

        table.drops = drops.Values.ToList();
        table.dropSuccess = 100;
        table.totalChance = table.drops.Sum(x => x.dropChance);

        return table;
    }

    public static ItemType GetItemType(int itemId)
    {
        if (itemId <= 0)
        {
            return default;
        }

        return (ItemType)(itemId < 1000000 ? itemId / 1000 : itemId / 1000000);
    }
}