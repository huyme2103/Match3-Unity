using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AutoPlayBot : MonoBehaviour
{
    private BoardController m_boardController;
    private SlotBarController m_slotBarController;
    private Board m_board;
    private GameManager m_gameManager;
    private BoardController.eAutoPlayMode m_autoPlayMode;
    private bool m_gameOver;

    public void Init(BoardController boardController, SlotBarController slotBarController, Board board, GameManager gameManager, BoardController.eAutoPlayMode mode)
    {
        m_boardController = boardController;
        m_slotBarController = slotBarController;
        m_board = board;
        m_gameManager = gameManager;
        m_autoPlayMode = mode;

        m_gameManager.StateChangedAction += OnGameStateChange;
    }

    private void OnDestroy()
    {
        if (m_gameManager != null)
        {
            m_gameManager.StateChangedAction -= OnGameStateChange;
        }
    }

    private void OnGameStateChange(GameManager.eStateGame state)
    {
        if (state == GameManager.eStateGame.GAME_OVER || state == GameManager.eStateGame.GAME_WIN)
        {
            m_gameOver = true;
        }
    }

    public void StartBot()
    {
        if (m_autoPlayMode != BoardController.eAutoPlayMode.NONE)
        {
            StartCoroutine(AutoPlayCoroutine());
        }
    }

    private IEnumerator AutoPlayCoroutine()
    {
        // Chờ xíu lúc đầu cho mượt
        yield return new WaitForSeconds(1.0f);

        while (!m_gameOver && m_gameManager.State == GameManager.eStateGame.GAME_STARTED)
        {
            // Chờ nếu đang bận xử lý logic bay/nổ cá của lượt trước
            while (m_boardController.IsBusy)
            {
                yield return null;
            }

            if (m_gameOver || m_gameManager.State != GameManager.eStateGame.GAME_STARTED) break;

            List<Cell> activeCells = m_board.GetAllActiveCells();
            if (activeCells.Count == 0) break; // Đã hết cá trên bàn

            Cell targetCell = null;

            if (m_autoPlayMode == BoardController.eAutoPlayMode.AUTO_WIN)
            {
                // Auto Win: Tìm cá cùng loại với cá đang có trong khay
                targetCell = FindBestCellForAutoWin(activeCells);
            }
            else if (m_autoPlayMode == BoardController.eAutoPlayMode.AUTO_LOSE)
            {
                // Auto Lose: Tìm cá KHÁC loại với cá đang có trong khay để nhanh đầy
                targetCell = FindWorstCellForAutoLose(activeCells);
            }

            if (targetCell != null && !m_slotBarController.IsFull)
            {
                Debug.Log($"Bot nhặt cá: Tọa độ ({targetCell.BoardX}, {targetCell.BoardY}) - Chế độ: {m_autoPlayMode}");
                m_slotBarController.PickupItem(targetCell);
            }

            // Tốc độ click của Bot (0.5s mỗi lượt)
            yield return new WaitForSeconds(0.5f); 
        }
    }

    private Cell FindBestCellForAutoWin(List<Cell> activeCells)
    {
        var groups = m_slotBarController.GetItems().Cast<NormalItem>().GroupBy(x => x.ItemType).ToList();
        if (groups.Count > 0)
        {
            var bestGroup = groups.OrderByDescending(g => g.Count()).First();
            NormalItem.eNormalType targetType = bestGroup.Key;

            Cell foundCell = activeCells.FirstOrDefault(c => (c.Item as NormalItem)?.ItemType == targetType);
            if (foundCell != null) return foundCell;
        }

        return activeCells[Random.Range(0, activeCells.Count)];
    }

    private Cell FindWorstCellForAutoLose(List<Cell> activeCells)
    {
        var typesInSlot = m_slotBarController.GetItems().Cast<NormalItem>().Select(x => x.ItemType).Distinct().ToList();

        Cell badCell = activeCells.FirstOrDefault(c => 
        {
            NormalItem item = c.Item as NormalItem;
            return item != null && !typesInSlot.Contains(item.ItemType);
        });

        if (badCell != null) return badCell;

        return activeCells[Random.Range(0, activeCells.Count)];
    }
}
