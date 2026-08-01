using RevolutionShared.Rose.Data.NPC;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// World GUI Controller.
/// </summary>
public class WorldGUIController : MonoBehaviour
{
    [Header("Parents")]
    public Transform entities;
    [Header("Prefabs")]
    public GameObject entityGUIPrefab;

    Dictionary<int, GameObject> entityGUIs;

    /// <summary>
    /// Awake.
    /// </summary>
    public void Awake()
    {
        entityGUIs = new Dictionary<int, GameObject>();
    }

    /// <summary>
    /// Spawn the entity GUI.
    /// </summary>
    /// <param name="id">ID of the entity.</param>
    /// <param name="entity">Entity.</param>
    /// <param name="data">Data.</param>
    public void SpawnEntityGUI(int id, GameObject entity, EntityData data)
    {
        var gui = Instantiate(entityGUIPrefab, entities.transform).GetComponent<EntityGUIController>();

        gui.SetEntity(entity);
        gui.SetName(data.displayName);

        entityGUIs[id] = gui.gameObject;
    }
}
