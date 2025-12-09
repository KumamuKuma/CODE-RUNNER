using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

public class UI_PlacedBlock : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Data")]
    public CommandType myCommand = CommandType.None;

    [Header("UI Components")]
    public Button myButton;
    public Image directionIconImage;
    public TextMeshProUGUI stepText;
    public Sprite arrowSprite;

    [Header("IF Condition Data")]
    public Vector2Int conditionDir = Vector2Int.zero;
    public ConditionOperator conditionOp = ConditionOperator.Equals;
    public TargetType conditionTarget = TargetType.Fire;

    // Popup references
    private UI_DirectionPopup _popupPanelRef;
    private UI_IfConfigPopup _ifPopupRef;

    // Indentation control
    [Header("Indent Settings")]
    public float indentStepWidth = 40f; // Indent width (in pixels) per level
    private LayoutElement _indentSpacer; // Left spacer used for visual indentation

    // Drag state
    private GameObject _dragGhost;        // Visual ghost object while dragging
    private CanvasGroup _selfCanvasGroup; // Used to fade the original while dragging
    private RectTransform _canvasRect;    // Canvas rect for coordinate conversion
    private UI_TimelineDropArea _parentDropArea;

    public bool IsDragging { get; private set; } = false;

    private void Awake()
    {
        _selfCanvasGroup = GetComponent<CanvasGroup>();
        if (_selfCanvasGroup == null) _selfCanvasGroup = gameObject.AddComponent<CanvasGroup>();

        // Cache canvas rect for ScreenPointToWorldPoint conversion
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null) _canvasRect = canvas.transform as RectTransform;
    }

    public void Initialize(UI_DirectionPopup dirPopup, UI_IfConfigPopup ifPopup = null)
    {
        _popupPanelRef = dirPopup;
        _ifPopupRef = ifPopup;
        _parentDropArea = GetComponentInParent<UI_TimelineDropArea>();

        // Find the indent spacer under the parent row
        // Assumes structure: Row -> [IndentSpacer][Block]
        Transform spacerTrans = transform.parent.Find("IndentSpacer");
        if (spacerTrans != null) _indentSpacer = spacerTrans.GetComponent<LayoutElement>();

        if (myButton != null)
        {
            myButton.onClick.RemoveAllListeners();
            // IF-block opens IF configuration popup; others open direction popup
            if (myCommand == CommandType.If_Start)
            {
                myButton.onClick.AddListener(OnIfBlockClicked);
            }
            else
            {
                myButton.onClick.AddListener(OnBlockClicked);
            }
        }
        UpdateIconDisplay();
    }

    void OnIfBlockClicked()
    {
        if (_ifPopupRef != null && !IsDragging) _ifPopupRef.OpenPopup(this);
    }

    void OnBlockClicked()
    {
        // Structural blocks do not open the direction popup
        if (myCommand == CommandType.Loop_Start || myCommand == CommandType.Loop_End ||
            myCommand == CommandType.Else || myCommand == CommandType.End_If) return;

        if (!IsDragging && _popupPanelRef != null) _popupPanelRef.OpenPopup(this);
    }

    public void SetIndentLevel(int level)
    {
        if (_indentSpacer != null)
        {
            float width = level * indentStepWidth;
            _indentSpacer.minWidth = width;
            _indentSpacer.preferredWidth = width;
        }
    }

    // ========================================================
    //  Drag & drop behaviour (ghost-style drag)
    // ========================================================

    public void OnBeginDrag(PointerEventData eventData)
    {
        IsDragging = true;

        // 1. Create a ghost copy of the entire row
        GameObject rowObj = transform.parent.gameObject;
        _dragGhost = Instantiate(rowObj, _canvasRect);
        _dragGhost.transform.SetAsLastSibling();
        _dragGhost.transform.localScale = Vector3.one;

        // 2. Make the ghost semi-transparent and non-blocking
        CanvasGroup ghostCG = _dragGhost.GetComponentInChildren<CanvasGroup>();
        if (ghostCG == null) ghostCG = _dragGhost.AddComponent<CanvasGroup>();
        ghostCG.blocksRaycasts = false;
        ghostCG.alpha = 0.6f;

        // 3. Hide the original visually
        _selfCanvasGroup.alpha = 0f;

        // 4. Position the ghost under the cursor
        SetGhostPosition(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_dragGhost != null) SetGhostPosition(eventData.position);

        // Reorder the row inside the timeline based on mouse position
        if (_parentDropArea != null)
        {
            Vector3 worldPos;
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(_canvasRect, eventData.position, Camera.main, out worldPos))
            {
                int newIndex = GetInsertIndexInParent(worldPos.y);
                transform.parent.SetSiblingIndex(newIndex);

                _parentDropArea.UpdateLineNumbersAndIndents();
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        IsDragging = false;

        // 1. Destroy the drag ghost
        if (_dragGhost != null) Destroy(_dragGhost);

        // 2. Restore original visibility
        _selfCanvasGroup.alpha = 1f;

        // 3. If released outside the drop area, delete the row
        if (!IsPointerOverDropArea(eventData))
        {
            Destroy(transform.parent.gameObject);

            if (_parentDropArea)
            {
                _parentDropArea.Invoke("UpdateLineNumbersAndIndents", 0.05f);
            }
        }
        else
        {
            if (_parentDropArea) _parentDropArea.UpdateLineNumbersAndIndents();
        }
    }

    // --- Helpers ---

    private void SetGhostPosition(Vector2 screenPos)
    {
        Vector3 worldPos;
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(_canvasRect, screenPos, Camera.main, out worldPos))
        {
            _dragGhost.transform.position = worldPos;
            Vector3 localPos = _dragGhost.transform.localPosition;
            localPos.z = 0f;
            _dragGhost.transform.localPosition = localPos;
        }
    }

    private bool IsPointerOverDropArea(PointerEventData eventData)
    {
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        foreach (RaycastResult result in results)
        {
            if (result.gameObject.GetComponent<UI_TimelineDropArea>() != null) return true;
        }
        return false;
    }

    // Calculate which row index this block should move to based on mouse world Y
    private int GetInsertIndexInParent(float mouseWorldY)
    {
        Transform container = transform.parent.parent;
        int childCount = container.childCount;

        for (int i = 0; i < childCount; i++)
        {
            Transform childRow = container.GetChild(i);
            if (childRow == transform.parent) continue;
            if (childRow.name == "Placeholder") continue;

            if (mouseWorldY > childRow.position.y)
            {
                return i;
            }
        }
        return childCount;
    }

    public void SetDirection(CommandType newDir)
    {
        myCommand = newDir;
        UpdateIconDisplay();
    }

    public void UpdateIconDisplay()
    {
        // 1. Hide icon if there is no command
        if (myCommand == CommandType.None)
        {
            if (directionIconImage != null) directionIconImage.enabled = false;
            return;
        }

        // 2. Structural commands: no arrow icon
        if (myCommand == CommandType.If_Start ||
            myCommand == CommandType.Else ||
            myCommand == CommandType.End_If ||
            myCommand == CommandType.Loop_Start ||
            myCommand == CommandType.Loop_End)
        {
            if (directionIconImage != null) directionIconImage.enabled = false;
            return;
        }

        // 3. Movement commands: show arrow icon and rotate accordingly
        if (directionIconImage != null)
        {
            directionIconImage.enabled = true;
            directionIconImage.sprite = arrowSprite;
            directionIconImage.transform.localRotation = Quaternion.identity;

            float angle = 0f;
            switch (myCommand)
            {
                case CommandType.Move_Up: angle = 0f; break;
                case CommandType.Move_Down: angle = 180f; break;
                case CommandType.Move_Left: angle = 90f; break;
                case CommandType.Move_Right: angle = -90f; break;
                case CommandType.Move_LeftUp: angle = 45f; break;
                case CommandType.Move_RightUp: angle = -45f; break;
                case CommandType.Move_LeftDown: angle = 135f; break;
                case CommandType.Move_RightDown: angle = -135f; break;
            }
            directionIconImage.transform.localRotation = Quaternion.Euler(0, 0, angle);
        }
    }
}
