using System.Collections;
using System.Text;
using Gameplay;
using Managers;
using UnityEngine;

/// <summary>
/// 处理卡牌拖拽出牌的基础行为：
/// 1. 拖到屏幕上半区域后松手 -> 视为使用，打印调试信息并让卡牌消失。
/// 2. 未到屏幕上半区域松手 -> 取消使用，卡牌回到手牌区（由布局脚本接管回位）。
/// </summary>
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CardView))]
public class CardDragPlayHandler : MonoBehaviour
{
    [Header("拖拽设置")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private float topHalfThreshold = 0.5f;
    [SerializeField] private bool hideCardOnUse = true;

    [Header("桌面落牌")]
    [SerializeField] private CardView tableCardPrefab;
    [SerializeField] private GameObject worldCardPrefab;
    [SerializeField] private GameObject worldCardParent;
    [SerializeField] private GameObject fixedTableAnchor;
    [SerializeField] private Vector3 fixedWorldPosition = new Vector3(0f, 0.75f, 0f);
    [SerializeField] private bool useAnchorRotation = true;
    [SerializeField] private Vector3 worldCardEuler = new Vector3(90f, 0f, 0f);
    [SerializeField] private float worldCardScale = 0.003f;

    [Header("打出动效")]
    [SerializeField] private bool playUseTravelAnimation = true;
    [SerializeField] private float useTravelDuration = 0.22f;
    [SerializeField] private float useTravelArcHeight = 0.3f;
    [SerializeField] private AnimationCurve useTravelEase;

    private RectTransform cardRect;
    private CardView cardView;
    private CardHoverMotion hoverMotion;
    private Vector3 dragWorldOffset;
    private float dragDepthToCamera;
    private bool isDragging;
    private bool isConsumed;
    private Camera dragCamera;
    private Coroutine useAnimationRoutine;

    /// <summary>
    /// 当前卡牌是否处于拖拽状态。布局脚本会用它跳过位置控制。
    /// </summary>
    public bool IsDragging => isDragging;

    /// <summary>
    /// 当前卡牌是否已被使用并标记为消失。
    /// </summary>
    public bool IsConsumed => isConsumed;

    public void ConfigureTablePlacement(
        CardView tablePrefab,
        GameObject cardPrefab,
        GameObject cardParent,
        GameObject tableAnchor,
        Vector3 fallbackWorldPosition,
        bool useTravelAnimation,
        float tableCardScale)
    {
        tableCardPrefab = tablePrefab;
        worldCardPrefab = cardPrefab;
        worldCardParent = cardParent;
        fixedTableAnchor = tableAnchor;
        fixedWorldPosition = fallbackWorldPosition;
        playUseTravelAnimation = useTravelAnimation;
        worldCardScale = tableCardScale;
    }

    private void Awake()
    {
        cardRect = transform as RectTransform;
        cardView = GetComponent<CardView>();
        hoverMotion = GetComponent<CardHoverMotion>();
        ResolveCanvasReference();
        EnsureMouseCollider();

        if (useTravelEase == null || useTravelEase.length == 0)
        {
            useTravelEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        }
    }

    private void Reset()
    {
        cardRect = transform as RectTransform;
        EnsureMouseCollider();
    }

    /// <summary>
    /// 使用 OnMouseDown 作为拖拽起点。
    /// </summary>
    private void OnMouseDown()
    {
        if (isConsumed || cardRect == null)
        {
            return;
        }

        ResolveCanvasReference();
        if (dragCamera == null)
        {
            Debug.LogWarning("CardDragPlayHandler: No camera found for drag ray projection.", this);
            return;
        }

        isDragging = true;

        if (hoverMotion != null)
        {
            hoverMotion.SetDragLock(true);
        }

        // 拖拽时提到最上层，防止被同级卡牌遮挡。
        cardRect.SetAsLastSibling();

        dragDepthToCamera = dragCamera.WorldToScreenPoint(cardRect.position).z;
        var pointerWorld = ScreenToWorldOnCardDepth(Input.mousePosition);
        dragWorldOffset = cardRect.position - pointerWorld;
    }

    /// <summary>
    /// 鼠标按住并移动时，卡牌跟随鼠标。
    /// </summary>
    private void OnMouseDrag()
    {
        if (!isDragging || isConsumed || cardRect == null)
        {
            return;
        }

        var pointerWorld = ScreenToWorldOnCardDepth(Input.mousePosition);
        cardRect.position = pointerWorld + dragWorldOffset;
    }

    /// <summary>
    /// 鼠标抬起时结束拖拽并进行“上半屏使用”判定。
    /// </summary>
    private void OnMouseUp()
    {
        if (!isDragging)
        {
            return;
        }

        isDragging = false;

        if (ShouldUseCard(Input.mousePosition.y))
        {
            if (UseCard())
            {
                return;
            }
        }
        // 未达到使用条件时，不做强制回弹。
        // CardHandLayout 会在下一帧重新接管该卡位置并平滑回位。

        if (hoverMotion != null)
        {
            hoverMotion.SetDragLock(false);
        }
    }

    /// <summary>
    /// 通过鼠标松手时的屏幕 Y 坐标判断是否达到“上半屏”使用条件。
    /// </summary>
    private bool ShouldUseCard(float pointerScreenY)
    {
        return pointerScreenY >= Screen.height * Mathf.Clamp01(topHalfThreshold);
    }

    /// <summary>
    /// 执行出牌：上半屏即成功，直接生成桌面固定位置的场景牌。
    /// </summary>
    private bool UseCard()
    {
        var definition = cardView != null ? cardView.BoundDefinition : null;
        if (definition == null)
        {
            Debug.LogWarning("CardDragPlayHandler: Cannot use card because CardDefinition is null.", this);
            return false;
        }

        if (tableCardPrefab == null && worldCardPrefab == null)
        {
            Debug.LogWarning("CardDragPlayHandler: No table/world card prefab is assigned.", this);
            return false;
        }

        var worldPosition = ResolveFixedWorldPosition();
        var worldRotation = ResolveFixedWorldRotation();

        isConsumed = true;
        SetMouseColliderEnabled(false);
        TriggerBoardPreview(definition);

        var builder = new StringBuilder();
        builder.AppendLine("=== Card Used (Debug) ===");
        builder.AppendLine($"Id: {definition.CardId}");
        builder.AppendLine($"Name: {definition.DisplayName}");
        builder.AppendLine($"Category: {definition.Category}");
        builder.AppendLine($"Cost: {definition.Cost}");
        builder.AppendLine($"Damage: {definition.Damage}");
        builder.AppendLine($"HitCount: {definition.HitCount}");
        builder.AppendLine($"Move: {definition.MovementAmount} ({definition.MovementPattern})");
        builder.AppendLine($"Draw: {definition.CardDraw}");
        builder.AppendLine($"VitalityGain: {definition.VitalityGain}");
        builder.AppendLine($"EnergyGain: {definition.EnergyGain}");
        builder.AppendLine($"Keywords: {definition.Keywords}");
        builder.AppendLine($"WorldPos: {worldPosition}");
        Debug.Log(builder.ToString(), this);

        if (useAnimationRoutine != null)
        {
            StopCoroutine(useAnimationRoutine);
            useAnimationRoutine = null;
        }

        if (playUseTravelAnimation && useTravelDuration > 0.01f)
        {
            useAnimationRoutine = StartCoroutine(PlayUseAnimationAndFinalize(definition, worldPosition, worldRotation));
        }
        else
        {
            FinalizeUse(definition, worldPosition, worldRotation);
        }

        return true;
    }

    /// <summary>
    /// 根据场景层级自动定位拖拽参考坐标系。
    /// </summary>
    private void ResolveCanvasReference()
    {
        Canvas[] parentCanvases = GetComponentsInParent<Canvas>(true);
        Canvas bestCanvas = null;

        for (int i = 0; i < parentCanvases.Length; i++)
        {
            Canvas canvas = parentCanvases[i];
            if (canvas == null)
            {
                continue;
            }

            if (bestCanvas == null)
            {
                bestCanvas = canvas;
            }

            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay && canvas.worldCamera != null)
            {
                bestCanvas = canvas;
                break;
            }
        }

        if (bestCanvas != null)
        {
            targetCanvas = bestCanvas;
        }
        else if (targetCanvas == null)
        {
            targetCanvas = GetComponentInParent<Canvas>();
        }

        dragCamera = null;
        if (targetCanvas != null && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            dragCamera = targetCanvas.worldCamera != null ? targetCanvas.worldCamera : ResolveFallbackCamera();
        }

        if (dragCamera == null)
        {
            dragCamera = ResolveFallbackCamera();
        }
    }

    private Camera ResolveFallbackCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            return mainCamera;
        }

        Camera namedMainCamera = FindCameraByName("Main Camera");
        if (namedMainCamera != null)
        {
            return namedMainCamera;
        }

        Camera[] cameras = UnityEngine.Object.FindObjectsOfType<Camera>(true);
        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i] != null && cameras[i].isActiveAndEnabled)
            {
                return cameras[i];
            }
        }

        return cameras.Length > 0 ? cameras[0] : null;
    }

    private static Camera FindCameraByName(string cameraName)
    {
        Camera[] cameras = UnityEngine.Object.FindObjectsOfType<Camera>(true);
        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i] != null && cameras[i].name == cameraName)
            {
                return cameras[i];
            }
        }

        return null;
    }

    /// <summary>
    /// OnMouse 系列事件依赖 Collider，自动补一个 BoxCollider 以减少手动配置。
    /// </summary>
    private void EnsureMouseCollider()
    {
        if (GetComponent<Collider>() != null)
        {
            return;
        }

        var boxCollider = gameObject.AddComponent<BoxCollider>();

        if (cardRect == null)
        {
            cardRect = transform as RectTransform;
        }

        var size = Vector3.one;
        if (cardRect != null)
        {
            size = new Vector3(Mathf.Max(0.1f, cardRect.rect.width), Mathf.Max(0.1f, cardRect.rect.height), 0.1f);
        }

        boxCollider.size = size;
        boxCollider.center = Vector3.zero;
    }

    /// <summary>
    /// 把屏幕坐标投影到“卡牌当前深度”的世界位置。
    /// 使用稳定的世界坐标拖拽，减少 UI 本地坐标换算导致的抖动。
    /// </summary>
    private Vector3 ScreenToWorldOnCardDepth(Vector3 mouseScreenPosition)
    {
        if (dragCamera == null)
        {
            return cardRect != null ? cardRect.position : Vector3.zero;
        }

        mouseScreenPosition.z = Mathf.Max(0.01f, dragDepthToCamera);
        return dragCamera.ScreenToWorldPoint(mouseScreenPosition);
    }

    /// <summary>
    /// 解析固定落牌位置。优先使用锚点位置，否则使用手工配置的位置。
    /// </summary>
    private Vector3 ResolveFixedWorldPosition()
    {
        if (fixedTableAnchor != null)
        {
            return fixedTableAnchor.transform.position;
        }

        return fixedWorldPosition;
    }

    /// <summary>
    /// 解析固定落牌旋转。可选跟随锚点旋转，否则使用配置欧拉角。
    /// </summary>
    private Quaternion ResolveFixedWorldRotation()
    {
        if (useAnchorRotation && fixedTableAnchor != null)
        {
            return fixedTableAnchor.transform.rotation;
        }

        return Quaternion.Euler(worldCardEuler);
    }

    /// <summary>
    /// 生成场景中的桌面牌，并把当前 CardDefinition 同步给它。
    /// </summary>
    private bool TrySpawnWorldCard(CardDefinition definition, Vector3 worldPosition, Quaternion worldRotation)
    {
        if (tableCardPrefab != null)
        {
            return TrySpawnTableCard(definition, worldPosition, worldRotation);
        }

        GameObject worldCard;

        if (fixedTableAnchor != null)
        {
            worldCard = Instantiate(worldCardPrefab, fixedTableAnchor.transform, false);
            worldCard.transform.localPosition = Vector3.zero;

            if (useAnchorRotation)
            {
                worldCard.transform.localRotation = Quaternion.identity;
            }
            else
            {
                worldCard.transform.rotation = worldRotation;
            }
        }
        else
        {
            worldCard = Instantiate(worldCardPrefab, worldPosition, worldRotation);

            if (worldCardParent != null)
            {
                worldCard.transform.SetParent(worldCardParent.transform, true);
                worldCard.transform.position = worldPosition;
                worldCard.transform.rotation = worldRotation;
            }
        }

        // 统一把桌面牌缩放到可控尺寸，避免不同 prefab 默认比例导致“打出后过大”。
        // 这里使用本地缩放，便于配合 worldCardParent 的层级管理。
        worldCard.transform.localScale = Vector3.one * Mathf.Max(0.0001f, worldCardScale);

        var worldCardView = worldCard.GetComponent<WorldCardView>();
        if (worldCardView != null)
        {
            worldCardView.Bind(definition);
        }

        return true;
    }

    private bool TrySpawnTableCard(CardDefinition definition, Vector3 worldPosition, Quaternion worldRotation)
    {
        Transform parentTransform = fixedTableAnchor != null ? fixedTableAnchor.transform : null;
        CardView spawnedCard = parentTransform != null
            ? Instantiate(tableCardPrefab, parentTransform, false)
            : Instantiate(tableCardPrefab, worldPosition, worldRotation);

        Transform spawnedTransform = spawnedCard.transform;

        if (parentTransform != null)
        {
            spawnedTransform.localPosition = Vector3.zero;
            if (useAnchorRotation)
            {
                spawnedTransform.localRotation = Quaternion.identity;
            }
            else
            {
                spawnedTransform.rotation = worldRotation;
            }
        }
        else
        {
            spawnedTransform.position = worldPosition;
            spawnedTransform.rotation = worldRotation;
        }

        spawnedTransform.localScale = Vector3.one * Mathf.Max(0.0001f, worldCardScale);
        spawnedCard.Bind(definition);
        spawnedCard.SetInteractable(false);

        CardDragPlayHandler dragHandler = spawnedCard.GetComponent<CardDragPlayHandler>();
        if (dragHandler != null)
        {
            dragHandler.enabled = false;
        }

        CardHoverMotion hoverMotionComponent = spawnedCard.GetComponent<CardHoverMotion>();
        if (hoverMotionComponent != null)
        {
            hoverMotionComponent.enabled = false;
        }

        Collider colliderComponent = spawnedCard.GetComponent<Collider>();
        if (colliderComponent != null)
        {
            colliderComponent.enabled = false;
        }

        return true;
    }

    /// <summary>
    /// 播放“打出飞向桌面”的动效，再执行最终落牌与手牌隐藏。
    /// </summary>
    private IEnumerator PlayUseAnimationAndFinalize(CardDefinition definition, Vector3 targetPosition, Quaternion targetRotation)
    {
        var startPosition = cardRect != null ? cardRect.position : transform.position;
        var startRotation = cardRect != null ? cardRect.rotation : transform.rotation;
        var duration = Mathf.Max(0.01f, useTravelDuration);
        var timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            var normalized = Mathf.Clamp01(timer / duration);
            var eased = useTravelEase != null ? useTravelEase.Evaluate(normalized) : normalized;

            var position = Vector3.Lerp(startPosition, targetPosition, eased);
            var arcOffset = 4f * useTravelArcHeight * eased * (1f - eased);
            position += Vector3.up * arcOffset;

            if (cardRect != null)
            {
                cardRect.position = position;
                cardRect.rotation = Quaternion.Slerp(startRotation, targetRotation, eased);
            }
            else
            {
                transform.position = position;
                transform.rotation = Quaternion.Slerp(startRotation, targetRotation, eased);
            }

            yield return null;
        }

        FinalizeUse(definition, targetPosition, targetRotation);
        useAnimationRoutine = null;
    }

    /// <summary>
    /// 动效结束后执行最终逻辑：生成桌面牌并处理手牌显示状态。
    /// </summary>
    private void FinalizeUse(CardDefinition definition, Vector3 worldPosition, Quaternion worldRotation)
    {
        TrySpawnWorldCard(definition, worldPosition, worldRotation);

        if (EventManager.Instance != null)
        {
            EventManager.Instance.Dispatch(CardEventNames.CardPlayedToBoard, definition);
        }

        if (hideCardOnUse)
        {
            gameObject.SetActive(false);
            return;
        }

        if (hoverMotion != null)
        {
            hoverMotion.SetDragLock(false);
        }

        SetMouseColliderEnabled(true);
    }

    /// <summary>
    /// 开关鼠标碰撞体，避免打出动效过程中重复触发拖拽事件。
    /// </summary>
    private void SetMouseColliderEnabled(bool enabled)
    {
        var colliderComponent = GetComponent<Collider>();
        if (colliderComponent != null)
        {
            colliderComponent.enabled = enabled;
        }
    }

    private void TriggerBoardPreview(CardDefinition definition)
    {
        if (definition == null)
        {
            return;
        }

        ChessboardController chessboard = FindObjectOfType<ChessboardController>();
        if (chessboard == null)
        {
            return;
        }

        chessboard.BeginCardActionPreview(definition);
    }
}
