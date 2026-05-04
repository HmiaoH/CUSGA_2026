using UnityEngine;

namespace Gameplay
{
    public sealed class BoardActionContext
    {
        public BoardActionContext(
            ChessboardController board,
            BoardPieceController sourcePiece,
            BoardActionType actionType,
            Vector2Int originCell,
            Vector2Int targetCell,
            BoardPieceController targetPiece)
        {
            Board = board;
            SourcePiece = sourcePiece;
            ActionType = actionType;
            OriginCell = originCell;
            TargetCell = targetCell;
            TargetPiece = targetPiece;
        }

        public ChessboardController Board { get; }

        public BoardPieceController SourcePiece { get; }

        public BoardActionType ActionType { get; }

        public Vector2Int OriginCell { get; }

        public Vector2Int TargetCell { get; }

        public BoardPieceController TargetPiece { get; }
    }
}
