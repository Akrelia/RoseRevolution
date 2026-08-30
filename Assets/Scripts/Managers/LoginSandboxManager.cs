using RevolutionShared.Data;
using RevolutionShared.Rose.Data;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityRose;

/// <summary>
/// Login Sandbox Manager.
/// </summary>
public class LoginSandboxManager : MonoBehaviour
{
    [Header("Scene")]
    public GameObject characterAnchor;
    public ModelPreview preview;

    public RosePlayer rosePlayer;
    public CharacterAppearance appearence;

    [HideInInspector]
    public EquipmentDatabase equipmentDatabase;
    [HideInInspector]
    public CharacterDatabase characterDatabase;
    [HideInInspector]
    public EffectDatabase effectDatabase;

    /// <summary>
    /// Awake.
    /// </summary>
    private void Awake()
    {
        Addressables.InitializeAsync().WaitForCompletion();

        equipmentDatabase = Utils.LoadDatabase<EquipmentDatabase>();
        characterDatabase = Utils.LoadDatabase<CharacterDatabase>();
        effectDatabase = Utils.LoadDatabase<EffectDatabase>();

        appearence = new CharacterAppearance(GenderType.FEMALE, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0);

        rosePlayer = SpawnPlayer(appearence);

        preview.Show(rosePlayer.player.gameObject, false, true);
    }

    public void UpdatePlayer()
    {
        //     rosePlayer.Equip(BodyPartType.FACEITEM, appearence.Mask);
        //     rosePlayer.Equip(BodyPartType.WEAPON, appearence.Weapon);
        //     rosePlayer.Equip(BodyPartType.SUBWEAPON, appearence.SubWeapon);

        preview.Show(rosePlayer.player.gameObject, false, false);
    }

    public void UpdatePreview()
    {
        preview.Show(rosePlayer.player.gameObject, false, true);
    }

    /// <summary>
    /// Spawn the player.
    /// </summary>
    /// <param name="appearence"></param>
    /// <returns></returns>
    public RosePlayer SpawnPlayer(CharacterAppearance appearence)
    {
        CharModel model = new CharModel();

        model.rig = RigType.FOOT;
        model.state = States.SIT;

        var rosePlayer = new RosePlayer(model);

        model.ApplyAppearence(appearence);

        rosePlayer.LoadPlayer(model, equipmentDatabase, characterDatabase, effectDatabase);

        rosePlayer.Equip(BodyPartType.FACEITEM, appearence.Mask);
        rosePlayer.Equip(BodyPartType.WEAPON, appearence.Weapon);
        rosePlayer.Equip(BodyPartType.SUBWEAPON, appearence.SubWeapon);

        rosePlayer.player.GetComponent<PlayerController>().enabled = false;

        return rosePlayer;
    }
}
