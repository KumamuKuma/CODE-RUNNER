using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class UI_SourceDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Settings")]
    public CommandType sourceCommandType = CommandType.None;

    [Header("Visual")]
    public GameObject dragGhostPrefab;
    [Range(0.1f, 3f)] public float ghostScale = 1.0f;

    private GameObject _currentGhost;
    private RectTransform _canvasRect;
    private Canvas _rootCanvas;
    private UI_TimelineDropArea _currentDropArea;
    private Vector3 _dragOffset; // ★ 新增：记录偏移量

    void Start()
    {
        _rootCanvas = GetComponentInParent<Canvas>();
        if (_rootCanvas != null) _canvasRect = _rootCanvas.transform as RectTransform;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (dragGhostPrefab == null) return;

        // 1. Create ghost
        _currentGhost = Instantiate(dragGhostPrefab, _canvasRect);
        _currentGhost.transform.SetAsLastSibling();
        _currentGhost.transform.localScale = Vector3.one * ghostScale;

        // 2. Setup visual
        CanvasGroup cg = _currentGhost.GetComponent<CanvasGroup>();
        if (cg == null) cg = _currentGhost.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.alpha = 0.6f;

        // Color setup
        Image img = _currentGhost.GetComponent<Image>();
        if (img == null) img = _currentGhost.GetComponentInChildren<Image>();
        if (img != null)
        {
            if (sourceCommandType == CommandType.If_Start) img.color = new Color(0.9f, 0.8f, 0.2f);
            else if (sourceCommandType == CommandType.Loop_End) img.color = new Color(0.3f, 0.5f, 0.8f);
            else img.color = new Color(0.2f, 0.8f, 0.2f);
        }

        // 3. ★ 核心修复：计算偏移量，防止鬼影跳动 ★
        // 先把鬼影放到鼠标位置
        SetGhostPosition(eventData.position);
        // 然后计算 offset = 鬼影当前世界坐标 - 鼠标世界坐标
        // 实际上因为是新建物体，我们通常希望它居中或者就在鼠标下。
        // 为了修复"错位"，我们直接让鬼影中心对齐鼠标，然后不做 Offset，
        // 或者如果你希望抓哪里就是哪里，就用下面的 Offset 逻辑：

        // 这里采用最稳妥的方式：强制让鬼影的 Pivot 居中，然后对齐鼠标
        RectTransform ghostRect = _currentGhost.GetComponent<RectTransform>();
        if (ghostRect != null) ghostRect.pivot = new Vector2(0.5f, 0.5f);

        // 再次设置位置以确保 Pivot 居中生效
        SetGhostPosition(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_currentGhost != null)
        {
            SetGhostPosition(eventData.position);
        }
        CheckForDropArea(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_currentDropArea != null)
        {
            _currentDropArea.HidePlaceholder();
            _currentDropArea = null;
        }
        if (_currentGhost != null) Destroy(_currentGhost);
    }

    private void SetGhostPosition(Vector2 screenPos)
    {
        if (_currentGhost == null || _canvasRect == null) return;

        Vector3 worldPos;
        // ★ 核心修复：根据 Canvas 模式决定传什么 Camera
        Camera cam = null;
        if (_rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            cam = null;
        else
            cam = Camera.main;

        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(_canvasRect, screenPos, cam, out worldPos))
        {
            _currentGhost.transform.position = worldPos;

            // 保持 Z 轴正确
            Vector3 localPos = _currentGhost.transform.localPosition;
            localPos.z = 0f;
            _currentGhost.transform.localPosition = localPos;
        }
    }

    private void CheckForDropArea(PointerEventData eventData)
    {
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        UI_TimelineDropArea foundArea = null;
        foreach (RaycastResult result in results)
        {
            foundArea = result.gameObject.GetComponent<UI_TimelineDropArea>();
            if (foundArea != null) break;
        }

        if (foundArea != null)
        {
            if (_currentDropArea != foundArea)
            {
                if (_currentDropArea != null) _currentDropArea.HidePlaceholder();
                _currentDropArea = foundArea;
            }
            // 直接传屏幕坐标，让 DropArea 自己去转 Local
            _currentDropArea.UpdatePlaceholder(eventData.position);
        }
        else if (_currentDropArea != null)
        {
            _currentDropArea.HidePlaceholder();
            _currentDropArea = null;
        }
    }
}