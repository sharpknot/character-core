using UnityEngine;
using Kabir.PlayerComponents;
using UnityEngine.InputSystem;
using Kabir.ScriptableObjects;
using DG.Tweening;

namespace Kabir.PlayerStates
{
    [CreateAssetMenu(fileName = "PlayerStateMove", menuName = "Player States/Move")]
    public class PlayerStateMove : PlayerStateBase
    {
        [SerializeField] private float _maxSpeed = 5f, _gravityMultiplier = 2f, _maxRotationSpeed = 480f;
        [SerializeField] private PlayerStateBase _fallingState, _jumpState;
        [SerializeField] private AnimationBlends[] _motionBlends;

        private readonly float _maxFloatingDuration = 0.2f;
        private float _currentFloatingDuration;
        private Sequence _blendSequence;

        private int _blendIndex;
        public override void StartState(PlayerStateManager stateManager)
        {
            base.StartState(stateManager);

            StateManager.MotionController.GravityMultiplier = _gravityMultiplier;
            StateManager.MotionController.AutoEvaluate = true;
            StateManager.MotionController.FollowTerrainGradient = true;

            _currentFloatingDuration = 0f;

            StateManager.PlayerInput.Primary.Jump.performed += InputJump;

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
        }

        public override void StopState()
        {
            StateManager.PlayerInput.Primary.Jump.performed -= InputJump;
            KillBlendSequence();

            base.StopState();
        }

        private void OnValidate()
        {
            _maxSpeed = Mathf.Max(0.1f, _maxSpeed);
            _gravityMultiplier = Mathf.Max(0f, _gravityMultiplier);
            _maxRotationSpeed = Mathf.Max(0f, _maxRotationSpeed);
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

        private void InputJump(InputAction.CallbackContext callbackContext)
        {
            if(_jumpState == null) return;
            StateManager.StartNewState(_jumpState);
        }

    }
}
