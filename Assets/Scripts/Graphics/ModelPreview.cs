using System.Linq;
using UnityEngine;

/// <summary>
/// Model preview component that allows displaying a 3D model in a UI element using a RenderTexture.
/// </summary>
public class ModelPreview : MonoBehaviour
{
    public Camera previewCamera;
    public Transform root;
    public RenderTexture renderTexture;

    public float rotationY = 0;
    private GameObject currentModel;

    /// <summary>
    /// Shows the specified prefab in the model preview.
    /// </summary>
    /// <param name="prefab">Prefab.</param>
    public void Show(GameObject prefab, bool instantiate = true, bool frame = true)
    {
        if (currentModel != null && instantiate)
        {
            Destroy(currentModel);
        }

        if (instantiate)
        {
            currentModel = Instantiate(prefab, root);
        }

        else
        {
            currentModel = prefab;
        }

        SetLayerRecursively(currentModel, root.gameObject.layer);

        ChangeRotation(0);

        if (frame)
        {
            Frame(currentModel);
        }
    }

    public void ChangeRotation(float delta)
    {
        rotationY += delta;

        currentModel.transform.localRotation = Quaternion.Euler(0, 180 + rotationY, 0);
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