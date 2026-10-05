using Godot;

namespace AimTrainer.Targets
{
    /// <summary>
    /// Calculates 3D positions for a rectangular grid of cells floating in front
    /// of the player (Valorant-style: 4 columns × 3 rows at head height).
    /// The node itself should sit at the player's eye position; cells are computed
    /// relative to it, straight ahead (-Z).
    /// </summary>
    public partial class GridSpawner : Node3D
    {
        [Export] public int GridCols = 4;
        [Export] public int GridRows = 3;
        [Export] public float CellSpacingX = 3.0f;
        [Export] public float CellSpacingY = 2.0f;
        [Export] public float GridDistance = 10.0f;   // units in front of the player
        [Export] public float GridBaseHeight = 2.0f;  // center row height (eye level ~1.6)

        private Vector3[] _positions; // pre-computed — zero allocation at runtime

        public int CellCount => GridCols * GridRows;

        public override void _Ready()
        {
            BuildPositions();
        }

        private void BuildPositions()
        {
            int n = CellCount;
            _positions = new Vector3[n];

            float totalWidth = (GridCols - 1) * CellSpacingX;
            float startX = -totalWidth * 0.5f;

            for (int row = 0; row < GridRows; row++)
            {
                for (int col = 0; col < GridCols; col++)
                {
                    float x = startX + col * CellSpacingX;
                    // Row 0 = top row, so Y decreases with row index
                    float y = GridBaseHeight + ((GridRows - 1) * 0.5f - row) * CellSpacingY;
                    _positions[row * GridCols + col] = new Vector3(x, y, -GridDistance);
                }
            }
        }

        /// <summary>Local-space position of a grid cell (relative to this node).</summary>
        public Vector3 GetCellPosition(int index)
        {
            if (_positions == null)
                BuildPositions();
            return _positions[index];
        }

        /// <summary>
        /// Returns a random cell index whose occupied[] slot is false, or -1 if none free.
        /// Uses a shuffle-start scan — no allocations, no LINQ.
        /// </summary>
        public int GetRandomEmptyCell(bool[] occupied)
        {
            int n = occupied.Length;
            int start = GD.RandRange(0, n - 1);
            for (int i = 0; i < n; i++)
            {
                int idx = (start + i) % n;
                if (!occupied[idx])
                    return idx;
            }
            return -1;
        }
    }
}
