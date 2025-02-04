using UnityEngine;
using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Kabir.ScriptableObjects;
using UnityEngine.Events;
using Newtonsoft.Json.Bson;

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
        
        public event UnityAction<Vector3, Quaternion> OnAnimatorMoveUpdate;

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
            UpdateAnimatorUpdateables();
            OnAnimatorMoveUpdate?.Invoke(Animator.deltaPosition, Animator.deltaRotation);
        }

        private void OnDestroy()
        {
            if (_animationBlendTransitionProcess != null) StopCoroutine(_animationBlendTransitionProcess);
            if (_singleAnimationBlendTransitionProcess != null) StopCoroutine(_singleAnimationBlendTransitionProcess);

            if (_layerPlayables != null)
            {
                while (_layerPlayables.Count > 0)
                {
                    LayerPlayable lp = _layerPlayables[0];
                    _layerPlayables.RemoveAt(0);

                    if (lp == null) continue;
                    if (lp.TransitionProcess != null)
                    {
                        StopCoroutine(lp.TransitionProcess);
                    }
                }
            }

            StopAllCoroutines();

            if (_animatorMoveUpdatables != null)
            {
                _animatorMoveUpdatables.Clear();
                _animatorMoveUpdatables = null;
            }

            if(PlayableGraph.IsValid()) PlayableGraph.Destroy();
        }

        private List<IAnimatorUpdate> _animatorMoveUpdatables;

        /// <summary>
        /// Add an OnAnimatorMove object
        /// </summary>
        /// <param name="updateable"></param>
        /// <returns></returns>
        public bool AddAnimatorUpdateable(IAnimatorUpdate updateable)
        {
            if(updateable == null || updateable.IsNull()) return false;

            _animatorMoveUpdatables ??= new();
            _animatorMoveUpdatables.RemoveAll(u => u == null || u.IsNull());
            if(_animatorMoveUpdatables.Contains(updateable)) return true;

            _animatorMoveUpdatables.Add(updateable);

            return true;
        }

        /// <summary>
        /// Remove an OnAnimatorMove object
        /// </summary>
        /// <param name="updateable"></param>
        /// <returns></returns>
        public bool RemoveAnimatorUpdateable(IAnimatorUpdate updateable)
        {
            if (updateable == null || updateable.IsNull()) return false;

            _animatorMoveUpdatables ??= new();
            _animatorMoveUpdatables.RemoveAll(u => u == null || u.IsNull());

            if (!_animatorMoveUpdatables.Contains(updateable)) return false;
            _animatorMoveUpdatables.Remove(updateable);
            return true;
        }

        private void UpdateAnimatorUpdateables()
        {
            if(_animatorMoveUpdatables == null) return;

            Vector3 deltaPos = Animator.deltaPosition;
            Quaternion deltaRot = Animator.deltaRotation;

            foreach(var updateable in _animatorMoveUpdatables)
            {
                if(updateable == null || updateable.IsNull()) continue;
                updateable.AnimatorMove(deltaPos, deltaRot);
            }
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
            _layerPlayables = new();

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
        public void SetCurrentAnimationBlend(AnimationBlends blends) => SetCurrentAnimationBlend(blends, 0);

        /// <summary>
        /// Sets a new animation blend
        /// </summary>
        /// <param name="blends"></param>
        /// <param name="transitionDuration">Duration of the transition between current and new blend</param>
        public void SetCurrentAnimationBlend(AnimationBlends blends, float transitionDuration)
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
                if (Mixer.IsNull() || !Mixer.IsValid()) return;

                for (int i = 0; i < Mixer.GetInputCount(); i++)
                {
                    Playable p = Mixer.GetInput(i);
                    Mixer.DisconnectInput(i);

                    if (p.CanDestroy())
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

        #region Single Animation

        private IEnumerator _singleAnimationBlendTransitionProcess;

        /// <summary>
        /// Starts a new clip animation playable
        /// </summary>
        /// <param name="clip"></param>
        /// <param name="transitionDuration">Transition duration in seconds</param>
        /// <param name="clipSpeed">Clip playback speed</param>
        /// <returns>The playable applied</returns>
        public AnimationClipPlayable StartSingleAnimation(AnimationClip clip, float transitionDuration, float clipSpeed)
        {
            Initialize();
            AnimationClipPlayable playable = AnimationClipPlayable.Create(PlayableGraph, clip);
            playable.SetSpeed(Mathf.Max(0f, clipSpeed));

            return (AnimationClipPlayable)StartSingleAnimation(playable, transitionDuration);
        }

        /// <summary>
        /// Start a new single playable
        /// </summary>
        /// <param name="playable"></param>
        /// <param name="transitionDuration">Transition duration in seconds</param>
        /// <returns>The playable applied</returns>
        public Playable StartSingleAnimation(Playable playable, float transitionDuration)
        {
            Initialize();

            if (_singleAnimationBlendTransitionProcess != null)
                StopCoroutine(_singleAnimationBlendTransitionProcess);

            _singleAnimationBlendTransitionProcess = SingleAnimStartTransition(playable, transitionDuration);
            if (gameObject != null && gameObject.activeSelf)
            {
                StartCoroutine(_singleAnimationBlendTransitionProcess);
            }

            return playable;
        }

        /// <summary>
        /// Stop the current single animation
        /// </summary>
        /// <param name="transitionDuration">Transition duration in seconds</param>
        public void StopSingleAnimation(float transitionDuration)
        {
            Initialize();

            if( _singleAnimationBlendTransitionProcess != null )
                StopCoroutine( _singleAnimationBlendTransitionProcess );

            _singleAnimationBlendTransitionProcess = SingleAnimStopTransition(transitionDuration);
            if (gameObject != null && gameObject.activeSelf)
            {
                StartCoroutine( _singleAnimationBlendTransitionProcess );
            }
        }

        private IEnumerator SingleAnimStartTransition(Playable playable, float transitionDuration)
        {
            // Add playable as input for single mixer (additional input)
            int playableIndex = _singleAnimationMixer.GetInputCount();
            _singleAnimationMixer.SetInputCount(playableIndex + 1);
            _singleAnimationMixer.ConnectInput(playableIndex, playable, 0, 0f);

            float singleMixerStartWeight = _standardMixer.GetInputWeight(1);
            float currentDuration = 0f;

            int playableToRemoveAmount = playableIndex;
            while (currentDuration < transitionDuration)
            {
                float progress = currentDuration / transitionDuration;

                // Handle mixer weights
                if(singleMixerStartWeight < 1f)
                {
                    float currentSingleWeight = Mathf.Lerp(singleMixerStartWeight, 1f, progress);
                    currentSingleWeight = Mathf.Clamp01(currentSingleWeight);

                    float currentBlendWeight = 1f - currentSingleWeight;

                    _standardMixer.SetInputWeight(1, currentSingleWeight);
                    _standardMixer.SetInputWeight(0, currentBlendWeight);   
                }

                // Handle other playables
                if(playableToRemoveAmount > 0)
                {
                    float currentOtherTargetWeight = (1 - progress) / (float)playableToRemoveAmount;
                    for (int i = 0; i < playableIndex; i++)
                    {
                        float currentOtherWeight = _singleAnimationMixer.GetInputWeight(i);
                        if(currentOtherWeight < currentOtherTargetWeight) continue;
                        _singleAnimationMixer.SetInputWeight(i, currentOtherTargetWeight);
                    }
                }

                // Handle target playable
                _singleAnimationMixer.SetInputWeight(playableIndex, progress);

                yield return null;
                currentDuration += Time.deltaTime;
            }

            _standardMixer.SetInputWeight(1, 1f);   // Full weight on single mixer
            _standardMixer.SetInputWeight(0, 0f);   // Zero weight on blend mixer

            // Remove and destroy other playables
            for (int i = 0; i < _singleAnimationMixer.GetInputCount(); i++)
            {
                Playable p = _singleAnimationMixer.GetInput(i);
                _singleAnimationMixer.DisconnectInput(i);

                if (i == playableIndex) continue;
                DestroyWithChildren(p);
            }
            _singleAnimationMixer.SetInputCount(0);

            // Reattach playable (only if valid)
            if(!playable.IsNull() && playable.IsValid())
            {
                _singleAnimationMixer.SetInputCount(1);
                _singleAnimationMixer.ConnectInput(0, playable, 0, 1f);
            }

            _singleAnimationBlendTransitionProcess = null;
        }

        private IEnumerator SingleAnimStopTransition(float transitionDuration)
        {
            float startSingleWeight = _standardMixer.GetInputWeight(1);
            float currentDuration = 0f;

            while (currentDuration < transitionDuration)
            {
                float progress = currentDuration / transitionDuration;
                float currentSingleWeight = Mathf.Lerp(startSingleWeight, 0f, progress);
                currentSingleWeight = Mathf.Clamp01(currentSingleWeight);
                float currentBlendWeight = 1 - currentSingleWeight;

                _standardMixer.SetInputWeight(0, currentBlendWeight);
                _standardMixer.SetInputWeight(1, currentSingleWeight);

                yield return null;
                currentDuration += Time.deltaTime;
            }

            // Set final weights
            _standardMixer.SetInputWeight(0, 1f);
            _standardMixer.SetInputWeight(1, 0f);

            // Disconnect and destroy playables on single mixer
            for (int i = 0; i < _singleAnimationMixer.GetInputCount(); i++)
            {
                Playable p = _singleAnimationMixer.GetInput(i);
                _singleAnimationMixer.DisconnectInput(i);

                DestroyWithChildren(p);
            }
            _singleAnimationMixer.SetInputCount(0);

            _singleAnimationBlendTransitionProcess = null;
        }

        #endregion

        #region Layered Playables

        private List<LayerPlayable> _layerPlayables;

        /// <summary>
        /// Adds a masked playable on a new layer
        /// </summary>
        /// <param name="clip"></param>
        /// <param name="mask"></param>
        /// <param name="isAdditive"></param>
        /// <param name="transitionDuration"></param>
        /// <param name="childPlayable">Generated ClipPlayable attached as child to generated mixer</param>
        /// <returns>Generated mixer attached to the top layer</returns>
        public AnimationMixerPlayable AddMaskedPlayable(AnimationClip clip, AvatarMask mask, bool isAdditive, float transitionDuration, out AnimationClipPlayable childPlayable)
        {
            childPlayable = AnimationClipPlayable.Create(PlayableGraph, clip);
            return AddMaskedPlayable(childPlayable, mask, isAdditive, transitionDuration);
        }

        /// <summary>
        /// Adds a masked playable on a new layer
        /// </summary>
        /// <param name="playable"></param>
        /// <param name="mask"></param>
        /// <param name="isAdditive"></param>
        /// <param name="transitionDuration"></param>
        /// <returns>Generated mixer attached to the top layer</returns>
        public AnimationMixerPlayable AddMaskedPlayable(Playable playable, AvatarMask mask, bool isAdditive, float transitionDuration)
        {
            Initialize();

            AnimationMixerPlayable layeredMixer = AnimationMixerPlayable.Create(PlayableGraph, 1);
            layeredMixer.ConnectInput(0, playable, 0, 1f);

            int index = _topLevelMixer.GetInputCount();
            _topLevelMixer.SetInputCount(index + 1);
            _topLevelMixer.ConnectInput(index, layeredMixer, 0, 0f);
            _topLevelMixer.SetLayerAdditive((uint)index, isAdditive);
            _topLevelMixer.SetLayerMaskFromAvatarMask((uint)index, mask);

            LayerPlayable lp = new(layeredMixer, mask, isAdditive, index)
            {
                Show = true
            };

            lp.TransitionProcess = MaskedTransitionProcess(lp, transitionDuration, true);
            if (gameObject != null && gameObject.activeSelf)
            {
                StartCoroutine(lp.TransitionProcess);
            }

            // new LayerPlayable added at the end of the list (important!)
            _layerPlayables.Add(lp);    
            
            return layeredMixer;
        }

        /// <summary>
        /// Removes the generated mixer
        /// </summary>
        /// <param name="layeredMixer"></param>
        /// <param name="transitionDuration"></param>
        public void RemoveMaskedPlayable(AnimationMixerPlayable layeredMixer, float transitionDuration)
        {
            Initialize();

            if (layeredMixer.IsNull() || !layeredMixer.IsValid()) return;

            // Search for the layered mixer in the list
            LayerPlayable targetLayerPlayable = null;
            foreach(var lp in _layerPlayables)
            {
                if(lp == null) continue;
                if (lp.MixerPlayable.IsNull() || !lp.MixerPlayable.IsValid()) continue;
                if(layeredMixer.Equals(lp.MixerPlayable))
                {
                    targetLayerPlayable = lp;
                    break;
                }
            }
            
            // Unable to find
            if (targetLayerPlayable == null) return;

            if(targetLayerPlayable.TransitionProcess != null)
            {
                StopCoroutine(targetLayerPlayable.TransitionProcess);
            }

            targetLayerPlayable.Show = false;
            targetLayerPlayable.TransitionProcess = MaskedTransitionProcess(targetLayerPlayable, transitionDuration, false);
            if (gameObject != null && gameObject.activeSelf)
            {
                StartCoroutine(targetLayerPlayable.TransitionProcess);
            }
        }

        private IEnumerator MaskedTransitionProcess(LayerPlayable layerPlayable, float transitionDuration, bool show)
        {
            float startWeight = _topLevelMixer.GetInputWeight(layerPlayable.InputIndex);
            float currentTime = 0f;

            float finalWeight = 0f;
            if (show) finalWeight = 1f;

            while (currentTime < transitionDuration)
            {
                float progress = currentTime / transitionDuration;
                float curWeight = Mathf.Lerp(startWeight, finalWeight, progress);
                _topLevelMixer.SetInputWeight(layerPlayable.InputIndex, curWeight);

                yield return null;
                currentTime += Time.deltaTime;
            }

            layerPlayable.TransitionProcess = null;
            CleanMaskedPlayabes();
        }

        private void CleanMaskedPlayabes()
        {
            Initialize();

            bool hasProcess = false;
            foreach (var lp in _layerPlayables)
            {
                if(lp == null) continue;
                if(lp.TransitionProcess != null)
                {
                    hasProcess = true;
                    break;
                }
            }

            // There are process pending. No cleaning can be done
            if (hasProcess) return;

            // Disconnect all from top level
            for (int i = 0; i < _topLevelMixer.GetInputCount(); i++)
            {
                _topLevelMixer.DisconnectInput(i);
            }

            // Reconnect the standard mixer
            _topLevelMixer.SetInputCount(1);
            _topLevelMixer.ConnectInput(0, _standardMixer, 0, 1f);

            // Remove destroyable layers
            int lpi = 0;
            while (lpi < _layerPlayables.Count)
            {
                LayerPlayable lp = _layerPlayables[lpi];
                if(lp == null)
                {
                    _layerPlayables.RemoveAt(lpi);
                    continue;
                }

                if(lp.CanDestroy())
                {
                    _layerPlayables.RemoveAt(lpi);
                    DestroyWithChildren(lp.MixerPlayable);
                    continue;
                }

                lpi++;
            }

            // Reattach surviving layers
            for (int i = 0; i < _layerPlayables.Count; i++)
            {
                LayerPlayable lp = _layerPlayables[i];
                int inputIndex = _topLevelMixer.GetInputCount();

                _topLevelMixer.SetInputCount(inputIndex + 1);
                _topLevelMixer.ConnectInput(inputIndex, lp.MixerPlayable, 0, 1f);
                _topLevelMixer.SetLayerMaskFromAvatarMask((uint)inputIndex, lp.Mask);
                _topLevelMixer.SetLayerAdditive((uint)inputIndex, lp.IsAdditive);

                lp.InputIndex = inputIndex;
            }
        }

        private class LayerPlayable
        {
            public AnimationMixerPlayable MixerPlayable { get; private set; }
            public AvatarMask Mask { get; private set; }
            public bool IsAdditive { get; private set; }
            public bool Show;

            public IEnumerator TransitionProcess;
            public int InputIndex;

            public LayerPlayable(AnimationMixerPlayable mixerPlayable, AvatarMask mask, bool isAdditive, int inputIndex)
            {
                MixerPlayable = mixerPlayable;
                Mask = mask;
                IsAdditive = isAdditive;
                Show = true;
                InputIndex = inputIndex;
            }

            public bool CanDestroy()
            {        
                if(Show) return false;
                if(TransitionProcess != null) return false;
                if(!MixerPlayable.CanDestroy()) return false;
                return true;
            }

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

        private static void DestroyWithChildren(Playable playable)
        {
            if (playable.IsNull() || !playable.IsValid()) return;

            for (int i = 0; i < playable.GetInputCount(); i++)
            {
                Playable child = playable.GetInput(i);

                if (child.IsValid() && !child.IsNull())
                {
                    DestroyWithChildren(child);
                    if(child.IsValid() && child.CanDestroy()) child.Destroy();
                }
            }

            if(playable.CanDestroy()) playable.Destroy();
        }
    }
}
