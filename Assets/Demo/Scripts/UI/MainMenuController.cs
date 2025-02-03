using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Kabir
{
    public class MainMenuController : MonoBehaviour
    {

        [SerializeField] private InputAction _inputAction;
        [SerializeField] private RectTransform _exitMenuPanel;
        [SerializeField] private EventSystem _eventSystem;
        [SerializeField] private GameObject _firstSelected;
        private bool _exitMenuShown;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            ShowMainMenu(false);
            LevelEventManager.OnShowMainMenu += ShowMainMenu;

        }

        private void OnDestroy()
        {
            LevelEventManager.OnShowMainMenu -= ShowMainMenu;
        }

        private void OnInputCloseMenu(InputAction.CallbackContext ctx) => LevelEventManager.OnShowMainMenu?.Invoke(false);

        public void ExitGame() => Application.Quit();
        public void CloseMainMenu() => LevelEventManager.OnShowMainMenu?.Invoke(false);

        private void ShowMainMenu(bool show)
        {
            _exitMenuShown = show;
            if(_exitMenuPanel != null) _exitMenuPanel.gameObject.SetActive(_exitMenuShown);

            if (_exitMenuShown)
            {
                _inputAction.Enable();
                _inputAction.performed += OnInputCloseMenu;
                if (_eventSystem != null && _firstSelected != null) _eventSystem.SetSelectedGameObject(_firstSelected);
            }
            else
            {
                _inputAction.Disable();
                _inputAction.performed -= OnInputCloseMenu;
            }

        }
    }
}
