using System;
using System.Collections.Generic;
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
    public GameObject playerSpawner;
    public GameObject entityGUI;
    [Header("Components")]
    public CameraController cameraController;

    /// <summary>
    /// Spawn main player.
    /// </summary>
    /// <param name="position">Position.</param>
    public RosePlayer SpawnPlayer(bool mainPlayer, GenderType gender, string playerName, byte hairID, byte faceID, int backID, int bodyID, int glovesID, int shoesID, int maskID, int hatID, int weaponID, int subWeaponID, Vector3 position)
    {
        CharModel model = new CharModel();

        model.rig = RigType.FOOT;
        model.state = States.STANDING;
        model.pos = position;
        model.gender = gender;

        model.changeID(BodyPartType.HAIR, hairID);
        model.changeID(BodyPartType.FACE, faceID);

        model.changeID(BodyPartType.BACK, backID);
        model.changeID(BodyPartType.BODY, bodyID);
        model.changeID(BodyPartType.ARMS, glovesID);
        model.changeID(BodyPartType.FOOT, shoesID);
        model.changeID(BodyPartType.FACEITEM, maskID);
        model.changeID(BodyPartType.CAP, hatID);

        var rosePlayer = new RosePlayer(model);

        rosePlayer.player.GetComponent<PlayerController>().isMainPlayer = mainPlayer;

        rosePlayer.equip(BodyPartType.FACEITEM, maskID);
        rosePlayer.equip(BodyPartType.WEAPON, weaponID);
        //rosePlayer.equip(BodyPartType.SUBWEAPON, subWeaponID);

        if (mainPlayer)
            cameraController.target = rosePlayer.player;

        var gui = Instantiate(entityGUI, rosePlayer.player.transform).GetComponentInChildren<EntityGUIController>();

        var bubble = gui.gameObject.GetComponentInChildren<SpeechBubble>(true);

        bubble.gameObject.transform.localScale = new Vector3(bubble.transform.localScale.x, bubble.transform.localScale.y, 0.1F); // WTF I NEED THAT ?

        gui.SetName(playerName);

        return rosePlayer;
    }
}
