using System;
using UnityEngine;
using System.Collections.Generic;
using RevolutionShared.Rose.Data;
using RevolutionShared.Rose.Data.Equipment;

namespace UnityRose
{
    public static class RoseSkeletonLoader
    {
        public static GameObject LoadSkeleton(GenderType gender, WeaponType weapon)
        {
            string weaponName = Enum.GetName(typeof(WeaponType), weapon); // This was added because the old system used the WeaponType as a name when no value, but if there is a value, its using the ID. So explicity calling the name

            if (string.IsNullOrEmpty(weaponName))
            {
                Debug.LogError($"Invalid WeaponType value: {(int)weapon}");

                return null;
            }

            var prefab = Resources.Load($"Animation/{gender}/{weaponName}/skeleton");

            if (prefab == null)
            {
                Debug.LogError($"No skeleton prefab at Animation/{gender}/{weaponName}/skeleton");
                return null;
            }

            return GameObject.Instantiate(prefab) as GameObject;
        }
        public static GameObject LoadSkeleton(GenderType gender, RigType rig)
        {
            var prefab = Resources.Load($"Animation/{gender}/{rig}/skeleton");

            if (prefab == null)
            {
                Debug.LogError($"No skeleton prefab at Animation/{gender}/{rig}/skeleton");

                return null;
            }

            return GameObject.Instantiate(prefab) as GameObject;
        }

        public static BindPoses LoadBindPoses(GameObject skeleton, GenderType gender, WeaponType weapon)
        {
            var source = Resources.Load<BindPoses>($"Animation/{gender}/{weapon}/bindPoses");

            if (source == null)
            {
                Debug.LogWarning($"No BindPoses at Animation/{gender}/{weapon}/bindPoses");

                return null;
            }

            var poses = ScriptableObject.Instantiate(source);

            for (int i = 0; i < poses.boneNames.Length; i++)
            {
                poses.boneTransforms[i] = Utils.findChild(skeleton, poses.boneNames[i]);
            }

            return poses;
        }
    }

    public class RosePlayer
    {
        public GameObject player;
        public CharModel charModel;

        private BindPoses bindPoses;
        private GameObject skeleton;
        private Bounds playerBounds;

        private readonly Dictionary<BodyPartType, List<Transform>> equipped = new();

        public static EquipmentDatabase EquipmentDatabase { get; set; }

        public RosePlayer()
        {
            charModel = new CharModel();
        }

        public RosePlayer(GenderType gender)
        {
            charModel = new CharModel();
            charModel.gender = gender;
        }

        public RosePlayer(CharModel charModel)
        {
            this.charModel = charModel;
        }

        public RosePlayer(Vector3 position)
        {
            charModel = new CharModel();
            charModel.pos = position;
        }

        public void LoadPlayer(CharModel charModel, EquipmentDatabase database)
        {
            EquipmentDatabase = database;

            if (EquipmentDatabase == null)
            {
                Debug.LogError("RosePlayer.AvatarDatabase is not set - assign it before creating a RosePlayer.");

                return;
            }

            player = new GameObject(charModel.name);

            LoadPlayerSkeleton(charModel);

            if (skeleton == null)
                return;

            player.layer = LayerMask.NameToLayer("Players");

            PlayerController controller = player.AddComponent<PlayerController>();
            controller.rosePlayer = this;
            controller.playerInfo.tMovS = charModel.stats.movSpd;

            Vector3 center = skeleton.transform.Find("b1_pelvis").localPosition;
            center.y = 0.95f;
            float height = 1.7f;
            float radius = Math.Max(playerBounds.extents.x, playerBounds.extents.y) / 2.0f;

            CharacterController charController = player.AddComponent<CharacterController>();
            charController.center = center;
            charController.height = height;
            charController.radius = radius;
            charController.slopeLimit = 125;

            CapsuleCollider c = player.AddComponent<CapsuleCollider>();
            c.center = center;
            c.height = height;
            c.radius = radius;
            c.direction = 1;

            player.transform.position = charModel.pos;
            controller.SetAnimationStateMachine(charModel.rig, charModel.state);
        }

        public void Destroy()
        {
            if (bindPoses != null) GameObject.Destroy(bindPoses);
            if (skeleton != null) GameObject.Destroy(skeleton);
            if (player != null) GameObject.Destroy(player);
        }

        private void LoadPlayerSkeleton(CharModel charModel)
        {
            LoadPlayerSkeleton(
                charModel.gender,
                charModel.weapon,
                charModel.rig,
                charModel.equip.weaponID,
                charModel.equip.chestID,
                charModel.equip.handID,
                charModel.equip.footID,
                charModel.equip.hairID,
                charModel.equip.faceID,
                charModel.equip.backID,
                charModel.equip.capID,
                charModel.equip.shieldID,
                charModel.equip.maskID);
        }

        private void LoadPlayerSkeleton(GenderType gender, WeaponType weapType, RigType rig, int weapon, int body, int arms, int foot, int hair, int face, int back, int cap, int shield, int faceitem)
        {
            int childs = player.transform.childCount;

            for (int i = childs - 1; i > 0; i--)
                Utils.Destroy(player.transform.GetChild(i).gameObject);

            equipped.Clear();

            if (skeleton != null)
                Utils.Destroy(skeleton);

            skeleton = rig == RigType.FOOT ? RoseSkeletonLoader.LoadSkeleton(gender, weapType) : RoseSkeletonLoader.LoadSkeleton(gender, rig);

            if (skeleton == null)
                return;

            bindPoses = RoseSkeletonLoader.LoadBindPoses(skeleton, gender, weapType);
            skeleton.transform.parent = player.transform;
            skeleton.transform.localPosition = Vector3.zero;
            skeleton.transform.localRotation = Quaternion.identity;

            PlayerController controller = player.GetComponent<PlayerController>();

            if (controller != null)
                controller.OnSkeletonChange();

            playerBounds = new Bounds(player.transform.position, Vector3.zero);

            playerBounds.Encapsulate(LoadObject(BodyPartType.BODY, body));
            playerBounds.Encapsulate(LoadObject(BodyPartType.ARMS, arms));
            playerBounds.Encapsulate(LoadObject(BodyPartType.FOOT, foot));
            playerBounds.Encapsulate(LoadObject(BodyPartType.FACE, face));

            LoadObject(BodyPartType.CAP, cap);

            var hat = EquipmentDatabase.GetItem<HeadgearData>(BodyPartType.CAP, cap, gender);

            var hairOffset = hat != null  ? hat.item.hair : 0;

            playerBounds.Encapsulate(LoadObject(BodyPartType.HAIR, hair - hair % 5 + hairOffset));

            LoadObject(BodyPartType.SUBWEAPON, shield);
            LoadObject(BodyPartType.WEAPON, weapon);
            LoadObject(BodyPartType.BACK, back);
            LoadObject(BodyPartType.FACEITEM, faceitem);
        }

        public void Equip(BodyPartType bodyPart, int id, bool changeId = true)
        {
            if (bodyPart == BodyPartType.WEAPON)
            {
                var weapon = EquipmentDatabase.GetItem<WeaponData>(BodyPartType.WEAPON, id, GenderType.NONE);

                WeaponType weapType = weapon?.item.weaponType ?? WeaponType.EMPTY;

                if (changeId)
                    charModel.changeID(bodyPart, id);

                if (!Utils.isOneHanded(weapType) && charModel.equip.shieldID > 0)
                    Equip(BodyPartType.SUBWEAPON, 0);

                if (weapType != charModel.weapon)
                {
                    charModel.weapon = weapType;
                    LoadPlayerSkeleton(charModel);

                    return;
                }
            }

            if (bodyPart == BodyPartType.SUBWEAPON && id > 0 && !Utils.isOneHanded(charModel.weapon))
                Equip(BodyPartType.WEAPON, 0);

            if (bodyPart == BodyPartType.CAP)
            {
                var cap = EquipmentDatabase.GetItem<HeadgearData>(BodyPartType.CAP, id, charModel.gender);

                if (cap != null)
                {
                    var hairOffset = cap.item.hair;

                    Equip(BodyPartType.HAIR, charModel.equip.hairID - charModel.equip.hairID % 5 + hairOffset, false);
                }
            }

            if (changeId)
                charModel.changeID(bodyPart, id);

            if (equipped.TryGetValue(bodyPart, out var previousParts))
            {
                foreach (var part in previousParts)
                    if (part != null)
                        Utils.Destroy(part.gameObject);
            }
            equipped.Remove(bodyPart);

            LoadObject(bodyPart, id);
        }

        public void changeGender(GenderType gender)
        {
            charModel.gender = gender;
            LoadPlayerSkeleton(charModel);
        }

        public void changeName(string name)
        {
            charModel.name = name;
            player.name = name;
        }

        private Bounds LoadObject(BodyPartType bodyPart, int id)
        {
            var fallbackBounds = new Bounds(skeleton.transform.position, Vector3.zero);

            var prefab = EquipmentDatabase.GetPrefab(bodyPart, id, charModel.gender);

            if (prefab == null)
            {
                Debug.LogWarning($"No prefab for {bodyPart} id {id}");

                return fallbackBounds;
            }


            var instance = GameObject.Instantiate(prefab);

            instance.name = bodyPart.ToString();
            instance.transform.SetParent(player.transform, false);


            var parts = new List<Transform>();

            foreach (Transform child in instance.transform)
                parts.Add(child);


            foreach (var part in parts)
            {
                var attachment = part.GetComponent<RoseAttach>();

                var anchor = attachment != null
                    ? GetAnchorForDummy(bodyPart, attachment.dummy)
                    : GetDefaultAnchor(bodyPart);


                part.SetParent(anchor, false);
                part.localPosition = Vector3.zero;
                part.localRotation = Quaternion.identity;
                part.localScale = Vector3.one;
            }

            BindSkinnedRenderers(parts);

            GameObject.Destroy(instance);


            equipped[bodyPart] = parts;


            return GetBounds(parts, fallbackBounds);
        }

        private Transform GetAnchorForDummy(BodyPartType bodyPart, RoseAttachPoint dummy)
        {
            return dummy switch
            {
                RoseAttachPoint.RightHand => Utils.findChild(skeleton, "p_00"),
                RoseAttachPoint.LeftHand => Utils.findChild(skeleton, "p_01"),
                RoseAttachPoint.LeftShield => Utils.findChild(skeleton, "p_02"),
                RoseAttachPoint.Back => Utils.findChild(skeleton, "p_03"),
                RoseAttachPoint.Face => Utils.findChild(skeleton, "p_04"),
                RoseAttachPoint.Head => Utils.findChild(skeleton, "p_06"),
                RoseAttachPoint.Feet => Utils.findChild(skeleton, "b1_foot") != null
                    ? Utils.findChild(skeleton, "b1_foot")
                    : GetDefaultAnchor(bodyPart),
                _ => GetDefaultAnchor(bodyPart)
            };
        }

        private Transform GetDefaultAnchor(BodyPartType bodyPart)
        {
            return bodyPart switch
            {
                BodyPartType.FACE or BodyPartType.HAIR => Utils.findChild(skeleton, "b1_head"),
                BodyPartType.CAP => Utils.findChild(skeleton, "p_06"),
                BodyPartType.BACK => Utils.findChild(skeleton, "p_03"),
                BodyPartType.WEAPON => charModel.weapon == WeaponType.BOW
                    ? Utils.findChild(skeleton, "p_01")
                    : Utils.findChild(skeleton, "p_00"),
                BodyPartType.SUBWEAPON => Utils.findChild(skeleton, "p_02"),
                BodyPartType.FACEITEM => Utils.findChild(skeleton, "p_04"),
                _ => skeleton.transform.parent
            };
        }

        private void BindSkinnedRenderers(List<Transform> parts)
        {
            if (bindPoses == null) return;

            foreach (var part in parts)
            {
                var renderer = part.GetComponent<SkinnedMeshRenderer>();
                if (renderer == null || renderer.sharedMesh == null) continue;

                var mesh = UnityEngine.Object.Instantiate(renderer.sharedMesh);
                mesh.bindposes = bindPoses.bindPoses;

                renderer.sharedMesh = mesh;
                renderer.bones = bindPoses.boneTransforms;
                renderer.rootBone = bindPoses.boneTransforms.Length > 0 ? bindPoses.boneTransforms[0] : null;
            }
        }

        private static Bounds GetBounds(List<Transform> parts, Bounds fallback)
        {
            Bounds? bounds = null;

            foreach (var part in parts)
            {
                var renderer = part.GetComponent<Renderer>();
                if (renderer == null) continue;

                bounds = bounds == null ? renderer.bounds : Extend(bounds.Value, renderer.bounds);
            }

            return bounds ?? fallback;
        }

        private static Bounds Extend(Bounds a, Bounds b)
        {
            a.Encapsulate(b);
            return a;
        }

        public void setAnimationState(States state)
        {
            charModel.state = state;
            player.GetComponent<PlayerController>().SetAnimationState(state);
        }
    }
}