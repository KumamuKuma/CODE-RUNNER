using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class UI_IfConfigPopup : MonoBehaviour
{
    [Header("Direction Buttons")]
    public Button btnUp;
    public Button btnDown;
    public Button btnLeft;
    public Button btnRight;
    public Button btnUpLeft;
    public Button btnUpRight;
    public Button btnDownLeft;
    public Button btnDownRight;
    public Button btnCenter;

    [Header("Comparison Operator")]
    public TMP_Dropdown operatorDropdown;

    [Header("Target Type")]
    public TMP_Dropdown targetDropdown;

    [Header("Close Popup")]
    public Button closeButton;

    // Button colors
    private Color normalColor = Color.white;
    private Color selectedColor = new Color(1f, 0.8f, 0.2f);

    private UI_PlacedBlock _currentBlock;

    void Start()
    {
        if (closeButton) closeButton.onClick.AddListener(ClosePopup);

        if (operatorDropdown) operatorDropdown.onValueChanged.AddListener(OnOperatorChanged);
        if (targetDropdown) targetDropdown.onValueChanged.AddListener(OnTargetChanged);

        BindDirectionBtn(btnUp, Vector2Int.up);
        BindDirectionBtn(btnDown, Vector2Int.down);
        BindDirectionBtn(btnLeft, Vector2Int.left);
        BindDirectionBtn(btnRight, Vector2Int.right);
        BindDirectionBtn(btnCenter, Vector2Int.zero);

        BindDirectionBtn(btnUpLeft, new Vector2Int(-1, 1));
        BindDirectionBtn(btnUpRight, new Vector2Int(1, 1));
        BindDirectionBtn(btnDownLeft, new Vector2Int(-1, -1));
        BindDirectionBtn(btnDownRight, new Vector2Int(1, -1));

        gameObject.SetActive(false);
    }

    // Bind directional button and refresh highlight
    void BindDirectionBtn(Button btn, Vector2Int dir)
    {
        if (btn != null)
        {
            btn.onClick.AddListener(() =>
            {
                OnDirectionSelected(dir);
                UpdateButtonVisuals(dir);
            });
        }
    }

    public void OpenPopup(UI_PlacedBlock block)
    {
        _currentBlock = block;
        gameObject.SetActive(true);

        if (operatorDropdown) operatorDropdown.value = (int)_currentBlock.conditionOp;
        if (targetDropdown) targetDropdown.value = (int)_currentBlock.conditionTarget;

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
            _currentBlock.conditionDir = dir;
    }

    void OnOperatorChanged(int index)
    {
        if (_currentBlock != null)
            _currentBlock.conditionOp = (ConditionOperator)index;
    }

    void OnTargetChanged(int index)
    {
        if (_currentBlock != null)
            _currentBlock.conditionTarget = (TargetType)index;
    }

    // Refresh highlight colors
    void UpdateButtonVisuals(Vector2Int activeDir)
    {
        SetBtnColor(btnUp, Vector2Int.up, activeDir);
        SetBtnColor(btnDown, Vector2Int.down, activeDir);
        SetBtnColor(btnLeft, Vector2Int.left, activeDir);
        SetBtnColor(btnRight, Vector2Int.right, activeDir);
        SetBtnColor(btnCenter, Vector2Int.zero, activeDir);

        SetBtnColor(btnUpLeft, new Vector2Int(-1, 1), activeDir);
        SetBtnColor(btnUpRight, new Vector2Int(1, 1), activeDir);
        SetBtnColor(btnDownLeft, new Vector2Int(-1, -1), activeDir);
        SetBtnColor(btnDownRight, new Vector2Int(1, -1), activeDir);
    }

    void SetBtnColor(Button btn, Vector2Int myDir, Vector2Int activeDir)
    {
        if (btn == null) return;

        Image img = btn.GetComponent<Image>();
        if (img != null)
            img.color = (myDir == activeDir) ? selectedColor : normalColor;
    }
}
