using System.Collections.Generic;
using System.Linq;
using _Scripts.Character.Model;
using UnityEngine;

namespace _Scripts.Character.MonoBehavior.Player
{
    public class PhysicsController : MonoBehaviour
    {
        [Header("COLLISION")] [SerializeField] private Bounds characterBounds;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private int detectorCount = 3;
        [SerializeField] private float detectionRayLength = 0.1f;

        [SerializeField] [Range(0.1f, 0.3f)]
        private float rayBuffer = 0.1f; // Prevents side detectors hitting the ground
        [SerializeField] [Range(0.0f, 3.0f)] private float currentGizmoSize = 0.05f;

        #region Collisions
        private RayRange _rayRangeUp, _rayRangeRight, _rayRangeDown, _rayRangeLeft;
        private bool _collidingUp, _collidingRight, _collidingDown, _collidingLeft;
        private Vector3 _halfSize;
        public bool CollidingUp => _collidingUp;
        public bool Grounded => _collidingDown;
        public bool TouchGroundThisFrame => _collidingDown && !_previousGroundCheck;
        public bool ExitGroundThisFrame => !_collidingDown && _previousGroundCheck;

        public bool CollidingLeft => _collidingLeft;
        public bool CollidingRight => _collidingRight;

        public Vector3 GetTransformPosition => transform.position + characterBounds.center;

        private bool _previousGroundCheck;
        private void Awake()
        {
            Physics2D.colliderAwakeColor = Color.yellow;
        }

        private void FixedUpdate()
        {
            CalculateRayRanged();
            if (_previousGroundCheck != _collidingDown)
            {
                Debug.Log("this is grounded state " + _collidingDown);
            }
            _previousGroundCheck = _collidingDown;
            
            RunCollisionChecks();
        }
        
        private void RunCollisionChecks()
        {

            _collidingDown = RunDetection(_rayRangeDown);
            _collidingUp = RunDetection(_rayRangeUp);
            _collidingLeft = RunDetection(_rayRangeLeft);
            _collidingRight = RunDetection(_rayRangeRight);

            bool RunDetection(RayRange range)
            {
                return EvaluateRayPositions(range).Any(point =>
                    Physics2D.Raycast(point, range.Dir, detectionRayLength, groundLayer));
            }
        }

        private void CalculateRayRanged()
        {
            // This is crying out for some kind of refactor. 
            var b = new Bounds(transform.position + characterBounds.center, characterBounds.size);

            _rayRangeDown = new RayRange(b.min.x + rayBuffer, b.min.y, b.max.x - rayBuffer, b.min.y, Vector2.down);
            _rayRangeUp = new RayRange(b.min.x + rayBuffer, b.max.y, b.max.x - rayBuffer, b.max.y, Vector2.up);
            _rayRangeLeft = new RayRange(b.min.x, b.min.y + rayBuffer, b.min.x, b.max.y - rayBuffer, Vector2.left);
            _rayRangeRight = new RayRange(b.max.x, b.min.y + rayBuffer, b.max.x, b.max.y - rayBuffer, Vector2.right);
        }

        public Collider2D OverlapBoxDetector(Vector2 furthestPoint) => Physics2D.OverlapBox(furthestPoint, characterBounds.size, 0, groundLayer);


        private IEnumerable<Vector2> EvaluateRayPositions(RayRange range)
        {
            for (var detector = 0; detector < detectorCount; detector++)
            {
                var t = (float) detector / (detectorCount - 1);
                yield return Vector2.Lerp(range.Start, range.End, t);
            }
        }

        #endregion

        #region Forces

        [Header("GRAVITY")] [SerializeField] private float fallClamp = -40f;
        [SerializeField] private float minFallSpeed = 80f;
        [SerializeField] private float maxFallSpeed = 120f;
        [SerializeField] private float jumpHeight = 30;
        
        [Header("Acceleration")] [SerializeField] private float acceleration = 90;
        [SerializeField] private float moveClamp = 13;
        [SerializeField] private float deAcceleration = 60f;

        private float _currentHorizontalSpeed, _currentVerticalSpeed;
        public float CurrentHorizontalSpeed => _currentHorizontalSpeed;
        public float CurrentVerticalSpeed => _currentVerticalSpeed;
        
        public void StopMovingVertically()
        {
            _currentVerticalSpeed = 0;
        }

        public void AddVerticalSpeedHeight()
        {
            _currentVerticalSpeed = jumpHeight;
        }
        public bool IsCollidingTop()
        {
            if (!_collidingUp) return false;
                
            _currentVerticalSpeed = 0;
            return true;
        }
        private float CalculateFallSpeedHelper(float apexPoint) =>
            !_collidingDown ? Mathf.Lerp(minFallSpeed, maxFallSpeed, apexPoint) : 0;

        public float CalculateFallSpeed(float apexPoint)
        {
            if (!_collidingDown) return CalculateFallSpeedHelper(apexPoint);

            _currentVerticalSpeed = 0;
            return 0;
        }

        public void UpdateCurrentVerticalSpeed(float fallSpeedModifier)
        {
            _currentVerticalSpeed -= fallSpeedModifier * Time.deltaTime;

            if (_currentVerticalSpeed < fallClamp) _currentVerticalSpeed = fallClamp;
        }

        public void CalculateAcceleration(float horizontalMove, float tempApexBonus)
        {
            _currentHorizontalSpeed += horizontalMove * acceleration * Time.deltaTime;

            _currentHorizontalSpeed = Mathf.Clamp(_currentHorizontalSpeed, -moveClamp, moveClamp);

            _currentHorizontalSpeed += tempApexBonus * Time.deltaTime;
        }

        public void CalculateDeceleration()
        {
            _currentHorizontalSpeed = Mathf.MoveTowards(_currentHorizontalSpeed, 0, deAcceleration * Time.deltaTime);
        }

        public bool CanAccelerate()
        {
            if ((!(_currentHorizontalSpeed > 0) || !CollidingRight) &&
                (!(_currentHorizontalSpeed < 0) || !CollidingLeft)) return true;

            _currentHorizontalSpeed = 0;
            return false;
        }

        public bool MoveIfNeeded() => !Grounded || _currentVerticalSpeed != 0 || _currentHorizontalSpeed != 0;
        public Vector2 GetRawMovement() => new Vector2(_currentHorizontalSpeed, _currentVerticalSpeed);
        
        public Vector2 GetClosestPointToMoveTo(Collider2D hit)
        {
            Vector2 closestPoint = hit.ClosestPoint(transform.position);
            Vector2 halfSize = characterBounds.size / 2;
            Vector2 direction = CalculateDirection(closestPoint);
            Vector2 finalDirection = direction * halfSize;
            return closestPoint - finalDirection;
        }

        private Vector2 CalculateDirection(Vector2 closestPoint)
        {
            Vector3 transformPosition = transform.position;
            Debug.Log($"finalDirection {closestPoint.y - transformPosition.y}, vector.down { Vector2.down }");

            if (closestPoint.y - transformPosition.y < 0)
            {
                return Vector2.down;
            }
            
            return Vector2.up;
        }

        private void OnDrawGizmos()
        {
            var transformPosition = transform.position;

            // Bounds
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transformPosition + characterBounds.center, characterBounds.size);
            
            Vector3 gizmoHalfSize = characterBounds.size / 2;
            Gizmos.color = Color.white;
            Vector3 feetPosition = new Vector3(transformPosition.x, transformPosition.y - gizmoHalfSize.y, transformPosition.z);
            Gizmos.DrawSphere(feetPosition, currentGizmoSize + 0.02f);
            
            // Rays
            CalculateRayRanged();
            Gizmos.color = Color.blue;
            foreach (var range in new List<RayRange> { _rayRangeUp, _rayRangeRight, _rayRangeDown, _rayRangeLeft }) {
                foreach (var point in EvaluateRayPositions(range)) {
                    Gizmos.DrawRay(point, range.Dir * detectionRayLength);
                }
            }
                
            // Draw the future position. Handy for visualizing gravity
            Gizmos.color = Color.red;
            var move = new Vector2(_currentHorizontalSpeed, _currentVerticalSpeed) * Time.deltaTime;
            Gizmos.DrawWireCube((transformPosition + new Vector3(move.x, move.y, 0)), characterBounds.size);
        }
        
        #endregion
    }
}