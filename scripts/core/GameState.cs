namespace AimTrainer.Core
{
    /// <summary>
    /// High-level state of the trainer. Owned by GameManager.
    /// </summary>
    public enum GameState
    {
        Ready,    // Waiting for player to click to start the round
        Playing,  // Round active, timer running
        Paused,   // Mouse released (ESC), round frozen
        GameOver  // Round finished, results shown
    }
}
