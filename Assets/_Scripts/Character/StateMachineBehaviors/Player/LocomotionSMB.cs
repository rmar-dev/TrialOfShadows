using _Scripts.Character.MonoBehavior.Player;
using UnityEngine;

namespace _Scripts.Character.StateMachineBehaviors.Player
{
    public class LocomotionSMB : SceneLinkedSMB<PlayerCharacter>
    {
        public override void OnSLStateEnter (Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            
        }

        public override void OnSLStateNoTransitionUpdate (Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            MMonoBehaviour.UpdateFacing();
        }
    }
}