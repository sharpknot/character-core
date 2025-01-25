using UnityEngine;
using Kabir.PlayerComponents;

namespace Kabir.PlayerStates
{
    [CreateAssetMenu(fileName = "PlayerStateFalling", menuName = "Player States/Falling")]
    public class PlayerStateFalling : PlayerStateBase
    {
        [SerializeField] private float _maxSpeed = 5f, _gravityMultiplier = 2f, _maxRotationSpeed = 480f;
        [SerializeField] private PlayerStateBase _landingState;
        public override void StartState(PlayerStateManager stateManager)
        {
            base.StartState(stateManager);

            StateManager.MotionController.GravityMultiplier = _gravityMultiplier;
            StateManager.MotionController.AutoEvaluate = true;
            StateManager.MotionController.FollowTerrainGradient = false;
        }

        public override void UpdateState(float deltaTime)
        {
            base.UpdateState(deltaTime);

            if(StateManager.MotionController.HasFullyGrounded(out _))
            {
                StartLanding();
                return;
            }

            Vector3 motion = GetNetMotion(deltaTime, _maxSpeed);
            UpdateRotation(motion, _maxRotationSpeed);

            ApplyMotion(motion);
        }

        private void StartLanding()
        {
            if (_landingState != null)
            {
                StateManager.StartNewState(_landingState);
                return;
            }

            StateManager.StartDefaultState();
        }

        private void OnValidate()
        {
            _maxSpeed = Mathf.Max(0f, _maxSpeed);
            _gravityMultiplier = Mathf.Max(0f, _gravityMultiplier);
            _maxRotationSpeed = Mathf.Max(0f, _maxRotationSpeed);
        }


    }
}
