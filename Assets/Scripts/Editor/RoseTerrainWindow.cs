#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using UnityRose;
using UnityRose.Formats;
using UnityRose.Import;
using System;
using System.IO;
using System.Text;
using RevolutionShared.Rose.Data;

namespace UnityRose.ImportEditor
{
    /// <summary>
    /// Akima : This whole class is now useless, but I leave it for the Character Creation since it will be the same code. So remove it when the Character Creation is done.
    /// </summary>
    public class RoseTerrainWindow : EditorWindow
    {
     /*   BodyPartType bodyPart;
        int objID;
        RosePlayer player;
        Transform transform;
        GameObject playerObject;

        [MenuItem("ROSE Online/Character Creator")]
        static void Init()
        {
            GetWindow(typeof(RoseTerrainWindow), true, "ROSE Character Creator");
        }

        public static GameObject ImportMap(int mapID) => RoseMapImporter.ImportMap(mapID);

        void OnGUI()
        {
            EditorGUILayout.BeginToggleGroup("Characters", true);
            objID = EditorGUILayout.IntField("ID: ", objID);
            bodyPart = (BodyPartType)EditorGUILayout.EnumPopup("Body Part: ", bodyPart);
            transform = EditorGUILayout.ObjectField("Transform: ", transform, typeof(Transform), true) as Transform;
            playerObject = EditorGUILayout.ObjectField("Player Game Object: ", playerObject, typeof(GameObject), true) as GameObject;

            if (GUILayout.Button("Create Player"))
                player = transform != null ? new RosePlayer(transform.position) : new RosePlayer();

            if (GUILayout.Button("Create Player (Selection)"))
            {
                var model = new CharModel { rig = RigType.CHARSELECT, state = States.HOVERING };
                if (transform != null) model.pos = transform.position;
                player = new RosePlayer(model);
            }

            if (GUILayout.Button("Equip to Character"))
            {
                if (playerObject != null)
                {
                    var playerController = playerObject.GetComponent<PlayerController>();

                    playerController.rosePlayer.equip(bodyPart, objID);
                }

                else
                {
                    Debug.Log("Please set a player in the window");
                }
            }

            if (GUILayout.Button("Generate Player Animations"))
            {
                GenerateCharSelectAnimations();
            }

            EditorGUILayout.EndToggleGroup();
        }

        void GenerateCharSelectAnimations()
        {
            foreach (GenderType gender in Enum.GetValues(typeof(GenderType)))
            {
                bool m = gender == GenderType.MALE;

                var clips = new System.Collections.Generic.Dictionary<string, string>
                {
                    { "standup", "3ddata/motion/avatar/empty_stand_" + (m ? "m" : "f") + "1.zmo" },
                    { "standing", "3ddata/motion/avatar/empty_stop1_" + (m ? "m" : "f") + "1.zmo" },
                    { "sit", "3ddata/motion/avatar/empty_sit_" + (m ? "m" : "f") + "1.zmo" },
                    { "sitting", "3ddata/motion/avatar/empty_siting_" + (m ? "m" : "f") + "1.zmo" },
                    { "hovering", "3ddata/motion/avatar/event_creat_m1.zmo" },
                    { "select", "3ddata/motion/avatar/event_select_m1.zmo" },
                };

                ResourceManager.Instance.GenerateAnimationAsset(gender, RigType.CHARSELECT, clips);
            }
        }
     */
    }
}
#endif