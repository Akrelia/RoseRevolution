using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;
using System.Linq;
using System.IO;
using RevolutionShared.Rose.Data;
using RevolutionShared.Data;
using RevolutionShared.Rose.Data.Items.Equipment;

/// <summary>
/// Login Sandbox GUI Controller.
/// </summary>
public class LoginSandboxGUIController : MonoBehaviour
{
    [Header("Components")]
    public LoginSandboxManager manager;
    public FakeDictionary<TMP_InputField, BodyPartType> inputFields;
    [Header("Appearence Panel")]
    public TextMeshProUGUI genderLabel;
    public TextMeshProUGUI hairLabel;
    public TextMeshProUGUI faceLabel;
    [Header("Clan")]
    public Image clanImage;
    public TMP_InputField clanInput;
    public Sprite defaultIcon;
    public Slider clanGradeSlider;
    public TextMeshProUGUI clanGradeLabel;
    [Header("Equipment Panel")]
    public GameObject subWeaponPanel;
    public TextMeshProUGUI weaponLabel;
    public TextMeshProUGUI subweaponLabel;
    public TextMeshProUGUI bodyLabel;
    public TextMeshProUGUI capLabel;
    public TextMeshProUGUI shoesLabel;
    public TextMeshProUGUI armsLabel;
    public TextMeshProUGUI backLabel;
    public TextMeshProUGUI maskLabel;
    [Header("Infos")]
    public TMP_InputField nameInput;
    public TMP_InputField weapInput;
    public TMP_InputField subWeapInput;
    public TMP_InputField bodyInput;
    public TMP_InputField capInput;
    public TMP_InputField footInput;
    public TMP_InputField armsInput;
    public TMP_InputField backInput;
    public TMP_InputField maskInput;
    public Sprite clanIcon;

    int currentHairIndex = 0;
    int currentFaceIndex = 0;
    int currentWeaponIndex = 0;
    int currentSubweaponIndex = 0;
    int currentBodyIndex = 0;
    int currentCapIndex = 0;
    int currentShoesIndex = 0;
    int currentArmsIndex = 0;
    int currentBackIndex = 0;
    int currentMaskIndex = 0;

    public void Start()
    {
        LoadPrefs();

        RefreshView();

        RefreshIcon();
    }

    public void RefreshView()
    {
        genderLabel.text = manager.rosePlayer.charModel.gender == GenderType.MALE ? "Male" : "Female";

        SwitchHair(0);
        SwitchFace(0);
        SwitchWeapon(0);
        SwitchSubweapon(0);
        SwitchBody(0);
        SwitchCap(0);
        SwitchShoes(0);
        SwitchArms(0);
        SwitchBack(0);
        SwitchMask(0);

        manager.UpdatePreview();
    }

    public void ConnectClick()
    {
        SavePrefs();

        var player = manager.rosePlayer.charModel;
        var equip = player.equip;

        SandboxManager.appearance = new CharacterAppearance(player.gender, (byte)equip.hairID, (byte)equip.faceID, equip.backID, equip.chestID, equip.handID, equip.footID, equip.maskID, equip.capID, equip.weaponID, equip.shieldID);
        SandboxManager._playerName = nameInput.text;
        SandboxManager._clanName = clanInput.text;
        SandboxManager._clanSprite = clanIcon;
        SandboxManager._clanGrade = (byte)clanGradeSlider.value;

        BootstrapManager.Instance.loadingManager.LoadScene(2, "Sandbox");
    }

    public void SwitchGender()
    {
        manager.rosePlayer.SetGender(manager.rosePlayer.charModel.gender == GenderType.MALE ? GenderType.FEMALE : GenderType.MALE);

        genderLabel.text = manager.rosePlayer.charModel.gender == GenderType.MALE ? "Male" : "Female";

        manager.UpdatePlayer();
    }

    public void SwitchFace(int direction)
    {
        currentFaceIndex = Utils.Modulo(currentFaceIndex + direction, manager.equipmentDatabase.appearenceDatabase.faces.Count);
        var faceData = manager.equipmentDatabase.appearenceDatabase.faces[currentFaceIndex];
        faceLabel.text = "Face " + faceData.id;
        manager.rosePlayer.Equip(BodyPartType.FACE, faceData.id);
        manager.UpdatePlayer();
    }

    public void SwitchHair(int direction)
    {
        currentHairIndex = Utils.Modulo(currentHairIndex + (direction * 4), manager.equipmentDatabase.appearenceDatabase.hairs.Count);
        var hairData = manager.equipmentDatabase.appearenceDatabase.hairs[currentHairIndex]; hairLabel.text = "Hair " + hairData.id;
        manager.rosePlayer.Equip(BodyPartType.HAIR, hairData.id);
        manager.UpdatePlayer();

        SwitchCap(0);
    }

    public void SwitchWeapon(int direction)
    {
        SwitchEquipment(ref currentWeaponIndex, direction, manager.equipmentDatabase.weaponDatabase.entries, BodyPartType.WEAPON, weaponLabel, weapInput, true);

        if (manager.rosePlayer.charModel.equip.weaponID == 0)
        {
            subWeaponPanel.SetActive(false);

            return;
        }

        var weapon = manager.equipmentDatabase.weaponDatabase.entries[currentWeaponIndex - 1];


        var oneHanded = Utils.IsOneHanded(weapon.item.weaponType);

        if (oneHanded)
        {
            SwitchSubweapon(0);
        }

        subWeaponPanel.SetActive(oneHanded);
    }

    public void SwitchSubweapon(int direction)
    {
        SwitchEquipment(ref currentSubweaponIndex, direction, manager.equipmentDatabase.subWeaponDatabase.entries, BodyPartType.SUBWEAPON, subweaponLabel, subWeapInput, true);
    }

    public void SwitchBody(int direction)
    {
        SwitchEquipment(ref currentBodyIndex, direction, manager.equipmentDatabase.bodyDatabase.entries, BodyPartType.BODY, bodyLabel, bodyInput);
    }

    public void SwitchCap(int direction)
    {
        SwitchEquipment(ref currentCapIndex, direction, manager.equipmentDatabase.headgearDatabase.entries, BodyPartType.CAP, capLabel, capInput, true);

        if (currentCapIndex == 0)
        {
            currentHairIndex = Utils.Modulo(currentHairIndex + (0 * 4), manager.equipmentDatabase.appearenceDatabase.hairs.Count);
            var hairData = manager.equipmentDatabase.appearenceDatabase.hairs[currentHairIndex]; hairLabel.text = "Hair " + hairData.id;
            manager.rosePlayer.Equip(BodyPartType.HAIR, hairData.id);
            manager.UpdatePlayer();
        }
    }

    public void SwitchShoes(int direction)
    {
        SwitchEquipment(ref currentShoesIndex, direction, manager.equipmentDatabase.footwearDatabase.entries, BodyPartType.FOOT, shoesLabel, footInput);
    }

    public void SwitchArms(int direction)
    {
        SwitchEquipment(ref currentArmsIndex, direction, manager.equipmentDatabase.armDatabase.entries, BodyPartType.ARMS, armsLabel, armsInput);
    }

    public void SwitchBack(int direction)
    {
        SwitchEquipment(ref currentBackIndex, direction, manager.equipmentDatabase.backDatabase.entries, BodyPartType.BACK, backLabel, backInput, true);
    }

    public void SwitchMask(int direction)
    {
        SwitchEquipment(ref currentMaskIndex, direction, manager.equipmentDatabase.faceItemDatabase.entries, BodyPartType.FACEITEM, maskLabel, maskInput, true);
    }

    private void SwitchEquipment<T>(ref int index, int direction, List<ItemPartDatabaseEntry<T>> items, BodyPartType bodyPart, TextMeshProUGUI label, TMP_InputField input, bool allowEmpty = false) where T : EquipmentData
    {
        var gender = manager.rosePlayer.charModel.gender;
        var list = items.Where(x => x.gender == GenderType.NONE || x.gender == gender).ToList();

        int count = list.Count + (allowEmpty ? 1 : 0);

        if (count == 0)
        {
            return;
        }

        index = Utils.Modulo(index + direction, count);

        if (allowEmpty && index == 0)
        {
            label.text = $"No {bodyPart.GetName()}";
            input.SetTextWithoutNotify("");

            manager.rosePlayer.Equip(bodyPart, 0);
            manager.UpdatePlayer();

            return;
        }

        var item = list[allowEmpty ? index - 1 : index];

        input.SetTextWithoutNotify(item.id.ToString());
        label.text = $"[{item.id}] {item.Item.name}";

        manager.rosePlayer.Equip(bodyPart, item.id);
        manager.UpdatePlayer();
    }

    public void SavePrefs()
    {
        PlayerPrefs.SetInt("HairIndex", currentHairIndex);
        PlayerPrefs.SetInt("FaceIndex", currentFaceIndex);
        PlayerPrefs.SetInt("WeaponIndex", currentWeaponIndex);
        PlayerPrefs.SetInt("SubweaponIndex", currentSubweaponIndex);
        PlayerPrefs.SetInt("BodyIndex", currentBodyIndex);
        PlayerPrefs.SetInt("CapIndex", currentCapIndex);
        PlayerPrefs.SetInt("ShoesIndex", currentShoesIndex);
        PlayerPrefs.SetInt("ArmsIndex", currentArmsIndex);
        PlayerPrefs.SetInt("BackIndex", currentBackIndex);
        PlayerPrefs.SetInt("MaskIndex", currentMaskIndex);
        PlayerPrefs.SetInt("Gender", (int)manager.rosePlayer.charModel.gender);
        PlayerPrefs.SetString("Name", nameInput.text);
        PlayerPrefs.SetString("ClanName", clanInput.text);
        PlayerPrefs.SetInt("ClanGrade", (int)clanGradeSlider.value);
        PlayerPrefs.Save();
    }

    public void LoadPrefs()
    {
        currentHairIndex = PlayerPrefs.GetInt("HairIndex", 0);
        currentFaceIndex = PlayerPrefs.GetInt("FaceIndex", 0);
        currentWeaponIndex = PlayerPrefs.GetInt("WeaponIndex", 1);
        currentSubweaponIndex = PlayerPrefs.GetInt("SubweaponIndex", 0);
        currentBodyIndex = PlayerPrefs.GetInt("BodyIndex", 2);
        currentCapIndex = PlayerPrefs.GetInt("CapIndex", 2);
        currentShoesIndex = PlayerPrefs.GetInt("ShoesIndex", 2);
        currentArmsIndex = PlayerPrefs.GetInt("ArmsIndex", 2);
        currentBackIndex = PlayerPrefs.GetInt("BackIndex", 0);
        currentMaskIndex = PlayerPrefs.GetInt("MaskIndex", 0);

        var gender = (GenderType)PlayerPrefs.GetInt("Gender", (int)GenderType.MALE);

        if (manager.rosePlayer.charModel.gender != gender)
        {
            manager.rosePlayer.SetGender(gender);
        }

        nameInput.text = PlayerPrefs.GetString("Name", "");
        clanInput.text = PlayerPrefs.GetString("ClanName", "");
        clanGradeSlider.value = PlayerPrefs.GetInt("ClanGrade", 1);
    }

    public void WeaponInputChanged(string text)
    {
        var id = Convert.ToInt32(text);
        currentWeaponIndex = id;
        SwitchWeapon(0);
    }

    public void SubWeaponInputChanged(string text)
    {
        var id = Convert.ToInt32(text);
        currentSubweaponIndex = id;
        SwitchSubweapon(0);
    }

    public void BodyInputChanged(string text)
    {
        var id = Convert.ToInt32(text);
        currentBodyIndex = id;
        SwitchBody(0);
    }

    public void CapInputChanged(string text)
    {
        var id = Convert.ToInt32(text);
        currentCapIndex = id;
        SwitchCap(0);
    }

    public void FootInputChanged(string text)
    {
        var id = Convert.ToInt32(text);
        currentShoesIndex = id;
        SwitchShoes(0);
    }

    public void ArmsInputChanged(string text)
    {
        var id = Convert.ToInt32(text);
        currentArmsIndex = id;
        SwitchArms(0);
    }

    public void BackInputChanged(string text)
    {
        var id = Convert.ToInt32(text);
        currentBackIndex = id;
        SwitchBack(0);
    }

    public void MaskInputChanged(string text)
    {
        var id = Convert.ToInt32(text);
        currentMaskIndex = id;
        SwitchMask(0);
    }

    public void GradeChanged(float value)
    {
        clanGradeLabel.text = $"Clan Grade ({(int)value})";
    }

    public void RefreshIcon()
    {
        int iconSize = 64;
        Texture2D texture = null;

#if UNITY_EDITOR
        if (defaultIcon != null)
        {
            Texture2D source = defaultIcon.texture;
            texture = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            texture.SetPixels(source.GetPixels());
            texture.Apply();
        }
        else
#endif
        {
            string path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "mark.png");

            if (!File.Exists(path))
            {
                clanIcon = null;
                clanImage.sprite = null;
                return;
            }

            byte[] data = File.ReadAllBytes(path);
            texture = new Texture2D(2, 2);

            if (!texture.LoadImage(data))
            {
                Destroy(texture);
                clanIcon = null;
                clanImage.sprite = null;
                return;
            }
        }

        if (texture.width > iconSize || texture.height > iconSize)
        {
            float scale = Mathf.Min((float)iconSize / texture.width, (float)iconSize / texture.height);
            int width = Mathf.Max(1, Mathf.RoundToInt(texture.width * scale));
            int height = Mathf.Max(1, Mathf.RoundToInt(texture.height * scale));

            Texture2D resized = new Texture2D(width, height, TextureFormat.RGBA32, false);

            RenderTexture renderTexture = RenderTexture.GetTemporary(width, height);
            Graphics.Blit(texture, renderTexture);

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTexture;

            resized.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            resized.Apply();

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTexture);

            Destroy(texture);
            texture = resized;
        }

        clanIcon = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        clanImage.sprite = clanIcon;
    }
}