using System;
using _Scripts.Character.Interface;
using UnityEngine;

namespace _Scripts.Character.MonoBehavior.Player
{
    public enum MecanimStates
    {
        Grounded,
        Crouching,
        Jumping,
        Idle,
        Running
    }

    public enum AnimationKey
    {
        PlayerIdle,
        PlayerRun,
        PlayerJump,
        PlayerCrouch,
        PlayerGrounded
    }

    public class MecanimStateController
    {
        private readonly Animator _animator;
        [Range(-1.0f, 1.0f)] private float _horizontalDirection;
        [Range(-1.0f, 1.0f)] private float _verticalDirection;
        private float _horizontalSpeed;
        private float _verticalSpeed;
        private bool _grounded;
        private bool _crouching;
        private int _currentAnimationState;
        private MecanimStates _currentMecanimState;

        private static readonly int PlayerIdleKey = Animator.StringToHash("Player_Idle");
        private static readonly int PlayerRunKey = Animator.StringToHash("Player_Run");
        private static readonly int PlayerJumpKey = Animator.StringToHash("Player_Jump");
        private static readonly int PlayerCrouchKey = Animator.StringToHash("Player_Crouch");
        private static readonly int PlayerGroundedKey = Animator.StringToHash("Grounded");

        public MecanimStateController (Animator animator)
        {
            _animator = animator;
        }

        private int GetAnimationFor(AnimationKey animation)
        {
            switch (animation)
            {
                case AnimationKey.PlayerIdle:
                    return PlayerIdleKey;
                case AnimationKey.PlayerJump:
                    return PlayerJumpKey;
                case AnimationKey.PlayerCrouch:
                    return PlayerCrouchKey;
                case AnimationKey.PlayerRun:
                    return PlayerRunKey;
                case AnimationKey.PlayerGrounded:
                    return PlayerGroundedKey;
            }

            return 0;
        }
        
        private void ChangeAnimationState(AnimationKey newState)
        {
            var newStateKey = GetAnimationFor(newState);
            
            if (_currentAnimationState == newStateKey) return;

            _animator.Play(newStateKey);

            _currentAnimationState = newStateKey;
        }

        private void PlayAnimationIdleIfNeeded()
        {
            if (_grounded && _horizontalSpeed == 0)
            {
                ChangeAnimationState(AnimationKey.PlayerIdle);

            }
        }
        
        private void PlayAnimationRunIfNeeded()
        {
            if (_grounded && _horizontalSpeed != 0)
            {
                ChangeAnimationState(AnimationKey.PlayerRun);
            }
        }
        
        private void PlayJumpAnimationIfNeeded()
        {
            if (!_grounded && _verticalSpeed != 0)
            {
                ChangeAnimationState(AnimationKey.PlayerJump);
            }
        }

        public void UpdateWithNewState(IPlayerHandler playerHandler)
        {
            _horizontalSpeed = playerHandler.Velocity.x;
            _horizontalDirection = Math.Clamp(playerHandler.RawMovement.x, -1.0f, 1.0f);
            _verticalDirection = Math.Clamp(playerHandler.RawMovement.y, -1.0f, 1.0f);
            _verticalSpeed = playerHandler.Velocity.y;
            _grounded = playerHandler.Grounded;
            
            PlayAnimationIdleIfNeeded();
            PlayAnimationRunIfNeeded();
            PlayJumpAnimationIfNeeded();
        }

        public void UpdateHorizontalInput(float horizontalMove)
        {
            _horizontalSpeed = horizontalMove;
            _horizontalDirection = Math.Clamp(horizontalMove, -1.0f, 1.0f);
            ChangeAnimationState(_horizontalSpeed != 0 ? AnimationKey.PlayerRun : AnimationKey.PlayerIdle);

        }

        public void setMecanimState(MecanimStates state)
        {
            _currentMecanimState = state;
        }
    }
}