using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class UI_IfConfigPopup : MonoBehaviour
{
    [Header("1. Direction Selection (9-grid)")]
    public Button btnUp;
    public Button btnDown;
    public Button btnLeft;
    public Button btnRight;
    public Button btnCenter;

    [Header("2. Operator Selection")]
    public TMP_Dropdown operatorDropdown;

    [Header("3. Target Selection")]
    public TMP_Dropdown targetDropdown;

    [Header("4. Close Button")]
    public Button closeButton;

    // Selected color vs default color
    private Color normalColor = Color.white;
    private Color selectedColor = new Color(1f, 0.8f, 0.2f); // Golden yellow

    private UI_PlacedBlock _currentBlock;

    void Start()
    {
        if (closeButton) closeButton.onClick.AddListener(ClosePopup);

        if (operatorDropdown) operatorDropdown.onValueChanged.AddListener(OnOperatorChanged);
        if (targetDropdown) targetDropdown.onValueChanged.AddListener(OnTargetChanged);

        // Bind directional buttons
        BindDirectionBtn(btnUp, Vector2Int.up);
        BindDirectionBtn(btnDown, Vector2Int.down);
        BindDirectionBtn(btnLeft, Vector2Int.left);
        BindDirectionBtn(btnRight, Vector2Int.right);
        BindDirectionBtn(btnCenter, Vector2Int.zero);

        gameObject.SetActive(false);
    }

    // Helper: bind click event to a direction button
    void BindDirectionBtn(Button btn, Vector2Int dir)
    {
        if (btn != null)
        {
            btn.onClick.AddListener(() => {
                OnDirectionSelected(dir);
                UpdateButtonVisuals(dir); // Refresh highlight immediately
            });
        }
    }

    public void OpenPopup(UI_PlacedBlock block)
    {
        _currentBlock = block;
        gameObject.SetActive(true);

        // Restore dropdown selections
        if (operatorDropdown) operatorDropdown.value = (int)_currentBlock.conditionOp;
        if (targetDropdown) targetDropdown.value = (int)_currentBlock.conditionTarget;

        // Restore highlight of direction buttons
        UpdateButtonVisuals(_currentBlock.conditionDir);
    }

    public void ClosePopup()
    {
        gameObject.SetActive(false);
        _currentBlock = null;
    }

    void OnDirectionSelected(Vector2Int dir)
    {
        if (_currentBlock != null)
        {
            _currentBlock.conditionDir = dir;
            Debug.Log($"IF direction updated: {dir}");
        }
    }

    void OnOperatorChanged(int index)
    {
        if (_currentBlock != null) _currentBlock.conditionOp = (ConditionOperator)index;
    }

    void OnTargetChanged(int index)
    {
        if (_currentBlock != null) _currentBlock.conditionTarget = (TargetType)index;
    }

    // Update button highlight state
    void UpdateButtonVisuals(Vector2Int activeDir)
    {
        SetBtnColor(btnUp, Vector2Int.up, activeDir);
        SetBtnColor(btnDown, Vector2Int.down, activeDir);
        SetBtnColor(btnLeft, Vector2Int.left, activeDir);
        SetBtnColor(btnRight, Vector2Int.right, activeDir);
        SetBtnColor(btnCenter, Vector2Int.zero, activeDir);
    }

    void SetBtnColor(Button btn, Vector2Int myDir, Vector2Int activeDir)
    {
        if (btn == null) return;
        Image img = btn.GetComponent<Image>();
        if (img != null)
        {
            img.color = (myDir == activeDir) ? selectedColor : normalColor;
        }
    }
}
