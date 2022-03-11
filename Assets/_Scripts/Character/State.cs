using System.Collections;

namespace _Scripts.Character
{
    public abstract class State
    {
        public virtual IEnumerator Idle()
        {
            yield break;
        }

        public virtual IEnumerator Move()
        {
            yield break;
        }

        public virtual IEnumerator Jump()
        {
            yield break;
        }
    }
}