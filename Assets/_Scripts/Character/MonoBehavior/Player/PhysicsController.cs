using System;
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
        private RayRange rayRangeUp, rayRangeRight, rayRangeDown, rayRangeLeft;
        private bool collidingUp, collidingRight, collidingDown, collidingLeft;
        private Vector3 halfSize;
        public bool CollidingUp => collidingUp;
        public bool Grounded => collidingDown;
        public bool TouchGroundThisFrame => collidingDown && !previusGroundCheck;
        public bool ExitGroundThisFrame => !collidingDown && previusGroundCheck;

        public bool CollidingLeft => collidingLeft;
        public bool CollidingRight => collidingRight;

        public Vector3 GetTransformPosition => transform.position + characterBounds.center;

        private bool previusGroundCheck;
        private void Awake()
        {
            Physics2D.colliderAwakeColor = Color.yellow;
        }

        private void FixedUpdate()
        {
            CalculateRayRanged();
            if (previusGroundCheck != collidingDown)
            {
                Debug.Log("this is grounded state " + collidingDown);
            }
            previusGroundCheck = collidingDown;
            
            RunCollisionChecks();
        }
        
        private void RunCollisionChecks()
        {

            collidingDown = RunDetection(rayRangeDown);
            collidingUp = RunDetection(rayRangeUp);
            collidingLeft = RunDetection(rayRangeLeft);
            collidingRight = RunDetection(rayRangeRight);

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

            rayRangeDown = new RayRange(b.min.x + rayBuffer, b.min.y, b.max.x - rayBuffer, b.min.y, Vector2.down);
            rayRangeUp = new RayRange(b.min.x + rayBuffer, b.max.y, b.max.x - rayBuffer, b.max.y, Vector2.up);
            rayRangeLeft = new RayRange(b.min.x, b.min.y + rayBuffer, b.min.x, b.max.y - rayBuffer, Vector2.left);
            rayRangeRight = new RayRange(b.max.x, b.min.y + rayBuffer, b.max.x, b.max.y - rayBuffer, Vector2.right);
        }

        public Collider2D OverlapBoxDetector(Vector2 furthestPoint) => Physics2D.OverlapBox(furthestPoint, characterBounds.size, 0, groundLayer);


        public IEnumerable<Vector2> EvaluateRayPositions(RayRange range)
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

        private float currentHorizontalSpeed, currentVerticalSpeed;
        public float CurrentHorizontalSpeed => currentHorizontalSpeed;
        public float CurrentVerticalSpeed => currentVerticalSpeed;
        
        public void StopMovingVerticaly()
        {
            currentVerticalSpeed = 0;
        }

        public void AddVerticalSpeedHeight()
        {
            currentVerticalSpeed = jumpHeight;
        }
        public bool IsCollidingTop()
        {
            if (!collidingUp) return false;
                
            currentVerticalSpeed = 0;
            return true;
        }
        private float CalculateFallSpeedHelper(float apexPoint) =>
            !collidingDown ? Mathf.Lerp(minFallSpeed, maxFallSpeed, apexPoint) : 0;

        public float CalculateFallSpeed(float apexPoint)
        {
            if (!collidingDown) return CalculateFallSpeedHelper(apexPoint);

            currentVerticalSpeed = 0;
            return 0;
        }

        public Vector3 GetClosestPosition(Collider2D collider)
        {
            var transformPosition = transform.position;
            var positionToCheck = new Vector3(transformPosition.x + characterBounds.size.x , transformPosition.y + characterBounds.size.y , transformPosition.z);
            return collider.ClosestPoint(positionToCheck);
        }
        
        public void UpdateCurrentVerticalSpeed(float fallSpeedModifier)
        {
            currentVerticalSpeed -= fallSpeedModifier * Time.deltaTime;

            if (currentVerticalSpeed < fallClamp) currentVerticalSpeed = fallClamp;
        }

        public void CalculateAcceleration(float horizontalMove, float tempApexBonus)
        {
            currentHorizontalSpeed += horizontalMove * acceleration * Time.deltaTime;

            currentHorizontalSpeed = Mathf.Clamp(currentHorizontalSpeed, -moveClamp, moveClamp);

            currentHorizontalSpeed += tempApexBonus * Time.deltaTime;
        }

        public void CalculateDeceleration()
        {
            currentHorizontalSpeed = Mathf.MoveTowards(currentHorizontalSpeed, 0, deAcceleration * Time.deltaTime);
        }

        public bool CanAccelerate()
        {
            if ((!(currentHorizontalSpeed > 0) || !CollidingRight) &&
                (!(currentHorizontalSpeed < 0) || !CollidingLeft)) return true;

            currentHorizontalSpeed = 0;
            return false;
        }

        public bool MoveIfNeeded() => !Grounded || currentVerticalSpeed != 0 || currentHorizontalSpeed != 0;
        public Vector2 GetRawMovement() => new Vector2(currentHorizontalSpeed, currentVerticalSpeed);
        
        public Vector2 GetClosestPointToMoveTo(Collider2D hit)
        {
            halfSize = characterBounds.size / 2;
            var transformPosition = transform.position;
            Vector2 feetPosition = new Vector2(transformPosition.x, transformPosition.y - halfSize.y);
            return hit.ClosestPoint(feetPosition);
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
            foreach (var range in new List<RayRange> { rayRangeUp, rayRangeRight, rayRangeDown, rayRangeLeft }) {
                foreach (var point in EvaluateRayPositions(range)) {
                    Gizmos.DrawRay(point, range.Dir * detectionRayLength);
                }
            }
                
            // Draw the future position. Handy for visualizing gravity
            Gizmos.color = Color.red;
            var move = new Vector2(currentHorizontalSpeed, currentVerticalSpeed) * Time.deltaTime;
            Gizmos.DrawWireCube((transformPosition + new Vector3(move.x, move.y, 0)), characterBounds.size);
        }
        
        #endregion
    }
}