using Godot;

namespace AimTrainer.Targets
{
    /// <summary>
    /// Object pool for targets.
    /// - Pre-creates ALL targets in _Ready() (pool size = grid cells + a few spares).
    /// - Targets stay in the scene tree FOREVER; we only toggle Visible / collision.
    /// - ZERO Instantiate()/QueueFree() during gameplay.
    /// - ONE shared material for every target, unshaded, no shadows.
    /// </summary>
    public partial class TargetPool : Node3D
    {
        [Export] public int PoolSize = 15;
        [Export] public float TargetRadius = 0.4f;
        [Export] public Color TargetColor = new Color(1f, 0.15f, 0.15f);

        /// <summary>Fallback material if none is assigned in the inspector.</summary>
        [Export] public StandardMaterial3D SharedMaterialOverride;

        private Target[] _targets;
        private CollisionShape3D[] _collisions; // toggled with visibility (hitscan must miss hidden targets)
        private StandardMaterial3D _sharedMaterial;

        public int Count => _targets?.Length ?? 0;

        public override void _Ready()
        {
            _sharedMaterial = SharedMaterialOverride ?? CreateDefaultMaterial();

            _targets = new Target[PoolSize];
            _collisions = new CollisionShape3D[PoolSize];
            SphereMesh sphere = new SphereMesh();
            sphere.Radius = TargetRadius;
            sphere.Height = TargetRadius * 2f;
            sphere.RadialSegments = 24;
            sphere.RingSegments = 16;

            SphereShape3D shape = new SphereShape3D();
            shape.Radius = TargetRadius;

            for (int i = 0; i < PoolSize; i++)
            {
                _targets[i] = BuildTarget(i, sphere, shape, out _collisions[i]);
                AddChild(_targets[i]);
                Hide(i); // start hidden — mode will show them
            }
        }

        /// <summary>Builds one pooled target entirely in C# (no scene loading at runtime).</summary>
        private Target BuildTarget(int index, PrimitiveMesh mesh, SphereShape3D shape, out CollisionShape3D collision)
        {
            Target t = new Target
            {
                Name = "Target_" + index,
                PoolIndex = index
            };

            MeshInstance3D meshNode = new MeshInstance3D
            {
                Mesh = mesh,
                MaterialOverride = _sharedMaterial,
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off // perf: no shadow casting
            };

            // One collision shape is SHARED across all pooled targets (perf).
            // Godot 4 forbids reusing a Resource under multiple owners, so each
            // node gets its own copy referencing the same radius — cheap, done once.
            SphereShape3D myShape = shape.Duplicate() as SphereShape3D;
            CollisionShape3D col = new CollisionShape3D { Shape = myShape };
            collision = col;

            t.AddChild(meshNode);
            t.AddChild(col);
            return t;
        }

        private StandardMaterial3D CreateDefaultMaterial()
        {
            StandardMaterial3D m = new StandardMaterial3D
            {
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, // perf: skip lighting
                VertexColorUseAsAlbedo = false,
                AlbedoColor = TargetColor
            };
            m.SetFeature(BaseMaterial3D.Feature.ShadingDisabled, true); // redundant safety
            return m;
        }

        public Target Get(int index) => _targets[index];

        /// <summary>Show a pooled target at a world position (local to this node).</summary>
        public void Show(int index, Vector3 localPosition)
        {
            Target t = _targets[index];
            t.Position = localPosition;
            t.Visible = true;
            _collisions[index].Disabled = false;
        }

        public void Hide(int index)
        {
            Target t = _targets[index];
            t.Visible = false;
            t.CellIndex = -1;
            _collisions[index].Disabled = true; // raycasts must pass through hidden targets
        }
    }
}
