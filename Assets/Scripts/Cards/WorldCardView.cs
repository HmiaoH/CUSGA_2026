using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 场景中“已打出卡牌”的展示组件。
/// 用于把 CardDefinition 的核心信息同步到世界空间卡牌对象。
/// </summary>
public class WorldCardView : MonoBehaviour
{
    [Header("文本")]
    [SerializeField] private TMP_Text cardNameText;
    [SerializeField] private TMP_Text cardCostText;
    [SerializeField] private TMP_Text cardDescText;
    [SerializeField] private TMP_Text cardIdText;

    [Header("图像/材质")]
    [SerializeField] private Image artworkImage;
    [SerializeField] private Renderer tintRenderer;
    [SerializeField] private string tintColorProperty = "_BaseColor";
    [SerializeField] private Color fallbackTint = new Color(0.18f, 0.16f, 0.14f, 1f);

    /// <summary>
    /// 绑定卡牌定义并刷新世界牌表现。
    /// </summary>
    public void Bind(CardDefinition definition)
    {
        if (definition == null)
        {
            return;
        }

        if (cardNameText != null)
        {
            cardNameText.text = definition.DisplayName;
        }

        if (cardCostText != null)
        {
            cardCostText.text = definition.Cost.ToString();
        }

        if (cardDescText != null)
        {
            cardDescText.text = definition.RulesText;
        }

        if (cardIdText != null)
        {
            cardIdText.text = definition.CardId;
        }

        if (artworkImage != null)
        {
            artworkImage.sprite = definition.Artwork;
            artworkImage.enabled = definition.Artwork != null;
        }

        if (tintRenderer != null)
        {
            var color = definition.AccentColor;
            if (color.a <= 0.0001f)
            {
                color = fallbackTint;
            }

            var material = tintRenderer.material;
            if (material != null && material.HasProperty(tintColorProperty))
            {
                material.SetColor(tintColorProperty, color);
            }
        }
    }
}
