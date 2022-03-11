using UnityEngine;

namespace _Scripts.Character.Interface
{
    public interface IPhysicsController
    {
        public Bounds CharacterBounds { get; }
        public LayerMask GroundLayer { get; }
        public int DetectorCount { get; }
        public float DetectionRayLength { get; }
        public float RayBuffer { get; }
        public float CurrentHorizontalSpeed { get; }
        public float CurrentVerticalSpeed { get; }
    }
}