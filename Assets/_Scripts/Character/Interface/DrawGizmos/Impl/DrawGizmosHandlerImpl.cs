using _Scripts.Character.Interface.DrawGizmos.API;
using _Scripts.Character.MonoBehavior.Player;
using UnityEngine;

namespace _Scripts.Character.Interface.DrawGizmos.Impl
{
    public class DrawGizmosHandlerImpl : IDrawGizmosHandler
    {
        public void DrawGizmos(ScriptableStats scriptableStats, Bounds bounds, int wallDirection, Vector3 position) 
        {
            if (scriptableStats.ShowWallDetection) {
                Gizmos.color = Color.white;
                Gizmos.DrawWireCube(bounds.center, bounds.size);
            }

            if (!scriptableStats.ShowLedgeDetection) return;
            
            Gizmos.color = Color.green;
            var facingDir = Mathf.Sign(wallDirection);
            var grabHeight = position + scriptableStats.LedgeGrabPoint.y * Vector3.up;
            var grabPoint = grabHeight + facingDir * scriptableStats.LedgeGrabPoint.x * Vector3.right;
            Gizmos.DrawWireSphere(grabPoint, 0.05f);
            Gizmos.DrawWireSphere(grabPoint + Vector3.Scale(scriptableStats.StandUpOffset, new(facingDir, 1)), 0.05f);
            Gizmos.DrawRay(grabHeight - scriptableStats.LedgeRaycastSpacing * Vector3.up, 0.5f * facingDir * Vector3.right);
            Gizmos.DrawRay(grabHeight + scriptableStats.LedgeRaycastSpacing * Vector3.up, 0.5f * facingDir * Vector3.right);
        }
    }
}