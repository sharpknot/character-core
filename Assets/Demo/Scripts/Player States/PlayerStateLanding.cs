using UnityEngine;
using Kabir.PlayerComponents;
using DG.Tweening;

namespace Kabir.PlayerStates
{
    [CreateAssetMenu(fileName = "PlayerStateLanding", menuName = "Player States/Landing")]
    public class PlayerStateLanding : PlayerStateBase
    {
        [SerializeField] private float _landingDuration = 0.1f;
        [SerializeField] private float _gravityMultiplier = 2f;

        private Sequence _landingSequence;

        public override void StartState(PlayerStateManager stateManager)
        {
            base.StartState(stateManager);

            StateManager.MotionController.GravityMultiplier = _gravityMultiplier;
            StateManager.MotionController.AutoEvaluate = true;
            StateManager.MotionController.FollowTerrainGradient = true;

            KillSequence();

            if (_landingDuration > 0)
            {
                _landingSequence = DOTween.Sequence().AppendInterval(_landingDuration).AppendCallback(FinishLanding);
            }
            else
            {
                FinishLanding();
            }
            
        }

        public override void StopState()
        {
            KillSequence();
            base.StopState();
        }

        private void FinishLanding()
        {
            KillSequence();
            if(StateManager != null)
            {
                StateManager.StartDefaultState();
            }
        }

        private void KillSequence()
        {
            if (_landingSequence == null) return;
            _landingSequence.Kill();
            _landingSequence = null;
        }

        private void OnValidate()
        {
            _gravityMultiplier = Mathf.Max(0f, _gravityMultiplier);
            _landingDuration = Mathf.Max(0f, _landingDuration);
        }
    }
}
