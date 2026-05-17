using UnityEngine;
using Cinemachine;

/// <summary>
/// Keeps the card UI rig as an independent world-space object while matching a camera transform.
/// This avoids parenting the whole card system under the real camera, but still lets the hand
/// stay locked to the TopBehind view as that camera moves.
/// </summary>
[ExecuteAlways]
[DefaultExecutionOrder(10000)]
public class CardCameraRigFollower : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCamera followVirtualCamera;
    [SerializeField] private Transform followTarget;
    [SerializeField] private Vector3 localPositionOffset;
    [SerializeField] private Vector3 localEulerOffset;
    [SerializeField] private bool followRotation = true;

    public void Configure(
        CinemachineVirtualCamera virtualCamera,
        Vector3 positionOffset,
        Vector3 eulerOffset,
        bool shouldFollowRotation)
    {
        followVirtualCamera = virtualCamera;
        followTarget = virtualCamera != null ? virtualCamera.transform : null;
        localPositionOffset = positionOffset;
        localEulerOffset = eulerOffset;
        followRotation = shouldFollowRotation;
        ApplyFollow();
    }

    public void Configure(
        Transform target,
        Vector3 positionOffset,
        Vector3 eulerOffset,
        bool shouldFollowRotation)
    {
        followVirtualCamera = null;
        followTarget = target;
        localPositionOffset = positionOffset;
        localEulerOffset = eulerOffset;
        followRotation = shouldFollowRotation;
        ApplyFollow();
    }

    private void LateUpdate()
    {
        ApplyFollow();
    }

    private void ApplyFollow()
    {
        if (followVirtualCamera == null && followTarget == null)
        {
            return;
        }

        Vector3 basePosition;
        Quaternion baseRotation;

        if (followVirtualCamera != null && Application.isPlaying)
        {
            CameraState state = followVirtualCamera.State;
            basePosition = state.FinalPosition;
            baseRotation = state.FinalOrientation;
        }
        else
        {
            Transform target = followVirtualCamera != null ? followVirtualCamera.transform : followTarget;
            basePosition = target.position;
            baseRotation = target.rotation;
        }

        transform.position = basePosition + baseRotation * localPositionOffset;
        transform.rotation = baseRotation * Quaternion.Euler(localEulerOffset);

        if (!followRotation)
        {
            transform.rotation = Quaternion.Euler(localEulerOffset);
        }
    }
}
