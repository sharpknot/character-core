using UnityEngine;
using Kabir.PlayerComponents;
using UnityEngine.InputSystem;
using Kabir.ScriptableObjects;
using DG.Tweening;
using System.Collections.Generic;
using UnityEngine.Animations;

namespace Kabir.PlayerStates
{
    [CreateAssetMenu(fileName = "PlayerStateMove", menuName = "Player States/Move")]
    public class PlayerStateMove : PlayerStateBase
    {
        [SerializeField] private float _maxSpeed = 5f, _gravityMultiplier = 2f, _maxRotationSpeed = 480f;
        [SerializeField] private PlayerStateBase _fallingState, _jumpState, _attackState;
        [SerializeField] private AnimationBlends[] _motionBlends;
        [SerializeField] private InteractAnim[] _interactAnims;
        [SerializeField] private AvatarMask _interactMask;

        private InteractAnim[] _validInteractAnim;

        private readonly float _maxFloatingDuration = 0.2f;
        private float _currentFloatingDuration;
        private Sequence _blendSequence;
        private List<InteractInstance> _interactInstances;

        private int _blendIndex;
        public override void StartState(PlayerStateManager stateManager)
        {
            base.StartState(stateManager);

            StateManager.MotionController.GravityMultiplier = _gravityMultiplier;
            StateManager.MotionController.AutoEvaluate = true;
            StateManager.MotionController.FollowTerrainGradient = true;

            _currentFloatingDuration = 0f;
            _validInteractAnim = GetValidInteractAnims();

            StateManager.PlayerInput.Primary.Jump.performed += InputJump;
            StateManager.PlayerInput.Primary.LightAttack.performed += InputAttack;
            StateManager.PlayerInput.Primary.Interact.performed += InputInteract;

            KillBlendSequence();
            _blendIndex = 0;
            StateManager.PlayableManager.SetCurrentAnimationBlend(GetAnimationBlend());
            _blendSequence = DOTween.Sequence().AppendInterval(Random.Range(3f, 5f)).AppendCallback(SetAnimationBlend);

            StateManager.PlayableManager.StopSingleAnimation(0.1f);
            StateManager.PlayableManager.SetBlendPosition(Vector2.zero);
        }

        public override void UpdateState(float deltaTime)
        {
            base.UpdateState(deltaTime);

            if(CanChangeToFallingState(deltaTime))
            {
                StateManager.StartNewState(_fallingState);
                return;
            }

            Vector3 netMotion = GetNetMotion(deltaTime, _maxSpeed);
            ApplyMotion(netMotion);
            UpdateRotation(netMotion, _maxRotationSpeed);

            UpdateAnimation(netMotion, deltaTime);
            UpdateInteractInstance(deltaTime);
        }

        public override void StopState()
        {
            StateManager.PlayerInput.Primary.Jump.performed -= InputJump;
            StateManager.PlayerInput.Primary.LightAttack.performed -= InputAttack;
            StateManager.PlayerInput.Primary.Interact.performed -= InputInteract;

            KillBlendSequence();
            RemoveAllInteractInstances();
            base.StopState();
        }

        private void OnValidate()
        {
            _maxSpeed = Mathf.Max(0.1f, _maxSpeed);
            _gravityMultiplier = Mathf.Max(0f, _gravityMultiplier);
            _maxRotationSpeed = Mathf.Max(0f, _maxRotationSpeed);

            if (_interactAnims != null)
            {
                foreach(var ia in _interactAnims)
                {
                    ia?.OnValidate();
                }
            }
        }

        private void SetAnimationBlend()
        {
            KillBlendSequence();

            AnimationBlends b = GetAnimationBlend();
            StateManager.PlayableManager.SetCurrentAnimationBlend(b, 0.5f);

            _blendSequence = DOTween.Sequence().AppendInterval(Random.Range(3f, 5f)).AppendCallback(SetAnimationBlend);
        }

        private void KillBlendSequence()
        {
            if (_blendSequence == null) return;
            _blendSequence.Kill();
            _blendSequence = null;
        }

        private AnimationBlends GetAnimationBlend()
        {
            AnimationBlends b = _motionBlends[_blendIndex];

            _blendIndex++;
            if(_blendIndex >= _motionBlends.Length) _blendIndex = 0;
            return b;
        }

        private void UpdateAnimation(Vector3 netMotion, float deltaTime)
        {
            if(!StateManager.MotionController.HasFlatDirections(out Vector3 flatForward, out Vector3 flatRight))
            {
                StateManager.PlayableManager.SetBlendPosition(Vector2.zero);
                return;
            }

            Vector3 velocity = netMotion / deltaTime;
            Vector3 normVelocity = velocity / _maxSpeed;

            float fwd = Vector3.Dot(flatForward.normalized, normVelocity);
            float right = Vector3.Dot(flatRight.normalized, normVelocity);

            Vector2 blendPos =new(right, fwd);
            blendPos = Vector2.ClampMagnitude(blendPos, 1f);

            StateManager.PlayableManager.SetBlendPosition(blendPos, 10f);
        }

        private bool CanChangeToFallingState(float deltaTime)
        {
            if(_fallingState == null) return false;

            if(StateManager.MotionController.HasFullyGrounded(out _))
            {
                _currentFloatingDuration = 0f;
                return false;
            }

            if(_currentFloatingDuration < _maxFloatingDuration)
            {
                _currentFloatingDuration += deltaTime;
                return false;
            }

            return true;
        }

        private InteractAnim[] GetValidInteractAnims()
        {
            if(_interactAnims == null) return new InteractAnim[0];

            List<InteractAnim> result = new();
            foreach (var ia in _interactAnims)
            {
                if(ia == null) continue;
                if(ia.InteractClip == null) continue;
                if(result.Contains(ia)) continue;
                result.Add(ia);
            }

            return result.ToArray();
        }

        private void UpdateInteractInstance(float deltaTime)
        {
            if(_interactInstances == null) return;

            foreach (var ia in _interactInstances)
            {
                if(ia == null) continue;
                ia.Update(deltaTime);
            }

            // Destroy completed
            int index = 0;
            while (index < _interactInstances.Count)
            {
                var ia = _interactInstances[index];
                if(ia == null)
                {
                    _interactInstances.RemoveAt(index);
                    continue;
                }

                if(ia.HasFinished)
                {
                    StateManager.PlayableManager.RemoveMaskedPlayable(ia.Mixer, 0.3f);
                    _interactInstances.Remove(ia);
                    continue;
                }

                index++;
            }
        }

        private void RemoveAllInteractInstances()
        {
            if (_interactInstances == null) return;

            while(_interactInstances.Count > 0)
            {
                var ia = _interactInstances[0];
                if(ia != null)
                {
                    StateManager.PlayableManager.RemoveMaskedPlayable(ia.Mixer, 0f);
                }

                _interactInstances.RemoveAt(0);
            }
        }

        private void AddRandomInteractInstance()
        {
            if (_validInteractAnim == null || _validInteractAnim.Length <= 0) return;

            InteractAnim ia = _validInteractAnim[Random.Range(0, _validInteractAnim.Length)];
            AnimationMixerPlayable mixer = StateManager.PlayableManager.AddMaskedPlayable(ia.InteractClip, _interactMask, false, 0.2f, out _);

            InteractInstance ins = new(ia, mixer);

            _interactInstances ??= new();
            _interactInstances.Add(ins);
        }

        private void InputJump(InputAction.CallbackContext callbackContext)
        {
            if(_jumpState == null) return;
            StateManager.StartNewState(_jumpState);
        }

        private void InputAttack(InputAction.CallbackContext callbackContext)
        {
            if (_attackState == null) return;
            StateManager.StartNewState(_attackState);
        }

        private void InputInteract(InputAction.CallbackContext callbackContext)
        {
            AddRandomInteractInstance();
        }


        private class InteractInstance
        {
            public AnimationMixerPlayable Mixer { get; private set; }
            public InteractAnim InteractData { get; private set; }
            public bool HasFinished => (_currentDuration >= _maxDuration);

            private float _currentDuration = 0f;
            private float _maxDuration = 0f;

            public InteractInstance(InteractAnim data, AnimationMixerPlayable mixer)
            {
                InteractData = data;
                Mixer = mixer;

                _currentDuration = 0f;
                _maxDuration = InteractData.InteractClip.length / InteractData.ClipSpeed;
            }

            public void Update(float deltaTime)
            {
                _currentDuration += Mathf.Max(0f, deltaTime);
            }
        }

        [System.Serializable]
        private class InteractAnim
        {
            [field: SerializeField] public AnimationClip InteractClip;
            [field: SerializeField] public float ClipSpeed = 1f;

            public void OnValidate()
            {
                ClipSpeed = Mathf.Max(ClipSpeed, 0.01f);
            }
        }

    }
}
