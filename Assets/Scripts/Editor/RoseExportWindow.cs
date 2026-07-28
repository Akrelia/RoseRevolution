using Newtonsoft.Json;
using RevolutionShared.Rose.Data.NPC;
using RevolutionShared.Rose.Data.NPC.Drops;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityRose;

public class RoseExportWindow : EditorWindow
{
    [MenuItem("ROSE Online/Drop Exporter")]
    public static void ShowWindow()
    {
        GetWindow<RoseExportWindow>("Drop Exporter");
    }

    private void OnGUI()
    {
        GUILayout.Space(10);

        if (GUILayout.Button("Export Drops"))
        {
            ExportDrops();
        }
    }

    private void ExportDrops()
    {
        Debug.Log("Starting drop export...");

        var table = RoseExport.ExportDropTable(ResourceManager.Instance.stb_drops_list, 196); // Worm dragon Test

        ExportJson(table);

        Debug.Log("Drop export finished.");
    }

    public static string ExportJson(DropTableData table)
    {
        var export = new
        {
            dropSuccess = table.dropSuccess,
            totalWeight = table.totalChance,
            drops = table.drops.Select(x => new DropDataExport
            {
                ID = x.ID,
                dropChance = x.dropChance,
                Type = x.Type.ToString()
            }).ToList()
        };

        string json = JsonConvert.SerializeObject(export, Formatting.Indented);

        string path = EditorUtility.SaveFilePanel("Export Drop Table", "", "DropTable.json", "json");

        if (!string.IsNullOrEmpty(path))
        {
            File.WriteAllText(path, json);

            Debug.Log($"Drop table exported: {path}");
        }

        return JsonConvert.SerializeObject(export, Formatting.Indented);
    }

    public static string ExportJson(EnemyData enemy)
    {
        var export = enemy;

        string json = JsonConvert.SerializeObject(export, Formatting.Indented);

        string fileName = $"[{enemy.ID}]{enemy.displayName}.json";

        string path = EditorUtility.SaveFilePanel(
            "Export Enemy",
            "",
            fileName,
            "json");

        if (!string.IsNullOrEmpty(path))
        {
            File.WriteAllText(path, json);

            Debug.Log($"Enemy exported: {path}");
        }

        return json;
    }
}

public class DropDataExport
{
    public int ID;
    public float dropChance;
    public string Type;
}