using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BoardController : MonoBehaviour
{
    #region Variables & Enums
    // Giai đoạn 4: Enum cho chế độ AutoPlay
    public enum eAutoPlayMode
    {
        NONE,
        AUTO_WIN,
        AUTO_LOSE
    }

    public event Action OnMoveEvent = delegate { };

    public bool IsBusy { get; private set; }

    private Board m_board;

    private GameManager m_gameManager;

    private Camera m_cam;

    private GameSettings m_gameSettings;

    private bool m_gameOver;

    // --- Giai đoạn 2: Biến cho Khay chứa (Slot Bar) ---
    private List<Item> m_slotBar = new List<Item>();
    private const int MAX_SLOT_SIZE = 5;
    private Vector3 m_slotBarOrigin;
    private float m_slotSpacing = 1.0f;

    private eAutoPlayMode m_autoPlayMode = eAutoPlayMode.NONE;
    #endregion

    #region Setup & Initialization
    public void StartGame(GameManager gameManager, GameSettings gameSettings, eAutoPlayMode autoPlayMode = eAutoPlayMode.NONE)
    {
        m_gameManager = gameManager;

        m_gameSettings = gameSettings;

        m_autoPlayMode = autoPlayMode;

        m_gameManager.StateChangedAction += OnGameStateChange;

        m_cam = Camera.main;

        m_board = new Board(this.transform, gameSettings);

        CreateSlotBarBackground();

        Fill();
    }

    private void CreateSlotBarBackground()
    {
        // Tính toán vị trí khay chứa nằm dưới đáy bàn chơi
        m_slotBarOrigin = new Vector3(-2f, -m_gameSettings.BoardSizeY * 0.5f - 1.5f, 0f);
        GameObject prefabBG = Resources.Load<GameObject>(Constants.PREFAB_CELL_BACKGROUND);
        
        for (int i = 0; i < MAX_SLOT_SIZE; i++)
        {
            GameObject bg = Instantiate(prefabBG);
            bg.transform.position = m_slotBarOrigin + new Vector3(i * m_slotSpacing, 0, 0);
            bg.transform.SetParent(this.transform);
        }
    }

    private void Fill()
    {
        m_board.Fill();

        // Tile Match: Chỉ cần fill bàn, không cần check match liên hoàn như cũ
        IsBusy = false;

        // --- Giai đoạn 4: Bắt đầu AutoPlay nếu có ---
        if (m_autoPlayMode != eAutoPlayMode.NONE)
        {
            StartCoroutine(AutoPlayCoroutine());
        }
    }

    private void OnGameStateChange(GameManager.eStateGame state)
    {
        switch (state)
        {
            case GameManager.eStateGame.GAME_STARTED:
                IsBusy = false;
                break;
            case GameManager.eStateGame.PAUSE:
                IsBusy = true;
                break;
            case GameManager.eStateGame.GAME_OVER:
                m_gameOver = true;
                break;
        }
    }
    #endregion

    #region Unity Logic (Input)
    public void Update()
    {
        if (m_gameOver) return;
        if (IsBusy) return;

        // Giai đoạn 2: Logic Click nhặt cá vào Khay chứa
        if (Input.GetMouseButtonDown(0))
        {
            var hit = Physics2D.Raycast(m_cam.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
            if (hit.collider != null)
            {
                Cell cell = hit.collider.GetComponent<Cell>();
                if (cell != null && !cell.IsEmpty)
                {
                    // Chỉ nhặt được nếu khay chứa chưa đầy
                    if (m_slotBar.Count < MAX_SLOT_SIZE)
                    {
                        PickupItem(cell);
                    }
                }
            }
        }
    }
    #endregion

    #region Slot Bar Logic
    private void PickupItem(Cell cell)
    {
        NormalItem item = cell.Item as NormalItem;
        if (item == null) return;

        cell.Free(); // Nhấc khỏi lưới
        
        item.SetSortingLayerHigher(); // Hiển thị đè lên các item khác khi bay

        // Cá luôn xếp vào ô trống tiếp theo (cuối khay)
        m_slotBar.Add(item);
        
        // Di chuyển cá bay xuống vị trí cuối khay
        UpdateSlotBarVisuals();

        // Đợi di chuyển xong rồi check match
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
        IsBusy = true; // Khóa input người chơi
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
            // Xóa đúng 3 con đứng cạnh nhau ở vị trí startIndex
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
            // THẮNG
            Debug.Log("🎉 YOU WIN!");
            m_gameManager.GameWin();
        }
        else if (m_slotBar.Count == MAX_SLOT_SIZE && !hasMatch)
        {
            // THUA
            Debug.Log("💀 YOU LOSE!");
            m_gameManager.GameOver();
        }

        IsBusy = false; // Mở lại input
    }
    #endregion

    #region AutoPlay Logic
    private IEnumerator AutoPlayCoroutine()
    {
        // Chờ xíu lúc đầu cho mượt
        yield return new WaitForSeconds(1.0f);

        while (!m_gameOver && m_gameManager.State == GameManager.eStateGame.GAME_STARTED)
        {
            // Chờ nếu đang bận xử lý logic bay/nổ cá của lượt trước
            while (IsBusy)
            {
                yield return null;
            }

            if (m_gameOver || m_gameManager.State != GameManager.eStateGame.GAME_STARTED) break;

            List<Cell> activeCells = m_board.GetAllActiveCells();
            if (activeCells.Count == 0) break; // Đã hết cá trên bàn

            Cell targetCell = null;

            if (m_autoPlayMode == eAutoPlayMode.AUTO_WIN)
            {
                // Auto Win: Tìm cá cùng loại với cá đang có trong khay
                targetCell = FindBestCellForAutoWin(activeCells);
            }
            else if (m_autoPlayMode == eAutoPlayMode.AUTO_LOSE)
            {
                // Auto Lose: Tìm cá KHÁC loại với cá đang có trong khay để nhanh đầy
                targetCell = FindWorstCellForAutoLose(activeCells);
            }

            if (targetCell != null && m_slotBar.Count < MAX_SLOT_SIZE)
            {
                Debug.Log($"Bot nhặt cá: Tọa độ ({targetCell.BoardX}, {targetCell.BoardY}) - Chế độ: {m_autoPlayMode}");
                PickupItem(targetCell);
            }

            // Tốc độ click của Bot (0.5s mỗi lượt)
            yield return new WaitForSeconds(0.5f); 
        }
    }

    private Cell FindBestCellForAutoWin(List<Cell> activeCells)
    {
        // 1. Nếu trong khay đang có cá, tìm cá cùng loại trên bàn
        var groups = m_slotBar.Cast<NormalItem>().GroupBy(x => x.ItemType).ToList();
        if (groups.Count > 0)
        {
            // Tìm loại cá đang có số lượng nhiều nhất trong khay (để ưu tiên nhặt cho nổ sớm)
            var bestGroup = groups.OrderByDescending(g => g.Count()).First();
            NormalItem.eNormalType targetType = bestGroup.Key;

            Cell foundCell = activeCells.FirstOrDefault(c => (c.Item as NormalItem)?.ItemType == targetType);
            if (foundCell != null) return foundCell;
        }

        // 2. Nếu khay trống hoặc không tìm thấy cá cùng loại trên bàn, nhặt bừa 1 con
        return activeCells[UnityEngine.Random.Range(0, activeCells.Count)];
    }

    private Cell FindWorstCellForAutoLose(List<Cell> activeCells)
    {
        // Auto Lose: Tìm loại cá CHƯA có trong khay để làm đầy khay mà không nổ
        var typesInSlot = m_slotBar.Cast<NormalItem>().Select(x => x.ItemType).Distinct().ToList();

        // Tìm con cá có loại không nằm trong typesInSlot
        Cell badCell = activeCells.FirstOrDefault(c => 
        {
            NormalItem item = c.Item as NormalItem;
            return item != null && !typesInSlot.Contains(item.ItemType);
        });

        if (badCell != null) return badCell;

        // Nếu xui xẻo không có (ví dụ trên bàn chỉ còn đúng các loại đang có trong khay), nhặt bừa
        return activeCells[UnityEngine.Random.Range(0, activeCells.Count)];
    }
    #endregion

    internal void Clear()
    {
        m_board.Clear();
    }
}

