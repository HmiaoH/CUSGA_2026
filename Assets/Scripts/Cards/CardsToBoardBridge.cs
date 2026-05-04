using Gameplay;
using Managers;
using UnityEngine;

/// <summary>
/// 将卡牌使用事件映射到棋盘动作预览。
/// Move 类卡 -> 移动预览
/// Damage 类卡 -> 攻击预览
/// </summary>
public class CardsToBoardBridge : MonoBehaviour
{
    [SerializeField] private ChessboardController chessboard;
    [SerializeField] private bool clearExistingPreviewBeforeApply = true;

    private void OnEnable()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.AddListener(CardEventNames.CardPlayedToBoard, OnCardPlayed);
        }
    }

    private void OnDisable()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.RemoveListener(CardEventNames.CardPlayedToBoard, OnCardPlayed);
        }
    }

    private void OnCardPlayed(object payload)
    {
        CardDefinition definition = payload as CardDefinition;
        if (definition == null)
        {
            return;
        }

        if (chessboard == null)
        {
            chessboard = FindObjectOfType<ChessboardController>();
        }

        if (chessboard == null)
        {
            return;
        }

        if (clearExistingPreviewBeforeApply)
        {
            chessboard.ClearPreview();
        }

        switch (definition.Category)
        {
            case CardCategory.Move:
                chessboard.BeginActionPreview(BoardActionType.Move);
                break;
            case CardCategory.Damage:
                chessboard.BeginActionPreview(BoardActionType.Attack);
                break;
        }
    }
}
