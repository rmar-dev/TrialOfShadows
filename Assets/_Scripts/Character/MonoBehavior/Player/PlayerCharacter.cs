using System.Runtime.Serialization;
using _Scripts.Character.Interface;
using _Scripts.Character.StateMachineBehaviors.Player;
using UnityEditor;
using UnityEngine;

namespace _Scripts.Character.MonoBehavior.Player
{
    public class PlayerCharacter : MonoBehaviour, IPlayerHandler, IPhysicsHandler, IPlayerAnimator
    {
        [SerializeField] private bool spriteOriginallyFacesLeft;
        [SerializeField] [Range(0.0f, 3.0f)] private float closestGizmoSize = 0.1f;
        [SerializeField] private Animator animator;
        [SerializeField] private AudioSource source ;
        [SerializeField] private LayerMask groundMask ;
        [SerializeField] private ParticleSystem jumpParticles, launchParticles, moveParticles, landParticles; 
        [SerializeField] private AudioClip[] footsteps;
        [SerializeField] private float maxTilt = .1f;
        [SerializeField] private float tiltSpeed = 1f ; 
        [SerializeField] private float maxParticleFallSpeed = -40f;
        
        private PlayerAnimator<PlayerCharacter> _playerAnimator;
        private MecanimStateController _mecanimStateController;
        private Vector3 _lastPosition;
        private PhysicsController _mPhysicsController;
        private PlayerInputController _playerInputController;
        public SpriteRenderer spriteRenderer;
        public Animator Animator => animator;
        public AudioSource AudioSource => source;
        public LayerMask GroundMask => groundMask;
        public ParticleSystem JumpParticles => jumpParticles;
        public ParticleSystem LaunchParticles => launchParticles;
        public ParticleSystem MoveParticles => moveParticles;
        public ParticleSystem LandParticles => landParticles;
        public AudioClip[] Footsteps => footsteps;
        public float MaxTilt => maxTilt;
        public float TiltSpeed => tiltSpeed;
        public float MaxParticleFallSpeed => maxParticleFallSpeed;

        #region IPLayerController

        public Vector3 Velocity { get; private set; }
        public bool JumpingThisFrame { get; private set; }
        public bool LandingThisFrame { get; private set; }
        public Vector3 RawMovement { get; private set; }
        public Transform Transform { get; private set; }

        public bool Grounded => _mPhysicsController.Grounded;
        public bool CollidingTop => _mPhysicsController.CollidingUp;
        public bool CollidingLeft => _mPhysicsController.CollidingLeft;
        public bool CollidingRight => _mPhysicsController.CollidingRight;

        public Bounds CharacterBounds => _mPhysicsController.CharacterBounds;
        public float CurrentVerticalSpeed => _mPhysicsController.CurrentVerticalSpeed;
        public float CurrentHorizontalSpeed => _mPhysicsController.CurrentHorizontalSpeed;

        #endregion

        // This is horrible, but for some reason colliders are not fully established when update starts...
        private bool active;
        private void Activate() => active = true;

        private void Awake()
        {
            Invoke(nameof(Activate), 0.5f);
            _mPhysicsController = GetComponent<PhysicsController>();
            _playerInputController = new PlayerInputController().Initialise();
            _mecanimStateController = new MecanimStateController(Animator);
            _playerAnimator = new PlayerAnimator<PlayerCharacter>(this);
        }

        private void Start()
        {
            _playerInputController.EnablePlayerNormalInput = true;
            SceneLinkedSMB<PlayerCharacter>.Initialise(Animator, this);
        }

        private void FixedUpdate()
        {
            CalculateGravity();
        }

        private void Update()
        {
            if (!active) return;
            UpdateVelocity(transform.position);
            _playerAnimator.Update();
            _mecanimStateController.UpdateWithNewState(this);

            CalculateWalk();
            CalculateJumpApex();
            CalculateJump();

            MoveCharacter();
            UpdateFacing();

            RunCollisionsCheck();
            
        }

        private void UpdateVelocity(Vector3 position)
        {
            Velocity = (position - _lastPosition) / Time.deltaTime;
            _lastPosition = position;
            Transform = transform;
        }

        #region SpriteFacing

        public void UpdateFacing()
        {
            bool faceLeft = _playerInputController.MovementAxisRaw.x < 0f;
            bool faceRight = _playerInputController.MovementAxisRaw.x > 0f;

            if (faceLeft)
            {
                spriteRenderer.flipX = !spriteOriginallyFacesLeft;
            }
            else if (faceRight)
            {
                spriteRenderer.flipX = spriteOriginallyFacesLeft;
            }
        }

        #endregion

        #region Jump

        [Header("JUMPING")] [SerializeField] private float jumpApexThreshold = 10f;
        [SerializeField] private float coyoteTimeThreshold = 0.1f;
        [SerializeField] private float jumpBuffer = 0.1f;

        private bool Jumping => _playerInputController.Jump;
        private float LastJumpPressed => _playerInputController.LastJumpPressedTime;

        private bool _coyoteUsable;
        private bool _endedJumpEarly = true;
        private float _apexPoint; // Becomes 1 at the apex of a jump

        private bool CanUseCoyote => _coyoteUsable && !Grounded && _timeLeftGrounded + coyoteTimeThreshold > Time.time;
        private bool HasBufferedJump => Grounded && LastJumpPressed + jumpBuffer > Time.time;

        private void CalculateJumpApex() => _apexPoint = CalculateJumpApexPoint();

        private float CalculateJumpApexPoint()
        {
            if (Grounded) return 0;

            return Mathf.InverseLerp(jumpApexThreshold, 0, Mathf.Abs(Velocity.y));
        }

        private void CalculateJump()
        {
            if (!CollidingTop)
            {
                ApplyJumpIfNeeded();
            }
            
            EndedJumpEarly();
        }

        private void ApplyJumpIfNeeded()
        {
            if (HasBufferedJump || Jumping && CanUseCoyote)
            {
                _mPhysicsController.AddVerticalSpeedHeight();
                _endedJumpEarly = false;
                _coyoteUsable = false;
                _timeLeftGrounded = float.MinValue;
                JumpingThisFrame = true;
            }
            else
            {
                JumpingThisFrame = false;
            }
        }

        private void EndedJumpEarly()
        {
            if (!Grounded && Jumping && !_endedJumpEarly && Velocity.y != 0)
                _endedJumpEarly = true;
        }

        #endregion

        #region Collisions

        [Header("Collision")] [SerializeField] private float jumpEndEarlyGravityModifier = 3f;
        private float _fallSpeed;

        private float _timeLeftGrounded;

        private void RunCollisionsCheck()
        {
            if (_mPhysicsController.ExitGroundThisFrame) _timeLeftGrounded = Time.time;

            var touchGroundThisFrame = _mPhysicsController.TouchGroundThisFrame;

            if (touchGroundThisFrame) _coyoteUsable = true; // Only trigger when first touching

            LandingThisFrame = touchGroundThisFrame;
        }

        private void CalculateGravity()
        {
            if (Grounded && CurrentVerticalSpeed != 0)
            {
                _mPhysicsController.StopMovingVertically();
                return;
            }

            ;

            _fallSpeed = _mPhysicsController.CalculateFallSpeed(_apexPoint);
            if (_fallSpeed == 0) return;

            // Add downward force while ascending if we ended the jump early
            var fallSpeedModifier = _endedJumpEarly && CurrentVerticalSpeed > 0
                ? _fallSpeed * jumpEndEarlyGravityModifier
                : _fallSpeed;

            _mPhysicsController.UpdateCurrentVerticalSpeed(fallSpeedModifier);
        }

        #endregion

        #region Walk

        [Header("Acceleration")] [SerializeField]
        private float apexBonus = 2;

        public float HorizontalMove => _playerInputController.MovementAxisRaw.x;

        private void CalculateWalk()
        {
            if (HorizontalMove != 0 && _mPhysicsController.CanAccelerate())
            {
                var tempApexBonus = Mathf.Sign(HorizontalMove) * apexBonus * _apexPoint;
                _mPhysicsController.CalculateAcceleration(HorizontalMove, _apexPoint);
                return;
            }

            _mPhysicsController.CalculateDeceleration();
        }

        #endregion

        #region Move

        // We cast our bounds before moving to avoid future collisions
        private void MoveCharacter()
        {
            if (!_mPhysicsController.MoveIfNeeded()) return;

            var position = _mPhysicsController.GetTransformPosition;
            RawMovement = _mPhysicsController.GetRawMovement();
            var move = RawMovement * Time.deltaTime;
            moveGizmo = position + move;
            var furthestPoint = position + move;

            var hit = _mPhysicsController.OverlapBoxDetector(furthestPoint);

            if (!hit)
            {
                transform.position += move;
                return;
            }

            var closestPoint = _mPhysicsController.GetClosestPointToMoveTo(hit);

            contactPoint = closestPoint;
            var finalPoint = Vector2.Lerp(transform.position, closestPoint, (float) 0.6);

            gizmoClosestTransformPosition = finalPoint;
            transform.position = finalPoint;
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

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            
        }
    }
}