using System;
using Godot;
using AimTrainer.Modes;
using AimTrainer.Targets;
using AimTrainer.Utils;

namespace AimTrainer.Core
{
    /// <summary>
    /// Central brain of the trainer.
    /// - Owns the game state (Ready / Playing / Paused / GameOver)
    /// - Owns the active IGameMode (Gridshot for now)
    /// - Runs a frame-based round timer (accumulates delta, NO Timer node)
    /// - Bridges mode events to the UI layer.
    /// </summary>
    public partial class GameManager : Node
    {
        [Export] public float RoundDuration = 30.0f;
        [Export] public bool AutoStartOnClick = true;

        public GameState State { get; private set; } = GameState.Ready;

        // ---- References resolved in _Ready ----
        private IGameMode _mode;
        private ScoreTracker _scoreTracker;
        private Player.PlayerController _player;
        private Player.ShootingSystem _shooting;

        private float _timeLeft;

        // ---- Events for the UI (HUD / Results) ----
        public event Action OnStateChanged;
        public event Action OnScoreChanged;      // fired only when a value actually changes
        public event Action OnRoundFinished;

        public float TimeLeft => _timeLeft;
        public ScoreTracker Score => _scoreTracker;
        public IGameMode Mode => _mode;

        public override void _Ready()
        {
            // Locate children wired up in Main.tscn
            ScoreTrackerNode trackerNode = GetNodeOrNull<ScoreTrackerNode>("ScoreTracker");
            _scoreTracker = trackerNode?.Tracker;
            _mode = FindActiveMode();
            _player = GetNode<Player.PlayerController>("../Player");
            _shooting = GetNode<Player.ShootingSystem>("../ShootingSystem");

            if (_scoreTracker == null)
                GD.PushWarning("GameManager: ScoreTracker child not found.");

            if (_mode != null)
            {
                _mode.ScoreChanged += HandleScoreChanged;
                _mode.ModeEnded += HandleModeEnded;
                _mode.SetGameManager(this);
            }
            else
            {
                GD.PushError("GameManager: No active IGameMode child found!");
            }

            // Shooting callbacks
            _shooting.HitOccurred += HandleHit;
            _shooting.MissOccurred += HandleMiss;

            // Player clicks: first click starts the round; R restarts;
            // ESC capture changes pause/resume the timer.
            _player.MouseClicked += HandleMouseClicked;
            _player.RoundStartRequested += StartRound;
            _player.RestartRequested += RestartRound;
            _player.MouseCaptureToggled += OnMouseCaptureToggled;

            ResetToReady();
        }

        private IGameMode FindActiveMode()
        {
            foreach (Node child in GetChildren())
            {
                if (child is IGameMode mode)
                    return mode;
            }
            return null;
        }

        // ══════════════════ STATE MACHINE ══════════════════

        private void ResetToReady()
        {
            SetState(GameState.Ready);
            _timeLeft = RoundDuration;
            _scoreTracker?.Reset();
            _mode?.ResetMode();
            UpdateHudLabels();
        }

        public void StartRound()
        {
            if (State == GameState.Playing)
                return;

            _scoreTracker.Reset();
            _lastDisplayedSecond = -1;
            _timeLeft = RoundDuration;
            _mode.StartMode();
            SetState(GameState.Playing);
            UpdateHudLabels();
        }

        public void RestartRound()
        {
            if (_mode != null)
                _mode.EndMode();
            StartRound();
        }

        private void EndRound()
        {
            SetState(GameState.GameOver);
            _mode.EndMode();
            OnRoundFinished?.Invoke();
        }

        private void SetState(GameState next)
        {
            if (State == next)
                return;
            State = next;

            // Shooting is only live while Playing; the click that starts the
            // round is handled explicitly by HandleMouseClicked below.
            _shooting.InputEnabled = next == GameState.Playing;

            OnStateChanged?.Invoke();
        }

        /// <summary>First click starts the round; clicks while unlocked resume it.</summary>
        private void HandleMouseClicked()
        {
            if (State != GameState.Ready || !AutoStartOnClick)
                return;

            StartRound();
            _shooting.InputEnabled = true; // this very click also shoots
        }

        // ══════════════════ FRAME TIMER ══════════════════
        // Accumulate delta manually — no Timer nodes, zero allocations.

        public override void _Process(double delta)
        {
            if (State != GameState.Playing)
                return;

            _timeLeft -= (float)delta;

            // Whole-second display update only (UI refreshes only on change)
            int displayed = Mathf.CeilToInt(Mathf.Max(_timeLeft, 0f));
            if (displayed != _lastDisplayedSecond)
            {
                _lastDisplayedSecond = displayed;
                OnScoreChanged?.Invoke(); // HUD reads TimeLeft too
            }

            if (_timeLeft <= 0f)
            {
                _timeLeft = 0f;
                EndRound();
            }
        }

        private int _lastDisplayedSecond = -1;

        // ══════════════════ SHOOT CALLBACKS ══════════════════

        private void HandleHit(Target target)
        {
            // Shooting is gated by InputEnabled (only true while Playing, plus the
            // single click that starts the round), so just forward to the mode.
            if (State != GameState.Playing)
                return;

            _mode?.OnTargetHit(target);
        }

        private void HandleMiss()
        {
            if (State != GameState.Playing)
                return;

            _mode?.OnTargetMissed();
        }

        // ══════════════════ PAUSE (ESC toggles mouse capture) ══════════════════

        public void OnMouseCaptureToggled(bool captured)
        {
            if (captured && State == GameState.Paused)
            {
                SetState(GameState.Playing);
            }
            else if (!captured && State == GameState.Playing)
            {
                SetState(GameState.Paused);
            }
        }

        // ══════════════════ EVENTS → SCORE ══════════════════

        private void HandleScoreChanged()
        {
            UpdateHudLabels();
            OnScoreChanged?.Invoke();
        }

        private void HandleModeEnded()
        {
            // Mode ended itself (e.g. all cells exhausted) — finish the round.
            if (State == GameState.Playing)
                EndRound();
        }

        // Pre-built label strings are cached here so the HUD never formats
        // strings every frame — only when a value actually changes.
        public string ScoreText { get; private set; } = "";
        public string HitsText { get; private set; } = "";
        public string MissesText { get; private set; } = "";
        public string AccuracyText { get; private set; } = "";
        public string StreakText { get; private set; } = "";

        private void UpdateHudLabels()
        {
            if (_scoreTracker == null)
                return;

            ScoreText = "SCORE  " + _scoreTracker.Score;
            HitsText = "HITS   " + _scoreTracker.Hits;
            MissesText = "MISS   " + _scoreTracker.Misses;
            AccuracyText = "ACC    " + _scoreTracker.CalculateAccuracy().ToString("F1") + "%";
            StreakText = "STREAK " + _scoreTracker.Streak;
        }
    }
}
