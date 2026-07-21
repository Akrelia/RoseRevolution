using System.Linq;
using UnityEngine;
using static UnityRose.Formats.ZSC.Object;

/// <summary>
/// Model preview component that allows displaying a 3D model in a UI element using a RenderTexture.
/// </summary>
public class ModelPreview : MonoBehaviour
{
    public Camera previewCamera;
    public Transform root;
    public RenderTexture renderTexture;

    private GameObject currentModel;

    /// <summary>
    /// Shows the specified prefab in the model preview.
    /// </summary>
    /// <param name="prefab">Prefab.</param>
    public void Show(GameObject prefab)
    {
        if (currentModel != null)
        {
            Destroy(currentModel);
        }

        currentModel = Instantiate(prefab, root);

        currentModel.transform.localRotation = Quaternion.Euler(0, 180, 0);

        SetLayerRecursively(currentModel, root.gameObject.layer);

        Frame(currentModel);
    }

    /// <summary>
    /// Sets the layer of the specified GameObject and all its children recursively.
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="layer"></param>
    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;

        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    /// <summary>
    /// Clears the current model from the preview.
    /// </summary>
    public void Clear()
    {
        if (currentModel != null)
        {
            Destroy(currentModel);
        }
    }

    /// <summary>
    /// Frames the camera to fit the specified object in view.
    /// </summary>
    /// <param name="model"></param>
    public void Frame(GameObject model)
    {
        var renderers = model.GetComponentsInChildren<Renderer>();

        var bounds = renderers[0].bounds;

        foreach (var r in renderers.Skip(1))
        {
            bounds.Encapsulate(r.bounds);
        }

        var center = bounds.center;
        var radius = bounds.extents.magnitude;

        previewCamera.transform.position = center + new Vector3(0, radius * 0.3f, -radius * 2f);
        previewCamera.transform.LookAt(center);

        previewCamera.nearClipPlane = 0.01f;
        previewCamera.farClipPlane = radius * 10f;
    }

    /// <summary>
    /// Gets the current model.
    /// </summary>
    public GameObject CurrentModel
    {
        get { return currentModel; }
    }
}