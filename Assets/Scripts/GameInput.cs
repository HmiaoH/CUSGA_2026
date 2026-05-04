using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameInput : MonoBehaviour
{
    private PlayerInputActions playerInputActions;
    private PlayerInput playerInput;

    private void Awake()
    {
        playerInputActions = new PlayerInputActions();
        playerInput = GetComponent<PlayerInput>();
        playerInputActions.Enable();
    }

    public Vector2 GetCameraMovement()
    {
        var inputVector = playerInputActions.Player.CameraMovement.ReadValue<Vector2>();
        return inputVector;
    }


}
