using System.Collections.Generic;
using Managers;
using UnityEngine;

public class CameraModeVisibilityController : MonoBehaviour
{
    [SerializeField] private List<GameObject> targets = new List<GameObject>();
    [SerializeField] private List<string> visibleCameraNames = new List<string> { "TopBehind" };
    [SerializeField] private bool hideWhenCameraManagerUnavailable = false;

    private bool lastVisibleState = true;

    private void OnEnable()
    {
        bool shouldShow = ResolveVisibility();
        lastVisibleState = shouldShow;
        ApplyVisibility(shouldShow);
    }

    private void LateUpdate()
    {
        bool shouldShow = ResolveVisibility();
        if (shouldShow == lastVisibleState)
        {
            return;
        }

        lastVisibleState = shouldShow;
        ApplyVisibility(shouldShow);
    }

    private bool ResolveVisibility()
    {
        if (CameraManager.Instance == null)
        {
            return !hideWhenCameraManagerUnavailable;
        }

        string currentCameraName = CameraManager.Instance.CurrentCameraName;
        for (int i = 0; i < visibleCameraNames.Count; i++)
        {
            if (visibleCameraNames[i] == currentCameraName)
            {
                return true;
            }
        }

        return false;
    }

    private void ApplyVisibility(bool visible)
    {
        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i] != null)
            {
                targets[i].SetActive(visible);
            }
        }
    }
}
