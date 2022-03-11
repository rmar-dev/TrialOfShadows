using System;
using System.Collections.Generic;
using _Scripts.Character.Interface;
using _Scripts.Character.Model;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace _Scripts.Character.MonoBehavior.Player
{
    public class PlayerCharacter :MonoBehaviour, IPlayerController
    {
        private static PlayerCharacter sPlayerInstance;
        public static PlayerCharacter PlayerInstance => sPlayerInstance;
        
        public SpriteRenderer spriteRenderer;
       
        private Vector3 lastPosition;
        private Vector3 startingTransformPosition = Vector3.zero;
        private Vector3 currentTransformPosition = Vector3.zero;
        private bool startingFacingLeft = false;
        private PhysicsController mPhysicsController;
        private PlayerInputController playerInputController;
        [SerializeField] public bool spriteOriginallyFacesLeft;

        #region IPLayerController
        public Vector3 Velocity { get; private set; }
        public bool JumpingThisFrame { get; private set; }
        public bool LandingThisFrame { get; private set; }
        public Vector3 RawMovement { get; private set; }
        public bool Grounded => mPhysicsController.Grounded;
        private bool CollidingTop => mPhysicsController.CollidingUp;
        private bool CollidingLeft => mPhysicsController.CollidingLeft;
        private bool CollidingRight => mPhysicsController.CollidingRight;

        private float CurrentVerticalSpeed => mPhysicsController.CurrentVerticalSpeed;
        private float CurrentHorizontalSpeed => mPhysicsController.CurrentHorizontalSpeed;


        #endregion
        
        // This is horrible, but for some reason colliders are not fully established when update starts...
        private bool active;
        private void Activate() =>  active = true;
        
        private void Awake()
        {
            Invoke(nameof(Activate), 0.5f);
            sPlayerInstance = GetComponent<PlayerCharacter>();
            mPhysicsController = GetComponent<PhysicsController>();
            playerInputController = new PlayerInputController().Initialise();
        }
        private void Start()
        {
            playerInputController.EnablePlayerNormalInput = true;
            startingTransformPosition = transform.position;
            startingFacingLeft = GetFacing() < 0.0f;
        }

        private void FixedUpdate()
        {
            currentTransformPosition = transform.position;
            CalculateGravity();
        }

        private void Update()
        {

            if(!active) return;
            
            UpdateVelocity(currentTransformPosition);
            RunCollisionsCheck();

            CalculateWalk();
            CalculateJumpApex();
            CalculateJump();
            
            MoveCharacter();
            UpdateFacing();
            
        }
        
        private void UpdateVelocity(Vector3 position)
        {
            Velocity = (position - lastPosition) / Time.deltaTime;
            lastPosition = position; 
        }

        #region SpriteFacing

        public void UpdateFacing()
        {
            bool faceLeft = playerInputController.MovementAxisRaw.x < 0f;
            bool faceRight = playerInputController.MovementAxisRaw.x > 0f;

            if (faceLeft)
            {
                spriteRenderer.flipX = !spriteOriginallyFacesLeft;
            }
            else if (faceRight)
            {
                spriteRenderer.flipX = spriteOriginallyFacesLeft;
            }
        }

        public void UpdateFacing(bool faceLeft)
        {
            if (faceLeft)
            {
                spriteRenderer.flipX = !spriteOriginallyFacesLeft;
            }
            else
            {
                spriteRenderer.flipX = spriteOriginallyFacesLeft;
            }
        }
        
        public float GetFacing()
        {
            return spriteRenderer.flipX != spriteOriginallyFacesLeft ? -1f : 1f;
        }


        #endregion
       
        #region Jump
        
        [Header("JUMPING")]
        [SerializeField] private float jumpApexThreshold = 10f;
        [SerializeField] private float coyoteTimeThreshold = 0.1f;
        [SerializeField] private float jumpBuffer = 0.1f;
        
        private bool Jumping => playerInputController.Jump;
        private float LastJumpPressed => playerInputController.LastJumpPressedTime;

        private bool coyoteUsable;
        private bool endedJumpEarly = true;
        private float apexPoint; // Becomes 1 at the apex of a jump

        private bool CanUseCoyote => coyoteUsable && !Grounded && timeLeftGrounded + coyoteTimeThreshold > Time.time;
        private bool HasBufferedJump => Grounded && LastJumpPressed + jumpBuffer > Time.time;

        private void CalculateJumpApex() => apexPoint = CalculateJumpApexPoint();
        
        private float CalculateJumpApexPoint()
        {
            if (Grounded) return 0;
            
            return Mathf.InverseLerp(jumpApexThreshold, 0, Mathf.Abs(Velocity.y));;
        }

        private void CalculateJump() {
            if (!mPhysicsController.IsCollidingTop())
            {
                ApplyJumpIfNeeded();
            };
            EndedJumpEarly();
        }

        private void ApplyJumpIfNeeded()
        {
            if (HasBufferedJump || Jumping && CanUseCoyote)
            {
                mPhysicsController.AddVerticalSpeedHeight();
                endedJumpEarly = false;
                coyoteUsable = false;
                timeLeftGrounded = float.MinValue;
                JumpingThisFrame = true;
            }
            else
            {
                JumpingThisFrame = false;
            }
        }

        private void EndedJumpEarly()
        {
            if (!Grounded && !Jumping && !endedJumpEarly && Velocity.y > 0) 
                endedJumpEarly = true;
        }

        #endregion

        #region Collisions
        [Header("Collision")]
        [SerializeField] private float jumpEndEarlyGravityModifier = 3f;
        private float fallSpeed;

        private float timeLeftGrounded;
        
        private void RunCollisionsCheck()
        {
            if (mPhysicsController.ExitGroundThisFrame) timeLeftGrounded = Time.time; 
            
            var touchGroundThisFrame = mPhysicsController.TouchGroundThisFrame;
            
            if (touchGroundThisFrame) coyoteUsable = true; // Only trigger when first touching
            
            LandingThisFrame = touchGroundThisFrame;
        }

        private void CalculateGravity()
        {
            if (Grounded)
            {
                mPhysicsController.StopMovingVerticaly();
                return;
            };
                
            fallSpeed = mPhysicsController.CalculateFallSpeed(apexPoint);
            if (fallSpeed == 0) return;
            
            // Add downward force while ascending if we ended the jump early
            var fallSpeedModifier = endedJumpEarly && CurrentVerticalSpeed > 0
                ? fallSpeed * jumpEndEarlyGravityModifier
                : fallSpeed;

            mPhysicsController.UpdateCurrentVerticalSpeed(fallSpeedModifier);
        }

        #endregion
        
        #region Walk
        [Header("Acceleration")] [SerializeField] private float apexBonus = 2;
        public float HorizontalMove => playerInputController.MovementAxisRaw.x;
        private void CalculateWalk() {
            if (HorizontalMove != 0 && mPhysicsController.CanAccelerate()) {
                var tempApexBonus = Mathf.Sign(HorizontalMove) * apexBonus * apexPoint;
                mPhysicsController.CalculateAcceleration(HorizontalMove, apexPoint);
                return;
            }
                
            mPhysicsController.CalculateDeceleration();
        }

        #endregion
        
        #region Move

        [Header("MOVE")]
        [SerializeField, Tooltip("Raising this value increases collision accuracy at the cost of performance.")]
        private int freeColliderIterations = 3;

        // We cast our bounds before moving to avoid future collisions
        private void MoveCharacter()
        {
            if (!mPhysicsController.MoveIfNeeded()) return;

            var position = mPhysicsController.GetTransformPosition;
            RawMovement = mPhysicsController.GetRawMovement();
            var move = RawMovement * Time.deltaTime;
            var furthestPoint = position + move;
            
            var hit = mPhysicsController.OverlapBoxDetector(furthestPoint);
            Debug.Log("object is Moving: " + RawMovement);
            Debug.Log("object is hitting: " + hit);

            if (!hit)
            {
                transform.position += move;
                return;
            }

            transform.position = mPhysicsController.GetClosestPosition(hit);

            /*// otherwise increment away from current pos; see what closest position we can move to
            for (int collider = 1; collider < freeColliderIterations; collider++) {
                // increment to check all but furthestPoint - we did that already
                var t = (float)collider / freeColliderIterations;
                var posToTry = Vector2.Lerp(position, furthestPoint, t);
                
                if (mPhysicsController.OverlapBoxDetector(posToTry)) {

                    transform.position = positionToMoveTo;
                    return;
                }

                positionToMoveTo = posToTry;
            }*/
        }
        #endregion
    }
}