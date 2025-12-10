using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] Button startGameButton,quitButton;
    [SerializeField] GameObject mainMenuPanel, selectLevelPanel;

    [SerializeField] Button returnMainMenuButton;
    [SerializeField] string[] levelNames;
    [SerializeField] LevelItemUI levelItemUIPrefab;
    [SerializeField] Transform levelItemUIContainer;

    void Start()
    {
        startGameButton.onClick.AddListener(StartGame);
        quitButton.onClick.AddListener(QuitGame);

        returnMainMenuButton.onClick.AddListener(ReturnMainMenu);
        UpdateLevelItemUI();
    }
    #region MainMenu
    void StartGame()
    {
        mainMenuPanel.SetActive(false);
        selectLevelPanel.SetActive(true);
    }
    public void QuitGame()
    {
        // 在编辑器中退出播放模式
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // 在构建的应用程序中退出
        Application.Quit();
#endif
    }
    #endregion
    #region LevelPanel
    void ReturnMainMenu()
    {
        mainMenuPanel.SetActive(true);
        selectLevelPanel.SetActive(false);
    }
    void UpdateLevelItemUI()
    {
        for(int i=0; i< levelNames.Length; i++)
        {
            LevelItemUI ui = Instantiate(levelItemUIPrefab, levelItemUIContainer);
            ui.Set(levelNames[i], i + 1);
        }
    }
    #endregion
}
