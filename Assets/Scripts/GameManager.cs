using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    Playing, // Game is in progress
    Success, // All objectives completed
    Fail     // Failure condition reached
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Configuration")]
    public PriestUnit[] allPriests;       // Reference to all priests in the scene
    public string successSceneName = "Success";
    public string failSceneName = "Fail";
    public float sceneLoadDelay = 1f;

    public GameState CurrentState { get; private set; }

    void Awake()
    {
        // Simple singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // If you want the manager to persist across scenes, uncomment:
        // DontDestroyOnLoad(gameObject);

        CurrentState = GameState.Playing;

        // If FirePit has static state, it can be reset here if needed.
    }

    void Update()
    {
        if (CurrentState != GameState.Playing) return;

        // Win condition: all priests are alive AND all priests have reached their endpoints
        if (CheckAllPriestsAlive() && CheckAllPriestsReachEnd())
        {
            SetGameState(GameState.Success);
        }
    }

    /// <summary>
    /// Central method to set game state and trigger success/fail logic.
    /// </summary>
    public void SetGameState(GameState newState)
    {
        // Avoid repeated transitions (e.g., multiple Fail calls)
        if (CurrentState == newState) return;

        CurrentState = newState;

        switch (newState)
        {
            case GameState.Success:
                Debug.Log("Game success!");
                // Trigger victory animations for all alive priests
                foreach (PriestUnit priest in allPriests)
                {
                    if (priest != null && priest.IsAlive)
                    {
                        priest.PlayVictoryAnimation();
                    }
                }
                Invoke(nameof(LoadSuccessScene), sceneLoadDelay);
                break;

            case GameState.Fail:
                Debug.Log("Game failed: priest died or conditions not satisfied.");
                Invoke(nameof(LoadFailScene), sceneLoadDelay);
                break;
        }
    }

    /// <summary>
    /// Check whether all priests are alive.
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
    /// Check whether all priests have reached their endpoints.
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

    private void LoadSuccessScene()
    {
        SceneManager.LoadScene(successSceneName);
    }

    private void LoadFailScene()
    {
        SceneManager.LoadScene(failSceneName);
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        CurrentState = GameState.Playing;
    }
}
