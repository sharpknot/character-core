using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using Kabir.CharacterComponents;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

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
        [field: SerializeField, Required] public PlayerInteractableManager InteractableManager { get; private set; }

        [SerializeField] private CinemachineInputAxisController _freeCamInputController;

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
            PlayerInput.Primary.MainMenu.performed += InputMainMenu;

            LevelEventManager.OnShowMainMenu += OnShowMainMenu;

            InitializeState();
            PlayableManager.AddAnimatorUpdateable(this);
        }

        void Update()
        {
            float deltaTime = Time.deltaTime;
            if (CurrentState != null) CurrentState.UpdateState(deltaTime);
        }

        void OnDestroy()
        {
            StopCurrentState();
            PlayerInput.Primary.MainMenu.performed -= InputMainMenu;
            LevelEventManager.OnShowMainMenu -= OnShowMainMenu;
            if (PlayableManager != null) PlayableManager.RemoveAnimatorUpdateable(this);
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

        public void AnimatorMove(Vector3 deltaPosition, Quaternion deltaRotation)
        {
            if (CurrentState == null) return;
            CurrentState.AnimatorMove(deltaPosition, deltaRotation);
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

        public bool IsNull() => (this == null || gameObject == null);

        #endregion

        private void InputMainMenu(InputAction.CallbackContext context)
        {
            LevelEventManager.OnShowMainMenu?.Invoke(true);
        }

        private void OnShowMainMenu(bool showMainMenu)
        {
            EnableLookAxis(!showMainMenu);

            if (showMainMenu)
            {
                PlayerInput.Disable();
                return;
            }

            PlayerInput.Enable();
        }

        private void EnableLookAxis(bool enable)
        {
            if(_freeCamInputController == null) return;
            var inputList = _freeCamInputController.Controllers;
            foreach (var controller in inputList)
            {
                if (controller != null)
                {
                    controller.Enabled = enable;
                }
            }
        }

    }
}
