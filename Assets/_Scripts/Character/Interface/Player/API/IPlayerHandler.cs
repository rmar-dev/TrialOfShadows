using _Scripts.Character.Model;
using UnityEngine;

namespace _Scripts.Character.Interface
{
    public interface IPlayerHandler {
        public Vector3 Velocity { get; }
        public bool JumpingThisFrame { get; }
        public float HorizontalMove { get; }
        public bool LandingThisFrame { get; }
        public Vector3 RawMovement { get; }
        public bool Grounded { get; }
        public Transform Transform { get; }
    }
}