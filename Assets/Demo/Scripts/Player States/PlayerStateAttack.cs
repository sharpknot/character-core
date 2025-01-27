using UnityEngine;
using Kabir.PlayerComponents;
using DG.Tweening;

namespace Kabir.PlayerStates
{
    [CreateAssetMenu(fileName = "PlayerStateAttack", menuName = "Player States/Attack")]
    public class PlayerStateAttack : PlayerStateBase
    {
        [SerializeField] private float _gravityMultiplier = 2f;
        [SerializeField] private AnimationClip _clip;
        [SerializeField] private float _clipSpeed = 2f;

        private Sequence _attackSequence;

        public override void StartState(PlayerStateManager stateManager)
        {
            base.StartState(stateManager);

            StateManager.MotionController.GravityMultiplier = _gravityMultiplier;
            StateManager.MotionController.AutoEvaluate = false;
            StateManager.MotionController.FollowTerrainGradient = true;

            KillSequence();

            _clip.wrapMode = WrapMode.ClampForever;
            StateManager.PlayableManager.StartSingleAnimation(_clip, 0.1f, _clipSpeed);

            float attackDuration = _clip.length / _clipSpeed;
            _attackSequence = DOTween.Sequence().AppendInterval(attackDuration).AppendCallback(FinishAttack);

        }

        public override void StopState()
        {
            StateManager.PlayableManager.StopSingleAnimation(0.1f);
            KillSequence();
            base.StopState();
        }

        public override void AnimatorMove(Vector3 deltaPosition, Quaternion deltaRotation)
        {
            base.AnimatorMove(deltaPosition, deltaRotation);
            
            if (StateManager == null) return;

            StateManager.MotionController.EvaluateMotion(deltaPosition, Time.deltaTime);
        }

        private void FinishAttack()
        {
            KillSequence();
            if (StateManager != null)
            {
                StateManager.StartDefaultState();
            }
        }

        private void KillSequence()
        {
            if (_attackSequence == null) return;
            _attackSequence.Kill();
            _attackSequence = null;
        }

        private void OnValidate()
        {
            _clipSpeed = Mathf.Max(0.01f, _clipSpeed);
            _gravityMultiplier = Mathf.Max(0f, _gravityMultiplier);
        }
    }
}
