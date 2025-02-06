using TMPro;
using UnityEngine;

namespace Kabir.DebugTools
{
    public class CurrentInteractableMonitor : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text;


        void Start()
        {
            LevelEventManager.OnUpdatePlayerInteractable += OnCurrentInteractableUpdate;
        }

        private void OnDestroy()
        {
            LevelEventManager.OnUpdatePlayerInteractable -= OnCurrentInteractableUpdate;
        }

        private void OnCurrentInteractableUpdate(InteractableController interactable)
        {
            if (_text == null) return;
            if(interactable == null)
            {
                _text.text = "";
                return;
            }

            _text.text = $"{interactable.gameObject.name}: {interactable.GetInteractText()}";

        }
    }
}
