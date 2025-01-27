using UnityEngine;

namespace Kabir
{
    /// <summary>
    /// Interface used to recieve updates a script's OnAnimatorMove
    /// </summary>
    public interface IAnimatorUpdate
    {
        public void AnimatorMove(Vector3 deltaPosition, Quaternion deltaRotation);
        public bool IsNull();
    }
}
