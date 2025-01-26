using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;

namespace Kabir.ScriptableObjects
{
    [CreateAssetMenu(fileName = "AnimationBlends", menuName = "Animation/Animation Blends")]
    public class AnimationBlends : ScriptableObject
    {
        [SerializeField]
        private List<BlendData> _blendList;

        /// <summary>
        /// Gets the list of animation blends according to their blend position
        /// </summary>
        /// <returns></returns>
        public Dictionary<Vector2, AnimationClip> GetBlendList(out Dictionary<Vector2, float> blendClipSpeed)
        {
            Dictionary<Vector2, AnimationClip> result = new();
            blendClipSpeed = new();

            if (_blendList == null) return result;

            for(int i = 0; i < _blendList.Count; i++)
            {
                var blend = _blendList[i];
                if(blend == null) continue;
                if(blend.Clip == null) continue;

                if (result.ContainsKey(blend.BlendPosition)) continue;
                result.Add(blend.BlendPosition, blend.Clip);
                blendClipSpeed.Add(blend.BlendPosition, blend.ClipSpeed);
            }

            return result;
        }

        [System.Serializable]
        private class BlendData
        {
            [field: SerializeField, AllowNesting, ShowAssetPreview]  public AnimationClip Clip { get; private set; }
            [field: SerializeField]  public Vector2 BlendPosition { get; private set; }
            [field: SerializeField] public float ClipSpeed { get; private set; } = 1f;

            public void OnValidate()
            {
                ClipSpeed = Mathf.Max(0.001f, ClipSpeed);
            }
        }
    }
}
