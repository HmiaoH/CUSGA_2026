using Frameworks;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Managers
{
    public class InputManager : ManagerBase<InputManager>
    {
        [SerializeField] private PlayerInput playerInput; // component
        private PlayerInputAction playerInputAction; // class

        private Vector2 cameraMoveInputVector = Vector2.zero;
        /// <summary>
        /// 相机移动的具体三维方向 已经归一化
        /// </summary>
        public Vector3 cameraMoveDirection = Vector3.zero;

        // 更新
        protected override void OnInit()
        {
            playerInput =  GetComponent<PlayerInput>();
            playerInputAction = new PlayerInputAction();
        }

        protected override void OnTick(float deltaTime)
        {
            cameraMoveInputVector = GetCameraMoveInputDirection();
            cameraMoveDirection = new Vector3(cameraMoveInputVector.x, 0, cameraMoveInputVector.y);
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
