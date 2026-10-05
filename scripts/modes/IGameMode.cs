using System;
using AimTrainer.Core;
using AimTrainer.Targets;

namespace AimTrainer.Modes
{
    /// <summary>
    /// Contract every training mode (Gridshot, Flick, Tracking, ...) implements.
    /// All spawn/despawn logic lives inside the mode — targets themselves are dumb.
    /// </summary>
    public interface IGameMode
    {
        void SetGameManager(GameManager manager);
        void StartMode();
        void EndMode();
        void ResetMode();

        /// <summary>Called by GameManager when a raycast hit lands on a Target.</summary>
        void OnTargetHit(Target target);

        /// <summary>Called by GameManager when a shot hits nothing.</summary>
        void OnTargetMissed();

        event Action ScoreChanged;
        event Action ModeEnded;
    }
}
