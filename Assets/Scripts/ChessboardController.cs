using System.Collections.Generic;
using Managers;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

namespace Gameplay
{
    public class ChessboardController : MonoBehaviour
    {
        [Header("Board")]
        [SerializeField] private int gridSize = 8;
        [SerializeField] private bool useManualBoardBounds = false;
        [SerializeField] private Vector3 manualBoardCenterOffset = Vector3.zero;
        [SerializeField] private Vector2 manualBoardSize = new Vector2(8f, 8f);
        [SerializeField] [Range(0.6f, 1f)] private float pieceFootprintRatio = 0.82f;
        [SerializeField] private Renderer boardSurfaceRenderer;
        [SerializeField] private Collider boardSurfaceCollider;
        [SerializeField] private Transform boardSurfaceTransform;

        [Header("Input")]
        [SerializeField] private Camera inputCamera;
        [SerializeField] private BoardPieceController controlledPiece;

        [Header("Highlight")]
        [SerializeField] private float highlightHeightOffset = 0.03f;
        [SerializeField] private Color moveHighlightColor = new Color(0.2f, 0.9f, 0.3f, 0.65f);
        [SerializeField] private Color attackHighlightColor = new Color(0.95f, 0.35f, 0.25f, 0.65f);

        [Header("Grid Lines")]
        [SerializeField] private bool showGridLines = true;
        [SerializeField] private float gridLineHeightOffset = 0.035f;
        [SerializeField] private float gridLineThickness = 0.02f;
        [SerializeField] private Color gridLineColor = new Color(1f, 1f, 1f, 0.75f);

        private readonly Dictionary<Vector2Int, Renderer> cellHighlights = new Dictionary<Vector2Int, Renderer>();
        private readonly Dictionary<Vector2Int, BoardPieceController> occupiedPieces = new Dictionary<Vector2Int, BoardPieceController>();
        private readonly HashSet<Vector2Int> previewCells = new HashSet<Vector2Int>();

        private Transform highlightRoot;
        private Transform gridLineRoot;
        private Material highlightMaterial;
        private Material gridLineMaterial;
        private Bounds boardBounds;
        private BoardActionType armedAction = BoardActionType.None;
        private bool initialized;

        public int GridSize => gridSize;

        public float CellWidth => boardBounds.size.x / gridSize;

        public float CellDepth => boardBounds.size.z / gridSize;

        public float CellSize => Mathf.Min(CellWidth, CellDepth);

        public float PieceFootprintRatio => pieceFootprintRatio;

        public float BoardTopY => boardBounds.max.y;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void Start()
        {
            EnsureInitialized();

            if (controlledPiece != null)
            {
                controlledPiece.AssignBoard(this);
                controlledPiece.SnapToNearestCell();
            }
        }

        private void Update()
        {
            EnsureInitialized();
            HandleInput();
        }

        public void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }

            RecalculateBoardBounds();
            RebuildHighlights();
            initialized = true;
        }

        [ContextMenu("Rebuild Board Grid")]
        public void RebuildBoardGrid()
        {
            initialized = false;
            EnsureInitialized();

            foreach (BoardPieceController piece in occupiedPieces.Values)
            {
                if (piece != null)
                {
                    piece.ResizeToBoardCell();
                    piece.SnapToCurrentCell();
                }
            }
        }

        public void SetControlledPiece(BoardPieceController piece)
        {
            controlledPiece = piece;
        }

        public void RegisterPiece(BoardPieceController piece)
        {
            EnsureInitialized();

            if (piece == null)
            {
                return;
            }

            Vector2Int cell = GetClosestCell(piece.transform.position);
            PlacePiece(piece, cell, true);
        }

        public bool PlacePiece(BoardPieceController piece, Vector2Int cell, bool snapImmediately)
        {
            EnsureInitialized();

            if (piece == null || !IsInsideBoard(cell))
            {
                return false;
            }

            BoardPieceController occupyingPiece;
            if (occupiedPieces.TryGetValue(cell, out occupyingPiece) && occupyingPiece != null && occupyingPiece != piece)
            {
                return false;
            }

            RemovePiece(piece);

            occupiedPieces[cell] = piece;
            piece.SetCurrentCell(cell);
            piece.AssignBoard(this);
            piece.ResizeToBoardCell();

            if (snapImmediately)
            {
                piece.SnapToCurrentCell();
            }

            return true;
        }

        public void RemovePiece(BoardPieceController piece)
        {
            if (piece == null)
            {
                return;
            }

            Vector2Int keyToRemove = new Vector2Int(-1, -1);
            bool found = false;

            foreach (KeyValuePair<Vector2Int, BoardPieceController> pair in occupiedPieces)
            {
                if (pair.Value == piece)
                {
                    keyToRemove = pair.Key;
                    found = true;
                    break;
                }
            }

            if (found)
            {
                occupiedPieces.Remove(keyToRemove);
            }
        }

        public bool IsInsideBoard(Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < gridSize && cell.y >= 0 && cell.y < gridSize;
        }

        public bool IsCellOccupied(Vector2Int cell)
        {
            BoardPieceController piece;
            return occupiedPieces.TryGetValue(cell, out piece) && piece != null;
        }

        public bool IsCellWalkable(BoardPieceController movingPiece, Vector2Int cell)
        {
            if (!IsInsideBoard(cell))
            {
                return false;
            }

            BoardPieceController piece;
            if (!occupiedPieces.TryGetValue(cell, out piece))
            {
                return true;
            }

            return piece == null || piece == movingPiece;
        }

        public BoardPieceController GetPieceAtCell(Vector2Int cell)
        {
            BoardPieceController piece;
            if (occupiedPieces.TryGetValue(cell, out piece))
            {
                return piece;
            }

            return null;
        }

        public Vector2Int GetClosestCell(Vector3 worldPosition)
        {
            Vector2Int cell;
            if (TryGetCellFromWorld(worldPosition, out cell))
            {
                return cell;
            }

            float clampedX = Mathf.Clamp(worldPosition.x, boardBounds.min.x, boardBounds.max.x - 0.0001f);
            float clampedZ = Mathf.Clamp(worldPosition.z, boardBounds.min.z, boardBounds.max.z - 0.0001f);
            TryGetCellFromWorld(new Vector3(clampedX, worldPosition.y, clampedZ), out cell);
            return cell;
        }

        public bool TryGetCellFromWorld(Vector3 worldPosition, out Vector2Int cell)
        {
            cell = new Vector2Int(-1, -1);

            if (worldPosition.x < boardBounds.min.x || worldPosition.x > boardBounds.max.x ||
                worldPosition.z < boardBounds.min.z || worldPosition.z > boardBounds.max.z)
            {
                return false;
            }

            float xRatio = Mathf.InverseLerp(boardBounds.min.x, boardBounds.max.x, worldPosition.x);
            float zRatio = Mathf.InverseLerp(boardBounds.min.z, boardBounds.max.z, worldPosition.z);

            int x = Mathf.Clamp(Mathf.FloorToInt(xRatio * gridSize), 0, gridSize - 1);
            int y = Mathf.Clamp(Mathf.FloorToInt(zRatio * gridSize), 0, gridSize - 1);

            cell = new Vector2Int(x, y);
            return true;
        }

        public bool TryGetCellFromMousePosition(out Vector2Int cell)
        {
            cell = new Vector2Int(-1, -1);

            Camera cameraToUse = inputCamera != null ? inputCamera : Camera.main;
            if (cameraToUse == null)
            {
                return false;
            }

            Ray ray = cameraToUse.ScreenPointToRay(Input.mousePosition);
            Plane plane = new Plane(Vector3.up, new Vector3(0f, BoardTopY, 0f));

            float enter;
            if (!plane.Raycast(ray, out enter))
            {
                return false;
            }

            Vector3 hitPoint = ray.GetPoint(enter);
            return TryGetCellFromWorld(hitPoint, out cell);
        }

        public Vector3 GetCellCenterWorld(Vector2Int cell)
        {
            float x = boardBounds.min.x + ((cell.x + 0.5f) * CellWidth);
            float z = boardBounds.min.z + ((cell.y + 0.5f) * CellDepth);
            return new Vector3(x, BoardTopY, z);
        }

        public void ShowPreview(BoardActionType actionType, List<Vector2Int> cells)
        {
            ClearPreview();

            Color previewColor = actionType == BoardActionType.Attack ? attackHighlightColor : moveHighlightColor;

            for (int i = 0; i < cells.Count; i++)
            {
                Vector2Int cell = cells[i];
                Renderer renderer;
                if (!cellHighlights.TryGetValue(cell, out renderer) || renderer == null)
                {
                    continue;
                }

                previewCells.Add(cell);
                renderer.enabled = true;
                renderer.SetPropertyBlock(CreateColorPropertyBlock(previewColor));
            }

            if (EventManager.Instance != null)
            {
                EventManager.Instance.Dispatch(BoardEventNames.ActionPreviewChanged, actionType);
            }
        }

        public void ClearPreview()
        {
            foreach (Renderer renderer in cellHighlights.Values)
            {
                if (renderer != null)
                {
                    renderer.enabled = false;
                }
            }

            previewCells.Clear();
            armedAction = BoardActionType.None;
        }

        public bool IsPreviewCell(Vector2Int cell)
        {
            return previewCells.Contains(cell);
        }

        private void HandleInput()
        {
            if (controlledPiece == null || controlledPiece.IsAnimating)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ClearPreview();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            {
                ArmAction(BoardActionType.Move);
            }

            if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            {
                ArmAction(BoardActionType.Attack);
            }

            if (armedAction == BoardActionType.None)
            {
                return;
            }

            if (!Input.GetMouseButtonDown(0))
            {
                return;
            }

            Vector2Int targetCell;
            if (!TryGetCellFromMousePosition(out targetCell) || !IsPreviewCell(targetCell))
            {
                return;
            }

            bool actionSucceeded = controlledPiece.TryResolveAction(armedAction, targetCell);
            if (actionSucceeded)
            {
                ClearPreview();
            }
        }

        private void ArmAction(BoardActionType actionType)
        {
            List<Vector2Int> targets = controlledPiece.GetActionTargets(actionType);
            ShowPreview(actionType, targets);
            armedAction = targets.Count > 0 ? actionType : BoardActionType.None;
        }

        private void RecalculateBoardBounds()
        {
            if (useManualBoardBounds)
            {
                Vector3 center = transform.position + manualBoardCenterOffset;
                boardBounds = new Bounds(center, new Vector3(manualBoardSize.x, 0.1f, manualBoardSize.y));
                return;
            }

            Bounds explicitSurfaceBounds;
            if (TryGetSurfaceBounds(out explicitSurfaceBounds))
            {
                boardBounds = explicitSurfaceBounds;
                return;
            }

            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            bool foundRenderer = false;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                if (highlightRoot != null && renderer.transform.IsChildOf(highlightRoot))
                {
                    continue;
                }

                if (!foundRenderer)
                {
                    boardBounds = renderer.bounds;
                    foundRenderer = true;
                    continue;
                }

                boardBounds.Encapsulate(renderer.bounds);
            }

            if (!foundRenderer)
            {
                boardBounds = new Bounds(transform.position, new Vector3(gridSize, 0.1f, gridSize));
            }
        }

        private void RebuildHighlights()
        {
            if (highlightRoot != null)
            {
                Destroy(highlightRoot.gameObject);
            }

            if (gridLineRoot != null)
            {
                Destroy(gridLineRoot.gameObject);
            }

            cellHighlights.Clear();
            previewCells.Clear();

            GameObject root = new GameObject("RuntimeCellHighlights");
            MoveRuntimeObjectToScene(root);
            highlightRoot = root.transform;

            if (highlightMaterial == null)
            {
                highlightMaterial = CreateHighlightMaterial();
            }

            if (showGridLines)
            {
                BuildGridLines();
            }

            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    Vector2Int cell = new Vector2Int(x, y);

                    GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    quad.name = "Cell_" + x + "_" + y;
                    quad.transform.SetParent(highlightRoot, false);
                    quad.transform.position = GetCellCenterWorld(cell) + (Vector3.up * highlightHeightOffset);
                    quad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                    quad.transform.localScale = new Vector3(CellWidth * 0.92f, CellDepth * 0.92f, 1f);

                    Collider quadCollider = quad.GetComponent<Collider>();
                    if (quadCollider != null)
                    {
                        Destroy(quadCollider);
                    }

                    Renderer renderer = quad.GetComponent<Renderer>();
                    renderer.sharedMaterial = highlightMaterial;
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                    renderer.enabled = false;

                    cellHighlights[cell] = renderer;
                }
            }
        }

        private static MaterialPropertyBlock CreateColorPropertyBlock(Color color)
        {
            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
            propertyBlock.SetColor("_Color", color);
            propertyBlock.SetColor("_BaseColor", color);
            return propertyBlock;
        }

        private void BuildGridLines()
        {
            GameObject root = new GameObject("RuntimeGridLines");
            MoveRuntimeObjectToScene(root);
            gridLineRoot = root.transform;

            if (gridLineMaterial == null)
            {
                gridLineMaterial = CreateHighlightMaterial();
            }

            float boardWidth = boardBounds.size.x;
            float boardDepth = boardBounds.size.z;
            float y = BoardTopY + gridLineHeightOffset;

            for (int x = 0; x <= gridSize; x++)
            {
                float lineX = boardBounds.min.x + (x * CellWidth);
                Vector3 position = new Vector3(lineX, y, boardBounds.center.z);
                Vector3 scale = new Vector3(gridLineThickness, boardDepth, 1f);
                CreateGridLine("Vertical_" + x, position, scale);
            }

            for (int yIndex = 0; yIndex <= gridSize; yIndex++)
            {
                float lineZ = boardBounds.min.z + (yIndex * CellDepth);
                Vector3 position = new Vector3(boardBounds.center.x, y, lineZ);
                Vector3 scale = new Vector3(boardWidth, gridLineThickness, 1f);
                CreateGridLine("Horizontal_" + yIndex, position, scale);
            }
        }

        private void CreateGridLine(string lineName, Vector3 worldPosition, Vector3 localScale)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = lineName;
            quad.transform.SetParent(gridLineRoot, true);
            quad.transform.position = worldPosition;
            quad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            quad.transform.localScale = localScale;

            Collider quadCollider = quad.GetComponent<Collider>();
            if (quadCollider != null)
            {
                Destroy(quadCollider);
            }

            Renderer renderer = quad.GetComponent<Renderer>();
            renderer.sharedMaterial = gridLineMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.SetPropertyBlock(CreateColorPropertyBlock(gridLineColor));
        }

        private static Material CreateHighlightMaterial()
        {
            Shader shader = Shader.Find("Unlit/Color");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material material = new Material(shader);
            material.color = Color.white;

            if (material.HasProperty("_Mode"))
            {
                material.SetFloat("_Mode", 3f);
            }

            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;

            return material;
        }

        private void OnDrawGizmosSelected()
        {
            RecalculateBoardBounds();

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(boardBounds.center, new Vector3(boardBounds.size.x, 0.05f, boardBounds.size.z));
        }

        private bool TryGetSurfaceBounds(out Bounds surfaceBounds)
        {
            if (boardSurfaceCollider != null)
            {
                surfaceBounds = boardSurfaceCollider.bounds;
                return true;
            }

            if (boardSurfaceRenderer != null)
            {
                surfaceBounds = boardSurfaceRenderer.bounds;
                return true;
            }

            if (boardSurfaceTransform != null)
            {
                Vector3 center = boardSurfaceTransform.position;
                Vector3 scale = boardSurfaceTransform.lossyScale;
                surfaceBounds = new Bounds(center, new Vector3(Mathf.Abs(scale.x), 0.1f, Mathf.Abs(scale.z)));
                return true;
            }

            surfaceBounds = default;
            return false;
        }

        private void MoveRuntimeObjectToScene(GameObject runtimeObject)
        {
            SceneManager.MoveGameObjectToScene(runtimeObject, gameObject.scene);
            runtimeObject.transform.SetParent(null, true);
        }
    }
}
