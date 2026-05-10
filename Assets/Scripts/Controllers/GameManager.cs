using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public event Action<eStateGame> StateChangedAction = delegate { };

    public enum eLevelMode
    {
        TIMER,
        MOVES,
        TIME_ATTACK
    }

    public enum eStateGame
    {
        SETUP,
        MAIN_MENU,
        GAME_STARTED,
        PAUSE,
        GAME_OVER,
        GAME_WIN,
    }

    private eStateGame m_state;
    public eStateGame State
    {
        get { return m_state; }
        private set
        {
            m_state = value;

            StateChangedAction(m_state);
        }
    }


    private GameSettings m_gameSettings;


    private BoardController m_boardController;

    private UIMainManager m_uiMenu;

    private LevelCondition m_levelCondition;

    private void Awake()
    {
        State = eStateGame.SETUP;

        m_gameSettings = Resources.Load<GameSettings>(Constants.GAME_SETTINGS_PATH);

        m_uiMenu = FindObjectOfType<UIMainManager>();
        m_uiMenu.Setup(this);
    }

    void Start()
    {
        State = eStateGame.MAIN_MENU;
    }

    // Update is called once per frame
    void Update()
    {
        if (m_boardController != null) m_boardController.Update();
    }


    internal void SetState(eStateGame state)
    {
        State = state;

        if(State == eStateGame.PAUSE)
        {
            DOTween.PauseAll();
        }
        else
        {
            DOTween.PlayAll();
        }
    }

    public void LoadLevel(eLevelMode mode)
    {
        LoadLevel(mode, BoardController.eAutoPlayMode.NONE);
    }

    public void LoadLevel(eLevelMode mode, BoardController.eAutoPlayMode autoPlayMode)
    {
        bool isTimeAttack = (mode == eLevelMode.TIME_ATTACK);

        m_boardController = new GameObject("BoardController").AddComponent<BoardController>();
        m_boardController.StartGame(this, m_gameSettings, autoPlayMode, isTimeAttack);

        // Nếu là Time Attack, đăng ký lắng nghe event để cập nhật Text đếm ngược trên UI
        if (isTimeAttack)
        {
            UnityEngine.UI.Text timerText = m_uiMenu.GetLevelConditionView();
            if (timerText != null)
            {
                timerText.gameObject.SetActive(true);
                m_boardController.OnTimeUpdateEvent += (timeRemaining) =>
                {
                    int seconds = Mathf.CeilToInt(Mathf.Max(0f, timeRemaining));
                    timerText.text = $"⏱ {seconds}s";

                    // Đổi màu đỏ khi dưới 10 giây
                    timerText.color = seconds <= 10 ? Color.red : Color.white;
                };
            }
        }

        State = eStateGame.GAME_STARTED;
    }

    public void GameOver()
    {
        StartCoroutine(WaitBoardController(eStateGame.GAME_OVER));
    }

    public void GameWin()
    {
        StartCoroutine(WaitBoardController(eStateGame.GAME_WIN));
    }

    internal void ClearLevel()
    {
        if (m_boardController)
        {
            m_boardController.Clear();
            Destroy(m_boardController.gameObject);
            m_boardController = null;
        }
    }

    private IEnumerator WaitBoardController(eStateGame endState)
    {
        while (m_boardController != null && m_boardController.IsBusy)
        {
            yield return new WaitForEndOfFrame();
        }

        yield return new WaitForSeconds(1f);

        State = endState;

        if (m_levelCondition != null)
        {
            m_levelCondition.ConditionCompleteEvent -= GameOver;

            Destroy(m_levelCondition);
            m_levelCondition = null;
        }
    }
}
