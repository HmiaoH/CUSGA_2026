using System;
using System.Collections.Generic;
using Frameworks;
using Cinemachine;
using UnityEngine;
using Utils;

namespace Managers
{

    public class CameraManager : ManagerBase<CameraManager>
    {
        [SerializeField] private List<CameraItem> camerasList = new List<CameraItem>();
        private Dictionary<string, CinemachineVirtualCamera> cameras =  new Dictionary<string, CinemachineVirtualCamera>();

        [SerializeField] private CinemachineVirtualCamera defaultCamera;
        private CinemachineVirtualCamera currentCamera;

        // 初始化默认相机
        protected override void OnInit()
        {
            ListToDictionary();

            if (defaultCamera == null)
            {
                Debug.LogWarning("CameraManager 未设置默认相机");
            }
            currentCamera = defaultCamera;
            SetCameraPriority();
        }

        /// <summary>
        /// 根据相机名称切换相机，并设置相机优先级
        /// </summary>
        /// <param name="cameraName">相机名称，CameraItem的name字段，使用Utils</param>
        public void SwitchCamera(string cameraName)
        {
            if (!cameras.TryGetValue(cameraName, out var nextCamera))
            {
                Debug.LogWarning("无法找到相机" + cameraName);
                return;
            }
            currentCamera = nextCamera;
            SetCameraPriority();
        }

        private void SetCameraPriority()
        {
            foreach (var c in cameras.Values)
            {
                c.Priority = 10;
            }

            currentCamera.Priority = 20;
        }

        // 将camera list转换成字典，方便后续读取。
        private void ListToDictionary()
        {
            foreach (var c in camerasList)
            {
                cameras[c.name] = c.virtualCamera;
            }
        }
    }
}
