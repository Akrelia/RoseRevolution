using RevolutionShared.Rose.Data.NPC;
using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UIElements;

public class GMPanelMonsterTab : MonoBehaviour
{
    [Header("Colors")]
    public Color valueColor;
    [Header("Monsters Panel")]
    public TMP_InputField monsterIDInput;
    public TMP_InputField monsterAmountInput;
    public TMP_InputField searchInput;
    public GameObject monsterSpawnZone;
    public GameObject monsterPreviewZone;
    public GameObject monsterSearchZone;
    public TextMeshProUGUI monsterNameLabel;
    public TextMeshProUGUI monsterLevelLabel;
    public TextMeshProUGUI monsterHealthLabel;
    public TextMeshProUGUI monsterAttackLabel;
    public TextMeshProUGUI monsterDefLabel;
    public TextMeshProUGUI monsterMDefLabel;
    public TextMeshProUGUI monsterSpeedLabel;
    public TextMeshProUGUI monsterAtkSpeedLabel;
    public TextMeshProUGUI monsterMagicLabel;
    public TextMeshProUGUI monsterExperienceLabel;
    [Header("Parents")]
    public Transform resultsParent;
    [Header("Values")]
    public float rotationSpeed = 100f;
    [Header("Prefab")]
    public GameObject searchResultPrefab;
    public ModelPreview modelPreviewPrefab;
    [Header("Components")]

    private ModelPreview modelPreview;
    private NPCDatabase npcDatabase;

    /// <summary>
    /// Start.
    /// </summary>
    private void Start()
    {
        Addressables.LoadAssetAsync<NPCDatabase>(nameof(NPCDatabase)).Completed += OnDatabaseLoaded;

        monsterPreviewZone.SetActive(false);
    }

    /// <summary>
    /// When the database is loaded.
    /// </summary>
    /// <param name="handle">Handle.</param>
    private void OnDatabaseLoaded(AsyncOperationHandle<NPCDatabase> handle)
    {
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            npcDatabase = handle.Result;

            monsterSpawnZone.SetActive(true); // Display the search zone only when the database is loaded
        }
    }

    private void Update()
    {
        RotateModel(1);
    }

    /// <summary>
    /// Changes the displayed model to the one at the specified index in the NPC database.
    /// </summary>
    /// <param name="index">Curret index.</param>
    public void ChangeModel(int index)
    {
        if (npcDatabase != null)
        {
            var npcData = npcDatabase.entries[index];

            DisplayModel(npcData);
        }
    }

    /// <summary>
    /// Displays the model of the specified NPC data in the model preview.
    /// </summary>
    /// <param name="npcData">NPC Data.</param>
    public void DisplayModel(NPCDatabaseEntry npcData)
    {
        if (modelPreview == null)
        {
            modelPreview = Instantiate(modelPreviewPrefab, transform).GetComponent<ModelPreview>();
        }

        if (npcData != null)
        {
            if (npcData.prefab)
            {
                modelPreview.Show(npcData.prefab);

                monsterNameLabel.text = $"{npcData.name} (Lvl {npcData.data.monsterData.level})";
            }

            else
            {
                RoseDebug.LogWarning($"Prefab not found for monster: {npcData.name}.");
            }
        }

        else
        {
            RoseDebug.LogWarning($"Monster not found in database.");
        }
    }

    /// <summary>
    /// Rotation the model based on the specified direction and rotation speed.
    /// </summary>
    /// <param name="direction">Direction.</param>
    public void RotateModel(int direction)
    {
        if (modelPreview)
        {
            if (modelPreview.CurrentModel)
            {
                modelPreview.CurrentModel.transform.Rotate(Vector3.up, direction * rotationSpeed * Time.deltaTime);
            }
        }
    }

    /// <summary>
    /// Display a monster.
    /// </summary>
    /// <param name="npcData">NPC Data.</param>
    public void DisplayMonster(NPCDatabaseEntry npcData)
    {
        searchInput.text = "";
        monsterIDInput.text = npcData.id.ToString();

        DisplayModel(npcData);

        var data = npcData.data.monsterData;

        monsterNameLabel.text = npcData.name;
        monsterLevelLabel.text = "Level : " + Utils.Colorize(data.level, valueColor);
        monsterHealthLabel.text = "Health : " + Utils.Colorize(data.healthPoints, valueColor);
        monsterExperienceLabel.text = "Experience : " + Utils.Colorize(data.experience, valueColor);
        monsterAttackLabel.text = "Attack : " + Utils.Colorize(data.attack, valueColor);
        monsterAtkSpeedLabel.text = "Attack Speed : " + Utils.Colorize(data.attackSpeed, valueColor);
        monsterMagicLabel.text = "Attack Type : " + Utils.Colorize(data.attackType, valueColor);
        monsterDefLabel.text = "Defense : " + Utils.Colorize(data.defense, valueColor);
        monsterMDefLabel.text = "Magic Defense : " + Utils.Colorize(data.magicDefense, valueColor);
        monsterSpeedLabel.text = "Move Speed : " + Utils.Colorize(data.moveSpeed, valueColor);

        monsterSearchZone.SetActive(false);
        monsterPreviewZone.SetActive(true);
    }

    /// <summary>
    /// Display monster click.
    /// </summary>
    public void DisplayMonsterClick()
    {
        if (!string.IsNullOrEmpty(monsterIDInput.text))
        {
            monsterPreviewZone.SetActive(true);

            var id = Convert.ToInt32(monsterIDInput.text);

            var npc = npcDatabase.GetEntry(id);

            DisplayMonster(npc);
        }
    }

    /// <summary>
    /// When search typing.
    /// </summary>
    /// <param name="search">Search.</param>
    public void OnSearchTyping(string search)
    {
        if (search.Length > 3)
        {
            Utils.DestroyChildren(resultsParent.gameObject);

            monsterSearchZone.SetActive(true);
            monsterPreviewZone.SetActive(false);

            var results = npcDatabase.entries.Where(e => e.name.ToUpperInvariant().Contains(search.ToUpperInvariant())).ToList();

            for (int i = 0; i < results.Count; i++)
            {
                var npc = results[i];

                var result = Instantiate(searchResultPrefab, resultsParent).GetComponent<ClickableText>();

                result.Clicked += () => DisplayMonster(npc);

                result.text.text = $"Lvl {npc.data.monsterData.level} {npc.name}";
            }
        }
    }

    /// <summary>
    /// Spawn monster click.
    /// </summary>
    public void SpawnMonsterClick()
    {
        Client.Instance.SendPacket(Packets.GMCommandSpawn(Convert.ToInt32(monsterIDInput.text), Convert.ToInt32(monsterAmountInput.text)));
    }
}
