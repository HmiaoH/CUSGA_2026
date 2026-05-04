using UnityEngine;

/// <summary>
/// 负责卡牌的基础交互动效。
/// 整体目标是做出偏“邪恶冥刻”的手感：悬停抬升、放大、鼠标倾斜、点击脉冲。
/// 如果同时使用 CardHandLayout，建议把 targetRect 指向卡牌内部的视觉子节点，
/// 这样根节点继续交给布局脚本控制，视觉节点再由本脚本做抬升和倾斜。
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class CardHoverMotion : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private RectTransform targetRect;
    [SerializeField] private CardView cardView;
    [SerializeField] private Canvas hoverCanvas;

    [Header("基础动画")]
    [SerializeField] private float hoverLift = 36f;
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private float followSpeed = 12f;

    [Header("倾斜效果")]
    [SerializeField] private float maxTiltX = 12f;
    [SerializeField] private float maxTiltY = 10f;

    [Header("点击反馈")]
    [SerializeField] private float pulseScale = 1.14f;
    [SerializeField] private float pulseDuration = 0.14f;
    [SerializeField] private int hoverSortingOrder = 20;

    private Vector3 baseLocalPosition;
    private Vector3 currentVelocity;
    private Vector3 targetPosition;
    private Vector3 targetScale = Vector3.one;
    private Quaternion targetRotation = Quaternion.identity;
    private bool isHovered;
    private float clickPulseTimer;
    private int defaultSortingOrder;
    private bool isDragLocked;

    private void Awake()
    {
        ResolveReferences();
        baseLocalPosition = targetRect.localPosition;
    }

    private void OnEnable()
    {
        ResolveReferences();
        baseLocalPosition = targetRect.localPosition;
        targetPosition = baseLocalPosition;
    }

    private void Reset()
    {
        ResolveReferences();
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    private void Update()
    {
        if (isDragLocked)
        {
            return;
        }

        UpdateTargetState();
        UpdateTransform();
        UpdateGlow();
    }

    /// <summary>
    /// 外部在“出牌成功”或“点击确认”时可以主动触发一次脉冲。
    /// </summary>
    public void PlayPulse()
    {
        clickPulseTimer = pulseDuration;
    }

    /// <summary>
    /// 使用 OnMouseOver 驱动悬停状态与倾斜计算。
    /// </summary>
    private void OnMouseOver()
    {
        if (isDragLocked)
        {
            return;
        }

        EnterHoverState();
        UpdateTiltByMousePosition();
    }

    /// <summary>
    /// 鼠标离开时恢复默认状态。
    /// </summary>
    private void OnMouseExit()
    {
        if (isDragLocked)
        {
            return;
        }

        ExitHoverState();
    }

    /// <summary>
    /// 拖拽过程中持续更新卡牌倾斜方向，增强“抓在手里”的反馈。
    /// </summary>
    private void OnMouseDrag()
    {
        if (isDragLocked)
        {
            return;
        }

        EnterHoverState();
        UpdateTiltByMousePosition();
    }

    /// <summary>
    /// 点击时触发一次脉冲反馈。
    /// </summary>
    private void OnMouseDown()
    {
        if (isDragLocked)
        {
            return;
        }

        PlayPulse();
    }

    /// <summary>
    /// 当前卡牌是否处于悬停状态，供手牌布局脚本决定是否要给这张牌让位。
    /// </summary>
    public bool IsHovered => isHovered;

    /// <summary>
    /// 强制退出悬停状态，常用于拖拽开始时清理残留的悬停动画与排序状态。
    /// </summary>
    public void CancelHoverState()
    {
        ExitHoverState();
    }

    /// <summary>
    /// 拖拽锁：拖拽期间暂停本脚本的插值更新，避免与拖拽位置控制互相抢写导致抖动。
    /// </summary>
    public void SetDragLock(bool locked)
    {
        isDragLocked = locked;

        if (isDragLocked)
        {
            ExitHoverState();
            currentVelocity = Vector3.zero;
        }
    }

    /// <summary>
    /// 每帧根据悬停状态更新目标状态。
    /// 当前不再做待机正弦浮动，只保留悬停时的明确抬升和放大。
    /// </summary>
    private void UpdateTargetState()
    {
        targetPosition = isHovered ? baseLocalPosition + Vector3.up * hoverLift : baseLocalPosition;
        targetScale = isHovered ? Vector3.one * hoverScale : Vector3.one;

        if (clickPulseTimer > 0f)
        {
            clickPulseTimer -= Time.unscaledDeltaTime;
        }
    }

    private void UpdateTransform()
    {
        targetRect.localPosition = Vector3.SmoothDamp(targetRect.localPosition, targetPosition, ref currentVelocity, 1f / Mathf.Max(1f, followSpeed));
        targetRect.localRotation = Quaternion.Slerp(targetRect.localRotation, targetRotation, Time.unscaledDeltaTime * followSpeed);

        var pulseT = clickPulseTimer > 0f ? 1f - clickPulseTimer / pulseDuration : 1f;
        var pulseValue = clickPulseTimer > 0f ? Mathf.Sin(pulseT * Mathf.PI) : 0f;
        var scaleTarget = targetScale * Mathf.Lerp(1f, pulseScale, pulseValue);

        targetRect.localScale = Vector3.Lerp(targetRect.localScale, scaleTarget, Time.unscaledDeltaTime * followSpeed);
    }

    private void UpdateGlow()
    {
        if (cardView == null || cardView.CardGlow == null)
        {
            return;
        }

        var glowColor = cardView.CardGlow.color;
        glowColor.a = Mathf.Lerp(glowColor.a, isHovered ? 0.55f : 0f, Time.unscaledDeltaTime * followSpeed);
        cardView.CardGlow.color = glowColor;
    }

    /// <summary>
    /// 自动查找常用引用，减少 prefab 首次搭建时的手动拖拽工作量。
    /// </summary>
    private void ResolveReferences()
    {
        if (cardView == null)
        {
            cardView = GetComponent<CardView>();
        }

        if (targetRect == null)
        {
            var motionRoot = transform.Find("MotionRoot") as RectTransform;
            targetRect = motionRoot != null ? motionRoot : transform as RectTransform;
        }

        if (hoverCanvas == null)
        {
            hoverCanvas = GetComponent<Canvas>();
        }

        if (hoverCanvas == null)
        {
            hoverCanvas = gameObject.AddComponent<Canvas>();
            hoverCanvas.overrideSorting = false;
        }

        defaultSortingOrder = hoverCanvas.sortingOrder;
    }

    /// <summary>
    /// 进入悬停状态时只做一次状态切换，避免 OnMouseOver 每帧重复写入。
    /// </summary>
    private void EnterHoverState()
    {
        if (isHovered)
        {
            return;
        }

        isHovered = true;
        targetScale = Vector3.one * hoverScale;
        targetPosition = baseLocalPosition + Vector3.up * hoverLift;
        SetHoverSorting(true);
    }

    /// <summary>
    /// 退出悬停状态并清理倾斜/排序。
    /// </summary>
    private void ExitHoverState()
    {
        isHovered = false;
        targetScale = Vector3.one;
        targetRotation = Quaternion.identity;
        targetPosition = baseLocalPosition;
        SetHoverSorting(false);
    }

    /// <summary>
    /// 基于当前鼠标屏幕坐标更新卡牌倾斜。
    /// </summary>
    private void UpdateTiltByMousePosition()
    {
        if (!isHovered || targetRect == null)
        {
            return;
        }

        var canvas = targetRect.GetComponentInParent<Canvas>();
        Camera eventCamera = null;

        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            eventCamera = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(targetRect, Input.mousePosition, eventCamera, out var localPoint))
        {
            return;
        }

        var normalizedX = Mathf.Clamp(localPoint.x / (targetRect.rect.width * 0.5f), -1f, 1f);
        var normalizedY = Mathf.Clamp(localPoint.y / (targetRect.rect.height * 0.5f), -1f, 1f);

        var tiltX = -normalizedY * maxTiltX;
        var tiltY = normalizedX * maxTiltY;
        targetRotation = Quaternion.Euler(tiltX, tiltY, 0f);
    }

    /// <summary>
    /// 把悬停中的卡牌临时提到更高的绘制层级，避免被左右相邻卡牌遮住。
    /// </summary>
    private void SetHoverSorting(bool hovered)
    {
        if (hoverCanvas == null)
        {
            return;
        }

        hoverCanvas.overrideSorting = hovered;
        hoverCanvas.sortingOrder = hovered ? hoverSortingOrder : defaultSortingOrder;
    }

    private void OnDisable()
    {
        SetHoverSorting(false);
    }
}
