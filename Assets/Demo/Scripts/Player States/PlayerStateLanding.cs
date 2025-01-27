using UnityEngine;
using Kabir.PlayerComponents;
using DG.Tweening;

namespace Kabir.PlayerStates
{
    [CreateAssetMenu(fileName = "PlayerStateLanding", menuName = "Player States/Landing")]
    public class PlayerStateLanding : PlayerStateBase
    {
        [SerializeField] private float _gravityMultiplier = 2f;
        [SerializeField] private AnimationClip _clip;
        [SerializeField] private float _clipSpeed = 2f;

        private Sequence _landingSequence;

        public override void StartState(PlayerStateManager stateManager)
        {
            base.StartState(stateManager);

            StateManager.MotionController.GravityMultiplier = _gravityMultiplier;
            StateManager.MotionController.AutoEvaluate = true;
            StateManager.MotionController.FollowTerrainGradient = true;

            KillSequence();

            _clip.wrapMode = WrapMode.ClampForever;
            StateManager.PlayableManager.StartSingleAnimation(_clip, 0.1f, _clipSpeed);

            float landingDuration = _clip.length / _clipSpeed;
            _landingSequence = DOTween.Sequence().AppendInterval(landingDuration).AppendCallback(FinishLanding);
            
        }

        public override void StopState()
        {
            StateManager.PlayableManager.StopSingleAnimation(0.1f);
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
            _clipSpeed = Mathf.Max(0.01f, _clipSpeed);
            _gravityMultiplier = Mathf.Max(0f, _gravityMultiplier);
        }
    }
}
