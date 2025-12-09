/// <summary>
/// 8-direction movement / casting command enum for priests.
/// </summary>
public enum CommandType
{
    None,           // No command
    Move_Up,        // Move up
    Move_Down,      // Move down
    Move_Left,      // Move left
    Move_Right,     // Move right
    Move_LeftUp,    // Move up-left
    Move_RightUp,   // Move up-right
    Move_LeftDown,  // Move down-left
    Move_RightDown, // Move down-right

    Loop_Start,     // Loop start marker (jump target)
    Loop_End,       // Loop end / jump command (jump initiator)

    If_Start,       // IF start block
    Else,           // ELSE block
    End_If          // IF end block (can be hidden; marks scope end)
}

public enum ConditionOperator
{
    Equals,     // Equal to
    NotEquals   // Not equal to
}

// Target types for conditional checks
public enum TargetType
{
    Floor,      // Floor tile
    Fire,       // Fire pit
    Chest,      // Chest
    Priest,     // Priest
    Wall        // Wall / obstacle
}
