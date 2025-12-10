using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;

public class UI_TimelineDropArea : MonoBehaviour, IDropHandler
{
    [Header("Configuration")]
    public GameObject stepRowPrefab;
    public UI_DirectionPopup directionPopupPanel;
    public UI_IfConfigPopup ifConfigPopup;

    private GameObject _placeholder;
    private RectTransform _rectTransform;
    private Canvas _rootCanvas;

    /// <summary>
    /// Camera used for UI ray / position conversion.
    /// Falls back to Camera.main if no canvas camera is set.
    /// </summary>
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

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _rootCanvas = GetComponentInParent<Canvas>();
    }

    /// <summary>
    /// Handles dropping a command from the source palette onto the timeline.
    /// </summary>
    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null)
        {
            UI_SourceDraggable source = eventData.pointerDrag.GetComponent<UI_SourceDraggable>();
            if (source != null)
            {
                if (source.sourceCommandType == CommandType.Loop_End)
                    SpawnLoopPair(source);
                else if (source.sourceCommandType == CommandType.If_Start)
                    SpawnIfStructure(source);
                else
                    SpawnSingleBlock(source);

                HidePlaceholder();
                UpdateLineNumbersOnly();
            }
        }
    }

    /// <summary>
    /// Applies a consistent font size and alignment for the block label.
    /// </summary>
    void FixTextSize(UI_PlacedBlock block)
    {
        if (block != null && block.stepText != null)
        {
            block.stepText.enableAutoSizing = false;
            block.stepText.fontSize = 12f;
            block.stepText.alignment = TextAlignmentOptions.Center;
        }
    }

    // ------------------------------------------------------
    // Spawning logic
    // ------------------------------------------------------

    /// <summary>
    /// Spawns a single command block at the drop position.
    /// </summary>
    void SpawnSingleBlock(UI_SourceDraggable source)
    {
        int index = GetInsertIndexFromScreenPos(Input.mousePosition);
        GameObject newRow = Instantiate(stepRowPrefab, transform);
        newRow.transform.SetSiblingIndex(index);

        UI_PlacedBlock block = newRow.GetComponentInChildren<UI_PlacedBlock>();
        if (block != null)
        {
            block.Initialize(directionPopupPanel, ifConfigPopup);
            block.myCommand = source.sourceCommandType;
            block.UpdateIconDisplay();
            FixTextSize(block);

            Image bg = block.GetComponentInChildren<Button>()?.GetComponent<Image>();
            if (bg != null) bg.color = new Color(0.2f, 0.8f, 0.2f);
        }
    }

    /// <summary>
    /// Spawns a LOOP_START + LOOP_END pair at the drop position.
    /// </summary>
    void SpawnLoopPair(UI_SourceDraggable source)
    {
        int index = GetInsertIndexFromScreenPos(Input.mousePosition);
        CreateBlockAt(index, CommandType.Loop_Start, "LOOP", new Color(0.6f, 0.6f, 0.6f));
        CreateBlockAt(index + 1, CommandType.Loop_End, "JUMP", new Color(0.3f, 0.5f, 0.8f));
    }

    /// <summary>
    /// Spawns an IF / ELSE / END IF structure at the drop position.
    /// </summary>
    void SpawnIfStructure(UI_SourceDraggable source)
    {
        int index = GetInsertIndexFromScreenPos(Input.mousePosition);
        CreateBlockAt(index, CommandType.If_Start, "IF", new Color(0.9f, 0.8f, 0.2f));
        CreateBlockAt(index + 1, CommandType.Else, "ELSE", new Color(0.8f, 0.8f, 0.2f));
        CreateBlockAt(index + 2, CommandType.End_If, "END IF", new Color(0.5f, 0.5f, 0.5f));
    }

    /// <summary>
    /// Helper for instantiating a row, assigning its command, text and color.
    /// </summary>
    void CreateBlockAt(int index, CommandType type, string labelText, Color color)
    {
        GameObject newRow = Instantiate(stepRowPrefab, transform);
        newRow.transform.SetSiblingIndex(index);

        UI_PlacedBlock block = newRow.GetComponentInChildren<UI_PlacedBlock>();
        if (block != null)
        {
            block.myCommand = type;
            block.Initialize(directionPopupPanel, ifConfigPopup);
            block.UpdateIconDisplay();

            if (block.stepText != null)
            {
                block.stepText.text = labelText;
                FixTextSize(block);
            }

            Image bg = block.GetComponentInChildren<Button>()?.GetComponent<Image>();
            if (bg != null) bg.color = color;
        }
    }

    /// <summary>
    /// Renumbers visible rows and cleans up invalid or broken rows.
    /// </summary>
    public void UpdateLineNumbersOnly()
    {
        int count = 1;
        Canvas.ForceUpdateCanvases();

        List<GameObject> garbage = new List<GameObject>();

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform row = transform.GetChild(i);

            bool isGhost = row.name == "TEMP_GHOST";

            Transform spacer = row.Find("IndentSpacer");
            bool isBrokenRow = (spacer != null && !spacer.gameObject.activeSelf);

            // Remove invalid rows: temporary ghosts or rows with broken layout
            if (row.name != "Placeholder" && (isGhost || isBrokenRow))
            {
                garbage.Add(row.gameObject);
                continue;
            }

            if (row.name == "Placeholder") continue;
            if (!row.gameObject.activeInHierarchy) continue;

            UI_PlacedBlock block = row.GetComponentInChildren<UI_PlacedBlock>();
            if (block == null)
            {
                garbage.Add(row.gameObject);
                continue;
            }

            // Assign line number
            if (block.indexText != null) block.indexText.text = count.ToString();
            count++;

            // Ensure control blocks have correct label text
            if (block.stepText != null)
            {
                if (block.myCommand == CommandType.If_Start) block.stepText.text = "IF";
                else if (block.myCommand == CommandType.Else) block.stepText.text = "ELSE";
                else if (block.myCommand == CommandType.End_If) block.stepText.text = "END IF";
                else if (block.myCommand == CommandType.Loop_Start) block.stepText.text = "LOOP";
                else if (block.myCommand == CommandType.Loop_End) block.stepText.text = "JUMP";

                FixTextSize(block);
            }
        }

        // Destroy all invalid rows at once
        foreach (var trash in garbage)
        {
            if (trash != null) DestroyImmediate(trash);
        }

        // If layout has changed, force a rebuild to avoid gaps or overlaps
        if (garbage.Count > 0)
        {
            ForceRefreshLayout();
        }
    }

    /// <summary>
    /// Destroys the temporary placeholder row, if any.
    /// </summary>
    public void HidePlaceholder()
    {
        if (_placeholder != null)
        {
            DestroyImmediate(_placeholder);
            _placeholder = null;
        }
    }

    /// <summary>
    /// Updates / creates the placeholder row and moves it to the proper index.
    /// </summary>
    public void UpdatePlaceholder(Vector2 screenMousePos)
    {
        if (_placeholder == null)
        {
            _placeholder = new GameObject("Placeholder");
            _placeholder.transform.SetParent(transform);

            LayoutElement le = _placeholder.AddComponent<LayoutElement>();
            float height = stepRowPrefab != null ? stepRowPrefab.GetComponent<RectTransform>().rect.height : 60f;
            le.preferredHeight = height;
            le.preferredWidth = 0;
            le.flexibleWidth = 0;
        }

        int newIndex = GetInsertIndexFromScreenPos(screenMousePos);
        _placeholder.transform.SetSiblingIndex(newIndex);
    }

    /// <summary>
    /// Computes the insertion index for a given screen-space mouse position.
    /// </summary>
    private int GetInsertIndexFromScreenPos(Vector2 screenPos)
    {
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rectTransform, screenPos, UICamera, out localPoint
        );

        int childCount = transform.childCount;
        for (int i = 0; i < childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child == _placeholder?.transform) continue;

            UI_PlacedBlock block = child.GetComponentInChildren<UI_PlacedBlock>();
            if (block != null && block.IsDragging) continue;

            if (localPoint.y > child.localPosition.y) return i;
        }
        return childCount;
    }

    /// <summary>
    /// Returns all placed blocks (excluding the placeholder).
    /// </summary>
    public List<UI_PlacedBlock> GetAllBlocks()
    {
        List<UI_PlacedBlock> list = new List<UI_PlacedBlock>();
        foreach (Transform row in transform)
        {
            if (row.name == "Placeholder") continue;
            UI_PlacedBlock block = row.GetComponentInChildren<UI_PlacedBlock>();
            if (block != null) list.Add(block);
        }
        return list;
    }

    /// <summary>
    /// Returns the sequence of commands from all blocks.
    /// </summary>
    public List<CommandType> GetAllCommands()
    {
        List<CommandType> list = new List<CommandType>();
        var blocks = GetAllBlocks();
        foreach (var b in blocks) list.Add(b.myCommand);
        return list;
    }

    /// <summary>
    /// Forces an immediate layout rebuild on this RectTransform and optionally again next frame.
    /// </summary>
    public void ForceRefreshLayout()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(_rectTransform);

        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(DelayedRefresh());
        }
    }

    /// <summary>
    /// Secondary layout rebuild executed at the end of the frame, after hierarchy updates.
    /// </summary>
    private System.Collections.IEnumerator DelayedRefresh()
    {
        yield return new WaitForEndOfFrame();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_rectTransform);

        // If needed, parent layout can also be refreshed here.
        // if (transform.parent != null)
        //     LayoutRebuilder.ForceRebuildLayoutImmediate(transform.parent as RectTransform);
    }

    /// <summary>
    /// Number of valid blocks currently on the timeline.
    /// </summary>
    public int CurrentLineCount
    {
        get { return GetAllBlocks().Count; }
    }
}
