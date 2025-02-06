using Kabir.CharacterComponents;
using NaughtyAttributes;
using UnityEngine;

namespace Kabir.PlayerComponents
{
    [RequireComponent(typeof(InteractableDetector))]
    public class PlayerInteractableManager : MonoBehaviour
    {
        public InteractableDetector Detector
        {
            get
            {
                if(_dectector == null) _dectector = GetComponent<InteractableDetector>();
                return _dectector;
            }
        }
        private InteractableDetector _dectector;

        public InteractableController CurrentInteractable => GetCurrentInteractable();

        [SerializeField, ReadOnly] private InteractableController _currentInteractable;
        [SerializeField, ReadOnly] private bool _allowInteract = true;

        private void Start()
        {
            Detector.CurrentInteractableUpdate += UpdateCurrentInteractable;
            _currentInteractable = Detector.CurrentInteractable;
            LevelEventManager.OnUpdatePlayerInteractable?.Invoke(_currentInteractable);
        }

        private void OnDestroy()
        {
            Detector.CurrentInteractableUpdate -= UpdateCurrentInteractable;
        }

        public bool InteractAllowed() => _allowInteract;
        public void SetInteractAllowed(bool allow)
        {
            if(allow ==  _allowInteract) return;
            _allowInteract = allow;

            LevelEventManager.OnUpdatePlayerInteractable?.Invoke(CurrentInteractable);
        }

        private InteractableController GetCurrentInteractable()
        {
            if(!InteractAllowed()) return null;
            return _currentInteractable;
        }

        private void UpdateCurrentInteractable(InteractableController interactable)
        {
            if (interactable == _currentInteractable) return;

            _currentInteractable = interactable;
            if(InteractAllowed())
            {
                LevelEventManager.OnUpdatePlayerInteractable?.Invoke(CurrentInteractable);
            }
        }
    }
}
