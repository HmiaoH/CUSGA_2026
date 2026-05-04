using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 卡牌 UI 视图层。
/// 只负责把 CardDefinition 的静态数据渲染到 UGUI，不直接参与战斗结算。
/// </summary>
public class CardView : MonoBehaviour
{
    [Header("基础节点")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image cardBackground;
    [SerializeField] private Image cardBorder;
    [SerializeField] private Image cardArtwork;
    [SerializeField] private Image cardGlow;

    [Header("文字")]
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text typeText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text keywordText;
    [SerializeField] private TMP_Text idText;

    [Header("风格")]
    [SerializeField] private Color defaultBackgroundColor = new Color(0.16f, 0.13f, 0.1f, 0.98f);
    [SerializeField] private Color skillTint = new Color(0.41f, 0.63f, 0.93f, 1f);
    [SerializeField] private Color damageTint = new Color(0.86f, 0.34f, 0.27f, 1f);
    [SerializeField] private Color moveTint = new Color(0.31f, 0.76f, 0.54f, 1f);

    private CardDefinition boundDefinition;

    /// <summary>
    /// 当前绑定的数据，供外部读取。
    /// </summary>
    public CardDefinition BoundDefinition => boundDefinition;

    /// <summary>
    /// 当前卡牌的透明度控制。
    /// </summary>
    public CanvasGroup CanvasGroup => canvasGroup;

    /// <summary>
    /// 当前卡面的高光图片，供动效脚本驱动。
    /// </summary>
    public Image CardGlow => cardGlow;

    private void Reset()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    /// <summary>
    /// 将一张卡牌定义渲染到卡面。
    /// </summary>
    public void Bind(CardDefinition definition)
    {
        boundDefinition = definition;

        if (definition == null)
        {
            ClearView();
            return;
        }

        if (costText != null)
        {
            costText.text = definition.Cost.ToString();
        }

        if (titleText != null)
        {
            titleText.text = definition.DisplayName;
        }

        if (typeText != null)
        {
            typeText.text = GetCategoryLabel(definition.Category);
        }

        if (descriptionText != null)
        {
            descriptionText.text = BuildDescription(definition);
        }

        if (keywordText != null)
        {
            keywordText.text = BuildKeywordLine(definition.GetKeywordLabels());
        }

        if (idText != null)
        {
            idText.text = definition.CardId;
        }

        if (cardArtwork != null)
        {
            cardArtwork.sprite = definition.Artwork;
            cardArtwork.enabled = definition.Artwork != null;
        }

        var accent = GetCategoryColor(definition.Category, definition.AccentColor);

        if (cardBackground != null)
        {
            cardBackground.color = defaultBackgroundColor;
        }

        if (cardBorder != null)
        {
            cardBorder.color = accent;
        }

        if (cardGlow != null)
        {
            var glowColor = accent;
            glowColor.a = 0f;
            cardGlow.color = glowColor;
        }
    }

    /// <summary>
    /// 设置卡牌是否可交互。后续接手牌逻辑时可以直接复用。
    /// </summary>
    public void SetInteractable(bool interactable)
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.alpha = interactable ? 1f : 0.72f;
        canvasGroup.interactable = interactable;
        canvasGroup.blocksRaycasts = interactable;
    }

    private void ClearView()
    {
        if (costText != null)
        {
            costText.text = "-";
        }

        if (titleText != null)
        {
            titleText.text = "未绑定卡牌";
        }

        if (typeText != null)
        {
            typeText.text = string.Empty;
        }

        if (descriptionText != null)
        {
            descriptionText.text = string.Empty;
        }

        if (keywordText != null)
        {
            keywordText.text = string.Empty;
        }

        if (idText != null)
        {
            idText.text = string.Empty;
        }

        if (cardArtwork != null)
        {
            cardArtwork.sprite = null;
            cardArtwork.enabled = false;
        }
    }

    private static string GetCategoryLabel(CardCategory category)
    {
        switch (category)
        {
            case CardCategory.Skill:
                return "技能";
            case CardCategory.Damage:
                return "伤害";
            case CardCategory.Move:
                return "移动";
            default:
                return category.ToString();
        }
    }

    private Color GetCategoryColor(CardCategory category, Color fallback)
    {
        switch (category)
        {
            case CardCategory.Skill:
                return skillTint;
            case CardCategory.Damage:
                return damageTint;
            case CardCategory.Move:
                return moveTint;
            default:
                return fallback;
        }
    }

    private static string BuildKeywordLine(IReadOnlyList<string> keywords)
    {
        if (keywords == null || keywords.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(" / ", keywords);
    }

    private static string BuildDescription(CardDefinition definition)
    {
        if (!string.IsNullOrWhiteSpace(definition.RulesText))
        {
            return definition.RulesText;
        }

        var builder = new StringBuilder();

        if (definition.MovementAmount > 0)
        {
            builder.Append(definition.MovementPattern == MovementPattern.Straight ? "直线移动 " : "移动 ");
            builder.Append(definition.MovementAmount);
        }

        if (definition.Damage > 0)
        {
            if (builder.Length > 0)
            {
                builder.Append('\n');
            }

            if (definition.HitCount > 1)
            {
                builder.Append(definition.Damage);
                builder.Append(" x ");
                builder.Append(definition.HitCount);
                builder.Append(" 伤害");
            }
            else
            {
                builder.Append(definition.Damage);
                builder.Append(" 伤害");
            }
        }

        if (definition.CardDraw > 0)
        {
            if (builder.Length > 0)
            {
                builder.Append('\n');
            }

            builder.Append("抽 ");
            builder.Append(definition.CardDraw);
            builder.Append(" 张牌");
        }

        if (definition.VitalityGain > 0)
        {
            if (builder.Length > 0)
            {
                builder.Append('\n');
            }

            builder.Append("获得 ");
            builder.Append(definition.VitalityGain);
            builder.Append(" 点活力");
        }

        if (definition.EnergyGain > 0)
        {
            if (builder.Length > 0)
            {
                builder.Append('\n');
            }

            builder.Append("获得 ");
            builder.Append(definition.EnergyGain);
            builder.Append(" 点费用");
        }

        if (definition.KnockbackDistance > 0)
        {
            if (builder.Length > 0)
            {
                builder.Append('\n');
            }

            builder.Append("击退 ");
            builder.Append(definition.KnockbackDistance);
            builder.Append(" 格");
        }

        return builder.ToString();
    }
}
