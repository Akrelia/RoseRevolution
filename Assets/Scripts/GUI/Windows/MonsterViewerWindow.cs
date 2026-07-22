using System.Collections;
using System.Linq;
using UnityEngine;
using TMPro;
using Unity.VisualScripting;
using RevolutionShared.Rose.Data.NPC;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// Monster viewer window.
/// </summary>
public class MonsterViewerWindow : MonoBehaviour
{
    [Header("Values")]
    public float rotationSpeed = 100f;
    [Header("Data")]
    public RoseNPCDatabase npcDatabase;
    [Header("Prefabs")]
    public GameObject modelPreviewPrefab;
    [Header("References")]
    public TextMeshProUGUI monsterNameLabel;
    public TextMeshProUGUI animationNameLabel;

    private int index;
    private int animationIndex;
    private ModelPreview modelPreview;

    /// <summary>
    /// Start.
    /// </summary>
    public void Start()
    {
        Addressables.LoadAssetAsync<RoseNPCDatabase>(nameof(RoseNPCDatabase)).Completed += OnDatabaseLoaded;
    }

    private void OnDatabaseLoaded(AsyncOperationHandle<RoseNPCDatabase> handle)
    {
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            npcDatabase = handle.Result;

            Debug.Log($"Loaded {npcDatabase.npcs.Count} NPCs");
        }
    }

    /// <summary>
    /// Changes the displayed model to the one at the specified index in the NPC database.
    /// </summary>
    /// <param name="index">Curret index.</param>
    public void ChangeModel(int index)
    {
        if (npcDatabase != null)
        {
            var npcData = npcDatabase.npcs[index];

            DisplayModel(npcData);
        }
    }

    /// <summary>
    /// Displays the model of the specified NPC data in the model preview.
    /// </summary>
    /// <param name="npcData">NPC Data.</param>
    public void DisplayModel(RoseNPCEntry npcData)
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

                animationIndex = 0;

                animationNameLabel.text = $"{npcData.data.animations[animationIndex].name}";
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
        modelPreview.CurrentModel.transform.Rotate(Vector3.up, direction * rotationSpeed * Time.deltaTime); // Put this into Model Preview instead ?
    }

    /// <summary>
    /// Skip the model.
    /// </summary>
    /// <param name="direction">Direction.</param>
    public void SkipModel(int direction)
    {
        index += direction;

        index = Mod(index, npcDatabase.npcs.Count);

        ChangeModel(index);
    }

    /// <summary>
    /// Skips the animation of the current model based on the specified direction.
    /// </summary>
    /// <param name="direction">Direction.</param>
    public void SkipAnimation(int direction)
    {
        var animations = npcDatabase.npcs[index].data.animations;

        if (animations == null || animations.Count == 0)
            return;

        var count = animations.Count;

        for (int i = 0; i < count; i++)
        {
            animationIndex = Mod(animationIndex + direction, count);

            if (animations[animationIndex] != null)
                break;

            if (i == count - 1)
                return;
        }

        var animatorController = modelPreview.CurrentModel.GetComponent<Animator>();

        if (animatorController != null)
        {
            animatorController.Play("Animation_" + animationIndex);
        }

        animationNameLabel.text = $"{animations[animationIndex].name}";
    }

    public static int Mod(int value, int mod)
    {
        return ((value % mod) + mod) % mod;
    }

    /// <summary>
    /// Destroys the model preview when the window is destroyed.
    /// </summary>
    private void OnDestroy()
    {
        if (modelPreview != null)
        {
            Destroy(modelPreview.gameObject); // In case the preview has been instantiate outside the window
        }
    }
}
