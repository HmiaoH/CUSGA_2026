using System.Collections;
using System.Collections.Generic;
using Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SimpleTurnController : MonoBehaviour
{
    [Header("Core")]
    [SerializeField] private ChessboardController chessboard;
    [SerializeField] private BoardPieceController playerPiece;
    [SerializeField] private Button endTurnButton;
    [SerializeField] private Transform gameplayUiRoot;

    [Header("Enemy Setup")]
    [SerializeField] private int enemyCount = 3;
    [SerializeField] private float enemyHeight = 0.7f;
    [SerializeField] private Color enemyColor = new Color(0.8f, 0.2f, 0.2f, 1f);
    [SerializeField] private int enemyHp = 4;
    [SerializeField] private int enemyAttack = 1;

    [Header("Turn UI")]
    [SerializeField] private string endTurnButtonText = "End Turn";
    [SerializeField] private string endTurnButtonObjectName = "EndTurnButton";
    [SerializeField] private bool autoFindEndTurnButtonByName = true;

    private readonly List<BoardPieceController> enemies = new List<BoardPieceController>();
    private bool enemyTurnRunning;

    private void Start()
    {
        if (chessboard == null)
        {
            chessboard = FindObjectOfType<ChessboardController>();
        }

        if (playerPiece == null && chessboard != null)
        {
            playerPiece = chessboard.ControlledPiece;
        }

        ResolveEndTurnButton();
        SpawnEnemies();
        RefreshEnemyThreatPreview();
    }

    private void ResolveEndTurnButton()
    {
        if (endTurnButton == null)
        {
            endTurnButton = FindEndTurnButton();
        }

        if (endTurnButton == null)
        {
            Debug.LogWarning(
                "SimpleTurnController: End turn button is not assigned. " +
                "Create a dedicated gameplay button named '" + endTurnButtonObjectName + "' or assign it in the Inspector.",
                this);
            return;
        }

        endTurnButton.onClick.RemoveListener(OnEndTurnClicked);
        endTurnButton.onClick.AddListener(OnEndTurnClicked);

        TMP_Text buttonText = endTurnButton.GetComponentInChildren<TMP_Text>();
        if (buttonText != null)
        {
            buttonText.text = endTurnButtonText;
        }
    }

    private Button FindEndTurnButton()
    {
        Button localButton = GetComponentInChildren<Button>(true);
        if (IsValidEndTurnButton(localButton))
        {
            return localButton;
        }

        if (gameplayUiRoot != null)
        {
            Button[] scopedButtons = gameplayUiRoot.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < scopedButtons.Length; i++)
            {
                if (IsValidEndTurnButton(scopedButtons[i]))
                {
                    return scopedButtons[i];
                }
            }
        }

        if (!autoFindEndTurnButtonByName)
        {
            return null;
        }

        Button[] buttons = FindObjectsOfType<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (IsValidEndTurnButton(buttons[i]))
            {
                return buttons[i];
            }
        }

        return null;
    }

    private bool IsValidEndTurnButton(Button button)
    {
        if (button == null || button.GetComponentInParent<DialoguePanel>(true) != null)
        {
            return false;
        }

        string objectName = button.gameObject.name;
        return string.Equals(objectName, endTurnButtonObjectName, System.StringComparison.OrdinalIgnoreCase) ||
               string.Equals(NormalizeButtonName(objectName), NormalizeButtonName(endTurnButtonText), System.StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeButtonName(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.Replace(" ", string.Empty).Replace("_", string.Empty).Replace("-", string.Empty);
    }

    private void SpawnEnemies()
    {
        if (chessboard == null || playerPiece == null)
        {
            return;
        }

        ClearEnemies();

        List<Vector2Int> availableCells = new List<Vector2Int>();
        for (int x = 0; x < chessboard.GridSize; x++)
        {
            for (int y = 0; y < chessboard.GridSize; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                if (cell == playerPiece.CurrentCell)
                {
                    continue;
                }

                if (!chessboard.IsCellOccupied(cell))
                {
                    availableCells.Add(cell);
                }
            }
        }

        int spawnCount = Mathf.Min(enemyCount, availableCells.Count);
        for (int i = 0; i < spawnCount; i++)
        {
            int randomIndex = Random.Range(0, availableCells.Count);
            Vector2Int spawnCell = availableCells[randomIndex];
            availableCells.RemoveAt(randomIndex);

            GameObject enemyObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            enemyObject.name = "Enemy_" + i;
            enemyObject.transform.SetParent(chessboard.transform.parent, true);

            Renderer renderer = enemyObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = enemyColor;
            }

            BoardPieceController enemyPiece = enemyObject.AddComponent<BoardPieceController>();
            enemyPiece.AssignBoard(chessboard);
            enemyPiece.SetTeam(BoardPieceTeam.Enemy);
            enemyPiece.SetStats(enemyHp, enemyAttack);
            enemyPiece.OverrideHeightRatio(enemyHeight);

            chessboard.PlacePiece(enemyPiece, spawnCell, true);
            enemies.Add(enemyPiece);
        }
    }

    private void ClearEnemies()
    {
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            if (enemies[i] != null)
            {
                Destroy(enemies[i].gameObject);
            }
        }

        enemies.Clear();
    }

    private void OnEndTurnClicked()
    {
        if (enemyTurnRunning || chessboard == null)
        {
            return;
        }

        StartCoroutine(EnemyTurnRoutine());
    }

    private IEnumerator EnemyTurnRoutine()
    {
        enemyTurnRunning = true;
        chessboard.ClearPreview();
        chessboard.ClearThreatPreview();

        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            BoardPieceController enemy = enemies[i];
            if (enemy == null || !enemy.IsAlive)
            {
                enemies.RemoveAt(i);
                continue;
            }

            enemy.TryPerformEnemyAttack(playerPiece);
            yield return new WaitForSeconds(0.18f);

            if (playerPiece == null || !playerPiece.IsAlive)
            {
                break;
            }

            enemy.TryPerformEnemyMoveTowards(playerPiece.CurrentCell);
            yield return new WaitForSeconds(0.22f);
        }

        RefreshEnemyThreatPreview();
        enemyTurnRunning = false;
    }

    public void RefreshEnemyThreatPreview()
    {
        if (chessboard == null)
        {
            return;
        }

        List<Vector2Int> threatCells = new List<Vector2Int>();
        for (int i = 0; i < enemies.Count; i++)
        {
            BoardPieceController enemy = enemies[i];
            if (enemy == null || !enemy.IsAlive)
            {
                continue;
            }

            threatCells.AddRange(enemy.GetThreatenedCells());
        }

        chessboard.ShowThreatPreview(threatCells);
    }
}
