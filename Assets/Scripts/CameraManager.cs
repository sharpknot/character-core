using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using Unity.Cinemachine;
using Kabir.PlayerComponents;

namespace Kabir
{
    public class CameraManager : MonoBehaviour
    {
        [field: SerializeField, Required]   public Camera MainCamera {  get; private set; }
        [field: SerializeField, Required]   public CinemachineBrain Brain { get; private set; }

        public bool HasCameraFlatDirections(out Vector3 cameraFlatForward, out Vector3 cameraFlatRight)
        {
            cameraFlatForward = Vector3.zero;
            cameraFlatRight = Vector3.zero;

            if (MainCamera == null) return false;

            Vector3 flatFwd = MainCamera.transform.forward;
            flatFwd.y = 0f;
            if(flatFwd == Vector3.zero) return false;

            Vector3 flatRight = MainCamera.transform.right;
            flatRight.y = 0f;
            if(flatRight == Vector3.zero) return false;

            cameraFlatForward = flatFwd.normalized;
            cameraFlatRight = flatRight.normalized;

            return true;

        }
        
    }
}
