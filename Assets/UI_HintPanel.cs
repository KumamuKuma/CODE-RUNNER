using UnityEngine;

public class UI_HintPanel : MonoBehaviour
{
    [Header("Hint Panel")]
    public GameObject hintPanel;

    void Start()
    {
        if (hintPanel != null)
            hintPanel.SetActive(false); // Ensure hidden on start
    }

    // Called by hint button
    public void ShowHint()
    {
        if (hintPanel != null)
            hintPanel.SetActive(true);
    }

    // Called by close button
    public void HideHint()
    {
        if (hintPanel != null)
            hintPanel.SetActive(false);
    }
}
