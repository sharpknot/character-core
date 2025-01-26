using UnityEngine;
using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.Animations;
using UnityEngine.Events;
using UnityEngine.Playables;
using Kabir.ScriptableObjects;
using UnityEngine.UIElements;

namespace Kabir.CharacterComponents
{
    [RequireComponent(typeof(Animator))]
    public class PlayableManager : MonoBehaviour
    {
        /// <summary>
        /// Animator attached to the manager
        /// </summary>
        public Animator Animator
        {
            get
            {
                if (_animator == null) _animator = GetComponent<Animator>();
                return _animator;
            }
        }
        private Animator _animator;

        /// <summary>
        /// Playable Graph of this manager
        /// </summary>
        public PlayableGraph PlayableGraph
        {
            get
            {
                Initialize();
                return _playableGraph;
            }
        }
        private PlayableGraph _playableGraph;

        private void Start()
        {
            Initialize();
        }

        private void Update()
        {
            UpdateBlendPosition();

            if(_allowDebug)
            {
                UpdateBlendDebugText();
            }
        }

        private void OnAnimatorMove()
        {
            
        }

        private void OnDestroy()
        {
            if(_animationBlendTransitionProcess != null) StopCoroutine(_animationBlendTransitionProcess);

            StopAllCoroutines();

            if(PlayableGraph.IsValid()) PlayableGraph.Destroy();
        }


        private bool _graphInitialized = false;
        private AnimationPlayableOutput _playableOutput;
        private AnimationMixerPlayable _standardMixer, _blendMixer, _singleAnimationMixer;
        private AnimationLayerMixerPlayable _topLevelMixer;

        [SerializeField, BoxGroup("Debug")] private TMP_Text _blendDebugText;
        [SerializeField, BoxGroup("Debug")] private bool _allowDebug = false;
        private void Initialize()
        {
            if (_graphInitialized) return;

            _playableGraph = PlayableGraph.Create();
            _playableOutput = AnimationPlayableOutput.Create(_playableGraph, "Main Output", Animator);

            // Initialize top level mixer
            _topLevelMixer = AnimationLayerMixerPlayable.Create(_playableGraph, 1);
            _playableOutput.SetSourcePlayable(_topLevelMixer);

            // Standard mixer >> Top level
            _standardMixer = AnimationMixerPlayable.Create(_playableGraph, 2);
            _topLevelMixer.ConnectInput(0, _standardMixer, 0, 1f);

            // Blended mixer >> Standard mixer
            _blendMixer = AnimationMixerPlayable.Create(_playableGraph, 0);
            _standardMixer.ConnectInput(0, _blendMixer, 0, 1f);

            // Single animation mixer >> Standard mixer
            _singleAnimationMixer = AnimationMixerPlayable.Create(_playableGraph, 0);
            _standardMixer.ConnectInput(1, _singleAnimationMixer, 0, 0f);

            // Init graph
            _playableGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            _playableGraph.Play();

            _blendList = new();

            _graphInitialized = true;
        }


        #region Blends

        private List<AnimationBlendInstance> _blendList;
        private IEnumerator _animationBlendTransitionProcess;
        private Vector2 _currentBlendPosition = Vector2.zero, _targetBlendPosition = Vector2.zero;
        private float _blendPositionChangeRate = -1f;

        /// <summary>
        /// Sets the blend position for animation blends
        /// </summary>
        /// <param name="position"></param>
        public void SetBlendPosition(Vector2 position) => SetBlendPosition(position, -1f);
        
        /// <summary>
        /// Sets the blend position for animation blends
        /// </summary>
        /// <param name="position">Target position</param>
        /// <param name="positionChangeRate">Rate of change from current position to the target position</param>
        public void SetBlendPosition(Vector2 position, float positionChangeRate)
        {
            _targetBlendPosition = position;
            _blendPositionChangeRate = positionChangeRate;
        }

        /// <summary>
        /// Sets a new animation blend
        /// </summary>
        /// <param name="blends"></param>
        public void SetAnimationBlend(AnimationBlends blends) => SetAnimationBlend(blends, 0);

        /// <summary>
        /// Sets a new animation blend
        /// </summary>
        /// <param name="blends"></param>
        /// <param name="transitionDuration">Duration of the transition between current and new blend</param>
        public void SetAnimationBlend(AnimationBlends blends, float transitionDuration)
        {
            Initialize();

            if (_animationBlendTransitionProcess != null) StopCoroutine(_animationBlendTransitionProcess);

            if (blends == null)
            {
                Debug.LogError("SetAnimationBlend: Null blends!");
                return;
            }

            AnimationBlendInstance existingBlend = GetExistingBlend(blends);

            // Create and attach new blend if doesn't exist
            if (existingBlend == null)
            {
                existingBlend = new(blends, PlayableGraph);
                _blendList.Add(existingBlend);

                int currInputCount = _blendMixer.GetInputCount();
                _blendMixer.SetInputCount(currInputCount + 1);
                _blendMixer.ConnectInput(currInputCount, existingBlend.Mixer, 0, 0f);
            }

            _animationBlendTransitionProcess = AnimBlendTransition(existingBlend, transitionDuration);
            if (gameObject != null && gameObject.activeSelf)
                StartCoroutine(_animationBlendTransitionProcess);
        }

        private void UpdateBlendPosition()
        {
            Initialize();
            
            if(_currentBlendPosition != _targetBlendPosition)
            {
                if(_blendPositionChangeRate >= 0f)
                {
                    float maxChange = _blendPositionChangeRate * Time.deltaTime;
                    Vector2 dir = _targetBlendPosition - _currentBlendPosition;
                    if (dir.magnitude <= maxChange)
                    {
                        _currentBlendPosition = _targetBlendPosition;
                    }
                    else
                    {
                        _currentBlendPosition += (dir.normalized * maxChange);
                    }
                }
                else
                {
                    _currentBlendPosition = _targetBlendPosition;
                }
            }

            foreach (var blend in _blendList)
            {
                if (blend == null) continue;
                blend.SetBlendWeights(_currentBlendPosition);
            }
        }

        private IEnumerator AnimBlendTransition(AnimationBlendInstance blendInstance, float transitionDuration)
        {
            transitionDuration = Mathf.Max(transitionDuration, 0f);

            // Get the input index
            if(!HasPlayableAsInput((Playable)blendInstance.Mixer, (Playable)_blendMixer, out int existingInputIndex))
            {
                Debug.LogError("AnimBlendTransition: Unable to find the blend playable!");
                _animationBlendTransitionProcess = null;
                yield break;
            }

            float startWeight = _blendMixer.GetInputWeight(existingInputIndex);
            float currentTime = 0f;

            int otherInputAmount = _blendMixer.GetInputCount() - 1;

            // Only transition when the starting weight is less than 100%
            if(startWeight < 1f)
            {
                while (currentTime < transitionDuration)
                {
                    float percent = currentTime / transitionDuration;

                    float currentTargetWeight = Mathf.Lerp(startWeight, 1f, percent);
                    currentTargetWeight = Mathf.Clamp01(currentTargetWeight);

                    float otherTargetWeight = 0f;
                    if (otherInputAmount > 0)
                    {
                        otherTargetWeight = (1 - currentTargetWeight) / (float)otherInputAmount;
                    }

                    for (int i = 0; i < _blendMixer.GetInputCount(); i++)
                    {
                        if(i == existingInputIndex)
                        {
                            _blendMixer.SetInputWeight(i, currentTargetWeight);
                            continue;
                        }

                        if (_blendMixer.GetInputWeight(i) > otherTargetWeight)
                        {
                            _blendMixer.SetInputWeight(i, otherTargetWeight);
                        }
                    }

                    yield return null;
                    currentTime += Time.deltaTime;
                }
            }
            
            // Remove previous blends
            while(_blendList.Count > 0)
            {
                AnimationBlendInstance curInstance = _blendList[0];
                _blendList.RemoveAt(0);

                if (curInstance == null) continue;
                if (blendInstance != null)
                {
                    if (blendInstance == curInstance) continue;
                }

                curInstance.DestroyPlayables();
            }

            Playable exceptionToRemove = new();
            if (blendInstance != null)
            {
                exceptionToRemove = blendInstance.Mixer;
                _blendList.Add(blendInstance);
            }

            DisconnectAndReconnectAllInputPlayables((Playable)_blendMixer, (Playable)exceptionToRemove, true);

            _animationBlendTransitionProcess = null;
        }

        private AnimationBlendInstance GetExistingBlend(AnimationBlends blends)
        {
            if (blends == null) return null;
            if (_blendList == null) return null;

            foreach (var bi in _blendList)
            {
                if(bi == null) continue;
                if (bi.Blends != blends) continue;

                return bi;
            }

            return null;
        }

        private class AnimationBlendInstance
        {
            public AnimationBlends Blends { get; private set; }
            public AnimationMixerPlayable Mixer { get; private set; }

            private readonly BlendData[] _dataList;

            public AnimationBlendInstance(AnimationBlends blends, PlayableGraph playableGraph)
            {
                Blends = blends;
                Mixer = GenerateMixer(playableGraph);

                Dictionary<Vector2, AnimationClip> blendAnimations = Blends.GetBlendList(out Dictionary<Vector2, float> blendClipSpeed);
                _dataList = GenerateBlendDataList(blendAnimations, playableGraph, blendClipSpeed);
            }

            /// <summary>
            /// - Evaluate the weights of each blend
            /// - Uses Inverse Distance Weighted Interpolation (6.2.1), from: https://runevision.com/thesis/rune_skovbo_johansen_thesis.pdf
            /// </summary>
            /// <param name="blendPosition"></param>
            public void SetBlendWeights(Vector2 blendPosition)
            {
                if(_dataList.Length <= 0) return;

                // See if there's a blend data on the position, then just make it 100% on it
                BlendData onPosBlend = GetBlendDataOnPos(blendPosition);
                if (onPosBlend != null)
                {
                    SetFullWeight(onPosBlend);
                    return;
                }

                // Calculate the inverted square distance value for each blend data, hi(p)
                Dictionary<BlendData, float> invertedSqDistList = new();
                float invertedSqDistSum = 0f;

                foreach (var data in _dataList)
                {
                    if(data == null) continue;

                    Vector2 dir = blendPosition - data.Position;
                    
                    // Potential error here if the square magnitude is too small or too high (floating point)
                    float invSqDist = 1 / dir.sqrMagnitude;
                    
                    invertedSqDistList.Add(data, invSqDist);    
                    invertedSqDistSum += invSqDist;
                }

                // If the sum of inverted is 0, do nothing. Cannot find the weights
                if(invertedSqDistSum <= 0f)
                {
                    return;
                }

                // Calculate the target weights
                Dictionary<BlendData, float> targetWeights = new();
                BlendData highestWeightedBlend = null;
                float weightSum = 0f;

                foreach (var blendData in invertedSqDistList.Keys)
                {
                    float weight = invertedSqDistList[blendData] / invertedSqDistSum;
                    
                    // Too small, just set to 0
                    if(weight < 0.001f) weight = 0f;

                    // Too large, just use this one fully
                    if(weight >= 0.999f)
                    {
                        SetFullWeight(blendData);
                        return;
                    }

                    targetWeights.Add(blendData, weight);
                    weightSum += weight;

                    if(highestWeightedBlend == null)
                    {
                        highestWeightedBlend = blendData;
                        continue;
                    }

                    // Check if there are no other weights larger than evaluated weight
                    if(!HigherWeightExist(targetWeights, weight))
                    {
                        highestWeightedBlend = blendData;
                    }
                }

                // Check if the weight sum actually reaches 100%
                if(weightSum < 1f && highestWeightedBlend != null)
                {
                    float difference = 1f - weightSum;

                    // Give the difference to the highest weighted one
                    if(targetWeights.ContainsKey(highestWeightedBlend))
                    {
                        float curWeight = targetWeights[highestWeightedBlend];
                        float newWeight = curWeight + difference;

                        targetWeights[highestWeightedBlend] = newWeight;
                    }
                }

                // Apply the weights
                foreach (var blendData in targetWeights.Keys)
                {
                    Mixer.SetInputWeight(blendData.InputIndex, Mathf.Clamp01(targetWeights[blendData]));
                }
            }

            public void DestroyPlayables()
            {
                if (!Mixer.IsNull() || !Mixer.IsValid()) return;

                for (int i = 0; i < Mixer.GetInputCount(); i++)
                {
                    Playable p = Mixer.GetInput(i);
                    Mixer.DisconnectInput(i);

                    if (p.IsValid() && !p.IsNull() && p.CanDestroy())
                    {
                        p.Destroy();
                    }
                }
            }

            private bool HigherWeightExist(Dictionary<BlendData, float> targetWeights, float evaluatedWeight)
            {
                if(targetWeights == null) return false;
                foreach(var blendData in targetWeights.Keys)
                {
                    if(blendData == null) continue;
                    if(targetWeights[blendData] > evaluatedWeight)
                    {
                        return true;
                    }
                }

                return false;
            }

            private BlendData GetBlendDataOnPos(Vector2 pos)
            {
                foreach (var data in _dataList)
                {
                    if(data == null) continue;
                    if(data.Position == pos) return data;
                }

                return null;
            }

            private void SetFullWeight(BlendData blendToFull)
            {
                if(blendToFull == null) return;
                foreach(var data in _dataList)
                {
                    if(data == null) continue;

                    float weight = 0f;
                    if (data == blendToFull) weight = 1f;

                    Mixer.SetInputWeight(data.InputIndex, weight);
                }
            }

            private AnimationMixerPlayable GenerateMixer(PlayableGraph playableGraph)
            {
                AnimationMixerPlayable result = AnimationMixerPlayable.Create(playableGraph);
                result.Play();

                return result;
            }
            private BlendData[] GenerateBlendDataList(Dictionary<Vector2, AnimationClip> blendAnimations, PlayableGraph playableGraph, Dictionary<Vector2, float> blendClipSpeed)
            {
                List<BlendData> result = new();
                if (blendAnimations == null) return result.ToArray();

                foreach (var pos in blendAnimations.Keys)
                {
                    int index = Mixer.GetInputCount();

                    AnimationClip clip = blendAnimations[pos];
                    clip.wrapMode = WrapMode.Loop;

                    AnimationClipPlayable playable = AnimationClipPlayable.Create(playableGraph, clip);

                    float clipSpeed = 1f;
                    if (blendClipSpeed.TryGetValue(pos, out float foundSpeed))
                    {
                        clipSpeed = Mathf.Max(0.01f, foundSpeed);
                    }
                    playable.SetSpeed(clipSpeed);

                    Mixer.SetInputCount(index + 1);
                    Mixer.ConnectInput(index, playable, 0, 0f);

                    playable.Play();

                    BlendData bd = new()
                    {
                        Position = pos,
                        InputIndex = index,
                        Playable = playable
                    };

                    result.Add(bd);                    
                }

                return result.ToArray();
            }

            private class BlendData
            {
                public Vector2 Position;
                public int InputIndex;
                public AnimationClipPlayable Playable;      
            }
        }

        private void UpdateBlendDebugText()
        {
            if (_blendDebugText == null) return;

            string str = $"Blend Debug, Pos {_currentBlendPosition}:\n";

            if(_blendList == null || _blendList.Count <= 0f)
            {
                str += "Invalid or empty blend list";
                _blendDebugText.text = str;
                return; 
            }

            for(int i = 0;  i < _blendList.Count; i++)
            {
                str += $"\n{i} ";
                AnimationBlendInstance bist = _blendList[i];
                if(bist == null)
                {
                    str += "NULL AnimationBlendInstance\n";
                    continue;
                }

                str += "Valid AnimationBlendInstance\n";
                float sum = 0f;
                for(int j = 0; j < bist.Mixer.GetInputCount(); j++)
                {
                    Playable p = bist.Mixer.GetInput(j);
                    string name = "NULL PLAYABLE";
                    if(!p.IsNull() && p.IsValid())
                    {
                        name = "Unknown playable";
                        AnimationClipPlayable acp = (AnimationClipPlayable)p;
                        if(!acp.IsNull() && acp.IsValid())
                        {
                            name = "Unknown clip playable";
                            AnimationClip ac = acp.GetAnimationClip();
                            if (ac != null)
                            {
                                name = ac.name;
                            }
                        }
                    }

                    float weight = bist.Mixer.GetInputWeight(j);
                    sum += weight;
                    string weightString = weight.ToString();

                    str += $"  {j}: {name} - {weightString}\n";
                }

                str += $"SUM: {sum}\n";
            }

            _blendDebugText.text = str;
        }

        #endregion

        private static bool HasPlayableAsInput(Playable inputPlayable, Playable parentPlayable, out int inputIndex)
        {
            inputIndex = -1;

            if (inputPlayable.IsNull() || !inputPlayable.IsValid())
            {
                Debug.Log("InputPlayable is invalid");
                return false;
            }

            if (parentPlayable.IsNull() || !parentPlayable.IsValid())
            {
                Debug.Log("parentPlayable is invalid");
                return false;
            }

            for (int i = 0; i < parentPlayable.GetInputCount(); i++)
            {
                Playable p = parentPlayable.GetInput(i);
                if (p.IsNull() || !p.IsValid()) continue;
                if (p.Equals(inputPlayable))
                {
                    inputIndex = i;
                    return true;
                }
            }

            return false;
        }

        private static void DisconnectAndReconnectAllInputPlayables(Playable parentPlayable, Playable exceptionInputPlayable, bool destroyDisconnected)
        {
            if (parentPlayable.IsNull() || !parentPlayable.IsValid()) return;

            bool foundException = false;

            for (int i = 0;i < parentPlayable.GetInputCount();i++)
            {
                Playable p = parentPlayable.GetInput(i);
                parentPlayable.DisconnectInput(i);

                // Check if exception
                if(!exceptionInputPlayable.IsNull() && exceptionInputPlayable.IsValid())
                {
                    if (p.Equals(exceptionInputPlayable))
                    {
                        foundException = true;
                        continue;
                    }
                }

                // Check if need to be destroyed
                if(destroyDisconnected)
                {
                    if(!p.IsNull() && p.IsValid() && p.CanDestroy())
                    {
                        p.Destroy();
                    }
                }
            }

            parentPlayable.SetInputCount(0);

            if(foundException)
            {
                parentPlayable.SetInputCount(1);
                parentPlayable.ConnectInput(0, exceptionInputPlayable, 0, 1f);
            }
        }


    }
}
