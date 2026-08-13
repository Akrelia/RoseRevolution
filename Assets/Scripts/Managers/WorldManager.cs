using RevolutionShared.Data;
using RevolutionShared.Rose.Data;
using RevolutionShared.Rose.Data.NPC;
using Unity.VisualScripting;
using UnityEngine;
using UnityRose;
using UnityRose.Game;

/// <summary>
/// World manager.
/// </summary>
public class WorldManager : MonoBehaviour
{
    public SandboxManager sandboxManager;
    public WorldGUIController worldGUIController;
    [Header("Prefabs")]
    public GameObject mainPlayer;
    public GameObject monstersParent;
    public GameObject entityGUI;
    public FakeDictionary<EntityType, GameObject> entityPrefabs;
    [Header("Components")]
    public CameraController cameraController;
    [Header("Data")]
    public ClanDisplayData clanData;

    /// <summary>
    /// Awake.
    /// </summary>
    private void Awake()
    {
        //   Screen.SetResolution(1600, 800, FullScreenMode.Windowed);

        Debug.Log("Metabel");
    }

    /// <summary>
    /// Spawn a map.
    /// </summary>
    /// <param name="id">ID of the map.</param>
    /// <returns>Spawn map.</returns>
    public RoseMap SpawnMap(int id)
    {
        var mapEntry = sandboxManager.mapDatabase.GetMapById(id); // Move the databases here now

        if (mapEntry != null)
        {
            var skyboxEntry = sandboxManager.skyboxDatabase.Get(mapEntry.data.skyID);

            var map = Instantiate(mapEntry.prefab).GetComponent<RoseMap>();

            if (skyboxEntry != null)
            {
                var skybox = new GameObject($"Skybox_{skyboxEntry.Id}");
                var meshFilter = skybox.AddComponent<MeshFilter>();
                var meshRenderer = skybox.AddComponent<MeshRenderer>();
                var controller = skybox.AddComponent<SkyboxController>(); // Turn this into a prefab

                meshFilter.sharedMesh = skyboxEntry.Mesh;
                meshRenderer.sharedMaterial = skyboxEntry.Material;

                map.skyboxData = skyboxEntry;
                map.skyboxRenderer = meshRenderer;
            }

            else
            {
                RoseDebug.LogWarning($"Can't find a skybox for the map");
            }

            RoseDebug.Log($"{mapEntry.data.mapName} has been loaded");

            return map;
        }

        else
        {
            RoseDebug.LogError("Can't find the map prefab for ID : " + id); // Should be a serious error but shouldn't happen, except for bad client

            return null;
        }
    }

    /// <summary>
    /// Spawn a character player.
    /// </summary>
    /// <param name="position">Position.</param>
    public RosePlayer SpawnPlayer(bool mainPlayer, string playerName, string clanName, int clanGrade, Sprite clanSprite, CharacterAppearance appearence, Vector3 position)
    {
        CharModel model = new CharModel();

        model.rig = RigType.FOOT;
        model.state = States.STANDING;
        model.pos = position;

        model.ApplyAppearence(appearence);

        var rosePlayer = new RosePlayer(model);

        rosePlayer.LoadPlayer(model, sandboxManager.equipmentDatabase, sandboxManager.characterDatabase);

        rosePlayer.player.GetComponent<PlayerController>().isMainPlayer = mainPlayer;

        rosePlayer.Equip(BodyPartType.FACEITEM, appearence.Mask); // Hair adjustment ?
        rosePlayer.Equip(BodyPartType.WEAPON, appearence.Weapon); // TODOD : This is redundant a bit with LoadObject, but this load the right stance etc ... so maybe remove LoadObject from this
        rosePlayer.Equip(BodyPartType.SUBWEAPON, appearence.SubWeapon);

        if (mainPlayer)
        {
            cameraController.target = rosePlayer.player;
        }

        var gui = Instantiate(entityGUI, rosePlayer.player.transform).GetComponentInChildren<PlayerGUIController>();

        var bubble = gui.gameObject.GetComponentInChildren<SpeechBubble>(true);

        bubble.gameObject.transform.localScale = new Vector3(bubble.transform.localScale.x, bubble.transform.localScale.y, 0.1F); // Hackish trick (still needed ?)

        rosePlayer.changeName(playerName);

        gui.SetName(playerName);

        if (!string.IsNullOrEmpty(clanName))
        {
            gui.SetClan(clanName, clanData.Get(clanGrade).color, clanSprite);
        }

        else
        {
            gui.DisableClan();
        }

        return rosePlayer;
    }

    /// <summary>
    /// Spawn a player without clan.
    /// </summary>
    /// <param name="mainPlayer"></param>
    /// <param name="playerName"></param>
    /// <param name="appearence"></param>
    /// <param name="position"></param>
    /// <returns></returns>
    public RosePlayer SpawnPlayer(bool mainPlayer, string playerName, CharacterAppearance appearence, Vector3 position)
    {
        return SpawnPlayer(mainPlayer, playerName, "", 0, null, appearence, position);
    }

    /// <summary>
    /// Spawn an entity.
    /// </summary>
    /// <param name="id">Id.</param>
    /// <param name="dataId">Data id.</param>
    /// <param name="position">Position.</param>
    /// <returns>Entity spawned.</returns>
    public EntityBehavior SpawnEntity(EntityInfos infos, EntitySubInfos subInfos, NPCDatabaseEntry entityData, Vector3 position)
    {
        if (entityData != null)
        {
            var prefab = entityPrefabs[infos.type];
            var data = entityData.data.monsterData;

            var entity = Instantiate(prefab, monstersParent.transform).GetComponent<EntityBehavior>();

            entity.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));

            var entityModel = Instantiate(entityData.prefab);

            entityModel.transform.SetParent(entity.transform, false);

            var mod = entity.GetComponent<IEntityMod>();

            mod?.LoadMod(subInfos);

            entity.name = $"{data.ID}{data.displayName}";
            entity.mod = mod;
            entity.model = entityModel.GetComponent<EntityModelBehavior>();

            worldGUIController.SpawnEntityGUI(infos.id, entity.gameObject, entityData.data.monsterData);

            return entity;
        }

        RoseDebug.LogError($"Can't find the entity prefab for ID : {infos.id}");

        return null;
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

    /// <summary>
    /// Unity to rose posiition.
    /// </summary>
    /// <param name="unity"></param>
    /// <returns></returns>
    public static Vector3 UnityToRose(Vector3 unity)
    {
        return new Vector3(unity.z, unity.y, 10400 - unity.x);
    }
}
