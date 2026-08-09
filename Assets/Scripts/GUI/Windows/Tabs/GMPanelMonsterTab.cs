using RevolutionShared.Rose.Data;
using RevolutionShared.Rose.Data.Equipment;
using RevolutionShared.Rose.Data.NPC;
using RevolutionShared.Utils;
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
    public TextMeshProUGUI monsterDatabaseCountLabel;
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
    public Transform dropListParent;
    [Header("Values")]
    public float rotationSpeed = 100f;
    [Header("Prefab")]
    public GameObject searchResultPrefab;
    public ModelPreview modelPreviewPrefab;
    public GameObject itemPreviewPrefab;
    [Header("Components")]

    private ModelPreview modelPreview;
    private NPCDatabase npcDatabase;
    private IconDatabase iconDatabase; // Put this somewhere to be accessible
    private DropTableDatabase dropTableDatabase;
    private EquipmentDatabase equipmentDatabase;

    /// <summary>
    /// Start.
    /// </summary>
    private void Start()
    {
        Addressables.LoadAssetAsync<NPCDatabase>(nameof(NPCDatabase)).Completed += OnDatabaseLoaded;
        Addressables.LoadAssetAsync<IconDatabase>(nameof(IconDatabase)).Completed += OnDatabaseLoaded;
        Addressables.LoadAssetAsync<DropTableDatabase>(nameof(DropTableDatabase)).Completed += OnDatabaseLoaded;
        Addressables.LoadAssetAsync<EquipmentDatabase>(nameof(EquipmentDatabase)).Completed += OnDatabaseLoaded;

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

            monsterDatabaseCountLabel.gameObject.SetActive(true);
            monsterDatabaseCountLabel.text = $"Enemies in Database: {npcDatabase.entries.Count}";
        }
    }

    /// <summary>
    /// When the database is loaded.
    /// </summary>
    /// <param name="handle">Handle.</param>
    private void OnDatabaseLoaded(AsyncOperationHandle<IconDatabase> handle)
    {
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            iconDatabase = handle.Result;
        }
    }

    /// <summary>
    /// When the database is loaded.
    /// </summary>
    /// <param name="handle">Handle.</param>
    private void OnDatabaseLoaded(AsyncOperationHandle<DropTableDatabase> handle)
    {
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            dropTableDatabase = handle.Result;
        }
    }

    /// <summary>
    /// When the database is loaded.
    /// </summary>
    /// <param name="handle">Handle.</param>
    private void OnDatabaseLoaded(AsyncOperationHandle<EquipmentDatabase> handle)
    {
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            equipmentDatabase = handle.Result;
        }
    }

    /// <summary>
    /// Update.
    /// </summary>
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
    /// <param name="npcEntry">NPC entry.</param>
    public void DisplayMonster(NPCDatabaseEntry npcEntry)
    {
        searchInput.text = "";
        monsterIDInput.text = npcEntry.id.ToString();

        DisplayModel(npcEntry);

        var data = npcEntry.data.monsterData;

        monsterNameLabel.text = npcEntry.name;
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

        DisplayDropList(data);
    }

    public void DisplayDropList(EnemyData data)
    {
        Utils.DestroyChildren(dropListParent.gameObject);

        var dropTable = dropTableDatabase.GetEntry(data.dropTableID);

        if (dropTable != null)
        {
            for (int i = 0; i < dropTable.table.drops.Count; i++)
            {
                var drop = dropTable.table.drops[i];

                var type = GetBodyPartType(drop.Type);

                var preview = Instantiate(itemPreviewPrefab, dropListParent).GetComponent<ItemPreview>();

                if (type != 0)
                {
                    var itemDB = equipmentDatabase.GetItem(type, drop.ID, type == BodyPartType.WEAPON || type == BodyPartType.BACK ? GenderType.NONE : GenderType.MALE); // Akima : the whole system will be better

                    if (itemDB != null)
                    {
                        preview.SetIcon(null, drop.dropChance, iconDatabase.GetIcon(itemDB.iconID));
                    }

                    else
                    {
                        preview.SetIcon(drop.dropChance);
                    }
                }

                else
                {
                    preview.SetIcon(drop.dropChance);
                }
            }
        }

        else
        {
            RoseDebug.LogWarning($"Drop table not found for ID: {data.dropTableID}.");
        }
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

    public static BodyPartType GetBodyPartType(ItemType itemType) // Debug method, remove ASAP
    {
        if (itemType == ItemType.FACEITEM)
        {
            return BodyPartType.FACEITEM;
        }
        else if (itemType == ItemType.HAT)
        {
            return BodyPartType.CAP;
        }
        else if (itemType == ItemType.BODY)
        {
            return BodyPartType.BODY;
        }
        else if (itemType == ItemType.GLOVES)
        {
            return BodyPartType.ARMS;
        }
        else if (itemType == ItemType.BOOTS)
        {
            return BodyPartType.FOOT;
        }
        else if (itemType == ItemType.BACK)
        {
            return BodyPartType.BACK;
        }
        else if (itemType == ItemType.WEAPON)
        {
            return BodyPartType.WEAPON;
        }
        else if (itemType == ItemType.SUBWEAPON)
        {
            return BodyPartType.SUBWEAPON;
        }

        return 0;
    }
}
