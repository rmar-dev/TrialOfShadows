using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.Player
{
    public abstract class StateMachine : MonoBehaviour
    {
        protected State State;

        public void SetState(State state) => State = state;
    }
}
