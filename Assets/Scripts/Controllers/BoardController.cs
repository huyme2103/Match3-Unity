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

    private SlotBarController m_slotBarController;
    private AutoPlayBot m_botAI;
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

        // Khởi tạo Khay chứa (Slot Bar)
        m_slotBarController = gameObject.AddComponent<SlotBarController>();
        m_slotBarController.Init(this, m_gameManager, m_board, m_gameSettings);

        // Khởi tạo Bot AutoPlay
        m_botAI = gameObject.AddComponent<AutoPlayBot>();
        m_botAI.Init(this, m_slotBarController, m_board, m_gameManager, m_autoPlayMode);

        Fill();
    }



    private void Fill()
    {
        m_board.Fill();

        // Tile Match: Chỉ cần fill bàn, không cần check match liên hoàn như cũ
        IsBusy = false;

        // Bắt đầu AutoPlay nếu có
        m_botAI.StartBot();
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
            case GameManager.eStateGame.GAME_WIN:
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
                    if (!m_slotBarController.IsFull)
                    {
                        m_slotBarController.PickupItem(cell);
                    }
                }
            }
        }
    }
    #endregion

    #region Public Methods
    public void SetBusy(bool state)
    {
        IsBusy = state;
    }

    internal void Clear()
    {
        m_board.Clear();
    }
    #endregion
}

