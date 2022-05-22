namespace _Scripts.Character.Interface
{
    public interface IExtendedPlayerHandler : IPlayerHandler {
        public bool DoubleJumpingThisFrame { get; set; }
        public bool Dashing { get; set; }  
    }
}