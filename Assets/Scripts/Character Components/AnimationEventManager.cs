using UnityEngine;
using UnityEngine.Events;

namespace Kabir.CharacterComponents
{
    [RequireComponent(typeof(Animator))]
    public class AnimationEventManager : MonoBehaviour
    {
        #region Footstep

        public event UnityAction<int> OnFootstep;
        protected void AnimFootstep(int index) => OnFootstep?.Invoke(index);

        #endregion

        #region Spawn Objects

        public event UnityAction<GameObject> OnObjectSpawn, OnObjectSpawnParent;

        protected void AnimObjectSpawn(UnityEngine.Object obj)
        {
            if (!Application.isPlaying) return;
            if (obj == null) return;
            GameObject g = obj as GameObject;
            if (g == null) return;

            GameObject result = Instantiate(g, transform.position, transform.rotation);
            OnObjectSpawn?.Invoke(result);
        }

        protected void AnimObjectSpawnParent(UnityEngine.Object obj)
        {
            if (!Application.isPlaying) return;
            if (obj == null) return;
            GameObject g = obj as GameObject;
            if (g == null) return;

            GameObject result = Instantiate(g, transform);
            result.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            OnObjectSpawnParent?.Invoke(result);
        }

        #endregion
    }
}
