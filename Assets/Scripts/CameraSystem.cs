using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraSystem : MonoBehaviour
{
    [Header("相机汇总")]
    [SerializeField] private CinemachineVirtualCamera[]  cameras;
    [SerializeField] private CinemachineVirtualCamera topDownCamera;
    [SerializeField] private GameObject topDownCameraTarget;
    [SerializeField] private CinemachineVirtualCamera topBehindCamera;
    [SerializeField] private GameObject topBehindCameraTarget;

    [Header("相机设置")]
    [SerializeField] private float cameraMoveSpeed = 7f;
    [SerializeField] private CinemachineVirtualCamera defaultCamera;
    [SerializeField] private float cameraMag = 3f;

    [Header("游戏输入")]
    [SerializeField] private GameInput gameInput;

    private CinemachineVirtualCamera currentCamera;

    private Vector3 topDownDefaultPostion, topBehindDefaultPostion;

    private void Awake()
    {
        ChangeMainCamera(defaultCamera);
        topBehindDefaultPostion = topBehindCameraTarget.transform.position;
        topDownDefaultPostion = topDownCameraTarget.transform.position;
    }


    private void Update()
    {
        MoveCamera(currentCamera);
    }


    private void MoveCamera(CinemachineVirtualCamera camera)
    {
        GameObject target = topBehindCameraTarget;
        if (camera == topDownCamera) target = topDownCameraTarget;
        if (camera == topBehindCamera) target = topBehindCameraTarget;

        var inputVector = gameInput.GetCameraMovement();
        var moveDir = new Vector3( -inputVector.x, 0f, -inputVector.y);

        target.transform.Translate(moveDir * (cameraMoveSpeed * Time.deltaTime));

        var transformPosition = target.transform.position;
        if (target == topBehindCameraTarget)
        {
            target.transform.position = new Vector3(
                Mathf.Clamp(transformPosition.x, topBehindDefaultPostion.x - cameraMag, topBehindDefaultPostion.x + cameraMag),
                topBehindDefaultPostion.y,
                Mathf.Clamp(transformPosition.z, topBehindDefaultPostion.z - cameraMag, topBehindDefaultPostion.z + cameraMag)
            );

        }
        else if (target == topDownCameraTarget)
        {
            target.transform.position = new Vector3(
                Mathf.Clamp(transformPosition.x, topDownDefaultPostion.x - cameraMag, topDownDefaultPostion.x + cameraMag),
                topDownDefaultPostion.y,
                Mathf.Clamp(transformPosition.z, topDownDefaultPostion.z - cameraMag, topDownDefaultPostion.z + cameraMag)
            );

        }
    }


    public void MouseScrollChangeCamera(InputAction.CallbackContext context)
    {
        var scroll = context.ReadValue<Vector2>().y;
        if (scroll > 0)
            ChangeMainCamera(topDownCamera);
        else if (scroll < 0)
            ChangeMainCamera(topBehindCamera);
    }
    private void ChangeMainCamera(CinemachineVirtualCamera newCamera)
    {
        currentCamera = newCamera;
        SetCameraPriority();
    }
    private void SetCameraPriority()
    {
        foreach (var i in cameras)
        {
            if (i == currentCamera) i.Priority = 20;
            else i.Priority = 10;
        }
    }

}
