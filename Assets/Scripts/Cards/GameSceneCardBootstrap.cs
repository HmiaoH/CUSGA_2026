using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Utils;
using Cinemachine;
using Managers;
using Frameworks;

/// <summary>
/// 为 GameScene 运行时搭建一套与 SampleScene 等效的世界空间卡牌界面。
/// 目标是减少手工复制场景层级，只保留少量 Inspector 引用绑定。
/// </summary>
public class GameSceneCardBootstrap : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] private CinemachineVirtualCamera targetCamera;
    [SerializeField] private Camera eventCamera;
    [SerializeField] private CardView handCardPrefab;
    [SerializeField] private GameObject worldCardPrefab;
    [SerializeField] private Transform worldCardsParent;
    [SerializeField] private Transform fixedTableAnchor;
    [SerializeField] private List<CardDefinition> demoCards = new List<CardDefinition>();

    [Header("Hand Canvas")]
    [SerializeField] private bool buildOnStart = true;
    [SerializeField] private bool rebuildIfAlreadyBuilt = false;
    [SerializeField] private Vector3 handCanvasLocalPosition = new Vector3(0f, 0f, 1.077f);
    [SerializeField] private Vector3 handCanvasLocalEuler = new Vector3(-25.661f, 0f, 0f);
    [SerializeField] private Vector3 handCanvasLocalScale = new Vector3(0.003f, 0.003f, 0.003f);
    [SerializeField] private Vector2 handCanvasSize = new Vector2(1037f, 670.5f);
    [SerializeField] private Vector2 handRootAnchoredPosition = new Vector2(0f, -215f);
    [SerializeField] private Vector2 handRootSize = new Vector2(100f, 100f);

    [Header("Camera Follow")]
    [SerializeField] private string followCameraName = CameraName.TOPBEHIND;
    [SerializeField] private Vector3 cameraRigPositionOffset;
    [SerializeField] private Vector3 cameraRigEulerOffset;
    [SerializeField] private bool followCameraRotation = true;

    [Header("Camera Mode Visibility")]
    [SerializeField] private bool showCardsOnlyInTopBehind = true;
    [SerializeField] private string visibleCameraName = CameraName.TOPBEHIND;

    [Header("Hand Layout")]
    [SerializeField] private float preferredCardSpacing = 170f;
    [SerializeField] private float minimumCardSpacing = 68f;
    [SerializeField] private float horizontalPadding = 48f;
    [SerializeField] private float arcHeight = 35f;
    [SerializeField] private float maxRotation = 10f;
    [SerializeField] private float animationSpeed = 18f;
    [SerializeField] private float hoverPushDistance = 55f;
    [SerializeField] private float hoverRaiseAmount = 18f;

    [Header("Fallback World Card Placement")]
    [SerializeField] private Vector3 fallbackWorldCardsPosition = new Vector3(-1.42f, 1.153f, 0.135f);
    [SerializeField] private Vector3 fallbackFixedWorldPosition = new Vector3(-1.42f, 1.2f, 0f);

    private const string RuntimeRootName = "RuntimeCardSystem";
    private const string HandCanvasName = "CardCanvas";
    private const string HandRootName = "HandRoot";
    private const string WorldCardsName = "WorldCards";
    private const string CameraRigName = "CameraCardRig";

    private void Start()
    {
        if (buildOnStart)
        {
            BuildOrRefresh();
        }
    }

    [ContextMenu("Build Or Refresh Card System")]
    public void BuildOrRefresh()
    {
        ResolveReferences();

        if (targetCamera == null || handCardPrefab == null || worldCardPrefab == null)
        {
            Debug.LogWarning("GameSceneCardBootstrap: Missing required references.", this);
            return;
        }

        Transform runtimeRoot = GetOrCreateRuntimeRoot();
        Transform cameraRig = GetOrCreateCameraRig(runtimeRoot);
        ConfigureCameraRigFollower(cameraRig);

        if (rebuildIfAlreadyBuilt)
        {
            ClearRuntimeRoot(runtimeRoot, CameraRigName);
            ClearRuntimeRoot(cameraRig);
            ConfigureCameraRigFollower(cameraRig);
        }

        Transform canvasTransform = FindChild(cameraRig, HandCanvasName);
        if (canvasTransform == null)
        {
            canvasTransform = CreateHandCanvas(cameraRig).transform;
        }
        else
        {
            ConfigureHandCanvas(canvasTransform.gameObject);
        }

        RectTransform handRoot = FindChild(canvasTransform, HandRootName) as RectTransform;
        if (handRoot == null)
        {
            handRoot = CreateHandRoot(canvasTransform).GetComponent<RectTransform>();
        }
        else
        {
            ConfigureHandRoot(handRoot);
        }

        CardHandLayout handLayout = handRoot.GetComponent<CardHandLayout>();
        if (handLayout == null)
        {
            handLayout = handRoot.gameObject.AddComponent<CardHandLayout>();
        }
        ApplyLayoutSettings(handLayout, handRoot);

        CardHandDemo handDemo = handRoot.GetComponent<CardHandDemo>();
        if (handDemo == null)
        {
            handDemo = handRoot.gameObject.AddComponent<CardHandDemo>();
        }
        ApplyDemoSettings(handDemo, handRoot);
        ClearExistingHandCards(handRoot);
        handDemo.RebuildHand();

        if (worldCardsParent == null)
        {
            worldCardsParent = FindChild(runtimeRoot, WorldCardsName);
            if (worldCardsParent == null)
            {
                worldCardsParent = CreateWorldCardsRoot(runtimeRoot).transform;
            }
        }

        ConfigureCameraVisibility(cameraRig, canvasTransform.gameObject, worldCardsParent != null ? worldCardsParent.gameObject : null);

        RetargetExistingDragHandlers(handRoot);
    }

    private void ResolveReferences()
    {
        if (eventCamera == null)
        {
            eventCamera = ResolveEventCamera();
        }

        if (targetCamera != null && IsFollowCamera(targetCamera))
        {
            return;
        }

        if (CameraManager.Instance != null &&
            CameraManager.Instance.TryGetCameraItem(followCameraName, out CameraItem cameraItem) &&
            cameraItem != null &&
            cameraItem.camera != null)
        {
            targetCamera = cameraItem.camera;
            return;
        }

        CinemachineVirtualCamera[] virtualCameras = UnityEngine.Object.FindObjectsOfType<CinemachineVirtualCamera>(true);
        for (int i = 0; i < virtualCameras.Length; i++)
        {
            CinemachineVirtualCamera virtualCamera = virtualCameras[i];
            if (virtualCamera != null && IsFollowCamera(virtualCamera))
            {
                targetCamera = virtualCamera;
                return;
            }
        }
    }

    private bool IsFollowCamera(CinemachineVirtualCamera virtualCamera)
    {
        return virtualCamera != null &&
               !string.IsNullOrEmpty(followCameraName) &&
               virtualCamera.name.Contains(followCameraName);
    }

    private Transform GetOrCreateRuntimeRoot()
    {
        Transform existing = transform.Find(RuntimeRootName);
        if (existing != null)
        {
            return existing;
        }

        GameObject root = new GameObject(RuntimeRootName);
        root.transform.SetParent(transform, false);
        return root.transform;
    }

    private Transform GetOrCreateCameraRig(Transform runtimeRoot)
    {
        Transform existing = FindChild(runtimeRoot, CameraRigName);
        if (existing != null)
        {
            return existing;
        }

        if (targetCamera != null)
        {
            existing = targetCamera.transform.Find(CameraRigName);
            if (existing != null)
            {
                existing.SetParent(runtimeRoot, true);
                return existing;
            }
        }

        GameObject rig = new GameObject(CameraRigName);
        rig.transform.SetParent(runtimeRoot, true);
        if (targetCamera != null)
        {
            rig.transform.position = targetCamera.transform.position;
            rig.transform.rotation = targetCamera.transform.rotation;
        }
        rig.transform.localScale = Vector3.one;
        return rig.transform;
    }

    private void ConfigureCameraRigFollower(Transform cameraRig)
    {
        if (cameraRig == null || targetCamera == null)
        {
            return;
        }

        CardCameraRigFollower follower = cameraRig.GetComponent<CardCameraRigFollower>();
        if (follower == null)
        {
            follower = cameraRig.gameObject.AddComponent<CardCameraRigFollower>();
        }

        follower.Configure(
            targetCamera,
            cameraRigPositionOffset,
            cameraRigEulerOffset,
            followCameraRotation);
    }

    private static void ClearRuntimeRoot(Transform runtimeRoot, string preservedChildName = null)
    {
        for (int i = runtimeRoot.childCount - 1; i >= 0; i--)
        {
            GameObject child = runtimeRoot.GetChild(i).gameObject;
            if (!string.IsNullOrEmpty(preservedChildName) && child.name == preservedChildName)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(child);
            }
            else
            {
                Object.DestroyImmediate(child);
            }
        }
    }

    private GameObject CreateHandCanvas(Transform runtimeRoot)
    {
        GameObject canvasObject = new GameObject(
            HandCanvasName,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        canvasObject.transform.SetParent(runtimeRoot, false);

        ConfigureHandCanvas(canvasObject);

        return canvasObject;
    }

    private void ConfigureHandCanvas(GameObject canvasObject)
    {
        RectTransform rectTransform = canvasObject.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.localPosition = handCanvasLocalPosition;
            rectTransform.localEulerAngles = handCanvasLocalEuler;
            rectTransform.localScale = handCanvasLocalScale;
            rectTransform.sizeDelta = handCanvasSize;
        }

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = eventCamera != null ? eventCamera : ResolveEventCamera();
        }

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.dynamicPixelsPerUnit = 1f;
        }
    }

    private static Camera ResolveEventCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            return mainCamera;
        }

        Camera[] cameras = UnityEngine.Object.FindObjectsOfType<Camera>(true);
        for (int i = 0; i < cameras.Length; i++)
        {
            Camera camera = cameras[i];
            if (camera != null && camera.name == "Main Camera")
            {
                return camera;
            }
        }

        for (int i = 0; i < cameras.Length; i++)
        {
            Camera camera = cameras[i];
            if (camera != null && camera.isActiveAndEnabled)
            {
                return camera;
            }
        }

        return cameras.Length > 0 ? cameras[0] : null;
    }

    private GameObject CreateHandRoot(Transform canvasTransform)
    {
        GameObject handRootObject = new GameObject(
            HandRootName,
            typeof(RectTransform),
            typeof(CardHandDemo),
            typeof(CardHandLayout));

        handRootObject.transform.SetParent(canvasTransform, false);

        ConfigureHandRoot(handRootObject.GetComponent<RectTransform>());

        return handRootObject;
    }

    private void ConfigureHandRoot(RectTransform rectTransform)
    {
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = handRootAnchoredPosition;
        rectTransform.sizeDelta = handRootSize;
    }

    private GameObject CreateWorldCardsRoot(Transform runtimeRoot)
    {
        GameObject worldCardsObject = new GameObject(WorldCardsName);
        worldCardsObject.transform.SetParent(runtimeRoot, false);

        if (fixedTableAnchor != null)
        {
            worldCardsObject.transform.position = fixedTableAnchor.position;
            worldCardsObject.transform.rotation = fixedTableAnchor.rotation;
        }
        else
        {
            worldCardsObject.transform.position = fallbackWorldCardsPosition;
        }

        return worldCardsObject;
    }

    private void ApplyLayoutSettings(CardHandLayout layout, RectTransform handRoot)
    {
        layout.Configure(
            handRoot,
            preferredCardSpacing,
            minimumCardSpacing,
            horizontalPadding,
            arcHeight,
            maxRotation,
            animationSpeed,
            hoverPushDistance,
            hoverRaiseAmount);
    }

    private void ApplyDemoSettings(CardHandDemo demo, RectTransform handRoot)
    {
        demo.Configure(handCardPrefab, handRoot, demoCards, true);
    }

    private void RetargetExistingDragHandlers(RectTransform handRoot)
    {
        CardDragPlayHandler[] dragHandlers = handRoot.GetComponentsInChildren<CardDragPlayHandler>(true);

        for (int i = 0; i < dragHandlers.Length; i++)
        {
            CardDragPlayHandler handler = dragHandlers[i];
            handler.ConfigureTablePlacement(
                handCardPrefab,
                worldCardPrefab,
                worldCardsParent != null ? worldCardsParent.gameObject : null,
                fixedTableAnchor != null ? fixedTableAnchor.gameObject : null,
                fixedTableAnchor != null ? fixedTableAnchor.position : fallbackFixedWorldPosition,
                false,
                0.003f);
        }
    }

    private void ConfigureCameraVisibility(Transform cameraRig, GameObject handCanvasObject, GameObject worldCardsObject)
    {
        CameraModeVisibilityController controller = cameraRig.GetComponent<CameraModeVisibilityController>();
        if (controller == null)
        {
            controller = cameraRig.gameObject.AddComponent<CameraModeVisibilityController>();
        }

        List<GameObject> targets = new List<GameObject> { handCanvasObject };
        if (worldCardsObject != null)
        {
            targets.Add(worldCardsObject);
        }

        controller.Configure(targets, new List<string> { visibleCameraName }, showCardsOnlyInTopBehind);
    }

    private static void ClearExistingHandCards(RectTransform handRoot)
    {
        for (int i = handRoot.childCount - 1; i >= 0; i--)
        {
            GameObject child = handRoot.GetChild(i).gameObject;
            if (child.GetComponent<CardView>() == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(child);
            }
            else
            {
                Object.DestroyImmediate(child);
            }
        }
    }

    private static Transform FindChild(Transform parent, string childName)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName)
            {
                return child;
            }
        }

        return null;
    }

}
