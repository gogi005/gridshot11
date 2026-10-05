using Godot;

namespace AimTrainer.Targets
{
    /// <summary>
    /// Dumb marker class for raycast identification.
    /// NO logic here, NO _Process — all behavior is driven by GridshotMode.
    /// Just remembers which pool index and grid cell it currently occupies.
    /// </summary>
    public partial class Target : StaticBody3D
    {
        /// <summary>Index inside the TargetPool.</summary>
        public int PoolIndex { get; set; } = -1;

        /// <summary>Grid cell this target currently sits in (-1 = hidden).</summary>
        public int CellIndex { get; set; } = -1;
    }
}
