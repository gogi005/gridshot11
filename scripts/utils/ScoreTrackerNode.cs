using System;
using Godot;

namespace AimTrainer.Utils
{
    /// <summary>
    /// Thin Node wrapper so the tracker can live in the scene tree as a child of
    /// GameManager (per architecture). All stat logic is in the inner ScoreTracker.
    /// </summary>
    public partial class ScoreTrackerNode : Node
    {
        public ScoreTracker Tracker { get; private set; }

        public override void _Ready()
        {
            Tracker = new ScoreTracker();
        }
    }
}
