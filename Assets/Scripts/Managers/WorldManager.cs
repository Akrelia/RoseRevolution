using RevolutionShared.Data;
using RevolutionShared.Rose.Data;
using RevolutionShared.Rose.Data.NPC;
using System.Linq;
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
    public GameObject weather;
    public GameObject monstersParent;
    public GameObject entityGUI;
    public FakeDictionary<EntityType, GameObject> entityPrefabs;
    [Header("Components")]
    public CameraController cameraController;
    [Header("Data")]
    public GameData gameData;

    /// <summary>
    /// Awake.
    /// </summary>
    private void Awake()
    {
    }

    /// <summary>
    /// Spawn a map.
    /// </summary>
    /// <param name="id">ID of the map.</param>
    /// <returns>Spawn map.</returns>
    public RoseMap SpawnMap(int id)
    {
        Debug.Log(id);

        var mapEntry = sandboxManager.mapDatabase.GetMapById(id); // Move the databases here now

        if (mapEntry != null)
        {
            var skyboxEntry = sandboxManager.skyboxDatabase?.Get(mapEntry.data.skyID);

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
    public RosePlayer SpawnPlayer(bool mainPlayer, CharacterInfos infos,Sprite clanIcon, CharacterAppearance appearence, Vector3 position)
    {
        CharModel model = new CharModel();

        position.y = -10;

        model.rig = RigType.FOOT;
        model.state = States.STANDING;
        model.pos = GetNearestSurface(position);

        model.ApplyAppearence(appearence);

        var rosePlayer = new RosePlayer(model);

        rosePlayer.LoadPlayer(model, sandboxManager.equipmentDatabase, sandboxManager.characterDatabase, sandboxManager.effectDatabase);

        if (rosePlayer.player != null)
        {
            rosePlayer.player.GetComponent<PlayerController>().isMainPlayer = mainPlayer;

            rosePlayer.Equip(BodyPartType.FACEITEM, appearence.Mask); // Hair adjustment ?
            rosePlayer.Equip(BodyPartType.WEAPON, appearence.Weapon); // TODO : This is redundant a bit with LoadObject, but this load the right stance etc ... so rework this later
            rosePlayer.Equip(BodyPartType.SUBWEAPON, appearence.SubWeapon);

            if (mainPlayer)
            {
                cameraController.target = rosePlayer.player;
                weather.GetComponent<Follow>().target = rosePlayer.player;
            }

            var gui = Instantiate(entityGUI, rosePlayer.player.transform).GetComponentInChildren<PlayerGUIController>();

            var bubble = gui.gameObject.GetComponentInChildren<SpeechBubble>(true);

            bubble.gameObject.transform.localScale = new Vector3(bubble.transform.localScale.x, bubble.transform.localScale.y, 0.1F); // Hackish trick (still needed ?)

            rosePlayer.changeName(infos.name);

            gui.SetName(infos.name, gameData.GetPrefixNameColor(infos.name));

            if (!string.IsNullOrEmpty(infos.clanName))
            {
                gui.SetClan(infos.clanName, gameData.GetNameColor(gameData.clanGrades[infos.clanGrade].nameColor), clanIcon);
            }

            else
            {
                gui.DisableClan();
            }
        }

        else
        {
            RoseDebug.LogError("Can't load the character model for the player");
        }

        return rosePlayer;
    }

    /// <summary>
    /// Spawn a NPC.
    /// </summary>
    /// <param name="infos">Infos.</param>
    /// <param name="npcInfos">NPC Infos.</param>
    /// <param name="position">Position.</param>
    /// <returns>Spawned NPC.</returns>
    public EntityBehavior SpawnNPC(EntityInfos infos, NPCInfos npcInfos, Vector3 position)
    {
        var npc = SpawnEntity(infos, npcInfos, position);

        var npcSpawnData = sandboxManager.CurrentMap.data.npcSpawns.FirstOrDefault(n => n.npcID == npcInfos.dataID);

        if (npcSpawnData != null)
        {
            var rotation = npcSpawnData.rotation.ToQuaternion();

            var quaternion = Quaternion.Euler(0f, -rotation.eulerAngles.z + 180f, 0f); // Maybe there is a formula that I'm missing but that's working

            npc.transform.rotation = quaternion;
        }

        else
        {
            RoseDebug.LogWarning("Missing NPC Spawn Data infos for : " + npcInfos.dataID);
        }

        return npc;
    }

    /// <summary>
    /// Spawn an entity.
    /// </summary>
    /// <param name="id">Id.</param>
    /// <param name="dataId">Data id.</param>
    /// <param name="position">Position.</param>
    /// <returns>Entity spawned.</returns>
    public EntityBehavior SpawnEntity(EntityInfos infos, ServerEntityInfos subInfos, Vector3 position, bool spawnGUI = true)
    {
        if (sandboxManager.npcDatabase != null)
        {
            var entityData = sandboxManager.npcDatabase.GetEntry(subInfos.dataID);

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

                if (spawnGUI)
                {
                    worldGUIController.SpawnEntityGUI(infos.id, entity.gameObject, entityData.data.monsterData);
                }

                return entity;
            }

            RoseDebug.LogError($"Can't find the entity prefab for ID : {infos.id}");
        }

        RoseDebug.LogError("Can't spawn entity, NPC Database does not exist");

        return null;
    }

    public static Vector3 GetNearestSurface(Vector3 position, float maxDistance = 1000f)
    {
        int layerMask = LayerMask.GetMask("Floor", "MapObjects");

        Vector3 topOrigin = position + Vector3.up * maxDistance;

        if (Physics.Raycast(topOrigin, Vector3.down, out RaycastHit hit, maxDistance * 2f, layerMask, QueryTriggerInteraction.Ignore))
        {
            return hit.point;
        }

        Debug.LogWarning($"No surface found at {position} with mask {layerMask}");

        return position;
    }
}
