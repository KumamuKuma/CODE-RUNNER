using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class LevelItemUI : MonoBehaviour
{
    [SerializeField] int index;
    [SerializeField] TextMeshProUGUI levelNameText;
    Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
    }
    public void Set(string levelName, int index)
    {
        levelNameText.text = levelName;
        this.index = index;
    }

    void OnClick()
    {
        SceneManager.LoadScene(index);
    }
}
