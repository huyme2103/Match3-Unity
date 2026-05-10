using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BoardController : MonoBehaviour
{
    #region Variables & Enums

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
    private bool m_isTimeAttackMode = false;
    #endregion

    #region Setup & Initialization
    public void StartGame(GameManager gameManager, GameSettings gameSettings, eAutoPlayMode autoPlayMode = eAutoPlayMode.NONE, bool isTimeAttack = false)
    {
        m_gameManager = gameManager;

        m_gameSettings = gameSettings;

        m_autoPlayMode = autoPlayMode;

        m_isTimeAttackMode = isTimeAttack;

        m_gameManager.StateChangedAction += OnGameStateChange;

        m_cam = Camera.main;

        m_board = new Board(this.transform, gameSettings);

        // Khởi tạo khay chứa cá
        m_slotBarController = gameObject.AddComponent<SlotBarController>();
        m_slotBarController.Init(this, m_gameManager, m_board, m_gameSettings);

        // Khởi tạo bot chơi tự động
        m_botAI = gameObject.AddComponent<AutoPlayBot>();
        m_botAI.Init(this, m_slotBarController, m_board, m_gameManager, m_autoPlayMode);

        // Thiết lập chế độ Time Attack
        m_slotBarController.SetTimeAttackMode(m_isTimeAttackMode);

        Fill();

        // Chạy đếm ngược thời gian
        if (m_isTimeAttackMode)
        {
            StartCoroutine(TimeAttackCoroutine(60f));
        }
    }



    private void Fill()
    {
        m_board.Fill();

        IsBusy = false;

        // Bật chế độ tự chơi
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

        if (Input.GetMouseButtonDown(0))
        {
            // Kiểm tra click vào cá dưới khay
            if (m_isTimeAttackMode)
            {
                Vector3 worldPos = m_cam.ScreenToWorldPoint(Input.mousePosition);
                worldPos.z = 0;
                Item clickedItem = m_slotBarController.GetItemAtPosition(worldPos);
                if (clickedItem != null)
                {
                    m_slotBarController.ReturnItemToBoard(clickedItem);
                    return;
                }
            }

            // Kiểm tra click vào cá trên bàn
            var hit = Physics2D.Raycast(m_cam.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
            if (hit.collider != null)
            {
                Cell cell = hit.collider.GetComponent<Cell>();
                if (cell != null && !cell.IsEmpty)
                {
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

    public void SetTimeAttackMode(bool isTimeAttack)
    {
        m_isTimeAttackMode = isTimeAttack;
    }

    internal void Clear()
    {
        m_board.Clear();
    }
    #endregion

    #region Time Attack
    private IEnumerator TimeAttackCoroutine(float totalTime)
    {
        float timeRemaining = totalTime;

        while (timeRemaining > 0f && !m_gameOver)
        {
            timeRemaining -= Time.deltaTime;

            //  texttime
            OnTimeUpdateEvent?.Invoke(timeRemaining);

            
            if (m_board.IsBoardEmpty() && m_slotBarController.ItemCount == 0)
            {
                //Debug.Log("WIN");
                m_gameManager.GameWin();
                yield break;
            }

            yield return null;
        }

        // 
        if (!m_gameOver)
        {
            //Debug.Log("YOU LOSE");
            m_gameManager.GameOver();
        }
    }

    public event Action<float> OnTimeUpdateEvent;
    #endregion
}

