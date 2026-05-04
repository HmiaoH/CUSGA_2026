using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 卡牌所属的大类，用于区分技能、伤害、移动三种基础体系。
/// </summary>
public enum CardCategory
{
    Skill = 0,
    Damage = 1,
    Move = 2
}

/// <summary>
/// 卡牌稀有度，当前与设计文档中的“初始 / 普通 / 稀有”保持一致。
/// </summary>
public enum CardRarity
{
    Starter = 0,
    Common = 1,
    Rare = 2
}

/// <summary>
/// 卡牌的核心定位，用于在 UI 中快速标记这张牌偏向哪类行为。
/// </summary>
public enum CardPrimaryEffect
{
    Utility = 0,
    Draw = 1,
    Damage = 2,
    MultiHit = 3,
    Movement = 4,
    Charge = 5,
    Control = 6,
    Buff = 7
}

/// <summary>
/// 卡牌标签。这里先覆盖当前设计文档中最核心的一批关键词。
/// </summary>
[Flags]
public enum CardKeyword
{
    None = 0,
    Charge = 1 << 0,
    Vitality = 1 << 1,
    Rewind = 1 << 2,
    Knockback = 1 << 3,
    Swap = 1 << 4,
    StraightMove = 1 << 5,
    FreeMove = 1 << 6,
    Bind = 1 << 7,
    Blind = 1 << 8
}

/// <summary>
/// 目标模式只做基础分类，方便后续技能逻辑和鼠标指示器扩展。
/// </summary>
public enum CardTargetingMode
{
    None = 0,
    Self = 1,
    Tile = 2,
    Enemy = 3,
    Any = 4
}

/// <summary>
/// 移动路径规则，用于区分直线与任意移动。
/// </summary>
public enum MovementPattern
{
    None = 0,
    Straight = 1,
    Free = 2,
    Rewind = 3,
    Swap = 4
}

/// <summary>
/// 当前项目的基础卡牌定义。
/// 这里保留 ScriptableObject 作为静态数据源，方便后续做掉落、奖励池和牌库构建。
/// </summary>
[CreateAssetMenu(menuName = "Game/Card Definition", fileName = "CardDefinition")]
public class CardDefinition : ScriptableObject
{
    [Header("基础信息")]
    [SerializeField] private string cardId;
    [SerializeField] private string displayName;
    [SerializeField] [TextArea(2, 4)] private string rulesText;
    [SerializeField] [TextArea(1, 3)] private string flavorText;
    [SerializeField] private CardCategory category;
    [SerializeField] private CardRarity rarity;
    [SerializeField] private CardPrimaryEffect primaryEffect;
    [SerializeField] private CardTargetingMode targetingMode = CardTargetingMode.None;
    [SerializeField] private CardKeyword keywords = CardKeyword.None;

    [Header("费用与数值")]
    [SerializeField] private int cost;
    [SerializeField] private int damage;
    [SerializeField] private int hitCount = 1;
    [SerializeField] private int cardDraw;
    [SerializeField] private int vitalityGain;
    [SerializeField] private int energyGain;
    [SerializeField] private int knockbackDistance;
    [SerializeField] private int movementAmount;
    [SerializeField] private MovementPattern movementPattern = MovementPattern.None;

    [Header("表现资源")]
    [SerializeField] private Sprite artwork;
    [SerializeField] private Color accentColor = new Color(0.87f, 0.79f, 0.58f);

    /// <summary>
    /// 卡牌唯一编号，对应策划文档中的编号体系，例如 1.1.0。
    /// </summary>
    public string CardId => cardId;

    /// <summary>
    /// 卡牌显示名称。
    /// </summary>
    public string DisplayName => displayName;

    /// <summary>
    /// 规则文本，建议直接面向玩家展示。
    /// </summary>
    public string RulesText => rulesText;

    /// <summary>
    /// 风味文本，可选。
    /// </summary>
    public string FlavorText => flavorText;

    /// <summary>
    /// 卡牌类别。
    /// </summary>
    public CardCategory Category => category;

    /// <summary>
    /// 稀有度。
    /// </summary>
    public CardRarity Rarity => rarity;

    /// <summary>
    /// 核心表现定位。
    /// </summary>
    public CardPrimaryEffect PrimaryEffect => primaryEffect;

    /// <summary>
    /// 目标模式。
    /// </summary>
    public CardTargetingMode TargetingMode => targetingMode;

    /// <summary>
    /// 关键词标签位掩码。
    /// </summary>
    public CardKeyword Keywords => keywords;

    /// <summary>
    /// 费用。
    /// </summary>
    public int Cost => cost;

    /// <summary>
    /// 单段伤害值。
    /// </summary>
    public int Damage => damage;

    /// <summary>
    /// 伤害段数，多段牌用这个值配合活力系统。
    /// </summary>
    public int HitCount => Mathf.Max(1, hitCount);

    /// <summary>
    /// 直接抽牌数。
    /// </summary>
    public int CardDraw => cardDraw;

    /// <summary>
    /// 直接获得的活力数。
    /// </summary>
    public int VitalityGain => vitalityGain;

    /// <summary>
    /// 直接获得的费用数。
    /// </summary>
    public int EnergyGain => energyGain;

    /// <summary>
    /// 击退距离。
    /// </summary>
    public int KnockbackDistance => knockbackDistance;

    /// <summary>
    /// 位移格数。
    /// </summary>
    public int MovementAmount => movementAmount;

    /// <summary>
    /// 位移模式。
    /// </summary>
    public MovementPattern MovementPattern => movementPattern;

    /// <summary>
    /// 卡面插画。
    /// </summary>
    public Sprite Artwork => artwork;

    /// <summary>
    /// 卡面主色，用于 UI 边框和高光。
    /// </summary>
    public Color AccentColor => accentColor;

    /// <summary>
    /// 是否拥有指定关键词。
    /// </summary>
    public bool HasKeyword(CardKeyword keyword)
    {
        return (keywords & keyword) == keyword;
    }

    /// <summary>
    /// 将关键词展开为字符串列表，方便卡面组件直接显示。
    /// </summary>
    public IReadOnlyList<string> GetKeywordLabels()
    {
        if (keywords == CardKeyword.None)
        {
            return Array.Empty<string>();
        }

        var labels = new List<string>();

        foreach (CardKeyword keyword in Enum.GetValues(typeof(CardKeyword)))
        {
            if (keyword == CardKeyword.None || !HasKeyword(keyword))
            {
                continue;
            }

            labels.Add(GetKeywordDisplayName(keyword));
        }

        return labels;
    }

    /// <summary>
    /// 根据标签返回面向玩家的中文名称。
    /// </summary>
    public static string GetKeywordDisplayName(CardKeyword keyword)
    {
        switch (keyword)
        {
            case CardKeyword.Charge:
                return "冲锋";
            case CardKeyword.Vitality:
                return "活力";
            case CardKeyword.Rewind:
                return "回溯";
            case CardKeyword.Knockback:
                return "击退";
            case CardKeyword.Swap:
                return "换位";
            case CardKeyword.StraightMove:
                return "直线移动";
            case CardKeyword.FreeMove:
                return "任意移动";
            case CardKeyword.Bind:
                return "束缚";
            case CardKeyword.Blind:
                return "致盲";
            default:
                return keyword.ToString();
        }
    }
}
