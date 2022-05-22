using _Scripts.Character.Model;
using UnityEngine;

namespace _Scripts.Character.Interface
{
    public interface IPlayerController {
        public Vector3 Velocity { get; }
        public bool JumpingThisFrame { get; }
        public float HorizontalMove { get; }
        public bool LandingThisFrame { get; }
        public Vector3 RawMovement { get; }
        public bool Grounded { get; }
        public Transform Transform { get; }
        public  Animator Animator { get; }
        public AudioSource AudioSource { get; }
        public LayerMask GroundMask { get; }
        public ParticleSystem JumpParticles { get; }
        public ParticleSystem LaunchParticles { get; }
        public ParticleSystem MoveParticles { get; }
        public ParticleSystem LandParticles { get; }
        public float MaxTilt { get; } 
        public float TiltSpeed { get; }
        public  float MaxParticleFallSpeed { get; }
    }
}