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
        [SerializeField] private BoardPieceTeam team = BoardPieceTeam.Player;
        [SerializeField] private int maxHp = 6;
        [SerializeField] private int attackPower = 1;

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
        private int currentHp;

        public Vector2Int CurrentCell => currentCell;

        public bool IsAnimating { get; private set; }

        public BoardPieceTeam Team => team;

        public bool IsAlive => currentHp > 0;

        private void Awake()
        {
            cachedRenderer = GetComponentInChildren<Renderer>();
            cachedMeshFilter = GetComponentInChildren<MeshFilter>();
            CacheModifiers();
            currentHp = Mathf.Max(1, maxHp);
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

        public void SetTeam(BoardPieceTeam newTeam)
        {
            team = newTeam;
        }

        public void SetStats(int hp, int attack)
        {
            maxHp = Mathf.Max(1, hp);
            currentHp = maxHp;
            attackPower = Mathf.Max(1, attack);
        }

        public void OverrideHeightRatio(float ratio)
        {
            minimumHeightToWidthRatio = Mathf.Max(1f, ratio);
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
                int moveRange = board.GetRangeForAction(actionType, this);
                for (int x = -moveRange; x <= moveRange; x++)
                {
                    for (int y = -moveRange; y <= moveRange; y++)
                    {
                        int manhattanDistance = Mathf.Abs(x) + Mathf.Abs(y);
                        if (manhattanDistance == 0 || manhattanDistance > moveRange)
                        {
                            continue;
                        }

                        AddTargetIfValid(targets, currentCell + new Vector2Int(x, y), true);
                    }
                }
            }
            else if (actionType == BoardActionType.Attack)
            {
                int attackRange = board.GetRangeForAction(actionType, this);
                for (int x = -attackRange; x <= attackRange; x++)
                {
                    for (int y = -attackRange; y <= attackRange; y++)
                    {
                        if (x == 0 && y == 0)
                        {
                            continue;
                        }

                        Vector2Int attackCell = currentCell + new Vector2Int(x, y);
                        if (!board.IsInsideBoard(attackCell))
                        {
                            continue;
                        }

                        BoardPieceController targetPiece = board.GetPieceAtCell(attackCell);
                        if (targetPiece != null && targetPiece.Team != team)
                        {
                            targets.Add(attackCell);
                        }
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
                board.GetPieceAtCell(targetCell),
                0);

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
                board.GetPieceAtCell(targetCell),
                board.GetPowerForAction(BoardActionType.Attack));

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
                context.TargetPiece.ReceiveDamage(context.Power);
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

        public void ReceiveDamage(int damage)
        {
            currentHp -= Mathf.Max(0, damage);
            if (currentHp > 0)
            {
                return;
            }

            currentHp = 0;
            if (board != null)
            {
                board.RemovePiece(this);
            }

            gameObject.SetActive(false);
        }

        public List<Vector2Int> GetThreatenedCells()
        {
            List<Vector2Int> threatCells = new List<Vector2Int>();
            if (board == null || !board.IsInsideBoard(currentCell))
            {
                return threatCells;
            }

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0)
                    {
                        continue;
                    }

                    Vector2Int cell = currentCell + new Vector2Int(x, y);
                    if (board.IsInsideBoard(cell))
                    {
                        threatCells.Add(cell);
                    }
                }
            }

            return threatCells;
        }

        public bool TryPerformEnemyAttack(BoardPieceController playerPiece)
        {
            if (playerPiece == null || !playerPiece.IsAlive)
            {
                return false;
            }

            if (Mathf.Abs(playerPiece.CurrentCell.x - currentCell.x) > 1 ||
                Mathf.Abs(playerPiece.CurrentCell.y - currentCell.y) > 1)
            {
                return false;
            }

            playerPiece.ReceiveDamage(attackPower);
            playerPiece.PlayHitReaction();
            return true;
        }

        public bool TryPerformEnemyMoveTowards(Vector2Int targetCell)
        {
            if (board == null)
            {
                return false;
            }

            Vector2Int bestCell = currentCell;
            float bestDistance = float.MaxValue;
            Vector2Int[] directions =
            {
                Vector2Int.up,
                Vector2Int.down,
                Vector2Int.left,
                Vector2Int.right
            };

            for (int i = 0; i < directions.Length; i++)
            {
                Vector2Int candidate = currentCell + directions[i];
                if (!board.IsCellWalkable(this, candidate))
                {
                    continue;
                }

                float distance = Vector2Int.Distance(candidate, targetCell);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestCell = candidate;
                }
            }

            if (bestCell == currentCell)
            {
                return false;
            }

            board.PlacePiece(this, bestCell, true);
            currentCell = bestCell;
            return true;
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
