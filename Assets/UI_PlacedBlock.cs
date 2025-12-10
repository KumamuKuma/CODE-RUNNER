using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

public class UI_PlacedBlock : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Data")]
    public CommandType myCommand = CommandType.None;

    [Header("UI References")]
    public Button myButton;
    public Image directionIconImage;
    public Sprite arrowSprite;
    public TextMeshProUGUI stepText;
    public TextMeshProUGUI indexText;

    [Header("IF Condition Data")]
    public Vector2Int conditionDir = Vector2Int.zero;
    public ConditionOperator conditionOp = ConditionOperator.Equals;
    public TargetType conditionTarget = TargetType.Fire;

    private UI_DirectionPopup _popupPanelRef;
    private UI_IfConfigPopup _ifPopupRef;

    private GameObject _dragGhost;
    private CanvasGroup _selfCanvasGroup;
    private RectTransform _canvasRect;
    private Canvas _rootCanvas;
    private UI_TimelineDropArea _parentDropArea;
    private Vector3 _dragOffset;

    public bool IsDragging { get; private set; } = false;

    // 获取正确的相机 (ScreenSpace-Camera 必须项)
    private Camera UICamera
    {
        get
        {
            if (_rootCanvas == null) _rootCanvas = GetComponentInParent<Canvas>();
            if (_rootCanvas == null) return Camera.main;
            if (_rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay) return null;
            return _rootCanvas.worldCamera != null ? _rootCanvas.worldCamera : Camera.main;
        }
    }

    // 在 UI_PlacedBlock.cs 中
    private void Awake()
    {
        _selfCanvasGroup = GetComponent<CanvasGroup>();
        if (_selfCanvasGroup == null) _selfCanvasGroup = gameObject.AddComponent<CanvasGroup>();

        // ★ 修改获取 Canvas 的方式，确保找最顶层的
        Canvas root = GetComponentInParent<Canvas>();
        if (root != null)
        {
            // 找到最根部的 Canvas，防止鬼影生成在局部 Canvas 里
            _rootCanvas = root.rootCanvas;
            _canvasRect = _rootCanvas.transform as RectTransform;
        }
    }

    public void Initialize(UI_DirectionPopup dirPopup, UI_IfConfigPopup ifPopup = null)
    {
        _popupPanelRef = dirPopup;
        _ifPopupRef = ifPopup;
        _parentDropArea = GetComponentInParent<UI_TimelineDropArea>();

        if (myButton != null)
        {
            myButton.onClick.RemoveAllListeners();
            if (myCommand == CommandType.If_Start) myButton.onClick.AddListener(OnIfBlockClicked);
            else myButton.onClick.AddListener(OnBlockClicked);
        }
        UpdateIconDisplay();
    }

    void OnIfBlockClicked() { if (_ifPopupRef != null && !IsDragging) _ifPopupRef.OpenPopup(this); }
    void OnBlockClicked()
    {
        if (myCommand == CommandType.Loop_Start || myCommand == CommandType.Loop_End ||
            myCommand == CommandType.Else || myCommand == CommandType.End_If) return;
        if (!IsDragging && _popupPanelRef != null) _popupPanelRef.OpenPopup(this);
    }

    // --- Drag Logic ---

    // 在 UI_PlacedBlock.cs 中替换 OnBeginDrag
    // 在 UI_PlacedBlock.cs 中
    public void OnBeginDrag(PointerEventData eventData)
    {
        IsDragging = true;
        GameObject rowObj = transform.parent.gameObject;

        // 1. 获取最根部的 Canvas (防止鬼影生在列表里)
        Canvas rootCanvas = GetComponentInParent<Canvas>().rootCanvas;

        // 2. 生成鬼影，并强制设为根 Canvas 的子物体
        _dragGhost = Instantiate(rowObj, rootCanvas.transform);
        _dragGhost.name = "TEMP_GHOST"; // ★ 改名，打上标记！

        // 3. 彻底禁用布局 (双重保险)
        LayoutElement le = _dragGhost.GetComponent<LayoutElement>();
        if (le == null) le = _dragGhost.AddComponent<LayoutElement>();
        le.ignoreLayout = true;

        // 4. 鬼影视觉处理 (保持你原有的)
        _dragGhost.transform.position = rowObj.transform.position;
        Transform ghostSpacer = _dragGhost.transform.Find("IndentSpacer");
        if (ghostSpacer != null) ghostSpacer.gameObject.SetActive(false); // 这就是为什么你之前看到它少东西

        CanvasGroup ghostCG = _dragGhost.GetComponentInChildren<CanvasGroup>();
        if (ghostCG == null) ghostCG = _dragGhost.AddComponent<CanvasGroup>();
        ghostCG.blocksRaycasts = false;
        ghostCG.alpha = 0.6f;

        // 计算偏移
        RectTransform rootRect = rootCanvas.transform as RectTransform;
        Vector3 mouseWorldPos;
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(rootRect, eventData.position, rootCanvas.worldCamera, out mouseWorldPos))
        {
            _dragOffset = _dragGhost.transform.position - mouseWorldPos;
        }

        _selfCanvasGroup.alpha = 0f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_dragGhost != null)
        {
            Canvas rootCanvas = GetComponentInParent<Canvas>().rootCanvas;
            RectTransform rootRect = rootCanvas.transform as RectTransform;
            Vector3 mouseWorldPos;
            // 使用 rootRect 计算位置
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(rootRect, eventData.position, rootCanvas.worldCamera, out mouseWorldPos))
            {
                _dragGhost.transform.position = mouseWorldPos + _dragOffset;
            }
        }

        if (_parentDropArea != null)
        {
            // ★★★ 使用新的“最近距离”排序法 ★★★
            int newIndex = GetClosestSiblingIndex(eventData.position);

            // 只有当索引真的改变时才移动，节省性能并防止闪烁
            if (transform.parent.GetSiblingIndex() != newIndex)
            {
                transform.parent.SetSiblingIndex(newIndex);
                _parentDropArea.UpdateLineNumbersOnly();

                // 强制立即刷新布局，防止布局滞后
                LayoutRebuilder.ForceRebuildLayoutImmediate(_parentDropArea.transform as RectTransform);
            }
        }
    }

    // 在 UI_PlacedBlock.cs 中找到 OnEndDrag 方法并替换为以下内容

    // 在 UI_PlacedBlock.cs 中
    public void OnEndDrag(PointerEventData eventData)
    {
        IsDragging = false;
        if (_dragGhost != null)
        {
            DestroyImmediate(_dragGhost);
            _dragGhost = null;
        }

        _selfCanvasGroup.alpha = 1f; // 恢复显示，防止误判

        // 获取 DropArea 引用（要在断开父子关系前获取）
        if (_parentDropArea == null) _parentDropArea = GetComponentInParent<UI_TimelineDropArea>();
        UI_TimelineDropArea targetArea = _parentDropArea;

        if (!IsPointerOverDropArea(eventData))
        {
            // ★★★ 核心修复：删除逻辑 ★★★
            GameObject rowObj = transform.parent.gameObject;

            // 1. 立即从父级移除：这步做完，transform.childCount 会立刻减少
            // 这样 UpdateLineNumbersOnly 循环时就绝对找不到它了，解决了 "3, 3, 3" 问题
            rowObj.transform.SetParent(null);

            // 2. 销毁物体
            Destroy(rowObj);

            // 3. 刷新区域
            if (targetArea != null)
            {
                targetArea.HidePlaceholder();       // 确保占位符没了
                targetArea.UpdateLineNumbersOnly(); // 重新编号
                targetArea.ForceRefreshLayout();    // 强制消除空隙
            }
        }
        else
        {
            // 只是改变了位置
            if (targetArea != null)
            {
                targetArea.HidePlaceholder();
                targetArea.UpdateLineNumbersOnly();
                targetArea.ForceRefreshLayout();
            }
        }
    }

    // ★★★ 核心修复：最近距离排序法 ★★★
    private int GetClosestSiblingIndex(Vector2 screenPos)
    {
        Transform container = transform.parent.parent;
        int childCount = container.childCount;

        // 把鼠标屏幕坐标转为 UI 本地坐标
        Vector2 localMousePos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
             container as RectTransform, screenPos, UICamera, out localMousePos
        );

        int bestIndex = 0;
        float minDistance = float.MaxValue;

        // 遍历所有兄弟（行），看谁离鼠标最近
        for (int i = 0; i < childCount; i++)
        {
            Transform child = container.GetChild(i);
            // 跳过自己和 Placeholder
            if (child == transform.parent) continue;
            if (child.name == "Placeholder") continue;

            // 计算该行中心的本地Y坐标
            float childY = child.localPosition.y;

            // 计算距离
            float dist = Mathf.Abs(localMousePos.y - childY);

            if (dist < minDistance)
            {
                minDistance = dist;
                bestIndex = i;
            }
        }

        // 再次修正：如果鼠标比最近的那个还要下面，就插在它后面；否则插在它前面
        Transform closestChild = container.GetChild(bestIndex);
        if (localMousePos.y < closestChild.localPosition.y)
        {
            // 鼠标在最近物体的下方（UI坐标越往下越小），所以应该插在后面
            // 但因为我们遍历时跳过了自己，索引可能需要微调，这里简化处理：
            // 直接返回 bestIndex，因为在 Unity SetSiblingIndex 中，
            // 如果从上往下拖，插在目标位置即可。
        }

        return bestIndex;
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

    // SetDirection, UpdateIconDisplay 省略不写，保持原样即可...
    public void SetDirection(CommandType newDir) { myCommand = newDir; UpdateIconDisplay(); }
    public void UpdateIconDisplay()
    {
        if (directionIconImage == null) return;
        bool showIcon = !(myCommand == CommandType.None || myCommand == CommandType.If_Start ||
            myCommand == CommandType.Else || myCommand == CommandType.End_If ||
            myCommand == CommandType.Loop_Start || myCommand == CommandType.Loop_End);
        directionIconImage.enabled = showIcon;
        if (showIcon)
        {
            directionIconImage.sprite = arrowSprite;
            directionIconImage.transform.localRotation = Quaternion.identity;
            float angle = 0f;
            switch (myCommand)
            {
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