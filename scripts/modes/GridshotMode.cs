using System;
using Godot;
using AimTrainer.Core;
using AimTrainer.Targets;
using AimTrainer.Utils;

namespace AimTrainer.Modes
{
    /// <summary>
    /// GRIDSHOT: 3 targets live on a 4×3 grid at all times.
    /// Hit a target → it vanishes, another appears in a random empty cell.
    /// All logic lives here — Target nodes are dumb markers (perf rule).
    /// No _Process needed: everything is event-driven (hit/miss callbacks).
    /// </summary>
    public partial class GridshotMode : Node3D, IGameMode
    {
        [Export] public int ActiveTargetCount = 3;
        [Export] public int ScorePerHit = 100;

        public event Action ScoreChanged;
        public event Action ModeEnded;

        private GridSpawner _spawner;
        private TargetPool _pool;
        private ScoreTracker _scoreTracker;
        private GameManager _manager;

        // Fixed-size arrays allocated once in _Ready — zero runtime allocation.
        private bool[] _occupied;                 // per grid cell
        private int[] _activePoolIndices;         // pool index of each active target
        private int[] _activeCellIndices;         // grid cell of each active target

        public override void _Ready()
        {
            _spawner = GetNode<GridSpawner>("GridSpawner");
            _pool = GetNode<TargetPool>("TargetPool");

            int cells = _spawner.CellCount;
            _occupied = new bool[cells];
            _activePoolIndices = new int[ActiveTargetCount];
            _activeCellIndices = new int[ActiveTargetCount];
            for (int i = 0; i < ActiveTargetCount; i++)
                _activePoolIndices[i] = -1;
        }

        // ══════════════════ IGameMode ══════════════════

        public void SetGameManager(GameManager manager)
        {
            _manager = manager;
            _scoreTracker = manager.Score;
        }

        public void StartMode()
        {
            SpawnInitialTargets();
        }

        public void EndMode()
        {
            HideAll();
        }

        public void ResetMode()
        {
            HideAll();
        }

        public void OnTargetHit(Target target)
        {
            if (_scoreTracker == null)
                return;

            int slot = FindSlotByPoolIndex(target.PoolIndex);
            if (slot < 0)
                return; // already-resolved hit (double raycast edge case) — ignore

            // Free the cell + hide the hit target (slot stays reserved for its respawn)
            int cell = _activeCellIndices[slot];
            _occupied[cell] = false;
            _pool.Hide(target.PoolIndex);

            _scoreTracker.RegisterHit(ScorePerHit);
            ScoreChanged?.Invoke();

            // Immediately respawn into a random empty cell (no instantiate!)
            SpawnTargetInSlot(slot);
        }

        public void OnTargetMissed()
        {
            _scoreTracker?.RegisterMiss();
            ScoreChanged?.Invoke();
        }

        // ══════════════════ SPAWNING ══════════════════

        private void SpawnInitialTargets()
        {
            Array.Clear(_occupied, 0, _occupied.Length);

            for (int i = 0; i < ActiveTargetCount; i++)
                SpawnTargetInSlot(i);
        }

        /// <summary>
        /// Shows pool object #slot in a random free grid cell.
        /// Pool index == slot index, so respawns always reuse the same pre-made node.
        /// </summary>
        private void SpawnTargetInSlot(int slot)
        {
            int cell = _spawner.GetRandomEmptyCell(_occupied);
            if (cell < 0)
            {
                ModeEnded?.Invoke(); // grid exhausted — safety valve
                return;
            }

            _occupied[cell] = true;

            Vector3 pos = _spawner.GetCellPosition(cell);
            _pool.Show(slot, pos);
            _pool.Get(slot).CellIndex = cell;

            _activePoolIndices[slot] = slot;
            _activeCellIndices[slot] = cell;
        }

        private void HideAll()
        {
            for (int i = 0; i < ActiveTargetCount; i++)
            {
                _pool.Hide(i);
                _activePoolIndices[i] = -1;
                _activeCellIndices[i] = -1;
            }
            if (_occupied != null)
                Array.Clear(_occupied, 0, _occupied.Length);
        }

        private int FindSlotByPoolIndex(int poolIndex)
        {
            for (int i = 0; i < ActiveTargetCount; i++)
                if (_activePoolIndices[i] == poolIndex)
                    return i;
            return -1;
        }
    }
}
