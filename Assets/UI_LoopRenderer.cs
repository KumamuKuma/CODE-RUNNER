using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class UI_LoopRenderer : MonoBehaviour
{
    [Header("Line Rendering Settings")]
    public Material lineMaterial;

    [Header("Indentation Settings")]
    public float baseXOffset = 2f;
    public float offsetStep = 2f;

    [Header("References")]
    public UI_TimelineDropArea connectedDropArea;

    private class LoopLine
    {
        public GameObject lineObj;
        public LineRenderer lr;
    }

    private List<LoopLine> _activeLines = new List<LoopLine>();

    void Start()
    {
        if (connectedDropArea == null)
            connectedDropArea = GetComponentInChildren<UI_TimelineDropArea>();
    }

    void Update()
    {
        if (connectedDropArea != null) DrawAllLoops();
    }

    public void DrawAllLoops()
    {
        // 1. Clear existing lines
        foreach (var line in _activeLines)
            if (line.lineObj != null) Destroy(line.lineObj);
        _activeLines.Clear();

        // 2. Collect placed blocks
        List<UI_PlacedBlock> allBlocks = new List<UI_PlacedBlock>();
        foreach (Transform child in connectedDropArea.transform)
        {
            if (child.name == "Placeholder") continue;
            var block = child.GetComponentInChildren<UI_PlacedBlock>();
            if (block != null) allBlocks.Add(block);
        }

        // 3. Match Loop_Start and Loop_End pairs
        Stack<UI_PlacedBlock> startStack = new Stack<UI_PlacedBlock>();
        int pairIndex = 0;

        foreach (var block in allBlocks)
        {
            if (block.myCommand == CommandType.Loop_Start)
            {
                startStack.Push(block);
            }
            else if (block.myCommand == CommandType.Loop_End)
            {
                if (startStack.Count > 0)
                {
                    UI_PlacedBlock startBlock = startStack.Pop();
                    int currentDepth = startStack.Count;
                    CreateLine(startBlock, block, currentDepth, pairIndex);
                    pairIndex++;
                }
            }
        }
    }

    void CreateLine(UI_PlacedBlock start, UI_PlacedBlock end, int depth, int colorIndex)
    {
        GameObject go = new GameObject("LoopLine");
        go.transform.SetParent(this.transform);
        go.transform.localScale = Vector3.one;

        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.material = lineMaterial;

        lr.sortingOrder = 1000;
        lr.sortingLayerName = "Default";

        float hue = (0.6f + (colorIndex * 0.15f)) % 1.0f;
        Color dynamicColor = Color.HSVToRGB(hue, 0.8f, 1f);
        lr.startColor = dynamicColor;
        lr.endColor = dynamicColor;

        lr.startWidth = 0.05f;
        lr.endWidth = 0.05f;

        lr.positionCount = 4;
        lr.useWorldSpace = true;
        lr.numCornerVertices = 5;

        Vector3 startPos = GetRightEdgePosition(start);
        Vector3 endPos = GetRightEdgePosition(end);

        float zFloating = -50f;
        startPos.z += zFloating;
        endPos.z += zFloating;

        float currentXOffset = baseXOffset + (depth * offsetStep);

        Vector3 p0 = endPos;
        Vector3 p1 = new Vector3(endPos.x + currentXOffset, endPos.y, endPos.z);
        Vector3 p2 = new Vector3(startPos.x + currentXOffset, startPos.y, startPos.z);
        Vector3 p3 = startPos;

        lr.SetPosition(0, p0);
        lr.SetPosition(1, p1);
        lr.SetPosition(2, p2);
        lr.SetPosition(3, p3);

        LoopLine newLine = new LoopLine();
        newLine.lineObj = go;
        newLine.lr = lr;
        _activeLines.Add(newLine);
    }

    Vector3 GetRightEdgePosition(UI_PlacedBlock block)
    {
        RectTransform rt = block.GetComponent<RectTransform>();
        if (rt == null) return block.transform.position;

        float halfWidth = (rt.rect.width / 2f) * rt.transform.lossyScale.x;

        return block.transform.position + new Vector3(halfWidth, 0, 0);
    }
}
