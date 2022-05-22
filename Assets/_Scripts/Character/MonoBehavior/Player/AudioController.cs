using _Scripts.Audio;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace _Scripts.Character.MonoBehavior.Player
{
    public class SpriteController : MonoBehaviour
    {
        [Header("Audio")][SerializeField] private RandomAudioPlayer footstepAudioPlayer;
        [SerializeField] private TileBase currentSurface;

        public void PlayFootstep()
        {
            footstepAudioPlayer.PlayRandomSound(currentSurface);
            var footstepPosition = transform.position;
            footstepPosition.z -= 1;
            VFXController.Instance.Trigger("DustPuff", footstepPosition, 0, false, null, currentSurface);
        }
    }
}
