using System;
using _Scripts.Character.Interface.Collisions.API;
using _Scripts.Character.MonoBehavior.Player;
using UnityEngine;

namespace _Scripts.Character.Interface.Collisions.Impl
{
    public sealed class CollisionsHandlerImpl: ICollisionsHandler
    {
        private readonly ICollisionController _controller;
        
        private readonly RaycastHit2D[] _groundHits = new RaycastHit2D[2];
        private readonly RaycastHit2D[] _ceilingHits = new RaycastHit2D[2];
        private readonly Collider2D[] _wallHits = new Collider2D[5];
        private readonly Collider2D[] _ladderHits = new Collider2D[1];
        
        private int _groundHitCount;
        private int _ceilingHitCount;
        private int _wallHitCount;
        private int _ladderHitCount;
        private int _frameLeftGrounded = int.MinValue;
        private bool _grounded;

        public RaycastHit2D[] GroundHits => _groundHits;
        public RaycastHit2D[] CeilingHits => _ceilingHits;
        public Collider2D[] WallHits => _wallHits;
        public Collider2D[] LadderHits => _ladderHits;
        public int GroundHitCount => _groundHitCount;
        public int CeilingHitCount => _ceilingHitCount;
        public int WallHitCount => _wallHitCount;
        public int LadderHitCount => _ladderHitCount;
        public int FrameLeftGrounded => _frameLeftGrounded;
        public bool Grounded => _grounded;

        public CollisionsHandlerImpl(ICollisionController controller)
        {
            _controller = controller;
        }

        public void CheckCollisions() {
            Physics2D.queriesHitTriggers = false;
            
            // Ground and Ceiling
            var origin = (Vector2)_controller.Transform.position + _controller.CapsuleCollider2D.offset;
            _groundHitCount = Physics2D.CapsuleCastNonAlloc(origin, _controller.CapsuleCollider2D.size, _controller.CapsuleCollider2D.direction, 0, Vector2.down, _groundHits, _controller.ScriptableStats.GrounderDistance, ~_controller.ScriptableStats.PlayerLayer);
            _ceilingHitCount = Physics2D.CapsuleCastNonAlloc(origin, _controller.CapsuleCollider2D.size, _controller.CapsuleCollider2D.direction, 0, Vector2.up, _ceilingHits, _controller.ScriptableStats.GrounderDistance, ~_controller.ScriptableStats.PlayerLayer);

            // Walls and Ladders
            var bounds = GetWallDetectionBounds();
            _wallHitCount = Physics2D.OverlapBoxNonAlloc(bounds.center, bounds.size, 0, _wallHits, _controller.ScriptableStats.ClimbableLayer);

            Physics2D.queriesHitTriggers = true; // Ladders are set to Trigger
            _ladderHitCount = Physics2D.OverlapBoxNonAlloc(bounds.center, bounds.size, 0, _ladderHits, _controller.ScriptableStats.LadderLayer);
            Physics2D.queriesHitTriggers = _controller.CachedTriggerSetting;
        }

        public Bounds GetWallDetectionBounds() {
            var colliderOrigin = _controller.Transform.position + _controller.StandingColliderBounds.center;
            return new Bounds(colliderOrigin, _controller.ScriptableStats.WallDetectorSize);
        }

        public Bounds GetWallDetectionBounds(ScriptableStats scriptableStats,
            Bounds standingColliderBounds, Transform transform)
        {
            var colliderOrigin = transform.position + standingColliderBounds.center;
            return new Bounds(colliderOrigin, scriptableStats.WallDetectorSize);
        }

        public float HandleCeilingHitCount(Vector2 speed) {
            // Hit a Ceiling
            if (speed.y > 0 && _ceilingHitCount > 0) return 0;

            return speed.y;
        }

        public void DidLeftTheGround(int fixedFrame, Action completion)
        {
            if (!_grounded || _groundHitCount != 0) return;
            _grounded = false;
            _frameLeftGrounded = fixedFrame;
            completion();
        }

        public void DidLandedOnGround(Action completion)
        {
            if (_grounded || _groundHitCount <= 0) return;
            _grounded = true;
            completion();
        }
    }
}