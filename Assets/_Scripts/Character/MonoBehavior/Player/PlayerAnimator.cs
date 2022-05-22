using System;
using _Scripts.Character.Interface;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _Scripts.Character.MonoBehavior.Player
{
    /// <summary>
    /// This is a pretty filthy script. I was just arbitrarily adding to it as I went.
    /// You won't find any programming prowess here.
    /// This is a supplementary script to help with effects and animation. Basically a juice factory.
    /// </summary>
    public class PlayerAnimator<TMonoBehavior> where TMonoBehavior :
        IPlayerAnimator, IPlayerHandler 
    {
        private TMonoBehavior monoBehavior;
        
        private bool _playerGrounded;
        private ParticleSystem.MinMaxGradient _currentGradient;
        private Vector2 _movement;
        
        public PlayerAnimator (TMonoBehavior handler)
        {
            this.monoBehavior = handler;
        }
        public void Update()
        {
            if (monoBehavior == null) return;
                
            var targetRotVector = new Vector3(0, 0, Mathf.Lerp(-monoBehavior.MaxTilt, monoBehavior.MaxTilt, Mathf.InverseLerp(-1, 1, monoBehavior.HorizontalMove)));
            monoBehavior.Animator.transform.rotation = Quaternion.RotateTowards(monoBehavior.Animator.transform.rotation, Quaternion.Euler(targetRotVector), monoBehavior.TiltSpeed * Time.deltaTime);
            // _mecanimInterface.UpdateWithNewState(_player);
            // Lean while running
            
            // Speed up idle while running
            
            //_anim.SetFloat(IdleSpeedKey, Mathf.Lerp(1, _maxIdleSpeed, Mathf.Abs(_player.HorizontalMove)));
            
            // Splat
            if (monoBehavior.LandingThisFrame) {
                monoBehavior.AudioSource.PlayOneShot(monoBehavior.Footsteps[Random.Range(0, monoBehavior.Footsteps.Length)]);
            }
            
            // Jump effects
            /*if (_player.JumpingThisFrame) {
                // Only play particles when grounded (avoid coyote)
                if (_player.Grounded) {
                    SetColor(jumpParticles);
                    SetColor(launchParticles);
                    jumpParticles.Play();
                }
            }*/
            
            // Play landing effects and begin ground movement effects
            if (!_playerGrounded && monoBehavior.Grounded) {
                _playerGrounded = true;
                monoBehavior.MoveParticles.Play();
                monoBehavior.LandParticles.transform.localScale = Vector3.one * Mathf.InverseLerp(0, monoBehavior.MaxParticleFallSpeed, _movement.y);
                SetColor(monoBehavior.LandParticles);
                monoBehavior.LandParticles.Play();
            }
            else if (_playerGrounded && !monoBehavior.Grounded) {
                _playerGrounded = false;
                monoBehavior.MoveParticles.Stop();
            }
            
            // Detect ground color
            var groundHit = Physics2D.Raycast(monoBehavior.Transform.position, Vector3.down, 2, monoBehavior.GroundMask);
            if (groundHit && groundHit.transform.TryGetComponent(out SpriteRenderer r)) {
                var color = r.color;
                _currentGradient = new ParticleSystem.MinMaxGradient(color * 0.9f, color * 1.2f);
                SetColor(monoBehavior.MoveParticles);
            }
            
            _movement = monoBehavior.RawMovement; // Previous frame movement is more valuable
        }
        
        public void PlayerJumping()
        {
            SetColor(monoBehavior.JumpParticles);
            SetColor(monoBehavior.LaunchParticles);
            monoBehavior.JumpParticles.Play();
        }
        public void UpdateFacing()
        {
            Debug.Log("Update facing");
        }
        private void OnDisable() {
            monoBehavior.MoveParticles.Stop();
        }

        private void OnEnable() {
            monoBehavior.MoveParticles.Play();
        }

        void SetColor(ParticleSystem ps) {
            var main = ps.main;
            main.startColor = _currentGradient;
        }
    }
}