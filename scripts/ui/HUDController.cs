using Godot;
using AimTrainer.Core;

namespace AimTrainer.UI
{
    /// <summary>
    /// HUD overlay. Labels update ONLY when GameManager fires its change events —
    /// no per-frame string building, no allocations in _Process (there is none).
    /// Also shows the "click to start" prompt and the pause hint.
    /// </summary>
    public partial class HUDController : Control
    {
        [Export] public NodePath GameManagerPath;

        private Label _scoreLabel;
        private Label _hitsLabel;
        private Label _missesLabel;
        private Label _accuracyLabel;
        private Label _streakLabel;
        private Label _timerLabel;
        private Label _promptLabel;
        private Crosshair _crosshair;

        private GameManager _manager;

        // Cached previous values so we only touch labels when something changed.
        private string _lastScore = "";
        private string _lastTimer = "";
        private GameState _lastState = (GameState)(-1);

        public override void _Ready()
        {
            _scoreLabel = GetNode<Label>("StatsPanel/ScoreLabel");
            _hitsLabel = GetNode<Label>("StatsPanel/HitsLabel");
            _missesLabel = GetNode<Label>("StatsPanel/MissesLabel");
            _accuracyLabel = GetNode<Label>("StatsPanel/AccuracyLabel");
            _streakLabel = GetNode<Label>("StatsPanel/StreakLabel");
            _timerLabel = GetNode<Label>("TimerLabel");
            _promptLabel = GetNodeOrNull<Label>("PromptLabel");
            _crosshair = GetNode<Crosshair>("Crosshair");

            _manager = GetNode<GameManager>(GameManagerPath);
            _manager.OnScoreChanged += Refresh;
            _manager.OnStateChanged += Refresh;
            _manager.OnRoundFinished += OnRoundFinished;

            MouseFilter = MouseFilterEnum.Ignore; // clicks pass through to the game

            Refresh();
        }

        private void OnRoundFinished()
        {
            if (_promptLabel != null)
                _promptLabel.Text = "ROUND OVER — PRESS R TO RESTART";
        }

        /// <summary>Called on value-change events only (never every frame).</summary>
        private void Refresh()
        {
            GameState s = _manager.State;
            if (s != _lastState)
            {
                _lastState = s;
                UpdatePrompt(s);
            }

            if (_manager.ScoreText != _lastScore)
            {
                _lastScore = _manager.ScoreText;
                _scoreLabel.Text = _manager.ScoreText;
                _hitsLabel.Text = _manager.HitsText;
                _missesLabel.Text = _manager.MissesText;
                _accuracyLabel.Text = _manager.AccuracyText;
                _streakLabel.Text = _manager.StreakText;
            }

            string timer = Mathf.CeilToInt(Mathf.Max(_manager.TimeLeft, 0f)).ToString();
            if (timer != _lastTimer)
            {
                _lastTimer = timer;
                _timerLabel.Text = timer;
            }
        }

        private void UpdatePrompt(GameState s)
        {
            switch (s)
            {
                case GameState.Ready:
                    if (_promptLabel != null)
                        _promptLabel.Text = "CLICK TO START";
                    _crosshair.Visible = false;
                    break;
                case GameState.Playing:
                    if (_promptLabel != null)
                        _promptLabel.Text = "";
                    _crosshair.Visible = true;
                    break;
                case GameState.Paused:
                    if (_promptLabel != null)
                        _promptLabel.Text = "PAUSED — CLICK TO RESUME (ESC toggles)";
                    break;
                case GameState.GameOver:
                    _crosshair.Visible = false;
                    break;
            }
        }
    }
}
