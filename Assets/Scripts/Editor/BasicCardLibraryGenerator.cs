#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 用于快速生成一批基础卡牌资产，方便先搭原型和验证卡面表现。
/// </summary>
public static class BasicCardLibraryGenerator
{
    private const string OutputFolder = "Assets/_Assets/CardDefinitions/Generated";

    [MenuItem("Tools/Cards/Generate Basic Prototype Cards")]
    public static void GenerateBasicCards()
    {
        EnsureFolder(OutputFolder);

        CreateOrUpdateCard(
            "Card_0_1_0_QuickStudy",
            card =>
            {
                card.name = "Card_0_1_0_QuickStudy";
                SetSerializedFields(
                    card,
                    "0.1.0",
                    "快速检索",
                    "抽两张牌",
                    "你需要更多选择。",
                    CardCategory.Skill,
                    CardRarity.Starter,
                    CardPrimaryEffect.Draw,
                    CardTargetingMode.Self,
                    CardKeyword.None,
                    0,
                    0,
                    1,
                    2,
                    0,
                    0,
                    0,
                    0,
                    MovementPattern.None,
                    new Color(0.43f, 0.61f, 0.9f, 1f));
            });

        CreateOrUpdateCard(
            "Card_1_1_0_BluntStrike",
            card =>
            {
                card.name = "Card_1_1_0_BluntStrike";
                SetSerializedFields(
                    card,
                    "1.1.0",
                    "钝击",
                    "造成 3 点伤害",
                    "稳定，但并不优雅。",
                    CardCategory.Damage,
                    CardRarity.Starter,
                    CardPrimaryEffect.Damage,
                    CardTargetingMode.Enemy,
                    CardKeyword.None,
                    0,
                    3,
                    1,
                    0,
                    0,
                    0,
                    0,
                    0,
                    MovementPattern.None,
                    new Color(0.82f, 0.35f, 0.3f, 1f));
            });

        CreateOrUpdateCard(
            "Card_1_2_1_TwinSlash",
            card =>
            {
                card.name = "Card_1_2_1_TwinSlash";
                SetSerializedFields(
                    card,
                    "1.2.1",
                    "双斩",
                    "造成 3 x 2 段伤害",
                    "更适合活力被点燃的时候。",
                    CardCategory.Damage,
                    CardRarity.Common,
                    CardPrimaryEffect.MultiHit,
                    CardTargetingMode.Enemy,
                    CardKeyword.Vitality,
                    1,
                    3,
                    2,
                    0,
                    0,
                    0,
                    0,
                    0,
                    MovementPattern.None,
                    new Color(0.89f, 0.45f, 0.27f, 1f));
            });

        CreateOrUpdateCard(
            "Card_2_1_0_StepForward",
            card =>
            {
                card.name = "Card_2_1_0_StepForward";
                SetSerializedFields(
                    card,
                    "2.1.0",
                    "踏步前移",
                    "直线移动 1 格",
                    "所有激进的动作都始于这一步。",
                    CardCategory.Move,
                    CardRarity.Starter,
                    CardPrimaryEffect.Movement,
                    CardTargetingMode.Tile,
                    CardKeyword.StraightMove,
                    0,
                    0,
                    1,
                    0,
                    0,
                    0,
                    0,
                    1,
                    MovementPattern.Straight,
                    new Color(0.31f, 0.77f, 0.58f, 1f));
            });

        CreateOrUpdateCard(
            "Card_2_1_1_Lunge",
            card =>
            {
                card.name = "Card_2_1_1_Lunge";
                SetSerializedFields(
                    card,
                    "2.1.1",
                    "穿刺冲锋",
                    "直线移动 3 格\n若路径终点为敌人，则触发【冲锋】",
                    "速度本身就是一种武器。",
                    CardCategory.Move,
                    CardRarity.Starter,
                    CardPrimaryEffect.Charge,
                    CardTargetingMode.Tile,
                    CardKeyword.Charge | CardKeyword.StraightMove,
                    1,
                    0,
                    1,
                    0,
                    0,
                    0,
                    0,
                    3,
                    MovementPattern.Straight,
                    new Color(0.22f, 0.83f, 0.56f, 1f));
            });

        CreateOrUpdateCard(
            "Card_2_1_2_Sidestep",
            card =>
            {
                card.name = "Card_2_1_2_Sidestep";
                SetSerializedFields(
                    card,
                    "2.1.2",
                    "游移蓄势",
                    "任意移动 2 格，获得 1 点活力",
                    "它不像冲锋那样显眼，但会让后续的攻击更重。",
                    CardCategory.Move,
                    CardRarity.Starter,
                    CardPrimaryEffect.Buff,
                    CardTargetingMode.Tile,
                    CardKeyword.FreeMove | CardKeyword.Vitality,
                    1,
                    0,
                    1,
                    0,
                    1,
                    0,
                    0,
                    2,
                    MovementPattern.Free,
                    new Color(0.42f, 0.86f, 0.67f, 1f));
            });

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("基础卡牌资产已生成或更新到 Assets/_Assets/CardDefinitions/Generated");
    }

    private static void CreateOrUpdateCard(string assetName, System.Action<CardDefinition> configure)
    {
        var assetPath = Path.Combine(OutputFolder, $"{assetName}.asset").Replace("\\", "/");
        var card = AssetDatabase.LoadAssetAtPath<CardDefinition>(assetPath);

        if (card == null)
        {
            card = ScriptableObject.CreateInstance<CardDefinition>();
            AssetDatabase.CreateAsset(card, assetPath);
        }

        configure(card);
        EditorUtility.SetDirty(card);
    }

    private static void EnsureFolder(string folderPath)
    {
        var normalizedPath = folderPath.Replace("\\", "/");

        if (AssetDatabase.IsValidFolder(normalizedPath))
        {
            return;
        }

        var segments = normalizedPath.Split('/');
        var currentPath = segments[0];

        for (var i = 1; i < segments.Length; i++)
        {
            var nextPath = $"{currentPath}/{segments[i]}";

            if (!AssetDatabase.IsValidFolder(nextPath))
            {
                AssetDatabase.CreateFolder(currentPath, segments[i]);
            }

            currentPath = nextPath;
        }
    }

    private static void SetSerializedFields(
        CardDefinition card,
        string cardId,
        string displayName,
        string rulesText,
        string flavorText,
        CardCategory category,
        CardRarity rarity,
        CardPrimaryEffect primaryEffect,
        CardTargetingMode targetingMode,
        CardKeyword keywords,
        int cost,
        int damage,
        int hitCount,
        int cardDraw,
        int vitalityGain,
        int energyGain,
        int knockbackDistance,
        int movementAmount,
        MovementPattern movementPattern,
        Color accentColor)
    {
        var serializedObject = new SerializedObject(card);

        serializedObject.FindProperty("cardId").stringValue = cardId;
        serializedObject.FindProperty("displayName").stringValue = displayName;
        serializedObject.FindProperty("rulesText").stringValue = rulesText;
        serializedObject.FindProperty("flavorText").stringValue = flavorText;
        serializedObject.FindProperty("category").enumValueIndex = (int)category;
        serializedObject.FindProperty("rarity").enumValueIndex = (int)rarity;
        serializedObject.FindProperty("primaryEffect").enumValueIndex = (int)primaryEffect;
        serializedObject.FindProperty("targetingMode").enumValueIndex = (int)targetingMode;
        serializedObject.FindProperty("keywords").intValue = (int)keywords;
        serializedObject.FindProperty("cost").intValue = cost;
        serializedObject.FindProperty("damage").intValue = damage;
        serializedObject.FindProperty("hitCount").intValue = hitCount;
        serializedObject.FindProperty("cardDraw").intValue = cardDraw;
        serializedObject.FindProperty("vitalityGain").intValue = vitalityGain;
        serializedObject.FindProperty("energyGain").intValue = energyGain;
        serializedObject.FindProperty("knockbackDistance").intValue = knockbackDistance;
        serializedObject.FindProperty("movementAmount").intValue = movementAmount;
        serializedObject.FindProperty("movementPattern").enumValueIndex = (int)movementPattern;
        serializedObject.FindProperty("accentColor").colorValue = accentColor;

        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif
