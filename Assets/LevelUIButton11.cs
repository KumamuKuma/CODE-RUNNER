using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelUIButton11 : MonoBehaviour
{
    public void RestartLevel()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(sceneName);
    }

    public void BackToMainMenu()
    {
        SceneManager.LoadScene("0 MainMenu");
    }
}
