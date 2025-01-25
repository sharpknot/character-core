using UnityEngine;
using Kabir.PlayerComponents;
using DG.Tweening;

namespace Kabir.PlayerStates
{
    [CreateAssetMenu(fileName = "PlayerStateJump", menuName = "Player States/Jumping")]
    public class PlayerStateJump : PlayerStateBase
    {
        [SerializeField] private float _maxSpeed = 5f, _gravityMultiplier = 2f, _maxRotationSpeed = 480f, _jumpHeight = 3f;
        [SerializeField] private PlayerStateBase _fallingState;

        private readonly float _minJumpDuration = 0.1f;
        private Sequence _minJumpSequence;
        private bool _canFall;

        public override void StartState(PlayerStateManager stateManager)
        {
            base.StartState(stateManager);

            StateManager.MotionController.GravityMultiplier = _gravityMultiplier;
            StateManager.MotionController.AutoEvaluate = true;
            StateManager.MotionController.FollowTerrainGradient = false;

            float jumpSpeed = Mathf.Sqrt(_jumpHeight * -2f * Physics.gravity.y * _gravityMultiplier);
            StateManager.MotionController.Jump(jumpSpeed);

            KillMinJumpSequence();
            _canFall = false;
            _minJumpSequence = DOTween.Sequence().
                AppendInterval(_minJumpDuration).
                AppendCallback(() => { _canFall = true; KillMinJumpSequence();});
        }

        public override void UpdateState(float deltaTime)
        {
            base.UpdateState(deltaTime);

            if (CanStartFalling())
            {
                if (_fallingState != null)
                {
                    StateManager.StartNewState(_fallingState);
                    return;
                }

                StateManager.StartDefaultState();
                return;
            }

            Vector3 motion = GetNetMotion(deltaTime, _maxSpeed);
            ApplyMotion(motion);
            UpdateRotation(motion, _maxRotationSpeed);
        }

        public override void StopState()
        {
            KillMinJumpSequence();
            base.StopState();
        }

        private bool CanStartFalling()
        {
            if (StateManager.MotionController.PreviousVerticalVelocity >= 0f) return false;
            return _canFall;
        }

        private void KillMinJumpSequence()
        {
            if(_minJumpSequence == null) return;
            _minJumpSequence.Kill();
            _minJumpSequence = null;
        }

        private void OnValidate()
        {
            _jumpHeight = Mathf.Max(_jumpHeight, 0.1f);
            _maxSpeed = Mathf.Max(0f, _maxSpeed);
            _gravityMultiplier = Mathf.Max(0f, _gravityMultiplier);
            _maxRotationSpeed = Mathf.Max(0f, _maxRotationSpeed);
        }
    }
}
