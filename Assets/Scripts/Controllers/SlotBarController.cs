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

    // Lưu vị trí ban đầu của icon
    private Dictionary<Item, Cell> m_originalCells = new Dictionary<Item, Cell>();

    public int ItemCount => m_slotBar.Count;
    public bool IsFull => m_slotBar.Count >= MAX_SLOT_SIZE;

    public void Init(BoardController boardController, GameManager gameManager, Board board, GameSettings gameSettings)
    {
        m_boardController = boardController;
        m_gameManager = gameManager;
        m_board = board;

        // Tính toán vị trí khay chứa
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

        // Lưu vị trí gốc trước khi nhấc icon
        m_originalCells[item] = cell;

        cell.Free(); 
        
        item.SetSortingLayerHigher(); 

        
        m_slotBar.Add(item);

      
        int slotIndex = m_slotBar.Count - 1;
        Vector3 targetPos = m_slotBarOrigin + new Vector3(slotIndex * m_slotSpacing, 0, 0);
        item.View.DOKill();
        item.View.DOJump(targetPos, 1.5f, 1, 0.35f).SetEase(Ease.OutQuad);

        StartCoroutine(CheckMatchInSlotBarCoroutine());
    }

    // Trả icon cá về
    public bool ReturnItemToBoard(Item item)
    {
        if (!m_slotBar.Contains(item)) return false;

        Cell originalCell;
        if (!m_originalCells.TryGetValue(item, out originalCell)) return false;

       
        if (!originalCell.IsEmpty) return false;

        StartCoroutine(ReturnItemCoroutine(item, originalCell));
        return true;
    }

    private IEnumerator ReturnItemCoroutine(Item item, Cell originalCell)
    {
        m_boardController.SetBusy(true); 

        m_slotBar.Remove(item);
        m_originalCells.Remove(item);

        // Đặt cá lại vào ô gốc
        originalCell.Assign(item);

       
        item.View.DOKill();
        item.View.DOJump(originalCell.transform.position, 1.5f, 1, 0.35f).SetEase(Ease.OutQuad);
        item.SetSortingLayerLower();

        // Dồn icon trong khay
        UpdateSlotBarVisuals();

        yield return new WaitForSeconds(0.35f); 
        m_boardController.SetBusy(false); 
    }

    private void UpdateSlotBarVisuals()
    {
        for (int i = 0; i < m_slotBar.Count; i++)
        {
            Vector3 targetPos = m_slotBarOrigin + new Vector3(i * m_slotSpacing, 0, 0);
            m_slotBar[i].View.DOKill();
            m_slotBar[i].View.DOMove(targetPos, 0.2f);
        }
    }

    private IEnumerator CheckMatchInSlotBarCoroutine()
    {
        m_boardController.SetBusy(true); 
        yield return new WaitForSeconds(0.25f); 

        // Tìm 3 cá giống nhau liền kề
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
            // Hiệu ứng thu nhỏ 
            for (int i = 0; i < 3; i++)
            {
                Item matchedItem = m_slotBar[startIndex];
                m_originalCells.Remove(matchedItem); // Xóa khỏi danh sách lưu vị trí

                if (matchedItem.View != null)
                {
                    Transform view = matchedItem.View;
                    view.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack).OnComplete(() =>
                    {
                        GameObject.Destroy(view.gameObject);
                    });
                }
                m_slotBar.RemoveAt(startIndex);
            }

            yield return new WaitForSeconds(0.35f); 
            UpdateSlotBarVisuals(); 
            
            yield return new WaitForSeconds(0.2f); 
        }

        // điều kiện thắng thua
        if (m_board.IsBoardEmpty() && m_slotBar.Count == 0)
        {
            
            m_gameManager.GameWin();
        }
        else if (m_slotBar.Count == MAX_SLOT_SIZE && !hasMatch && !m_isTimeAttackMode)
        {
           
            m_gameManager.GameOver();
        }
        m_boardController.SetBusy(false); 
    }

    public List<Item> GetItems()
    {
        return m_slotBar;
    }

    // Tìm cá trong khay dựa vào vị trí click
    public Item GetItemAtPosition(Vector3 worldPos)
    {
        float clickRadius = 0.5f; 
        foreach (var item in m_slotBar)
        {
            if (item.View != null)
            {
                float dist = Vector3.Distance(item.View.position, worldPos);
                if (dist < clickRadius)
                {
                    return item;
                }
            }
        }
        return null;
    }

    private bool m_isTimeAttackMode = false;

    public void SetTimeAttackMode(bool isTimeAttack)
    {
        m_isTimeAttackMode = isTimeAttack;
    }
}
