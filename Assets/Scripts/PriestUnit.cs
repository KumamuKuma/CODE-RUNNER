using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Animator), typeof(BoxCollider2D))]
public class PriestUnit : MonoBehaviour
{
    [Header("Basic Settings")]
    public int priestID;
    public Vector2Int currentGridPos;

    [Header("Auto-Bound Target Endpoint")]
    public FirePit linkedChest;
    public Vector2Int targetEndGridPos;

    public float gridCellSize = 1f;

    [Header("Movement Settings")]
    [Range(0.1f, 10f)]
    public float moveSpeed = 4f;

    [Header("Runtime State")]
    [SerializeField] private bool isAlive = true;
    [SerializeField] private bool isReachEnd = false;

    public bool IsAlive => isAlive;
    public bool IsReachEnd => isReachEnd;

    private Animator _anim;
    private BoxCollider2D _collider;
    private SpriteRenderer _sr;
    private bool _isCommandExecuting;

    // Command buffer to smooth movement and avoid stuttering between steps
    private CommandType _bufferedCommand = CommandType.None;

    // Coroutine used to delay stopping the walking animation
    private Coroutine _stopWalkCoroutine;

    void Awake()
    {
        _anim = GetComponent<Animator>();
        _collider = GetComponent<BoxCollider2D>();
        _sr = GetComponent<SpriteRenderer>();

        isAlive = true;
        isReachEnd = false;
        _isCommandExecuting = false;
        _collider.isTrigger = true;

        transform.position = GetWorldPos(currentGridPos);
        UpdateSortingOrder();
    }

    void Start()
    {
        if (linkedChest != null) targetEndGridPos = linkedChest.gridPos;
        UpdateSortingOrder();
    }

    /// <summary>
    /// Adjust sprite sorting order so the priest is rendered above floor/chest tiles.
    /// Base value: 500, minus Y * 10, plus 10 to ensure it stays above objects in the same cell.
    /// </summary>
    private void UpdateSortingOrder()
    {
        if (_sr != null)
        {
            _sr.sortingOrder = 500 - (currentGridPos.y * 10) + 10;
        }
    }

    /// <summary>
    /// Convert grid coordinates to world position.
    /// Z is set to -1 so the priest always renders in front of background (Z = 0).
    /// </summary>
    private Vector3 GetWorldPos(Vector2Int grid)
    {
        return new Vector3(grid.x * gridCellSize, grid.y * gridCellSize, -1f);
    }

    public void ExecuteCommand(CommandType command)
    {
        if (!isAlive) return;

        // If already executing a command, buffer movement commands instead of ignoring them
        if (_isCommandExecuting)
        {
            if (IsMoveCommand(command))
            {
                _bufferedCommand = command;
            }
            return;
        }

        // For movement commands, ensure the walk animation is active
        if (IsMoveCommand(command))
        {
            // Cancel any pending "stop walking" timer when a new move command comes in
            if (_stopWalkCoroutine != null)
            {
                StopCoroutine(_stopWalkCoroutine);
                _stopWalkCoroutine = null;
            }

            SetWalkingState(true);
        }

        _isCommandExecuting = true;

        switch (command)
        {
            case CommandType.Move_Up: TryMoveToGrid(new Vector2Int(currentGridPos.x, currentGridPos.y + 1)); break;
            case CommandType.Move_Down: TryMoveToGrid(new Vector2Int(currentGridPos.x, currentGridPos.y - 1)); break;
            case CommandType.Move_Left: TryMoveToGrid(new Vector2Int(currentGridPos.x - 1, currentGridPos.y)); break;
            case CommandType.Move_Right: TryMoveToGrid(new Vector2Int(currentGridPos.x + 1, currentGridPos.y)); break;

            case CommandType.Move_LeftUp: TryMoveToGrid(new Vector2Int(currentGridPos.x - 1, currentGridPos.y + 1)); break;
            case CommandType.Move_RightUp: TryMoveToGrid(new Vector2Int(currentGridPos.x + 1, currentGridPos.y + 1)); break;
            case CommandType.Move_LeftDown: TryMoveToGrid(new Vector2Int(currentGridPos.x - 1, currentGridPos.y - 1)); break;
            case CommandType.Move_RightDown: TryMoveToGrid(new Vector2Int(currentGridPos.x + 1, currentGridPos.y - 1)); break;

            case CommandType.Cast_Up: CastToGrid(new Vector2Int(currentGridPos.x, currentGridPos.y + 1)); break;
            case CommandType.Cast_Down: CastToGrid(new Vector2Int(currentGridPos.x, currentGridPos.y - 1)); break;
            case CommandType.Cast_Left: CastToGrid(new Vector2Int(currentGridPos.x - 1, currentGridPos.y)); break;
            case CommandType.Cast_Right: CastToGrid(new Vector2Int(currentGridPos.x + 1, currentGridPos.y)); break;

            case CommandType.Cast_LeftUp: CastToGrid(new Vector2Int(currentGridPos.x - 1, currentGridPos.y + 1)); break;
            case CommandType.Cast_RightUp: CastToGrid(new Vector2Int(currentGridPos.x + 1, currentGridPos.y + 1)); break;
            case CommandType.Cast_LeftDown: CastToGrid(new Vector2Int(currentGridPos.x - 1, currentGridPos.y - 1)); break;
            case CommandType.Cast_RightDown: CastToGrid(new Vector2Int(currentGridPos.x + 1, currentGridPos.y - 1)); break;

            case CommandType.None:
                _isCommandExecuting = false;
                break;
        }
    }

    private void TryMoveToGrid(Vector2Int targetGrid)
    {
        // Wall / non-passable check
        if (!GridManager.Instance.IsGridPassable(targetGrid))
        {
            _isCommandExecuting = false;
            _bufferedCommand = CommandType.None; // Clear buffer on collision
            TryStartStopWalkTimer();             // Short delay to give a sense of inertia
            return;
        }
        StartCoroutine(SmoothMoveRoutine(targetGrid));
    }

    private IEnumerator SmoothMoveRoutine(Vector2Int targetGrid)
    {
        currentGridPos = targetGrid;
        UpdateSortingOrder();

        Vector3 targetWorldPos = GetWorldPos(targetGrid);

        // Smooth movement towards the target cell
        while (Vector3.Distance(transform.position, targetWorldPos) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetWorldPos, moveSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = targetWorldPos;

        // Post-move checks

        if (GridManager.Instance.IsGridDeadly(targetGrid))
        {
            KillByEnvironment();
            _isCommandExecuting = false;
            _bufferedCommand = CommandType.None;
            yield break;
        }

        PriestUnit targetPriest = GetPriestAtGrid(targetGrid);
        if (targetPriest != null && targetPriest.IsAlive) targetPriest.KillByEnvironment();

        CheckEndPointState();

        _isCommandExecuting = false;

        // If a buffered move command exists, chain it immediately
        if (_bufferedCommand != CommandType.None)
        {
            CommandType nextCmd = _bufferedCommand;
            _bufferedCommand = CommandType.None;

            ExecuteCommand(nextCmd);
        }
        else
        {
            // No more commands; start a short delay before stopping walk animation
            TryStartStopWalkTimer();
        }
    }

    private void TryStartStopWalkTimer()
    {
        if (_stopWalkCoroutine != null) StopCoroutine(_stopWalkCoroutine);
        _stopWalkCoroutine = StartCoroutine(StopWalkDelay());
    }

    private IEnumerator StopWalkDelay()
    {
        // Short delay used only when there is no continuous input
        yield return new WaitForSeconds(0.2f);

        if (isAlive) SetWalkingState(false);
        _stopWalkCoroutine = null;
    }

    // Helper: centralised walking animation control
    private void SetWalkingState(bool isWalking)
    {
        if (_anim.GetBool("IsWalking") != isWalking)
        {
            _anim.SetBool("IsWalking", isWalking);
        }
    }

    private void CheckEndPointState()
    {
        bool atEnd = currentGridPos == targetEndGridPos;
        if (atEnd && !isReachEnd)
        {
            isReachEnd = true;
            Debug.Log($"Priest {priestID} reached the endpoint!");

            FirePit chest = GridManager.Instance.GetFirePitAtGrid(currentGridPos);
            if (chest != null && !chest.isLit && !chest.isTrap) chest.LightFire();
        }
        else if (!atEnd && isReachEnd)
        {
            isReachEnd = false;
        }
    }

    private void CastToGrid(Vector2Int targetGrid)
    {
        if (_stopWalkCoroutine != null) StopCoroutine(_stopWalkCoroutine);

        // Casting cancels walking
        SetWalkingState(false);
        _anim.SetTrigger("Casting");

        FirePit targetFire = GridManager.Instance.GetFirePitAtGrid(targetGrid);
        if (targetFire != null && !targetFire.isLit) targetFire.LightFire();

        PriestUnit targetPriest = GetPriestAtGrid(targetGrid);
        if (targetPriest != null && targetPriest.IsAlive) targetPriest.KillByEnvironment();

        _isCommandExecuting = false;
        _bufferedCommand = CommandType.None;
    }

    public void KillByEnvironment()
    {
        if (!isAlive) return;
        isAlive = false;
        _collider.enabled = false;
        if (_stopWalkCoroutine != null) StopCoroutine(_stopWalkCoroutine);

        SetWalkingState(false);
        _anim.SetTrigger("Die");

        GameManager.Instance.SetGameState(GameState.Fail);
    }

    public void PlayVictoryAnimation()
    {
        if (!isAlive) return;
        if (_stopWalkCoroutine != null) StopCoroutine(_stopWalkCoroutine);

        SetWalkingState(false);
        _anim.SetTrigger("Victory");
    }

    private bool IsMoveCommand(CommandType cmd) => cmd.ToString().StartsWith("Move");

    private PriestUnit GetPriestAtGrid(Vector2Int gridPos)
    {
        foreach (PriestUnit priest in FindObjectsOfType<PriestUnit>())
        {
            if (priest.currentGridPos == gridPos && priest.priestID != this.priestID) return priest;
        }
        return null;
    }

    public void ResetCommandState()
    {
        _isCommandExecuting = false;
        _bufferedCommand = CommandType.None;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        float size = gridCellSize > 0 ? gridCellSize : 1f;
        int x = Mathf.RoundToInt(transform.position.x / size);
        int y = Mathf.RoundToInt(transform.position.y / size);

        if (currentGridPos.x != x || currentGridPos.y != y)
        {
            currentGridPos = new Vector2Int(x, y);
        }

        // Keep Z at -1 in editor as well so visuals match runtime usage
        transform.position = new Vector3(currentGridPos.x * size, currentGridPos.y * size, -1f);

        if (linkedChest != null) targetEndGridPos = linkedChest.gridPos;
    }
#endif
}
