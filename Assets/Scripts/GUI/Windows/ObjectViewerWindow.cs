using System.Linq;
using UnityEngine;
using TMPro;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// Monster viewer window.
/// </summary>
public class ObjectViewerWindow : BaseWindow
{
    [Header("Values")]
    public float rotationSpeed = 100f;
    [Header("Prefabs")]
    public GameObject modelPreviewPrefab;
    [Header("References")]
    public TextMeshProUGUI modelNameLabel;

    private int index;
    private int animationIndex;
    private ModelPreview modelPreview;
    private EffectDatabase effectDatabase;

    /// <summary>
    /// Start.
    /// </summary>
    public void Start()
    {
        Addressables.LoadAssetAsync<EffectDatabase>(nameof(EffectDatabase)).Completed += OnDatabaseLoaded;
    }

    private void OnDatabaseLoaded(AsyncOperationHandle<EffectDatabase> handle)
    {
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            effectDatabase = handle.Result;

            DisplayModel(effectDatabase.entries.FirstOrDefault().prefab);

            Debug.Log($"Loaded {effectDatabase.entries.Count} Effects");
        }
    }

    /// <summary>
    /// Changes the displayed model to the one at the specified index in the NPC database.
    /// </summary>
    /// <param name="index">Curret index.</param>
    public void ChangeModel(int index)
    {
        if (effectDatabase != null)
        {
            var nextPrefab = effectDatabase.entries[index].prefab;

            DisplayModel(nextPrefab);
        }
    }

    /// <summary>
    /// Displays the model of the specified prefab in the model preview.
    /// </summary>
    /// <param name="prefab">Prefab.</param>
    public void DisplayModel(GameObject prefab)
    {
        if (modelPreview == null)
        {
            modelPreview = Instantiate(modelPreviewPrefab, transform).GetComponent<ModelPreview>();
        }

        if (prefab != null)
        {
            if (prefab)
            {
                modelPreview.Show(prefab);

                modelNameLabel.text = $"{prefab.name}";

                animationIndex = 0;
            }

            else
            {
                RoseDebug.LogWarning($"No Prefab");
            }
        }

        else
        {
            RoseDebug.LogWarning($"Effect not found in database.");
        }
    }

    /// <summary>
    /// Skip the model.
    /// </summary>
    /// <param name="direction">Direction.</param>
    public void SkipModel(int direction)
    {
        index += direction;

        index = Utils.Modulo(index, effectDatabase.entries.Count);

        ChangeModel(index);
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
