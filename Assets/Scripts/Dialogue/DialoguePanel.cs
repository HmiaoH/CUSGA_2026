using Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 对话面板 UI 控制器。
/// 职责：纯显示 + 转发点击，不持有业务逻辑。
/// </summary>
public class DialoguePanel : MonoBehaviour
{
    [Header("UI 引用")]
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text contentText;
    [SerializeField] private Button continueButton;

    private void Awake()
    {
        // 绑定按钮点击事件
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueClicked);
        }
    }

    private void OnDestroy()
    {
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(OnContinueClicked);
        }
    }

    /// <summary>
    /// 显示一行台词。由 DialogueManager 调用。
    /// </summary>
    public void ShowLine(DialogueLine line)
    {
        if (speakerText != null)
        {
            speakerText.text = line.speaker;
        }

        if (contentText != null)
        {
            contentText.text = line.content;
        }
    }

    /// <summary>
    /// Continue 按钮被点击时，转发给 DialogueManager。
    /// </summary>
    private void OnContinueClicked()
    {
        DialogueManager.Instance.NextLine();
    }
}