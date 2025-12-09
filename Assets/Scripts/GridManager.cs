using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Global grid manager:
/// Handles grid types, visuals, border walls, and passability checks.
/// </summary>
public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("Base Grid Settings")]
    public float gridCellSize = 1f;
    public Vector2Int gridMapSize;

    [Header("Common Sprites")]
    public Sprite safePathSprite;
    public Sprite trapCliffSprite;
    public Sprite endPointSprite;
    public Sprite wallSprite; // Generic wall

    [Header("Border Wall Sprites")]
    public Sprite topWallSprite;
    public Sprite bottomWallSprite;
    public Sprite leftWallSprite;
    public Sprite rightWallSprite;

    [Header("Corner Wall Sprites (Highest Priority)")]
    public Sprite topLeftWallSprite;
    public Sprite topRightWallSprite;
    public Sprite bottomLeftWallSprite;
    public Sprite bottomRightWallSprite;

    [Range(0f, 1f)] public float gridAlpha = 0.3f;

    [Header("Auto-Generation Settings")]
    [Tooltip("Enable this to display wall sprites around the map border. Logic allows priests to walk on border walls.")]
    public bool generateBorderWalls = true;

    [Header("Batch Configuration (Ranges)")]
    public List<string> wallRanges = new List<string>() { "2:4,5:5" };
    public List<string> trapRanges = new List<string>() { "5:5,5:7" };

    private Dictionary<Vector2Int, GridType> _gridData = new Dictionary<Vector2Int, GridType>();
    private Dictionary<Vector2Int, GameObject> _gridVisuals = new Dictionary<Vector2Int, GameObject>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitBaseGridMap();

        // Generate visible border walls (walkable logic handled in IsGridPassable)
        if (generateBorderWalls)
        {
            GenerateBorderWalls();
        }

        BatchSetGridByRange(wallRanges, GridType.Wall);
        BatchSetGridByRange(trapRanges, GridType.TrapCliff);
        AutoSetPriestEndPoints();
    }

    private void GenerateBorderWalls()
    {
        int width = gridMapSize.x;
        int height = gridMapSize.y;

        // Top & bottom borders
        for (int x = 0; x < width; x++)
        {
            SetGridType(new Vector2Int(x, 0), GridType.Wall);
            SetGridType(new Vector2Int(x, height - 1), GridType.Wall);
        }

        // Left & right borders
        for (int y = 0; y < height; y++)
        {
            SetGridType(new Vector2Int(0, y), GridType.Wall);
            SetGridType(new Vector2Int(width - 1, y), GridType.Wall);
        }
        Debug.Log("Border walls generated.");
    }

    [ContextMenu("Refresh All Grid Visuals")]
    public void RefreshAllGridVisual()
    {
        foreach (var gridPos in _gridData.Keys)
        {
            if (_gridVisuals.ContainsKey(gridPos))
            {
                UpdateGridVisual(gridPos);
            }
        }
        Debug.Log("All grid visuals refreshed.");
    }

    private void InitBaseGridMap()
    {
        for (int x = 0; x < gridMapSize.x; x++)
        {
            for (int y = 0; y < gridMapSize.y; y++)
            {
                Vector2Int gridPos = new Vector2Int(x, y);
                if (!_gridData.ContainsKey(gridPos))
                {
                    _gridData[gridPos] = GridType.SafePath;
                }
                if (!_gridVisuals.ContainsKey(gridPos))
                {
                    CreateGridVisual(gridPos);
                }
            }
        }
    }

    private void BatchSetGridByRange(List<string> rangeList, GridType targetType)
    {
        if (rangeList == null || rangeList.Count == 0) return;

        foreach (string rangeStr in rangeList)
        {
            if (string.IsNullOrWhiteSpace(rangeStr)) continue;

            string[] xyParts = rangeStr.Split(',');
            if (xyParts.Length != 2) continue;

            (int xStart, int xEnd) = ParseRangePart(xyParts[0]);
            (int yStart, int yEnd) = ParseRangePart(xyParts[1]);

            if (!IsRangeValid(xStart, xEnd, gridMapSize.x) || !IsRangeValid(yStart, yEnd, gridMapSize.y))
            {
                Debug.LogError($"Range {rangeStr} is out of map bounds.");
                continue;
            }

            for (int x = xStart; x <= xEnd; x++)
            {
                for (int y = yStart; y <= yEnd; y++)
                {
                    Vector2Int gridPos = new Vector2Int(x, y);
                    _gridData[gridPos] = targetType;
                    UpdateGridVisual(gridPos);
                }
            }
        }
    }

    private (int start, int end) ParseRangePart(string part)
    {
        string[] parts = part.Split(':');
        if (parts.Length == 1)
        {
            int val = int.TryParse(parts[0], out int v) ? v : 0;
            return (val, val);
        }
        else if (parts.Length == 2)
        {
            int start = int.TryParse(parts[0], out int s) ? s : 0;
            int end = int.TryParse(parts[1], out int e) ? e : 0;
            return start > end ? (end, start) : (start, end);
        }
        return (0, 0);
    }

    private bool IsRangeValid(int start, int end, int maxSize)
    {
        return start >= 0 && end < maxSize;
    }

    private void AutoSetPriestEndPoints()
    {
        PriestUnit[] allPriests = FindObjectsOfType<PriestUnit>();
        if (allPriests.Length == 0) return;

        foreach (PriestUnit priest in allPriests)
        {
            Vector2Int endGrid = priest.targetEndGridPos;
            if (endGrid.x < 0 || endGrid.x >= gridMapSize.x || endGrid.y < 0 || endGrid.y >= gridMapSize.y)
                continue;

            _gridData[endGrid] = GridType.EndPoint;
            UpdateGridVisual(endGrid);
        }
    }

    private void CreateGridVisual(Vector2Int gridPos)
    {
        GameObject gridObj = new GameObject($"Grid_{gridPos.x}_{gridPos.y}");
        gridObj.transform.SetParent(this.transform);
        gridObj.transform.position = new Vector3(gridPos.x * gridCellSize, gridPos.y * gridCellSize, 0);

        // 1. Floor layer (order = -10, bottom)
        SpriteRenderer floorSR = gridObj.AddComponent<SpriteRenderer>();
        floorSR.sprite = safePathSprite;
        floorSR.color = new Color(1, 1, 1, 1);
        floorSR.sortingOrder = -10;
        floorSR.drawMode = SpriteDrawMode.Sliced;
        floorSR.size = new Vector2(gridCellSize, gridCellSize);

        // 2. Prop layer (order = -5, between floor and units)
        GameObject propObj = new GameObject("Prop");
        propObj.transform.SetParent(gridObj.transform);
        propObj.transform.localPosition = Vector3.zero;

        SpriteRenderer propSR = propObj.AddComponent<SpriteRenderer>();
        propSR.sortingOrder = -5;
        propSR.drawMode = SpriteDrawMode.Sliced;
        propSR.size = new Vector2(gridCellSize, gridCellSize);

        _gridVisuals.Add(gridPos, gridObj);

        UpdateGridVisual(gridPos);
    }

    private void UpdateGridVisual(Vector2Int gridPos)
    {
        if (!_gridVisuals.ContainsKey(gridPos)) return;

        GameObject gridObj = _gridVisuals[gridPos];
        Transform propTrans = gridObj.transform.GetChild(0);
        SpriteRenderer propSR = propTrans.GetComponent<SpriteRenderer>();

        GridType type = _gridData[gridPos];

        propSR.sprite = GetPropSprite(gridPos, type);

        if (type == GridType.EndPoint)
        {
            propTrans.localPosition = new Vector3(-gridCellSize / 2, -gridCellSize / 2, 0);
        }
        else
        {
            propTrans.localPosition = Vector3.zero;
        }
    }

    private Sprite GetPropSprite(Vector2Int gridPos, GridType type)
    {
        if (type == GridType.Wall)
        {
            int width = gridMapSize.x;
            int height = gridMapSize.y;

            // Corner walls take highest priority
            if (gridPos.x == 0 && gridPos.y == 0 && bottomLeftWallSprite != null) return bottomLeftWallSprite;
            if (gridPos.x == width - 1 && gridPos.y == 0 && bottomRightWallSprite != null) return bottomRightWallSprite;
            if (gridPos.x == 0 && gridPos.y == height - 1 && topLeftWallSprite != null) return topLeftWallSprite;
            if (gridPos.x == width - 1 && gridPos.y == height - 1 && topRightWallSprite != null) return topRightWallSprite;

            // Border walls
            if (gridPos.y == 0 && bottomWallSprite != null) return bottomWallSprite;
            if (gridPos.y == height - 1 && topWallSprite != null) return topWallSprite;
            if (gridPos.x == 0 && leftWallSprite != null) return leftWallSprite;
            if (gridPos.x == width - 1 && rightWallSprite != null) return rightWallSprite;

            return wallSprite;
        }

        switch (type)
        {
            case GridType.TrapCliff: return trapCliffSprite;
            case GridType.EndPoint: return endPointSprite;
            default: return null;
        }
    }

    // ===== Public utility methods =====

    public void SetGridType(Vector2Int gridPos, GridType type)
    {
        if (!_gridData.ContainsKey(gridPos)) return;
        _gridData[gridPos] = type;
        UpdateGridVisual(gridPos);
    }

    public GridType GetGridType(Vector2Int gridPos) =>
        _gridData.ContainsKey(gridPos) ? _gridData[gridPos] : GridType.SafePath;

    /// <summary>
    /// Passability rule:
    /// - Disallow positions outside map bounds.
    /// - Internal walls are not passable.
    /// - Border walls (edges) are visually walls but treated as passable.
    /// </summary>
    public bool IsGridPassable(Vector2Int gridPos)
    {
        // 1. Out-of-bounds check
        if (gridPos.x < 0 || gridPos.x >= gridMapSize.x ||
            gridPos.y < 0 || gridPos.y >= gridMapSize.y)
        {
            return false;
        }

        // 2. Get type
        GridType type = GetGridType(gridPos);

        // 3. Non-wall is always passable
        if (type != GridType.Wall) return true;

        // 4. Border walls are visually walls but logically passable
        bool isBorder = (gridPos.x == 0 || gridPos.y == 0 ||
                         gridPos.x == gridMapSize.x - 1 || gridPos.y == gridMapSize.y - 1);

        if (isBorder)
        {
            return true;
        }

        // 5. Internal walls are not passable
        return false;
    }

    public bool IsGridDeadly(Vector2Int gridPos)
    {
        GridType type = GetGridType(gridPos);
        if (type == GridType.TrapCliff) return true;
        if (type == GridType.FirePit)
        {
            FirePit fire = GetFirePitAtGrid(gridPos);
            return fire != null && fire.isLit;
        }
        return false;
    }

    public FirePit GetFirePitAtGrid(Vector2Int gridPos)
    {
        foreach (FirePit fire in FindObjectsOfType<FirePit>())
        {
            if (fire.gridPos == gridPos) return fire;
        }
        return null;
    }

    public bool IsGridEndPoint(Vector2Int gridPos) =>
        GetGridType(gridPos) == GridType.EndPoint;

    void OnDrawGizmos()
    {
        for (int x = 0; x < gridMapSize.x; x++)
        {
            for (int y = 0; y < gridMapSize.y; y++)
            {
                Gizmos.color = Color.gray;
                Gizmos.DrawWireCube(
                    new Vector3(x * gridCellSize, y * gridCellSize, 0),
                    new Vector3(gridCellSize, gridCellSize, 1)
                );
            }
        }
    }
}
