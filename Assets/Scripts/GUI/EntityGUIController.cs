using UnityEngine;
using TMPro;

/// <summary>
/// GUI Controller for an Entity.
/// </summary>
public class EntityGUIController : MonoBehaviour
{
    [Header("Components")]
    public Transform anchor;
    public TextMeshProUGUI nameLabel;
    public BillboardEffect effect;

    [HideInInspector]
    public GameObject entity;

    /// <summary>
    /// Set the name of the entity.
    /// </summary>
    /// <param name="name">Name.</param>
    public void SetName(string name)
    {
        nameLabel.text = name;
    }

    public void SetEntity(GameObject entity)
    {
        this.entity = entity;

        var renderers = entity.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
            return;

        var bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        effect.SetHeightOffset(bounds.max.y - entity.transform.position.y);
    }

    /// <summary>
    /// Update the GUI.
    /// </summary>
    private void Update()
    {
        if (entity != null)
        {
            transform.position = entity.transform.position;
        }

        else
        {
            RoseDebug.LogWarning("Deleting an orphan Entity GUI, that shouldn't happen");

            Destroy(gameObject);
        }
    }
}
