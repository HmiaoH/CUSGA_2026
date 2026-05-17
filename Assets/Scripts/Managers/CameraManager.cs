using System.Collections.Generic;
using Frameworks;
using UnityEngine;
using UnityEngine.InputSystem;
using Utils;

namespace Managers
{

    public class CameraManager : ManagerBase<CameraManager>
    {
        [Header("相机存储")]
        [SerializeField] private List<CameraItem> camerasList = new List<CameraItem>();
        private Dictionary<string, CameraItem> cameras =  new Dictionary<string, CameraItem>();

        [SerializeField] private string defaultCameraName;
        private CameraItem currentCamera;

        public string CurrentCameraName => currentCamera != null ? currentCamera.name : string.Empty;

        public bool TryGetCameraItem(string cameraName, out CameraItem cameraItem)
        {
            if (cameras.Count == 0)
            {
                ListToDictionary();
            }

            return cameras.TryGetValue(cameraName, out cameraItem);
        }

        // 初始化默认相机
        protected override void OnInit()
        {
            ListToDictionary();

            if (cameras.Count == 0)
            {
                Debug.LogWarning("CameraManager 未配置任何相机");
                return;
            }
            if (defaultCameraName == "")
            {
                defaultCameraName = Utils.CameraName.TOPBEHIND;
            }

            currentCamera = cameras[defaultCameraName];

            SetCameraPriority();
        }

        protected override void OnTick(float deltaTime)
        {
            var moveDirection = InputManager.Instance.cameraMoveDirection;
            UpdateBoardCameraPosition(deltaTime, moveDirection);
        }

        /// <summary>
        /// 鼠标滚动更新棋盘上相机
        /// </summary>
        /// <param name="context">输入的回调参数，为鼠标滚动的方向大小</param>
        public void UpdateBoardCamera(InputAction.CallbackContext context)
        {
            var scroll = context.ReadValue<float>();
            if (scroll > 0)
            {
                SwitchCamera(Utils.CameraName.TOPDOWN);
                Debug.Log("Switch Camera to " + Utils.CameraName.TOPDOWN);
            }
            else if (scroll < 0)
            {
                SwitchCamera(Utils.CameraName.TOPBEHIND);
                Debug.Log("Switch Camera to " + Utils.CameraName.TOPBEHIND);

            }
        }

        // 根据相机名称切换相机，并设置相机优先级
        private void SwitchCamera(string cameraName)
        {
            if (!cameras.TryGetValue(cameraName, out var nextCamera))
            {
                Debug.LogWarning("无法找到相机" + cameraName);
                return;
            }
            currentCamera = nextCamera;
            SetCameraPriority();
        }

        // 仅将当前的相机优先级设置最高，用于切换相机。
        private void SetCameraPriority()
        {
            foreach (var c in cameras.Values)
            {
                c.camera.Priority = 10;
            }

            currentCamera.camera.Priority = 20;
        }

        // 更新当前相机的位置。根据输入的相机移动的方向。
        private void UpdateBoardCameraPosition(float deltaTime, Vector3 moveDirection)
        {
            var moveSpeed = currentCamera.moveSpeed;
            var bound = currentCamera.bounds;
            var defaultPos = currentCamera.defaultPosition;

            var minVector = new Vector3(defaultPos.x - bound, defaultPos.y - bound, defaultPos.z - bound);
            var maxVector = new Vector3(defaultPos.x + bound, defaultPos.y + bound, defaultPos.z + bound);

            var moveVector = moveDirection * (deltaTime * moveSpeed);
            var pos = currentCamera.target.position + moveVector;
            currentCamera.target.position = Utils.MathUtils.ClampVector3(pos, minVector, maxVector);
        }

        // 将cameraList转换成字典，方便后续读取。
        private void ListToDictionary()
        {
            cameras.Clear();

            foreach (var c in camerasList)
            {
                if (c == null || string.IsNullOrEmpty(c.name))
                {
                    continue;
                }

                if (c.camera == null)
                {
                    Debug.LogWarning("CameraManager 中的相机条目缺少 CinemachineVirtualCamera: " + c.name);
                    continue;
                }

                c.SetDefaultPosition();
                cameras[c.name] = c;
            }
        }

    }
}
