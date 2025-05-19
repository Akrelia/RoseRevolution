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
    [Header("Components")]
    public CameraController cameraController;

    /// <summary>
    /// Spawn main player.
    /// </summary>
    /// <param name="position">Position.</param>
    public void SpawnMainPlayer(GenderType gender,byte hairID, byte faceID, Vector3 position)
    {
        CharModel model = new CharModel();

        model.rig = RigType.FOOT;
        model.state = States.STANDING;
        model.pos = position;
        model.gender = gender;

        model.changeID(BodyPartType.HAIR, hairID);
        model.changeID(BodyPartType.FACE, faceID);

        var rosePlayer = new RosePlayer(model);

        //var rosePlayer = new RosePlayer(transform.position); // Note: Player reference is lost after hitting play.  Must create new after that.


        cameraController.target = rosePlayer.player;
    }
}
