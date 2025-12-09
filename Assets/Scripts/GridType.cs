/// <summary>
/// Types of grid tiles in the scene.
/// </summary>
public enum GridType
{
    SafePath,   // Walkable safe path
    TrapCliff,  // Trap / cliff (enter = death)
    Wall,       // Wall (non-passable)
    FirePit,    // Fire pit tile (passable, becomes deadly once lit)
    EndPoint    // Endpoint tile (passable, reaching it completes the objective)
}
