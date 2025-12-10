using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    Playing,
    Success,
    Fail
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Configuration")]
    public PriestUnit[] allPriests;

    [Header("Managers & Config")]
    public TurnManager turnManager;
    public UI_TimelineDropArea timelineDropArea;
    public LevelStarConfig starConfig;

    [Header("UI Panels")]
    public GameObject successPanel;
    public GameObject failPanel;
    public UI_SuccessPanel successPanelUI;

    [Header("Scenes")]
    public string mainMenuSceneName = "MainMenu";

    public GameState CurrentState { get; private set; }

    private void Awake()
    {
        // Simple singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        CurrentState = GameState.Playing;

        if (successPanel != null) successPanel.SetActive(false);
        if (failPanel != null) failPanel.SetActive(false);
    }

    private void Update()
    {
        if (CurrentState != GameState.Playing) return;

        // Win condition: all priests alive and all have reached the goal
        if (CheckAllPriestsAlive() && CheckAllPriestsReachEnd())
        {
            SetGameState(GameState.Success);
        }
    }

    /// <summary>
    /// Sets the current game state and triggers the corresponding UI / animations.
    /// </summary>
    public void SetGameState(GameState newState)
    {
        if (CurrentState == newState) return;

        CurrentState = newState;

        switch (newState)
        {
            case GameState.Success:
                Debug.Log("Game success!");

                foreach (PriestUnit priest in allPriests)
                {
                    if (priest != null && priest.IsAlive)
                        priest.PlayVictoryAnimation();
                }

                int stars = CalculateStars();
                Debug.Log("Stars earned: " + stars);

                if (successPanel != null) successPanel.SetActive(true);
                if (successPanelUI != null) successPanelUI.ShowStars(stars);

                // Optional: pause game here by setting Time.timeScale = 0
                break;

            case GameState.Fail:
                Debug.Log("Game failed.");

                if (failPanel != null) failPanel.SetActive(true);

                // Optional: pause game here by setting Time.timeScale = 0
                break;
        }
    }

    /// <summary>
    /// Computes star rating based on command line count and total steps.
    /// </summary>
    int CalculateStars()
    {
        int stars = 1;

        if (starConfig == null || turnManager == null || timelineDropArea == null)
            return stars;

        int lineCount = timelineDropArea.CurrentLineCount;
        int stepCount = turnManager.TotalStepCount;

        if (lineCount <= starConfig.maxCommandLinesForExtraStar)
            stars++;

        if (stepCount <= starConfig.maxStepsForExtraStar)
            stars++;

        return Mathf.Clamp(stars, 1, 3);
    }

    /// <summary>
    /// Checks if all priests are alive.
    /// </summary>
    private bool CheckAllPriestsAlive()
    {
        if (allPriests == null || allPriests.Length == 0) return false;

        foreach (PriestUnit priest in allPriests)
        {
            if (priest == null || !priest.IsAlive)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Checks if all priests have reached the goal tile.
    /// </summary>
    private bool CheckAllPriestsReachEnd()
    {
        if (allPriests == null || allPriests.Length == 0) return false;

        foreach (PriestUnit priest in allPriests)
        {
            if (priest == null || !priest.IsReachEnd)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Reloads the current scene and resets the game state.
    /// </summary>
    public void RestartGame()
    {
        // Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        CurrentState = GameState.Playing;
    }

    /// <summary>
    /// Returns to the main menu scene.
    /// </summary>
    public void BackToMainMenu()
    {
        // Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
