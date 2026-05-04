using System.Collections.Generic;

namespace Gameplay
{
    public interface IBoardPieceModifier
    {
        void ModifyTargets(
            BoardActionType actionType,
            ChessboardController board,
            BoardPieceController piece,
            List<UnityEngine.Vector2Int> targets);

        void BeforeResolveAction(BoardActionContext context);

        void AfterResolveAction(BoardActionContext context);
    }
}
