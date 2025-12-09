using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq; // Needed for convenient list handling

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

    private bool isRunning = false;

    // --- IF scope data structure ---
    // Records the branching state for priests that evaluated the condition as true/false.
    private class IfScope
    {
        public List<PriestUnit> trueGroup = new List<PriestUnit>();   // Priests where condition is true
        public List<PriestUnit> falseGroup = new List<PriestUnit>();  // Priests where condition is false
    }

    void Start()
    {
        if (startButton != null) startButton.onClick.AddListener(RunSequence);
    }

    public void RunSequence()
    {
        if (isRunning) return;
        if (GameManager.Instance.CurrentState != GameState.Playing) return;

        StartCoroutine(ExecuteCommandsRoutine());
    }

    IEnumerator ExecuteCommandsRoutine()
    {
        isRunning = true;
        if (startButton != null) startButton.interactable = false;

        List<UI_PlacedBlock> blocks = dropArea.GetAllBlocks();

        // 1. Initial active set: all priests that are currently alive
        List<PriestUnit> currentActivePriests = new List<PriestUnit>();
        foreach (var p in priestUnits)
        {
            if (p != null && p.IsAlive) currentActivePriests.Add(p);
        }

        // 2. Scope stack for nested IF structures
        Stack<IfScope> scopeStack = new Stack<IfScope>();

        // 3. Execute linearly; only LOOP logic modifies index i.
        // IF/ELSE/END_IF do not jump but change which priest group is active.
        for (int i = 0; i < blocks.Count; i++)
        {
            if (GameManager.Instance.CurrentState != GameState.Playing) break;

            UI_PlacedBlock currentBlock = blocks[i];
            CommandType cmd = currentBlock.myCommand;

            // -------------------------------------------------------------
            // IF START: branch priests into true/false groups
            // -------------------------------------------------------------
            if (cmd == CommandType.If_Start)
            {
                IfScope newScope = new IfScope();

                // Evaluate condition per active priest
                foreach (var priest in currentActivePriests)
                {
                    if (CheckConditionForPriest(priest, currentBlock))
                    {
                        newScope.trueGroup.Add(priest);
                    }
                    else
                    {
                        newScope.falseGroup.Add(priest);
                    }
                }

                // Push scope on stack
                scopeStack.Push(newScope);

                // Next commands (until ELSE) apply only to the true group
                currentActivePriests = new List<PriestUnit>(newScope.trueGroup);

                yield return new WaitForSeconds(loopDelay);
            }
            // -------------------------------------------------------------
            // ELSE: switch to the false group for this scope
            // -------------------------------------------------------------
            else if (cmd == CommandType.Else)
            {
                if (scopeStack.Count > 0)
                {
                    IfScope currentScope = scopeStack.Peek();
                    // Next commands (until End_If) apply only to the false group
                    currentActivePriests = new List<PriestUnit>(currentScope.falseGroup);
                }
                yield return new WaitForSeconds(loopDelay);
            }
            // -------------------------------------------------------------
            // END IF: merge true + false groups back together
            // -------------------------------------------------------------
            else if (cmd == CommandType.End_If)
            {
                if (scopeStack.Count > 0)
                {
                    IfScope finishedScope = scopeStack.Pop();

                    // Restore the pre-IF active set: union of true and false groups
                    currentActivePriests = new List<PriestUnit>();
                    currentActivePriests.AddRange(finishedScope.trueGroup);
                    currentActivePriests.AddRange(finishedScope.falseGroup);

                    // Remove dead or null priests for safety
                    currentActivePriests.RemoveAll(p => p == null || !p.IsAlive);
                }
            }
            // -------------------------------------------------------------
            // LOOP END: normal loop jump logic
            // -------------------------------------------------------------
            else if (cmd == CommandType.Loop_End)
            {
                // If the loop is inside an IF branch, inactive priests will still "see" the jump,
                // but since they are filtered out during normal commands, it has no side effects.
                int targetIndex = FindMatchingStartIndex(blocks, i);
                if (targetIndex != -1)
                {
                    i = targetIndex;
                    yield return new WaitForSeconds(loopDelay);
                }
            }
            // -------------------------------------------------------------
            // Normal commands (movement/cast etc.), excluding Loop_Start
            // -------------------------------------------------------------
            else if (cmd != CommandType.None && cmd != CommandType.Loop_Start)
            {
                // Only currently active priests for this branch execute the command
                if (currentActivePriests.Count > 0)
                {
                    bool anyMoved = false;
                    foreach (var priest in currentActivePriests)
                    {
                        if (priest != null && priest.IsAlive)
                        {
                            priest.ResetCommandState();
                            priest.ExecuteCommand(cmd);
                            anyMoved = true;
                        }
                    }
                    if (anyMoved) yield return new WaitForSeconds(stepDuration);
                }
            }
        }

        Debug.Log("Execution finished.");
        isRunning = false;
        if (startButton != null) startButton.interactable = true;
    }

    // =========================================================
    // Conditional check per priest
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

    // Determine what target type exists at a given grid position
    TargetType GetTargetTypeAt(Vector2Int pos)
    {
        // 1. Chests
        if (allChests != null)
        {
            foreach (var chest in allChests)
            {
                if (chest != null &&
                    Mathf.RoundToInt(chest.position.x) == pos.x &&
                    Mathf.RoundToInt(chest.position.y) == pos.y)
                    return TargetType.Chest;
            }
        }
        // 2. Fires
        if (allFires != null)
        {
            foreach (var fire in allFires)
            {
                if (fire != null &&
                    Mathf.RoundToInt(fire.position.x) == pos.x &&
                    Mathf.RoundToInt(fire.position.y) == pos.y)
                    return TargetType.Fire;
            }
        }
        // 3. Walls
        if (allWalls != null)
        {
            foreach (var wall in allWalls)
            {
                if (wall != null &&
                    Mathf.RoundToInt(wall.position.x) == pos.x &&
                    Mathf.RoundToInt(wall.position.y) == pos.y)
                    return TargetType.Wall;
            }
        }
        // 4. Other priests
        foreach (var p in priestUnits)
        {
            if (p == null || !p.IsAlive) continue;
            if (Mathf.RoundToInt(p.transform.position.x) == pos.x &&
                Mathf.RoundToInt(p.transform.position.y) == pos.y)
                return TargetType.Priest;
        }
        // 5. Default: floor
        return TargetType.Floor;
    }

    // Find matching Loop_Start index for a given Loop_End index
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
}
