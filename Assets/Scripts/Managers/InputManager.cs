using Frameworks;
using UnityEngine;

namespace Managers
{
    public class InputManager : ManagerBase<InputManager>
    {
        private PlayerInputAction playerInputAction; // class

        private Vector2 cameraMoveInputVector = Vector2.zero;
        /// <summary>
        /// 相机移动的具体三维方向 已经归一化
        /// </summary>
        public Vector3 cameraMoveDirection = Vector3.zero;

        // 更新
        protected override void OnInit()
        {
            playerInputAction = new PlayerInputAction();
            playerInputAction.Enable();
        }

        protected override void OnShutdown()
        {
            playerInputAction?.Disable();
            playerInputAction?.Dispose();
            playerInputAction = null;
        }

        protected override void OnTick(float deltaTime)
        {
            cameraMoveInputVector = GetCameraMoveInputDirection();
            cameraMoveDirection = new Vector3(cameraMoveInputVector.x, 0, cameraMoveInputVector.y);
            if (cameraMoveDirection != Vector3.zero)
                cameraMoveDirection.Normalize();
        }

        /// <summary>
        /// 获取当前相机移动的方向（根据input）
        /// </summary>
        /// <returns></returns>
        private Vector2 GetCameraMoveInputDirection()
        {
            var inputVector = playerInputAction.Play.CameraMove.ReadValue<Vector2>();
            return inputVector;
        }
    }
}
