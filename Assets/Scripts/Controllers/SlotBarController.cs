using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlotBarController : MonoBehaviour
{
    private List<Item> m_slotBar = new List<Item>();
    private const int MAX_SLOT_SIZE = 5;
    private Vector3 m_slotBarOrigin;
    private float m_slotSpacing = 1.0f;

    private GameManager m_gameManager;
    private Board m_board;
    private BoardController m_boardController;

    public int ItemCount => m_slotBar.Count;
    public bool IsFull => m_slotBar.Count >= MAX_SLOT_SIZE;

    public void Init(BoardController boardController, GameManager gameManager, Board board, GameSettings gameSettings)
    {
        m_boardController = boardController;
        m_gameManager = gameManager;
        m_board = board;

        // Tính toán vị trí khay chứa nằm dưới đáy bàn chơi
        m_slotBarOrigin = new Vector3(-2f, -gameSettings.BoardSizeY * 0.5f - 1.5f, 0f);
        GameObject prefabBG = Resources.Load<GameObject>(Constants.PREFAB_CELL_BACKGROUND);
        
        for (int i = 0; i < MAX_SLOT_SIZE; i++)
        {
            GameObject bg = Instantiate(prefabBG);
            bg.transform.position = m_slotBarOrigin + new Vector3(i * m_slotSpacing, 0, 0);
            bg.transform.SetParent(this.transform);
        }
    }

    public void PickupItem(Cell cell)
    {
        NormalItem item = cell.Item as NormalItem;
        if (item == null) return;

        cell.Free(); // Nhấc khỏi lưới
        
        item.SetSortingLayerHigher(); // Hiển thị đè lên các item khác khi bay

        // Cá luôn xếp vào ô trống tiếp theo (cuối khay)
        m_slotBar.Add(item);
        
        UpdateSlotBarVisuals();

        StartCoroutine(CheckMatchInSlotBarCoroutine());
    }

    private void UpdateSlotBarVisuals()
    {
        for (int i = 0; i < m_slotBar.Count; i++)
        {
            Vector3 targetPos = m_slotBarOrigin + new Vector3(i * m_slotSpacing, 0, 0);
            m_slotBar[i].View.DOMove(targetPos, 0.2f);
        }
    }

    private IEnumerator CheckMatchInSlotBarCoroutine()
    {
        m_boardController.SetBusy(true); // Khóa input người chơi
        yield return new WaitForSeconds(0.25f); // Đợi cá bay xuống khay xong

        // Quét tìm 3 con LIỀN KỀ cùng loại trong khay
        bool hasMatch = false;
        int startIndex = -1;

        for (int i = 0; i <= m_slotBar.Count - 3; i++)
        {
            NormalItem a = m_slotBar[i] as NormalItem;
            NormalItem b = m_slotBar[i + 1] as NormalItem;
            NormalItem c = m_slotBar[i + 2] as NormalItem;

            if (a != null && b != null && c != null &&
                a.ItemType == b.ItemType && b.ItemType == c.ItemType)
            {
                startIndex = i;
                hasMatch = true;
                break;
            }
        }

        if (hasMatch)
        {
            // Xóa đúng 3 con đứng cạnh nhau
            for (int i = 0; i < 3; i++)
            {
                m_slotBar[startIndex].ExplodeView();
                m_slotBar.RemoveAt(startIndex);
            }

            yield return new WaitForSeconds(0.2f); // Đợi nổ xong
            UpdateSlotBarVisuals(); // Dồn các cá còn lại về bên trái
            
            yield return new WaitForSeconds(0.2f); // Đợi cá dồn xong
        }

        // Kiểm tra điều kiện Thắng / Thua
        if (m_board.IsBoardEmpty() && m_slotBar.Count == 0)
        {
            Debug.Log("🎉 YOU WIN!");
            m_gameManager.GameWin();
        }
        else if (m_slotBar.Count == MAX_SLOT_SIZE && !hasMatch)
        {
            Debug.Log("💀 YOU LOSE!");
            m_gameManager.GameOver();
        }

        m_boardController.SetBusy(false); // Mở lại input
    }

    public List<Item> GetItems()
    {
        return m_slotBar;
    }
}
