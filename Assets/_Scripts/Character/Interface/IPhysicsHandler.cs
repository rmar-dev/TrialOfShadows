using UnityEngine;

namespace _Scripts.Character.Interface
{
    public interface IPhysicsHandler
    {
        public Bounds CharacterBounds { get; }
        public float CurrentHorizontalSpeed { get; }
        public float CurrentVerticalSpeed { get; }
        public bool Grounded { get; }
        public bool CollidingTop { get; }
        public bool CollidingLeft { get; }
        public bool CollidingRight { get; }

    }
}