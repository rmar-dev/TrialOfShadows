namespace _Scripts.Character.Interface
{
    public interface IExtendedPlayerController : IPlayerController {
        public bool DoubleJumpingThisFrame { get; set; }
        public bool Dashing { get; set; }  
    }
}