using UnityEngine;
using Kabir.PlayerComponents;
using UnityEngine.InputSystem;

namespace Kabir.PlayerStates
{
    [CreateAssetMenu(fileName = "PlayerStateMove", menuName = "Player States/Move")]
    public class PlayerStateMove : PlayerStateBase
    {
        [SerializeField] private float _maxSpeed = 5f, _gravityMultiplier = 2f, _maxRotationSpeed = 480f;
        [SerializeField] private PlayerStateBase _fallingState, _jumpState;

        private readonly float _maxFloatingDuration = 0.2f;
        private float _currentFloatingDuration;
        public override void StartState(PlayerStateManager stateManager)
        {
            base.StartState(stateManager);

            StateManager.MotionController.GravityMultiplier = _gravityMultiplier;
            StateManager.MotionController.AutoEvaluate = true;
            StateManager.MotionController.FollowTerrainGradient = true;

            _currentFloatingDuration = 0f;

            StateManager.PlayerInput.Primary.Jump.performed += InputJump;
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
        }

        public override void StopState()
        {
            StateManager.PlayerInput.Primary.Jump.performed -= InputJump;
            base.StopState();
        }

        private void OnValidate()
        {
            _maxSpeed = Mathf.Max(0f, _maxSpeed);
            _gravityMultiplier = Mathf.Max(0f, _gravityMultiplier);
            _maxRotationSpeed = Mathf.Max(0f, _maxRotationSpeed);
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
