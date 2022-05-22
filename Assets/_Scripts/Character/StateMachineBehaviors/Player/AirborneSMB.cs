using _Scripts.Character.MonoBehavior;
using _Scripts.Character.MonoBehavior.Player;
using UnityEngine;

namespace _Scripts.Character.StateMachineBehaviors
{
    public class AirborneSMB : SceneLinkedSMB<PlayerCharacter>
    {
        public override void OnSLStateNoTransitionUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            MMonoBehaviour.UpdateFacing();
        }
    }

}
