using UnityEngine;

namespace Kabir
{
    /// <summary>
    /// Interface used to recieve updates a script's OnAnimatorMove
    /// </summary>
    public interface IAnimatorUpdate
    {
        public void AnimatorMove(Animator animator);
    }
}
