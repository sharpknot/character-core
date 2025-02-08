using UnityEngine;
using Kabir.PlayerComponents;
using UnityEngine.InputSystem;
using Kabir.ScriptableObjects;
using DG.Tweening;
using System.Collections.Generic;
using UnityEngine.Animations;

namespace Kabir.PlayerStates
{
    [CreateAssetMenu(fileName = "PlayerStateInteract", menuName = "Player States/Interact")]
    public class PlayerStateInteract : PlayerStateBase
    {
        [SerializeField] private AnimationClip _defaultClip;
        [SerializeField] private float _clipSpeed = 1f;
        [SerializeField, Range(0f, 1f)] private float _clipRepositionDuration = 0.1f;

        private InteractableController _interactable = null;

        private Vector3 _startPosition;
        private Sequence _animSequence = null, _repositionSequence = null;
        private bool _repositionCompleted, _interactionStarted;
        private float _repositionProgress;

        private readonly static float _rotationSpeed = 480f;

        public override void StartState(PlayerStateManager stateManager)
        {
            base.StartState(stateManager);

            _interactable = GetInteractable();
            if(_interactable == null)
            {
                StateManager.StartDefaultState();
                return;
            }

            AnimationClip clip = GetAnimationClip(out float clipSpeed, out float repositionDuration);
            float clipDuration = clip.length / clipSpeed;
            KillAnimSequence();
            _animSequence = DOTween.Sequence().AppendInterval(clipDuration).AppendCallback(AnimationComplete);
            _interactionStarted = false;

            StateManager.MotionController.GravityMultiplier = 0f;
            StateManager.MotionController.AutoEvaluate = false;
            StateManager.MotionController.FollowTerrainGradient = false;

            StateManager.PlayableManager.StartSingleAnimation(clip, 0.1f, clipSpeed);

            _startPosition = StateManager.MotionController.transform.position;
            _repositionCompleted = false;
            KillRepositionSequence();
            _repositionProgress = 0f;
            _repositionSequence = DOTween.Sequence().Append(DOTween.To(x => _repositionProgress = x, 0, 1f, (clipDuration * repositionDuration))).AppendCallback(FinishReposition);

            AnimationEventManager.OnInteractStart += Interact;
        }

        public override void UpdateState(float deltaTime)
        {
            base.UpdateState(deltaTime);
            UpdateReposition();
        }

        public override void StopState()
        {
            AnimationEventManager.OnInteractStart -= Interact;
            KillAnimSequence();
            KillRepositionSequence();
            base.StopState();
        }

        public override void AnimatorMove(Vector3 deltaPosition, Quaternion deltaRotation)
        {
            base.AnimatorMove(deltaPosition, deltaRotation);
            if (_interactable == null) return;
            if(!_interactable.UseRootAnimation) return;
            if(!_repositionCompleted) return;

            StateManager.MotionController.Warp(StateManager.MotionController.transform.position + deltaPosition);

            Vector3 fwd = deltaRotation * StateManager.MotionController.transform.forward;
            StateManager.MotionController.FlatInstantRotateTowards(fwd);
        }

        private void OnValidate()
        {
            _clipSpeed = Mathf.Max(0.01f, _clipSpeed);
        }

        private void AnimationComplete()
        {
            KillAnimSequence();
            KillRepositionSequence() ;

            if(!_interactionStarted) Interact();

            StateManager.StartDefaultState();
        }

        private void Interact()
        {
            AnimationEventManager.OnInteractStart -= Interact;
            _interactionStarted = true;
            if (!_repositionCompleted) FinishReposition();
            if(_interactable == null) return;

            _interactable.Interact(StateManager.gameObject);
        }

        private void UpdateReposition()
        {
            if (_repositionSequence == null) return;

            if (_interactable == null) return;
            Transform t = _interactable.GetInteractPosition();
            if(t == null) return;

            float progress = Mathf.Clamp01(_repositionProgress);
            Vector3 curPos = Vector3.Lerp(_startPosition, t.position, progress);
            StateManager.MotionController.Warp(curPos);
            StateManager.MotionController.FlatClampedRotateTowards(t.forward, _rotationSpeed, false);
        }

        private void FinishReposition()
        {
            KillRepositionSequence();
            _repositionCompleted = true;

            if (_interactable == null) return;
            Transform t = _interactable.GetInteractPosition();
            if (t == null) return;

            StateManager.MotionController.Warp(t.position);
            StateManager.MotionController.FlatInstantRotateTowards(t.forward);
        }

        private InteractableController GetInteractable()
        {
            if(StateManager == null) return null;
            if(StateManager.InteractableManager == null) return null;
            return StateManager.InteractableManager.CurrentInteractable;
        }

        private AnimationClip GetAnimationClip(out float clipSpeed, out float repositionDuration)
        {
            clipSpeed = _clipSpeed;
            repositionDuration = _clipRepositionDuration;

            if (_interactable == null) return _defaultClip;
            if(_interactable.Clip == null) return _defaultClip;
            
            clipSpeed = Mathf.Max(0.01f, _interactable.ClipSpeed);
            repositionDuration = Mathf.Clamp01(_interactable.ClipRepositionDuration);
            return _interactable.Clip;
        }

        private void KillAnimSequence()
        {
            if(_animSequence == null) return;
            _animSequence.Kill();
            _animSequence = null;
        }
        private void KillRepositionSequence()
        {
            if(_repositionSequence == null) return;
            _repositionSequence.Kill();
            _repositionSequence = null;
        }
    }
}
