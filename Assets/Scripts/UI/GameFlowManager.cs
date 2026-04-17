using UnityEngine;

public enum GameFlowState
{
    Playing,
    Paused,
    GameOver,
    Victory
}

public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance;

    [Header("UI Panels")]
    public GameObject pausePanel;
    public GameObject gameOverPanel;
    public GameObject victoryPanel;
    [SerializeField] private GameFlowState debugState;

    public GameFlowState CurrentState { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        SetState(GameFlowState.Playing);
    }

    private void Update()
    {
        if (CurrentState == GameFlowState.GameOver || CurrentState == GameFlowState.Victory)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (CurrentState == GameFlowState.Playing)
                SetState(GameFlowState.Paused);
            else if (CurrentState == GameFlowState.Paused)
                SetState(GameFlowState.Playing);
        }
    }

    public void SetState(GameFlowState newState)
    {
        CurrentState = newState;
        debugState = newState;

        // Reset all panels first
        pausePanel.SetActive(false);
        gameOverPanel.SetActive(false);
        victoryPanel.SetActive(false);

        switch (newState)
        {
            case GameFlowState.Playing:
                Time.timeScale = 1f;
                SetCursor(false);
                break;

            case GameFlowState.Paused:
                pausePanel.SetActive(true);
                Time.timeScale = 0f;
                SetCursor(true);
                break;

            case GameFlowState.GameOver:
                gameOverPanel.SetActive(true);
                Time.timeScale = 0f;
                SetCursor(true);
                break;

            case GameFlowState.Victory:
                victoryPanel.SetActive(true);
                Time.timeScale = 0f;
                SetCursor(true);
                break;
        }
    }
    public void ResumeGame()
    {
        SetState(GameFlowState.Playing);
    }

    public void PauseGame()
    {
        SetState(GameFlowState.Paused);
    }

    private void SetCursor(bool visible)
    {
        Cursor.visible = visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
    }
}