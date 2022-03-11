using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Scripts.Character.MonoBehavior.Player
{
    public class PlayerInputController
    {
        private PlayerInput playerInput;

        public bool EnablePlayerNormalInput
        {
            get => playerInput.Player.enabled;
            set
            {
                if (value) playerInput.Player.Enable();
                else playerInput.Player.Disable();
            }
            
        }
        
        public PlayerInputController Initialise()
        {
            playerInput = new PlayerInput();
            SubscribeActions();
            return this;
        }

        private void SubscribeActions()
        {
            playerInput.Player.Jump.started += JumpInputPressed;
            playerInput.Player.Jump.canceled += JumpInputCanceled;

            playerInput.Player.Movement.performed += MovementInputPerformed;
            playerInput.Player.Movement.canceled += MovementInputPerformed;
        }

        public bool Jump { get; private set; }

        public float LastJumpPressedTime { get; private set; }

        public Vector2 MovementAxisRaw { get; private set; }

        private void JumpInputPressed(InputAction.CallbackContext context)
        {
            Jump = true;
            LastJumpPressedTime = Time.time;
        }
        private void JumpInputCanceled(InputAction.CallbackContext context)
        {
            Jump = false;
        }
        private void MovementInputPerformed(InputAction.CallbackContext context)
        {
            MovementAxisRaw = context.ReadValue<Vector2>();
        }
    }
}