using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 基础演示脚本。
/// 负责在运行时把几张卡牌资产实例化到手牌区，便于先验证视觉和交互。
/// </summary>
public class CardHandDemo : MonoBehaviour
{
    [SerializeField] private CardView cardPrefab;
    [SerializeField] private RectTransform handRoot;
    [SerializeField] private List<CardDefinition> demoCards = new List<CardDefinition>();
    [SerializeField] private bool rebuildOnStart = true;

    private readonly List<CardView> spawnedCards = new List<CardView>();

    public void Configure(
        CardView prefab,
        RectTransform root,
        List<CardDefinition> cards,
        bool shouldRebuildOnStart)
    {
        cardPrefab = prefab;
        handRoot = root;
        demoCards = cards != null ? cards : new List<CardDefinition>();
        rebuildOnStart = shouldRebuildOnStart;
    }

    private void Start()
    {
        if (rebuildOnStart)
        {
            RebuildHand();
        }
    }

    /// <summary>
    /// 按当前配置重新生成一轮卡牌实例。
    /// </summary>
    [ContextMenu("Rebuild Hand")]
    public void RebuildHand()
    {
        ClearSpawnedCards();

        if (cardPrefab == null || handRoot == null)
        {
            return;
        }

        foreach (var definition in demoCards)
        {
            if (definition == null)
            {
                continue;
            }

            var instance = Instantiate(cardPrefab, handRoot);
            instance.Bind(definition);

            EnsureDragHandler(instance);
            spawnedCards.Add(instance);
        }
    }

    private static void EnsureDragHandler(CardView instance)
    {
        if (instance != null && instance.GetComponent<CardDragPlayHandler>() == null)
        {
            instance.gameObject.AddComponent<CardDragPlayHandler>();
        }
    }

    private void ClearSpawnedCards()
    {
        for (var i = spawnedCards.Count - 1; i >= 0; i--)
        {
            var card = spawnedCards[i];

            if (card != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(card.gameObject);
                }
                else
                {
                    DestroyImmediate(card.gameObject);
                }
            }
        }

        spawnedCards.Clear();
    }
}
