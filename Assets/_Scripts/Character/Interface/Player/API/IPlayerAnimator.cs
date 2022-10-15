using System.Runtime.Serialization;
using UnityEngine;

namespace _Scripts.Character.Interface.Player.API
{
    public interface IPlayerAnimator: ISerializable
    {
        public Animator Animator { get; }
        public AudioSource AudioSource { get; }
        public LayerMask GroundMask { get; }
        public ParticleSystem JumpParticles { get; }
        public ParticleSystem LaunchParticles { get; }
        public ParticleSystem MoveParticles { get; }
        public ParticleSystem LandParticles { get; }
        public AudioClip[] Footsteps { get; }
        public float MaxTilt { get; }
        public float TiltSpeed { get; }
        public float MaxParticleFallSpeed { get; }
    }
}