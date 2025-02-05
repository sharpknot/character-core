using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Kabir
{
    [RequireComponent(typeof(Collider))]
    public class InteractableController : MonoBehaviour
    {
        [SerializeField] private bool _isActive = true;
        [SerializeField] private string _interactText = "Interact";
        [SerializeField] private Transform _interactPosition;

        /// <summary>
        /// Checks if the interactable can be interacted with
        /// </summary>
        public bool CanInteract => CheckCanInteract();

        /// <summary>
        /// Interact animation clip for this object
        /// </summary>
        [field: SerializeField, BoxGroup("Interact Animation")] public AnimationClip Clip { get; private set; }

        /// <summary>
        /// Animation clip speed for this interactable
        /// </summary>
        [field: SerializeField, BoxGroup("Interact Animation")] public float ClipSpeed { get; private set; } = 1f;

        /// <summary>
        /// Flag for animation clip to check whether it uses root animation or not
        /// </summary>
        [field: SerializeField, BoxGroup("Interact Animation")] public bool UseRootAnimation { get; private set; } = false;

        public event UnityAction<GameObject> InteractSuccess;
        public UnityEvent<GameObject> InteractSuccessEvent;

        /// <summary>
        /// Attempt to run interaction
        /// </summary>
        /// <param name="interactingObject">The object interacting with this</param>
        /// <returns>Interaction success</returns>
        public bool Interact(GameObject interactingObject)
        {
            if(!CanInteract) return false;

            InteractSuccess?.Invoke(interactingObject);
            InteractSuccessEvent?.Invoke(interactingObject);
            return true;
        }

        public bool IsActive() => _isActive;
        public void Activate(bool active) => _isActive = active;

        public string GetInteractText() => _interactText;
        public void SetInteractText(string interactText) => _interactText = interactText;

        public Transform GetInteractPosition() => _interactPosition;
        public void SetInteractPosition(Transform transform) => _interactPosition = transform;
        public void SetInteractPosition(Vector3 position, Quaternion rotation)
        {
            GameObject g = new($"{gameObject.name} interact position");
            SceneManager.MoveGameObjectToScene(g, gameObject.scene);

            g.transform.SetPositionAndRotation(position, rotation);
            SetInteractPosition(g.transform);
        }

        private void OnValidate()
        {
            ClipSpeed = Mathf.Max(0.01f, ClipSpeed);
        }

        private void OnDrawGizmos()
        {
            if (_interactPosition != null)
            {
                DebugExtension.DrawCapsule(_interactPosition.position, _interactPosition.position + new Vector3(0f, 1.75f, 0f), Color.magenta, 0.5f);
                
                Vector3 flatDir = _interactPosition.forward;
                flatDir.y = 0f;

                if(flatDir != Vector3.zero) 
                    DebugExtension.DrawArrow(_interactPosition.position, flatDir.normalized * 0.75f, Color.blue);

                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(_interactPosition.position + new Vector3(0f, 0.875f, 0f), transform.position);

                DebugExtension.DrawPoint(transform.position, Color.magenta, 0.25f);
            }
        }

        private bool CheckCanInteract()
        {
            if(!IsActive()) return false;
            return true;
        }

    }
}