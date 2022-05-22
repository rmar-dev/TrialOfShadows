using _Scripts.Character.MonoBehavior.Player;
using _Scripts.Character.StateMachineBehaviors.Player;
using UnityEngine;

namespace _Scripts.Character.StateMachineBehaviors.Player
{
    public class AirborneSMB : SceneLinkedSMB<PlayerCharacter>
    {
        public override void OnSLStateNoTransitionUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            MMonoBehaviour.UpdateFacing();
        }
    }

}
