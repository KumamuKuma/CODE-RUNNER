using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class TurnManager : MonoBehaviour
{
    [Header("Scene References")]
    public UI_TimelineDropArea dropArea;
    public PriestUnit[] priestUnits;

    [Header("Environment Objects")]
    public Transform[] allFires;
    public Transform[] allChests;
    public Transform[] allWalls;

    [Header("UI & Settings")]
    public Button startButton;
    public float stepDuration = 0.8f;
    public float loopDelay = 0.1f;

    // Total movement steps (used for scoring)
    public int TotalStepCount { get; private set; } = 0;

    private bool isRunning = false;

    // Priests that already reached a chest and should no longer execute commands
    private HashSet<PriestUnit> finishedPriests = new HashSet<PriestUnit>();

    // IF-scope structure to store true/false branch groups
    private class IfScope
    {
        public List<PriestUnit> trueGroup = new List<PriestUnit>();
        public List<PriestUnit> falseGroup = new List<PriestUnit>();
    }

    void Start()
    {
        if (startButton != null) startButton.onClick.AddListener(RunSequence);
    }

    public void RunSequence()
    {
        if (isRunning) return;
        if (GameManager.Instance.CurrentState != GameState.Playing) return;

        TotalStepCount = 0;
        finishedPriests.Clear();

        StartCoroutine(ExecuteCommandsRoutine());
    }

    IEnumerator ExecuteCommandsRoutine()
    {
        isRunning = true;
        if (startButton != null) startButton.interactable = false;

        List<UI_PlacedBlock> blocks = dropArea.GetAllBlocks();

        // Initial active set: all priests that are alive
        List<PriestUnit> currentActivePriests = new List<PriestUnit>();
        foreach (var p in priestUnits)
        {
            if (p != null && p.IsAlive)
                currentActivePriests.Add(p);
        }

        // Stack for nested IF structures
        Stack<IfScope> scopeStack = new Stack<IfScope>();

        // Main command execution loop
        for (int i = 0; i < blocks.Count; i++)
        {
            if (GameManager.Instance.CurrentState != GameState.Playing) break;

            UI_PlacedBlock currentBlock = blocks[i];
            CommandType cmd = currentBlock.myCommand;

            // -------------------------------------------------------
            // IF START — split active priests into true and false sets
            // -------------------------------------------------------
            if (cmd == CommandType.If_Start)
            {
                IfScope newScope = new IfScope();

                foreach (var priest in currentActivePriests)
                {
                    if (priest == null || !priest.IsAlive) continue;
                    if (finishedPriests.Contains(priest)) continue;

                    if (CheckConditionForPriest(priest, currentBlock))
                        newScope.trueGroup.Add(priest);
                    else
                        newScope.falseGroup.Add(priest);
                }

                scopeStack.Push(newScope);
                currentActivePriests = new List<PriestUnit>(newScope.trueGroup);

                yield return new WaitForSeconds(loopDelay);
            }

            // -------------------------------------------------------
            // ELSE — switch to false branch
            // -------------------------------------------------------
            else if (cmd == CommandType.Else)
            {
                if (scopeStack.Count > 0)
                {
                    IfScope currentScope = scopeStack.Peek();
                    currentActivePriests = new List<PriestUnit>(currentScope.falseGroup);
                }
                yield return new WaitForSeconds(loopDelay);
            }

            // -------------------------------------------------------
            // END IF — merge true and false groups
            // -------------------------------------------------------
            else if (cmd == CommandType.End_If)
            {
                if (scopeStack.Count > 0)
                {
                    IfScope finishedScope = scopeStack.Pop();

                    currentActivePriests = new List<PriestUnit>();
                    currentActivePriests.AddRange(finishedScope.trueGroup);
                    currentActivePriests.AddRange(finishedScope.falseGroup);

                    currentActivePriests.RemoveAll(p => p == null || !p.IsAlive);
                }
            }

            // -------------------------------------------------------
            // LOOP END — jump to matching LOOP START
            // -------------------------------------------------------
            else if (cmd == CommandType.Loop_End)
            {
                int targetIndex = FindMatchingStartIndex(blocks, i);
                if (targetIndex != -1)
                {
                    i = targetIndex;
                    yield return new WaitForSeconds(loopDelay);
                }
            }

            // -------------------------------------------------------
            // Normal commands (movement, casting, etc.)
            // -------------------------------------------------------
            else if (cmd != CommandType.None && cmd != CommandType.Loop_Start)
            {
                if (currentActivePriests.Count > 0)
                {
                    bool anyMoved = false;
                    int stepThisCommand = 0;

                    foreach (var priest in currentActivePriests)
                    {
                        if (priest == null || !priest.IsAlive) continue;

                        // Skip if priest already reached the chest
                        if (IsPriestOnChest(priest))
                        {
                            if (!finishedPriests.Contains(priest))
                                finishedPriests.Add(priest);
                            continue;
                        }

                        if (finishedPriests.Contains(priest)) continue;

                        priest.ResetCommandState();
                        priest.ExecuteCommand(cmd);

                        // Count movement steps
                        if (IsMoveCommand(cmd))
                        {
                            stepThisCommand++;

                            if (IsPriestOnChest(priest))
                                finishedPriests.Add(priest);
                        }

                        anyMoved = true;
                    }

                    if (stepThisCommand > 0)
                        TotalStepCount += stepThisCommand;

                    if (anyMoved)
                        yield return new WaitForSeconds(stepDuration);
                }
            }
        }

        Debug.Log($"Execution finished. Total steps = {TotalStepCount}");
        isRunning = false;
        if (startButton != null) startButton.interactable = true;
    }

    // =========================================================
    // Condition evaluation for a specific priest
    // =========================================================
    bool CheckConditionForPriest(PriestUnit priest, UI_PlacedBlock block)
    {
        if (priest == null || !priest.IsAlive) return false;

        Vector2Int priestPos = new Vector2Int(
            Mathf.RoundToInt(priest.transform.position.x),
            Mathf.RoundToInt(priest.transform.position.y)
        );

        Vector2Int checkPos = priestPos + block.conditionDir;
        TargetType actualType = GetTargetTypeAt(checkPos);

        if (block.conditionOp == ConditionOperator.Equals)
            return actualType == block.conditionTarget;
        else
            return actualType != block.conditionTarget;
    }

    // Get grid target type
    TargetType GetTargetTypeAt(Vector2Int pos)
    {
        foreach (var chest in allChests)
        {
            if (chest != null &&
                Mathf.RoundToInt(chest.position.x) == pos.x &&
                Mathf.RoundToInt(chest.position.y) == pos.y)
                return TargetType.Chest;
        }

        foreach (var fire in allFires)
        {
            if (fire != null &&
                Mathf.RoundToInt(fire.position.x) == pos.x &&
                Mathf.RoundToInt(fire.position.y) == pos.y)
                return TargetType.Fire;
        }

        foreach (var wall in allWalls)
        {
            if (wall != null &&
                Mathf.RoundToInt(wall.position.x) == pos.x &&
                Mathf.RoundToInt(wall.position.y) == pos.y)
                return TargetType.Wall;
        }

        foreach (var p in priestUnits)
        {
            if (p == null || !p.IsAlive) continue;
            if (Mathf.RoundToInt(p.transform.position.x) == pos.x &&
                Mathf.RoundToInt(p.transform.position.y) == pos.y)
                return TargetType.Priest;
        }

        return TargetType.Floor;
    }

    // Find matching Loop_Start index for Loop_End
    private int FindMatchingStartIndex(List<UI_PlacedBlock> blocks, int loopEndIndex)
    {
        int nestedLevel = 0;
        for (int k = loopEndIndex - 1; k >= 0; k--)
        {
            CommandType t = blocks[k].myCommand;
            if (t == CommandType.Loop_End) nestedLevel++;
            else if (t == CommandType.Loop_Start)
            {
                if (nestedLevel == 0) return k;
                else nestedLevel--;
            }
        }
        return -1;
    }

    // Check if a command is a movement command
    private bool IsMoveCommand(CommandType cmd)
    {
        switch (cmd)
        {
            case CommandType.Move_Up:
            case CommandType.Move_Down:
            case CommandType.Move_Left:
            case CommandType.Move_Right:
            case CommandType.Move_LeftUp:
            case CommandType.Move_RightUp:
            case CommandType.Move_LeftDown:
            case CommandType.Move_RightDown:
                return true;
            default:
                return false;
        }
    }

    // Check whether a priest is standing on a chest tile
    private bool IsPriestOnChest(PriestUnit priest)
    {
        if (priest == null) return false;

        Vector2Int pos = new Vector2Int(
            Mathf.RoundToInt(priest.transform.position.x),
            Mathf.RoundToInt(priest.transform.position.y)
        );

        return GetTargetTypeAt(pos) == TargetType.Chest;
    }
}
