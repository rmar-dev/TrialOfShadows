using System;
using _Scripts.Character.Model;
using _Scripts.Character.MonoBehavior.Player;
using UnityEngine;

namespace _Scripts.Character.Interface.Player.API
{
    public interface IPlayerController {
        /// <summary>
        /// true = Landed. false = Left the Ground. float is Impact Speed
        /// </summary>
        public event Action<bool, float> GroundedChanged;
        public event Action<bool, Vector2> DashingChanged; // Dashing - Dir
        public event Action<bool> WallGrabChanged;
        public event Action<bool> LedgeClimbChanged;
        public event Action<bool> Jumped; // Is wall jump
        public event Action DoubleJumped;
        public event Action Attacked;

        public ScriptableStats PlayerStats { get; }
        public Vector2 Input { get; }
        public Vector2 Speed { get; }
        public Vector2 GroundNormal { get; }
        public int WallDirection { get; }
        public bool Crouching { get; }
        public bool ClimbingLadder { get; }
        public bool GrabbingLedge { get; }
        public bool ClimbingLedge { get; }
        public void ApplyVelocity(Vector2 vel, PlayerForce forceType);
    }
}