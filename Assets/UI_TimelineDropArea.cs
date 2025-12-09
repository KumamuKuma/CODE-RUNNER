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

    // -----------------------------
    // Drag & Drop logic
    // -----------------------------
    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null)
        {
            UI_SourceDraggable source = eventData.pointerDrag.GetComponent<UI_SourceDraggable>();
            if (source != null)
            {
                if (source.sourceCommandType == CommandType.Loop_End)
                {
                    SpawnLoopPair(source);
                }
                else if (source.sourceCommandType == CommandType.If_Start)
                {
                    SpawnIfStructure(source);
                }
                else
                {
                    SpawnSingleBlock(source);
                }

                HidePlaceholder();
                UpdateLineNumbersAndIndents();
                return;
            }
        }
    }

    // -----------------------------
    // Text & Coordinate Helpers
    // -----------------------------

    void FixTextSize(UI_PlacedBlock block)
    {
        if (block != null && block.stepText != null)
        {
            block.stepText.enableAutoSizing = false;
            block.stepText.fontSize = 12f;
            block.stepText.alignment = TextAlignmentOptions.Center;
        }
    }

    float GetMouseWorldY()
    {
        Vector3 worldPos;
        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            transform as RectTransform,
            Input.mousePosition,
            Camera.main,
            out worldPos
        );
        return worldPos.y;
    }

    // -----------------------------
    // Block creation
    // -----------------------------

    void SpawnSingleBlock(UI_SourceDraggable source)
    {
        GameObject newRow = Instantiate(stepRowPrefab, transform);
        int index = GetInsertIndex(GetMouseWorldY());
        newRow.transform.SetSiblingIndex(index);

        UI_PlacedBlock block = newRow.GetComponentInChildren<UI_PlacedBlock>();
        if (block != null)
        {
            block.Initialize(directionPopupPanel, ifConfigPopup);
            block.myCommand = source.sourceCommandType;
            block.UpdateIconDisplay();
            FixTextSize(block);

            Image bg = block.GetComponentInChildren<Button>()?.GetComponent<Image>();
            if (bg != null)
            {
                bg.color = new Color(0.2f, 0.8f, 0.2f);
            }
        }
    }

    void SpawnLoopPair(UI_SourceDraggable source)
    {
        int index = GetInsertIndex(GetMouseWorldY());

        CreateBlockAt(index, CommandType.Loop_Start, "LOOP", new Color(0.6f, 0.6f, 0.6f));
        CreateBlockAt(index + 1, CommandType.Loop_End, "JUMP", new Color(0.3f, 0.5f, 0.8f));
    }

    void SpawnIfStructure(UI_SourceDraggable source)
    {
        int index = GetInsertIndex(GetMouseWorldY());

        CreateBlockAt(index, "IF", CommandType.If_Start, new Color(0.9f, 0.8f, 0.2f));
        CreateBlockAt(index + 1, "ELSE", CommandType.Else, new Color(0.8f, 0.8f, 0.2f));
        CreateBlockAt(index + 2, "END IF", CommandType.End_If, new Color(0.5f, 0.5f, 0.5f));
    }

    void CreateBlockAt(int index, CommandType type, string labelText, Color color)
    {
        GameObject newRow = Instantiate(stepRowPrefab, this.transform);
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

    // -----------------------------
    // Line numbers & indentation
    // -----------------------------

    public void UpdateLineNumbersAndIndents()
    {
        int count = 1;
        int currentIndent = 0;

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform row = transform.GetChild(i);
            if (row.name == "Placeholder") continue;

            UI_PlacedBlock block = row.GetComponentInChildren<UI_PlacedBlock>();
            if (block == null) continue;

            if (block.myCommand == CommandType.Else ||
                block.myCommand == CommandType.End_If ||
                block.myCommand == CommandType.Loop_End)
            {
                currentIndent--;
            }

            if (currentIndent < 0) currentIndent = 0;

            block.SetIndentLevel(currentIndent);

            TextMeshProUGUI indexText = row.GetComponentInChildren<TextMeshProUGUI>();
            if (indexText != null)
            {
                indexText.text = count.ToString();
                count++;
            }

            if (block.stepText != null)
            {
                if (block.myCommand == CommandType.If_Start) block.stepText.text = "IF";
                else if (block.myCommand == CommandType.Else) block.stepText.text = "ELSE";
                else if (block.myCommand == CommandType.End_If) block.stepText.text = "END IF";

                FixTextSize(block);
            }

            if (block.myCommand == CommandType.If_Start ||
                block.myCommand == CommandType.Loop_Start)
            {
                currentIndent++;
            }
            else if (block.myCommand == CommandType.Else)
            {
                currentIndent++;
            }
        }
    }

    // -----------------------------
    // Helpers
    // -----------------------------

    public void HidePlaceholder()
    {
        if (_placeholder != null)
        {
            Destroy(_placeholder);
            _placeholder = null;
        }
    }

    public void UpdatePlaceholder(Vector3 worldMousePos)
    {
        if (_placeholder == null)
        {
            _placeholder = new GameObject("Placeholder");
            _placeholder.transform.SetParent(this.transform);

            LayoutElement le = _placeholder.AddComponent<LayoutElement>();
            float height = stepRowPrefab != null ? stepRowPrefab.GetComponent<RectTransform>().rect.height : 80f;
            le.preferredHeight = height;
            le.preferredWidth = 100;
        }

        int newIndex = GetInsertIndex(worldMousePos.y);
        _placeholder.transform.SetSiblingIndex(newIndex);
    }

    private int GetInsertIndex(float worldY)
    {
        int childCount = transform.childCount;
        for (int i = 0; i < childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child == _placeholder?.transform) continue;

            UI_PlacedBlock block = child.GetComponentInChildren<UI_PlacedBlock>();
            if (block != null && block.IsDragging) continue;

            if (worldY > child.position.y)
                return i;
        }
        return childCount;
    }

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

    public List<CommandType> GetAllCommands()
    {
        List<CommandType> list = new List<CommandType>();
        var blocks = GetAllBlocks();
        foreach (var b in blocks) list.Add(b.myCommand);
        return list;
    }
}
