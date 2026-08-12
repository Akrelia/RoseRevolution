using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using RevolutionShared.Rose.Data;

namespace UnityRose
{
    public class PlayerController : MonoBehaviour
    {
        public bool isMainPlayer = false;
        public PlayerInfo playerInfo;
        public GameObject cursor;

        private int floorMask;
        private float camRayLength = 500f;
        public Vector3 destinationPosition;
        private CharacterController controller;
        private State animationStateMachine;
        private bool isWalking = false;
        private States state = States.STANDING;
        public RosePlayer rosePlayer;

        public void Start()
        {
            floorMask = LayerMask.GetMask("Floor") | LayerMask.GetMask("MapObjects");
            controller = this.gameObject.GetComponent<CharacterController>();
            destinationPosition = transform.position;
            playerInfo.name = this.name;
        }

        public void SetAnimationStateMachine(RigType rig, States initialState)
        {
            state = initialState;

            animationStateMachine = new PlayerState(initialState, "Player State Machine", rosePlayer.skeleton);

            animationStateMachine.Entry();
        }

        public void SetAnimationState(States state)
        {
            this.state = state;

            if (animationStateMachine != null)
            {
                animationStateMachine.Evaluate(state);
            }
        }

        public void OnSkeletonChange()
        {
            Debug.Log($"OnSkeletonChange - OLD MACHINE: {animationStateMachine}");

            animationStateMachine = null;

            SetAnimationStateMachine(rosePlayer.charModel.rig, state);

            Debug.Log($"OnSkeletonChange - NEW MACHINE: {animationStateMachine}");
        }

        public void OnChangeEquip(BodyPartType bodyPart, int id)
        {
            rosePlayer.Equip(bodyPart, id);
        }

        private void Update()
        {
            if (rosePlayer.charModel.rig == RigType.FOOT)
            {
                if (isMainPlayer)
                {
                    bool locate = false;

                    locate = Input.GetMouseButton(0);

                    if (locate)
                    {
                        LocatePosition();
                    }
                }

                MoveToPosition();

                if (isWalking)
                {
                    state = States.RUN;
                }

                else
                {
                    state = States.STANDING;
                }
            }

            if (animationStateMachine != null)
            {
                animationStateMachine.Evaluate(state);
            }
        }

        public void LocatePosition()
        {
            Vector2 screenPoint;
            bool fire = false;

            screenPoint = Input.mousePosition;
            fire = Input.GetMouseButtonDown(0);

            Ray camRay = Camera.main.ScreenPointToRay(screenPoint);
            RaycastHit floorHit;

            if (fire)
            {
                if (EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }

                else
                {
                    if (Physics.Raycast(camRay, out floorHit, camRayLength, floorMask))
                    {
                        destinationPosition = floorHit.point;
                    }

                    Client.Instance.SendPacket(Packets.Move(destinationPosition));
                }
            }
        }

        public void MoveToPosition()
        {
            if (Vector3.Distance(transform.position, destinationPosition) > 0.5f)
            {
                Vector3 playerToMouse = destinationPosition - transform.position;

                playerToMouse.y = 0;

                Quaternion newRotation = Quaternion.LookRotation(playerToMouse);

                transform.rotation = newRotation;

                controller.SimpleMove(transform.forward * playerInfo.tMovS);

                isWalking = true;
            }

            else
            {
                isWalking = false;
            }
        }
    }
}
