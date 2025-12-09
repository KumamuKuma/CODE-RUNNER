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
    [Range(0.1f, 3f)] public float ghostScale = 5.0f; // Manually control the drag ghost size

    private GameObject _currentGhost;
    private RectTransform _canvasRect;
    private UI_TimelineDropArea _currentDropArea;

    void Start()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null) _canvasRect = canvas.transform as RectTransform;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (dragGhostPrefab == null) return;

        // 1. Create the drag ghost object
        _currentGhost = Instantiate(dragGhostPrefab, _canvasRect);
        _currentGhost.transform.SetAsLastSibling();

        // 2. Apply scale using ghostScale
        _currentGhost.transform.localScale = Vector3.one * ghostScale;

        // 3. Set initial screen position
        SetGhostPosition(eventData.position);

        // 4. Make the ghost non-blocking and semi-transparent
        CanvasGroup cg = _currentGhost.GetComponent<CanvasGroup>();
        if (cg == null) cg = _currentGhost.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.alpha = 0.6f;

        // 5. Set ghost color based on command type (Step = green, IF = yellow, Jump = blue)
        Image img = _currentGhost.GetComponent<Image>();
        if (img == null) img = _currentGhost.GetComponentInChildren<Image>();

        if (img != null)
        {
            if (sourceCommandType == CommandType.If_Start)
            {
                // IF block – yellow
                img.color = new Color(0.9f, 0.8f, 0.2f);
            }
            else if (sourceCommandType == CommandType.Loop_End)
            {
                // JUMP (Loop_End) – blue
                img.color = new Color(0.3f, 0.5f, 0.8f);
            }
            else
            {
                // Default Step – green
                img.color = new Color(0.2f, 0.8f, 0.2f);
            }
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_currentGhost != null) SetGhostPosition(eventData.position);
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
        Vector3 worldPos;
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(_canvasRect, screenPos, Camera.main, out worldPos))
        {
            _currentGhost.transform.position = worldPos;
            Vector3 localPos = _currentGhost.transform.localPosition;
            localPos.z = 0f;
            _currentGhost.transform.localPosition = localPos;
        }
    }

    // Detect and update the active drop area under the cursor
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
            Vector3 worldPos;
            RectTransformUtility.ScreenPointToWorldPointInRectangle(_canvasRect, eventData.position, Camera.main, out worldPos);
            _currentDropArea.UpdatePlaceholder(worldPos);
        }
        else if (_currentDropArea != null)
        {
            _currentDropArea.HidePlaceholder();
            _currentDropArea = null;
        }
    }
}
