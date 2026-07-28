using RevolutionShared.Data;
using RevolutionShared.Rose.Data;
using RevolutionShared.Rose.Data.NPC;
using UnityEditorInternal;
using UnityEngine;
using UnityRose;

/// <summary>
/// World manager.
/// </summary>
public class WorldManager : MonoBehaviour
{
    public SandboxManager sandboxManager;
    [Header("Prefabs")]
    public GameObject mainPlayer;
    public GameObject monstersParent;
    public GameObject entityGUI;
    public FakeDictionary<EntityType, GameObject> entityPrefabs;
    [Header("Components")]
    public CameraController cameraController;

    /// <summary>
    /// Awake.
    /// </summary>
    private void Awake()
    {
        Screen.SetResolution(1600, 800, FullScreenMode.Windowed);
    }

    /// <summary>
    /// Spawn a character player.
    /// </summary>
    /// <param name="position">Position.</param>
    public RosePlayer SpawnPlayer(bool mainPlayer, string playerName, CharacterAppearance appearence, Vector3 position)
    {
        CharModel model = new CharModel();

        model.rig = RigType.FOOT;
        model.state = States.STANDING;
        model.pos = position;

        model.ApplyAppearence(appearence);

        var rosePlayer = new RosePlayer(model);

        rosePlayer.LoadPlayer(model, sandboxManager.equipmentDatabase);

        rosePlayer.player.GetComponent<PlayerController>().isMainPlayer = mainPlayer;

        rosePlayer.Equip(BodyPartType.FACEITEM, appearence.Mask); // Hair adjustment ?
        rosePlayer.Equip(BodyPartType.WEAPON, appearence.Weapon); // TODOD : This is redundant a bit with LoadObject, but this load the right stance etc ... so maybe remove LoadObject from this
        //rosePlayer.equip(BodyPartType.SUBWEAPON, subWeaponID);

        if (mainPlayer)
        {
            cameraController.target = rosePlayer.player;
        }

        var gui = Instantiate(entityGUI, rosePlayer.player.transform).GetComponentInChildren<EntityGUIController>();

        var bubble = gui.gameObject.GetComponentInChildren<SpeechBubble>(true);

        bubble.gameObject.transform.localScale = new Vector3(bubble.transform.localScale.x, bubble.transform.localScale.y, 0.1F); // Hackish trick (still needed ?)

        rosePlayer.changeName(playerName);

        gui.SetName(playerName);

        return rosePlayer;
    }

    /// <summary>
    /// Spawn an entity.
    /// </summary>
    /// <param name="id">Id.</param>
    /// <param name="dataId">Data id.</param>
    /// <param name="position">Position.</param>
    /// <returns>Entity spawned.</returns>
    public EntityModelBehavior SpawnEntity(EntityInfos infos, EntitySubInfos subInfos, NPCDatabaseEntry entityData)
    {
        var prefab = entityPrefabs[infos.type];
        var data = entityData.data.monsterData;

        var entity = Instantiate(prefab, monstersParent.transform);

        var entityModel = Instantiate(entityData.prefab);
        entityModel.transform.SetParent(entity.transform, false);

        entity.transform.SetPositionAndRotation(infos.position.ToVector3(),Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));

        var mod = entity.GetComponent<IEntityMod>();

        mod?.LoadMod(subInfos);

        entity.name = $"{data.ID}{data.displayName}";

        return entity.GetComponent<EntityModelBehavior>();
    }

    /// <summary>
    /// Rose to Unity position.
    /// </summary>
    /// <param name="rose"></param>
    /// <returns></returns>
    public static Vector3 RoseToUnity(Vector3 rose)
    {
        return new Vector3(10400 - rose.z, rose.y, rose.x);
    }

    public static Vector3 UnityToRose(Vector3 unity)
    {
        return new Vector3(unity.z, unity.y, 10400 - unity.x);
    }
}
