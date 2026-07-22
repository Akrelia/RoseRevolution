using RevolutionShared.Data;
using RevolutionShared.Rose.Data;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityRose;

/// <summary>
/// World manager.
/// </summary>
public class WorldManager : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject mainPlayer;
    public GameObject mobSpawner;
    public GameObject entityGUI;
    [Header("Components")]
    public CameraController cameraController;

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
        model.gender = appearence.Gender;

        model.changeID(BodyPartType.HAIR, (int)appearence.Hair);
        model.changeID(BodyPartType.FACE, (int)appearence.Face);

        model.changeID(BodyPartType.BACK, appearence.Back);
        model.changeID(BodyPartType.BODY, appearence.Body);
        model.changeID(BodyPartType.ARMS, appearence.Gloves);
        model.changeID(BodyPartType.FOOT, appearence.Shoes);
        model.changeID(BodyPartType.FACEITEM, appearence.Mask);
        model.changeID(BodyPartType.CAP, appearence.Hat);

        var rosePlayer = new RosePlayer(model);

        rosePlayer.player.GetComponent<PlayerController>().isMainPlayer = mainPlayer;

        rosePlayer.equip(BodyPartType.FACEITEM, appearence.Mask);
        rosePlayer.equip(BodyPartType.WEAPON, appearence.Weapon);
        //rosePlayer.equip(BodyPartType.SUBWEAPON, subWeaponID);

        if (mainPlayer)
        {
            cameraController.target = rosePlayer.player;
        }

        var gui = Instantiate(entityGUI, rosePlayer.player.transform).GetComponentInChildren<EntityGUIController>();

        var bubble = gui.gameObject.GetComponentInChildren<SpeechBubble>(true);

        bubble.gameObject.transform.localScale = new Vector3(bubble.transform.localScale.x, bubble.transform.localScale.y, 0.1F); // WTF I NEED THAT ?

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
    public RoseNpc SpawnEntity(int id, int dataId, Vector3 position)
    {
        // RoseImport.ImportNPC(dataId);

        GameObject entity = new GameObject();

        entity.name = "Entity_" + dataId;

        entity.transform.parent = mobSpawner.transform;

        entity.transform.position = RoseToUnity(position);
        entity.transform.rotation = Quaternion.identity; ;

        var roseNpc = entity.AddComponent<RoseNpc>();

        roseNpc.data = LoadNPCAssetStartingWith<RoseNPCInfos>($"[{dataId}]");

        return roseNpc;
    }


    public static Vector3 RoseToUnity(Vector3 rose)
    {
        return new Vector3(10400 - rose.z, rose.y, rose.x);
    }


    public static T LoadNPCAssetStartingWith<T>(string prefix) where T : ScriptableObject
    {
        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { "Assets/Npcs" });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string filename = System.IO.Path.GetFileNameWithoutExtension(path);

            if (filename.StartsWith(prefix))
            {
                return AssetDatabase.LoadAssetAtPath<T>(path);
            }
        }

        return null;
    }
}
