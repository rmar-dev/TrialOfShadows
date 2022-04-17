using _Scripts.Character.Interface;
using UnityEditor;
using UnityEngine;

namespace _Scripts.Character.MonoBehavior.Player
{
    public class PlayerCharacter : MonoBehaviour, IPlayerController
    {
        private static PlayerCharacter sPlayerInstance;
        public static PlayerCharacter PlayerInstance => sPlayerInstance;

        public SpriteRenderer spriteRenderer;

        private Vector3 lastPosition;
        private bool startingFacingLeft = false;
        private PhysicsController mPhysicsController;
        private PlayerInputController playerInputController;
        [SerializeField] public bool spriteOriginallyFacesLeft;
        [SerializeField] [Range(0.0f, 3.0f)] private float closestGizmoSize = 0.1f;

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
        private void Activate() => active = true;

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
            startingFacingLeft = GetFacing() < 0.0f;
        }

        private void FixedUpdate()
        {
            CalculateGravity();

        }

        private void Update()
        {
            if (!active) return;

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

        [Header("JUMPING")] [SerializeField] private float jumpApexThreshold = 10f;
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

            return Mathf.InverseLerp(jumpApexThreshold, 0, Mathf.Abs(Velocity.y));
            ;
        }

        private void CalculateJump()
        {
            if (!mPhysicsController.IsCollidingTop())
            {
                ApplyJumpIfNeeded();
            }

            ;
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

        [Header("Collision")] [SerializeField] private float jumpEndEarlyGravityModifier = 3f;
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
            if (Grounded && CurrentVerticalSpeed != 0)
            {
                mPhysicsController.StopMovingVertically();
                return;
            }

            ;

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

        [Header("Acceleration")] [SerializeField]
        private float apexBonus = 2;

        public float HorizontalMove => playerInputController.MovementAxisRaw.x;

        private void CalculateWalk()
        {
            if (HorizontalMove != 0 && mPhysicsController.CanAccelerate())
            {
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
            moveGizmo = position + move;
            var furthestPoint = position + move;

            var hit = mPhysicsController.OverlapBoxDetector(furthestPoint);

            if (!hit)
            {
                transform.position += move;
                return;
            }

            var closestPoint = mPhysicsController.GetClosestPointToMoveTo(hit);

            contactPoint = closestPoint;
            var finalPoint = Vector2.Lerp(transform.position, closestPoint, (float) 0.6);

            gizmoClosestTransformPosition = finalPoint;
            transform.position = finalPoint;

            // transform.position = new Vector3(transform.position.x, finalPoint.y, transform.position.z);


            // otherwise increment away from current pos; see what closest position we can move to
            /*for (int collider = 1; collider < freeColliderIterations; collider++) {
                // increment to check all but furthestPoint - we did that already
                var t = (float)collider / freeColliderIterations;
                var posToTry = Vector2.Lerp(positionToMoveTo, new Vector2(positionToMoveTo.x, closestPoint.y), t);
                contactPoint = positionToMoveTo;
                Debug.Log($"// collider: {collider} freeColliderIterations {freeColliderIterations}, t {t}");
                Debug.Log(message: "closestPoint x: "+ closestPoint.x + "y: "+ closestPoint.y);
                Debug.Log(message: $" posToTry x: {posToTry.x} y: {posToTry.y}");
                Debug.Log(message: $" positionToMoveTo x: {positionToMoveTo.x} y: {positionToMoveTo.y}");
                Debug.Log($"// mPhysicsController: {mPhysicsController.OverlapBoxDetector(posToTry)}");

                if (mPhysicsController.OverlapBoxDetector(posToTry)) {
                    
                    transform.position = positionToMoveTo;
                    return;
                }

                positionToMoveTo = posToTry;
            }*/
        }

        public Vector2 contactPoint { get; set; }

        public Vector3 moveGizmo { get; set; }

        public Vector3 gizmoClosestTransformPosition { get; set; }
        
        #endregion

        private void OnDrawGizmos()
        {
            // Bounds
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(gizmoClosestTransformPosition, closestGizmoSize);
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(contactPoint, closestGizmoSize - 0.01f);
            
            Gizmos.color = Color.magenta;
            Handles.Label(transform.position,"Transform");
            Gizmos.DrawSphere(transform.position, closestGizmoSize - 0.04f);
        }
    }
}