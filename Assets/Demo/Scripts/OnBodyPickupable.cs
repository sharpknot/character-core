using UnityEngine;

namespace Kabir
{
    public class OnBodyPickupable : MonoBehaviour
    {
        [SerializeField] private Transform _leftHand, _rightHand;

        void Update()
        {
            if(_leftHand ==null ||  _rightHand == null) return;

            Vector3 centerPos = (_leftHand.position + _rightHand.position) / 2f;
            transform.position = centerPos;
        }
    }
}
