using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using Kabir.CharacterComponents;

namespace Kabir.PlayerComponents
{
    /// <summary>
    /// - Script that handles the player's state (state machine design)
    /// - Only applied on players
    /// </summary>
    /// 
    public class PlayerStateManager : MonoBehaviour, IAnimatorUpdate
    {
        public PlayerInputMain PlayerInput
        {
            get
            {
                _playerInput ??= new();
                return _playerInput;
            }
        }
        private PlayerInputMain _playerInput;

        [field: SerializeField, Required] public CameraManager CameraManager { get; private set; }
        [field: SerializeField, Required] public MotionController MotionController { get; private set; }
        [field: SerializeField, Required] public PlayableManager PlayableManager { get; private set; }

        [field:SerializeField, ReadOnly, BoxGroup("Player States")]
        public PlayerStateBase CurrentState { get; private set; }
        [BoxGroup("Player States")]
        public PlayerStateBase DefaultState, StartState;

        void OnEnable()
        {
            PlayerInput.Enable();
        }

        void Start()
        {
            InitializeState();
        }

        void Update()
        {
            float deltaTime = Time.deltaTime;
            if (CurrentState != null) CurrentState.UpdateState(deltaTime);
        }

        void OnDestroy()
        {
            StopCurrentState();
        }

        void OnDisable()
        {
            PlayerInput.Disable();
        }

        #region State Controls
        public void StartDefaultState() => StartNewState(DefaultState);
        
        public void StartNewState(PlayerStateBase state)
        {
            StopCurrentState();

            if (state == null) return;
            CurrentState = Instantiate(state);
            CurrentState.StartState(this);
        }

        public void AnimatorMove(Animator animator)
        {
            if (CurrentState == null) return;
            CurrentState.AnimatorMove(animator);
        }

        private void StopCurrentState()
        {
            if(CurrentState == null) return;
            CurrentState.StopState();

            PlayerStateBase tempState = CurrentState;
            Destroy(tempState);

            CurrentState = null;
        }

        private void InitializeState()
        {
            if (CurrentState != null) return;
            
            if (StartState != null)
            {
                StartNewState(StartState);
                return;
            }

            StartDefaultState();
        }

        #endregion

    }
}
