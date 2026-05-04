using Cinemachine;
using UnityEngine;

/// <summary>
/// 让 UI 物体跟随指定相机的位置移动，但不跟随相机旋转。
/// 同时通过 SmoothDamp 提供延迟跟随动效。
/// </summary>
public class FollowCameraPositionUI : MonoBehaviour
{
    /// <summary>
    /// 更新时机。一般 UI 跟随建议使用 LateUpdate，减少抖动。
    /// </summary>
    private enum UpdateTiming
    {
        Update = 0,
        LateUpdate = 1
    }

    [Header("跟随目标")]
    [SerializeField] private CinemachineVirtualCamera followCamera;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2f, 0f);

    [Header("跟随参数")]
    [SerializeField] private float smoothTime = 0.18f;
    [SerializeField] private float maxSpeed = 30f;
    [SerializeField] private UpdateTiming updateTiming = UpdateTiming.LateUpdate;

    [Header("空间设置")]
    [SerializeField] private bool followInLocalSpace = false;
    [SerializeField] private bool lockInitialRotation = true;

    private Vector3 velocity;
    private Quaternion initialRotation;

    private void Awake()
    {
        initialRotation = transform.rotation;
    }

    private void Update()
    {
        if (updateTiming == UpdateTiming.Update)
        {
            FollowStep(Time.deltaTime);
        }
    }

    private void LateUpdate()
    {
        if (updateTiming == UpdateTiming.LateUpdate)
        {
            FollowStep(Time.deltaTime);
        }
    }

    /// <summary>
    /// 对外暴露接口，方便运行时切换跟随相机。
    /// </summary>
    public void SetFollowCamera(CinemachineVirtualCamera targetCamera)
    {
        followCamera = targetCamera;
    }

    /// <summary>
    /// 每帧执行一次平滑跟随。
    /// </summary>
    private void FollowStep(float deltaTime)
    {
        if (followCamera == null || deltaTime <= 0f)
        {
            return;
        }

        var desiredWorldPosition = followCamera.transform.position + worldOffset;

        if (followInLocalSpace && transform.parent != null)
        {
            var desiredLocalPosition = transform.parent.InverseTransformPoint(desiredWorldPosition);
            transform.localPosition = Vector3.SmoothDamp(
                transform.localPosition,
                desiredLocalPosition,
                ref velocity,
                Mathf.Max(0.0001f, smoothTime),
                maxSpeed,
                deltaTime);
        }
        else
        {
            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredWorldPosition,
                ref velocity,
                Mathf.Max(0.0001f, smoothTime),
                maxSpeed,
                deltaTime);
        }

        // 保持初始朝向，避免 UI 被相机旋转带着转。
        if (lockInitialRotation)
        {
            transform.rotation = initialRotation;
        }
    }
}
