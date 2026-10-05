using Godot;
using AimTrainer.Core;

namespace AimTrainer.UI
{
    /// <summary>
    /// End-of-round results panel: Score, Hits, Misses, Accuracy, Hits/Min,
    /// Best Streak + "Play Again" button. Hidden during gameplay.
    /// </summary>
    public partial class ResultsScreen : PanelContainer
    {
        [Export] public NodePath GameManagerPath;

        private Label _resultsLabel;
        private Button _playAgainBtn;
        private GameManager _manager;

        public override void _Ready()
        {
            Visible = false;
            SetAnchorsAndOffsetsPreset(LayoutPreset.Center);

            _resultsLabel = GetNode<Label>("VBox/ResultsLabel");
            _playAgainBtn = GetNode<Button>("VBox/PlayAgainBtn");
            _playAgainBtn.Pressed += OnPlayAgain;

            _manager = GetNode<GameManager>(GameManagerPath);
            _manager.OnRoundFinished += ShowResults;

            // Release the mouse so the player can click the button.
            _manager.OnStateChanged += HandleState;
        }

        private void HandleState()
        {
            if (_manager.State == Core.GameState.Playing)
                Visible = false;
        }

        private void ShowResults()
        {
            var t = _manager.Score;
            float duration = _manager.RoundDuration > 0 ? _manager.RoundDuration : 30f;

            // Built once per round — not a hot path, single string concat is fine.
            _resultsLabel.Text =
                "ROUND COMPLETE\n\n" +
                "Score:        " + t.Score + "\n" +
                "Hits:         " + t.Hits + "\n" +
                "Misses:       " + t.Misses + "\n" +
                "Accuracy:     " + t.CalculateAccuracy().ToString("F1") + "%\n" +
                "Hits/Minute:  " + t.CalculateHitsPerMinute(duration).ToString("F1") + "\n" +
                "Best Streak:  " + t.BestStreak;

            Visible = true;
            _playAgainBtn.GrabFocus();

            // Release the mouse so the player can actually click "Play Again".
            Input.MouseMode = InputMouseModeEnum.Visible;
        }

        private void OnPlayAgain()
        {
            Visible = false;
            _manager.RestartRound();
            // Re-grab the mouse for aiming immediately (PlayerController keeps its
            // own capture flag in sync via the click that will follow).
            Input.MouseMode = InputMouseModeEnum.Confined;
        }
    }
}
