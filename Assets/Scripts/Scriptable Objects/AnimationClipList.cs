using UnityEngine;
using System.Collections.Generic;

namespace Kabir.ScriptableObjects
{
    [CreateAssetMenu(fileName = "AnimationClipList", menuName = "Animation/Animation Clip List")]
    public class AnimationClipList : ScriptableObject
    {
        [SerializeField]
        private AnimationData[] _animationList;

        /// <summary>
        /// Gets the animation list with the associated clip speed
        /// </summary>
        /// <returns></returns>
        public Dictionary<AnimationClip, float> GetAnimationList()
        {
            Dictionary<AnimationClip, float> result = new();
            if(_animationList == null) return result;

            foreach(var data in _animationList)
            {
                if(data == null) continue;
                if(data.Clip == null) continue;

                if (result.ContainsKey(data.Clip)) continue;

                result.Add(data.Clip, Mathf.Max(0.01f, data.Speed));
            }

            return result;
        }

        private void OnValidate()
        {
            if(_animationList != null)
            {
                foreach(var a in _animationList) a?.OnValidate();
            }
        }

        [System.Serializable]
        private class AnimationData
        {
            [field: SerializeField] public AnimationClip Clip;
            [field: SerializeField] public float Speed = 1f;

            public void OnValidate()
            {
                Speed = Mathf.Max(0.01f, Speed);
            }
        }
    }
}
