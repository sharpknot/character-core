using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Kabir.CharacterComponents
{
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    public class InteractableDetector : MonoBehaviour
    {
        [field: SerializeField, ReadOnly]
        public InteractableController CurrentInteractable {  get; private set; }

        [SerializeField, Range(0f, 180f)]
        private float _interactMaxAngle = 45f;
        [SerializeField] private LayerMask _blockingLayer;

        public event UnityAction<InteractableController> CurrentInteractableUpdate;

        private List<InteractableController> _availableInteractables;

        void Update()
        {
            UpdateCurrentInteractable();
        }

        private void UpdateCurrentInteractable()
        {
            InteractableController prevInteract = CurrentInteractable;
            
            _availableInteractables ??= new();
            _availableInteractables.RemoveAll(p => p == null);

            InteractableController currController = null;
            float closestDist = -1f;
            foreach(var c in _availableInteractables)
            {
                if (c == null) continue;
                if (!c.CanInteract) continue;

                Vector3 dir = c.transform.position - transform.position;

                if (dir != Vector3.zero)
                {
                    float angle = Vector3.Angle(transform.forward, dir.normalized);
                    if(angle > _interactMaxAngle) continue;
                }

                if(HasBlockingObjects(c)) continue;

                float dist = dir.magnitude;
                if (dist < closestDist || currController == null)
                {
                    currController = c;
                    closestDist = dist;
                }
            }

            CurrentInteractable = currController;
            if(CurrentInteractable != prevInteract)
            {
                CurrentInteractableUpdate?.Invoke(CurrentInteractable);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other == null) return;
            InteractableController ic = other.GetComponent<InteractableController>();
            if(ic == null) return;

            _availableInteractables ??= new();
            if(_availableInteractables.Contains(ic)) return;
            _availableInteractables.Add(ic);
        }

        private void OnTriggerExit(Collider other)
        {
            if (other == null) return;
            InteractableController ic = other.GetComponent<InteractableController>();
            if (ic == null) return;

            _availableInteractables ??= new();
            if (!_availableInteractables.Contains(ic)) return;
            _availableInteractables.Remove(ic);
        }

        private bool HasBlockingObjects(InteractableController interactable)
        {
            Vector3 dir = interactable.transform.position - transform.position;

            RaycastHit[] hits = new RaycastHit[4];
            int hitCount = Physics.RaycastNonAlloc(transform.position, dir.normalized, hits, dir.magnitude, _blockingLayer, QueryTriggerInteraction.Ignore);
            
            return hitCount > 0;
            
        }
    }
}
