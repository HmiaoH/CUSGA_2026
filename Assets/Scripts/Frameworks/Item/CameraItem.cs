using Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;

namespace Frameworks
{
    [System.Serializable]
    public class CameraItem
    {
        public string name;

        [FormerlySerializedAs("virtualCamera")]
        public CinemachineVirtualCamera camera;

        public Transform target;
        public Vector3 defaultPosition;

        public float moveSpeed = 3f;
        public float bounds = 1f;

        public void SetDefaultPosition()
        {
            if (target == null)
            {
                return;
            }

            defaultPosition = target.position;
        }
    }
}
