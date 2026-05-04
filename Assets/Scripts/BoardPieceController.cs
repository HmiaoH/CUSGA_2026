using System.Collections;
using System.Collections.Generic;
using Managers;
using UnityEngine;

namespace Gameplay
{
    public class BoardPieceController : MonoBehaviour
    {
        [Header("Board Link")]
        [SerializeField] private ChessboardController board;

        [Header("Size")]
        [SerializeField] private float minimumHeightToWidthRatio = 1.4f;
        [SerializeField] private float verticalOffset = 0.02f;

        [Header("Animation")]
        [SerializeField] private float moveDuration = 0.2f;
        [SerializeField] private float attackDuration = 0.12f;
        [SerializeField] private float attackLungeDistanceRatio = 0.35f;
        [SerializeField] private float hitBounceScale = 1.15f;

        private readonly List<IBoardPieceModifier> modifiers = new List<IBoardPieceModifier>();

        private Renderer cachedRenderer;
        private MeshFilter cachedMeshFilter;
        private Vector2Int currentCell = new Vector2Int(-1, -1);
        private bool registered;

        public Vector2Int CurrentCell => currentCell;

        public bool IsAnimating { get; private set; }

        private void Awake()
        {
            cachedRenderer = GetComponentInChildren<Renderer>();
            cachedMeshFilter = GetComponentInChildren<MeshFilter>();
            CacheModifiers();
        }

        private void Start()
        {
            if (board == null)
            {
                board = FindObjectOfType<ChessboardController>();
            }

            if (board != null)
            {
                board.RegisterPiece(this);
                registered = true;
            }
        }

        public void AssignBoard(ChessboardController targetBoard)
        {
            board = targetBoard;
        }

        public void SetCurrentCell(Vector2Int cell)
        {
            currentCell = cell;
        }

        public void SnapToNearestCell()
        {
            if (board == null)
            {
                return;
            }

            if (!registered)
            {
                board.RegisterPiece(this);
                registered = true;
                return;
            }

            Vector2Int cell = board.GetClosestCell(transform.position);
            board.PlacePiece(this, cell, true);
        }

        public void SnapToCurrentCell()
        {
            if (board == null || !board.IsInsideBoard(currentCell))
            {
                return;
            }

            transform.position = GetPlacementWorldPosition(currentCell);
        }

        public void ResizeToBoardCell()
        {
            if (board == null)
            {
                return;
            }

            if (cachedRenderer == null)
            {
                cachedRenderer = GetComponentInChildren<Renderer>();
            }

            if (cachedRenderer == null)
            {
                return;
            }

            float targetWidth = board.CellSize * board.PieceFootprintRatio;
            float currentWidth = Mathf.Max(cachedRenderer.bounds.size.x, cachedRenderer.bounds.size.z);
            if (currentWidth <= 0.0001f)
            {
                return;
            }

            float scaleFactor = targetWidth / currentWidth;
            transform.localScale *= scaleFactor;

            float currentHeight = cachedRenderer.bounds.size.y;
            float desiredHeight = targetWidth * Mathf.Max(minimumHeightToWidthRatio, currentHeight / targetWidth);

            Vector3 localScale = transform.localScale;
            Vector3 parentLossyScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
            Vector3 meshSize = cachedMeshFilter != null && cachedMeshFilter.sharedMesh != null
                ? cachedMeshFilter.sharedMesh.bounds.size
                : Vector3.one;

            float parentHeightScale = Mathf.Max(0.0001f, Mathf.Abs(parentLossyScale.y));
            float meshHeight = Mathf.Max(0.0001f, meshSize.y);

            localScale.y = desiredHeight / (meshHeight * parentHeightScale);
            transform.localScale = localScale;
        }

        public List<Vector2Int> GetActionTargets(BoardActionType actionType)
        {
            List<Vector2Int> targets = new List<Vector2Int>();
            if (board == null || !board.IsInsideBoard(currentCell))
            {
                return targets;
            }

            if (actionType == BoardActionType.Move)
            {
                AddTargetIfValid(targets, currentCell + Vector2Int.up, true);
                AddTargetIfValid(targets, currentCell + Vector2Int.down, true);
                AddTargetIfValid(targets, currentCell + Vector2Int.left, true);
                AddTargetIfValid(targets, currentCell + Vector2Int.right, true);
            }
            else if (actionType == BoardActionType.Attack)
            {
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        if (x == 0 && y == 0)
                        {
                            continue;
                        }

                        AddTargetIfValid(targets, currentCell + new Vector2Int(x, y), false);
                    }
                }
            }

            for (int i = 0; i < modifiers.Count; i++)
            {
                modifiers[i].ModifyTargets(actionType, board, this, targets);
            }

            return RemoveDuplicateCells(targets);
        }

        public bool TryResolveAction(BoardActionType actionType, Vector2Int targetCell)
        {
            if (board == null || IsAnimating)
            {
                return false;
            }

            List<Vector2Int> legalTargets = GetActionTargets(actionType);
            if (!legalTargets.Contains(targetCell))
            {
                return false;
            }

            if (actionType == BoardActionType.Move)
            {
                return TryMove(targetCell);
            }

            if (actionType == BoardActionType.Attack)
            {
                return TryAttack(targetCell);
            }

            return false;
        }

        public void PlayHitReaction()
        {
            if (!gameObject.activeInHierarchy)
            {
                return;
            }

            StartCoroutine(HitReactionRoutine());
        }

        private bool TryMove(Vector2Int targetCell)
        {
            if (!board.IsCellWalkable(this, targetCell))
            {
                return false;
            }

            BoardActionContext context = new BoardActionContext(
                board,
                this,
                BoardActionType.Move,
                currentCell,
                targetCell,
                board.GetPieceAtCell(targetCell));

            StartCoroutine(MoveRoutine(context));
            return true;
        }

        private bool TryAttack(Vector2Int targetCell)
        {
            BoardActionContext context = new BoardActionContext(
                board,
                this,
                BoardActionType.Attack,
                currentCell,
                targetCell,
                board.GetPieceAtCell(targetCell));

            StartCoroutine(AttackRoutine(context));
            return true;
        }

        private IEnumerator MoveRoutine(BoardActionContext context)
        {
            IsAnimating = true;
            InvokeBeforeResolve(context);

            Vector3 startPosition = transform.position;
            Vector3 targetPosition = GetPlacementWorldPosition(context.TargetCell);
            float elapsed = 0f;

            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / moveDuration);
                float eased = Mathf.SmoothStep(0f, 1f, t);
                transform.position = Vector3.Lerp(startPosition, targetPosition, eased);
                yield return null;
            }

            transform.position = targetPosition;
            board.PlacePiece(this, context.TargetCell, false);
            currentCell = context.TargetCell;

            InvokeAfterResolve(context);
            if (EventManager.Instance != null)
            {
                EventManager.Instance.Dispatch(BoardEventNames.PieceMoved, context);
            }

            IsAnimating = false;
        }

        private IEnumerator AttackRoutine(BoardActionContext context)
        {
            IsAnimating = true;
            InvokeBeforeResolve(context);

            if (EventManager.Instance != null)
            {
                EventManager.Instance.Dispatch(BoardEventNames.PieceAttackStarted, context);
            }

            Vector3 startPosition = transform.position;
            Vector3 cellCenter = board.GetCellCenterWorld(context.TargetCell);
            Vector3 flatDirection = cellCenter - startPosition;
            flatDirection.y = 0f;

            Vector3 lungeTarget = startPosition;
            if (flatDirection.sqrMagnitude > 0.0001f)
            {
                lungeTarget += flatDirection.normalized * (board.CellSize * attackLungeDistanceRatio);
            }

            yield return MoveBetweenPoints(startPosition, lungeTarget, attackDuration);

            if (context.TargetPiece != null)
            {
                context.TargetPiece.PlayHitReaction();
            }

            yield return MoveBetweenPoints(lungeTarget, startPosition, attackDuration);

            InvokeAfterResolve(context);
            if (EventManager.Instance != null)
            {
                EventManager.Instance.Dispatch(BoardEventNames.PieceAttackResolved, context);
            }

            IsAnimating = false;
        }

        private IEnumerator MoveBetweenPoints(Vector3 from, Vector3 to, float duration)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transform.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }

            transform.position = to;
        }

        private IEnumerator HitReactionRoutine()
        {
            Vector3 startScale = transform.localScale;
            Vector3 peakScale = startScale * hitBounceScale;
            float elapsed = 0f;
            float duration = Mathf.Max(0.05f, attackDuration);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transform.localScale = Vector3.Lerp(startScale, peakScale, Mathf.Sin(t * Mathf.PI));
                yield return null;
            }

            transform.localScale = startScale;
        }

        private Vector3 GetPlacementWorldPosition(Vector2Int cell)
        {
            Vector3 cellCenter = board.GetCellCenterWorld(cell);
            float halfHeight = cachedRenderer != null ? cachedRenderer.bounds.size.y * 0.5f : 0.5f;
            return cellCenter + Vector3.up * (halfHeight + verticalOffset);
        }

        private void AddTargetIfValid(List<Vector2Int> targets, Vector2Int cell, bool requireWalkable)
        {
            if (!board.IsInsideBoard(cell))
            {
                return;
            }

            if (requireWalkable && !board.IsCellWalkable(this, cell))
            {
                return;
            }

            targets.Add(cell);
        }

        private void CacheModifiers()
        {
            modifiers.Clear();

            MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                IBoardPieceModifier modifier = behaviours[i] as IBoardPieceModifier;
                if (modifier != null)
                {
                    modifiers.Add(modifier);
                }
            }
        }

        private void InvokeBeforeResolve(BoardActionContext context)
        {
            for (int i = 0; i < modifiers.Count; i++)
            {
                modifiers[i].BeforeResolveAction(context);
            }
        }

        private void InvokeAfterResolve(BoardActionContext context)
        {
            for (int i = 0; i < modifiers.Count; i++)
            {
                modifiers[i].AfterResolveAction(context);
            }
        }

        private static List<Vector2Int> RemoveDuplicateCells(List<Vector2Int> cells)
        {
            List<Vector2Int> uniqueCells = new List<Vector2Int>();

            for (int i = 0; i < cells.Count; i++)
            {
                if (!uniqueCells.Contains(cells[i]))
                {
                    uniqueCells.Add(cells[i]);
                }
            }

            return uniqueCells;
        }
    }
}
