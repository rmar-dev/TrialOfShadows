using System;
using _Scripts.Character.MonoBehavior.Player;
using UnityEngine;

namespace _Scripts.Character.Interface.Collisions.API
{
    public interface ICollisionsHandler
    {
        public RaycastHit2D[] GroundHits { get; }
        public RaycastHit2D[] CeilingHits { get; }
        public Collider2D[] WallHits { get; }
        public Collider2D[] LadderHits { get; }
        
        public int GroundHitCount { get; }
        public int CeilingHitCount { get; }
        public int WallHitCount { get; }
        public int LadderHitCount { get; }
        public int FrameLeftGrounded { get; }
        public bool Grounded { get; }
        
        public void CheckCollisions();
        public Bounds GetWallDetectionBounds(ScriptableStats scriptableStats,
            Bounds standingColliderBounds, Transform transform);
        public void DidLeftTheGround(int fixedFrame, Action completion);
        public void DidLandedOnGround(Action completion);
        public float HandleCeilingHitCount(Vector2 speed);
    }
}