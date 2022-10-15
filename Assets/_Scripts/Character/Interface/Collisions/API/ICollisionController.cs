using _Scripts.Character.MonoBehavior.Player;
using UnityEngine;

namespace _Scripts.Character.Interface.Collisions.API
{
    public interface ICollisionController
    {
        public Transform Transform { get; }
        public CapsuleCollider2D CapsuleCollider2D { get; }
        public  ScriptableStats ScriptableStats { get; } 
        public bool CachedTriggerSetting { get; }
        public Bounds StandingColliderBounds { get; }
    }
}