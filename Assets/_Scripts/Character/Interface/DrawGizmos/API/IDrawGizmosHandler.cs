using _Scripts.Character.MonoBehavior.Player;
using UnityEngine;

namespace _Scripts.Character.Interface.DrawGizmos.API
{
    public interface IDrawGizmosHandler
    { 
        public void DrawGizmos(ScriptableStats scriptableStats, Bounds bounds, int direction, Vector3 position);

    }
}