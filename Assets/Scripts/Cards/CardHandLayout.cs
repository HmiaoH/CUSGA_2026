using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 用于把一组卡牌排成轻微扇形，增强手牌区的视觉层次。
/// 这个布局脚本只负责位置与旋转，不负责生成与销毁。
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class CardHandLayout : MonoBehaviour
{
    [SerializeField] private RectTransform layoutRect;
    [SerializeField] private float preferredCardSpacing = 170f;
    [SerializeField] private float minimumCardSpacing = 68f;
    [SerializeField] private float horizontalPadding = 48f;
    [SerializeField] private float arcHeight = 35f;
    [SerializeField] private float maxRotation = 10f;
    [SerializeField] private float animationSpeed = 12f;
    [SerializeField] private float hoverPushDistance = 96f;
    [SerializeField] private float hoverRaiseAmount = 18f;

    private readonly List<RectTransform> cachedCards = new List<RectTransform>();

    public void Configure(
        RectTransform targetLayoutRect,
        float targetPreferredCardSpacing,
        float targetMinimumCardSpacing,
        float targetHorizontalPadding,
        float targetArcHeight,
        float targetMaxRotation,
        float targetAnimationSpeed,
        float targetHoverPushDistance,
        float targetHoverRaiseAmount)
    {
        layoutRect = targetLayoutRect;
        preferredCardSpacing = targetPreferredCardSpacing;
        minimumCardSpacing = targetMinimumCardSpacing;
        horizontalPadding = targetHorizontalPadding;
        arcHeight = targetArcHeight;
        maxRotation = targetMaxRotation;
        animationSpeed = targetAnimationSpeed;
        hoverPushDistance = targetHoverPushDistance;
        hoverRaiseAmount = targetHoverRaiseAmount;
    }

    private void Awake()
    {
        if (layoutRect == null)
        {
            layoutRect = transform as RectTransform;
        }
    }

    private void Reset()
    {
        layoutRect = transform as RectTransform;
    }

    private void LateUpdate()
    {
        RebuildCardCache();
        ApplyLayout();
    }

    private void RebuildCardCache()
    {
        cachedCards.Clear();

        for (var i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i) as RectTransform;

            if (child != null && child.gameObject.activeSelf)
            {
                // 拖拽中的卡牌交给拖拽系统控制位置，避免布局脚本抢控制权。
                var dragHandler = child.GetComponent<CardDragPlayHandler>();
                if (dragHandler != null && (dragHandler.IsDragging || dragHandler.IsConsumed))
                {
                    continue;
                }

                cachedCards.Add(child);
            }
        }
    }

    private void ApplyLayout()
    {
        if (cachedCards.Count == 0)
        {
            return;
        }

        if (layoutRect == null)
        {
            layoutRect = transform as RectTransform;
        }

        var effectiveSpacing = GetEffectiveSpacing();
        var centerOffset = (cachedCards.Count - 1) * 0.5f;
        var hoveredIndex = GetHoveredCardIndex();

        for (var i = 0; i < cachedCards.Count; i++)
        {
            var card = cachedCards[i];
            var offsetFromCenter = i - centerOffset;
            var normalizedOffset = centerOffset <= 0f ? 0f : offsetFromCenter / centerOffset;
            var pushOffset = GetHoverPushOffset(i, hoveredIndex);
            var raiseOffset = i == hoveredIndex ? hoverRaiseAmount : 0f;

            var targetPosition = new Vector3(
                offsetFromCenter * effectiveSpacing + pushOffset,
                -Mathf.Abs(normalizedOffset) * arcHeight + raiseOffset,
                0f);
            var targetRotation = Quaternion.Euler(0f, 0f, i == hoveredIndex ? 0f : -normalizedOffset * maxRotation);

            card.localPosition = Vector3.Lerp(card.localPosition, targetPosition, Time.unscaledDeltaTime * animationSpeed);
            card.localRotation = Quaternion.Slerp(card.localRotation, targetRotation, Time.unscaledDeltaTime * animationSpeed);
        }
    }

    /// <summary>
    /// 根据容器宽度和当前手牌数量自动压缩间距，避免两侧卡牌跑出视野。
    /// </summary>
    private float GetEffectiveSpacing()
    {
        if (cachedCards.Count <= 1 || layoutRect == null)
        {
            return preferredCardSpacing;
        }

        var availableWidth = Mathf.Max(0f, layoutRect.rect.width - horizontalPadding * 2f);
        var cardWidth = GetReferenceCardWidth();
        var availableSpan = Mathf.Max(0f, availableWidth - cardWidth);
        var fitSpacing = availableSpan / (cachedCards.Count - 1);

        return Mathf.Clamp(fitSpacing, minimumCardSpacing, preferredCardSpacing);
    }

    /// <summary>
    /// 使用第一张卡的宽度作为排布参考值。
    /// </summary>
    private float GetReferenceCardWidth()
    {
        if (cachedCards.Count == 0)
        {
            return 0f;
        }

        return cachedCards[0].rect.width * cachedCards[0].localScale.x;
    }

    // ReSharper disable Unity.PerformanceAnalysis
    /// <summary>
    /// 找出当前正在悬停的卡牌索引。如果没有卡牌被悬停，返回 -1。
    /// </summary>
    private int GetHoveredCardIndex()
    {
        for (var i = 0; i < cachedCards.Count; i++)
        {
            var hoverMotion = cachedCards[i].GetComponent<CardHoverMotion>();

            if (hoverMotion != null && hoverMotion.IsHovered)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// 根据卡牌与悬停牌的距离，给左右相邻卡一个平滑的让位偏移。
    /// </summary>
    private float GetHoverPushOffset(int cardIndex, int hoveredIndex)
    {
        if (hoveredIndex < 0 || cardIndex == hoveredIndex)
        {
            return 0f;
        }

        var distance = Mathf.Abs(cardIndex - hoveredIndex);
        var direction = cardIndex < hoveredIndex ? -1f : 1f;
        var normalizedStrength = Mathf.Clamp01(1f - (distance - 1) * 0.35f);

        return direction * hoverPushDistance * normalizedStrength;
    }
}
