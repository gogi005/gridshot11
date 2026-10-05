using System;
using Godot;
using AimTrainer.Targets;

namespace AimTrainer.Player
{
    /// <summary>
    /// Instant hitscan from the exact center of the screen (where the STATIC
    /// crosshair always is). Fires "HitOccurred" or "MissOccurred" events that
    /// GameManager routes to the active mode.
    /// Zero allocations per shot: PhysicsRayQueryParameters3D is created once.
    /// </summary>
    public partial class ShootingSystem : Node
    {
        [Export] public float RayLength = 100f;
        [Export] public uint CollisionMask = 0xFFFFFFFF;

        public event Action<Target> HitOccurred;
        public event Action MissOccurred;

        private Camera3D _camera;
        private PhysicsRayQueryParameters3D _query;
        private Vector3 _from; // reused, no allocation in hot path
        private Vector3 _to;

        /// <summary>GameManager sets this false while the round is Ready/Paused/Over.</summary>
        public bool InputEnabled = true;

        public override void _Ready()
        {
            // The camera lives under ../Player/Camera3D in Main.tscn
            _camera = GetNode<Camera3D>("../Player/Camera3D");

            _query = PhysicsRayQueryParameters3D.Create(Vector3.Zero, Vector3.Forward, CollisionMask);
            _query.CollideWithAreas = false;
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (!InputEnabled)
                return;

            if (!@event.IsActionPressed("shoot"))
                return;

            // Only shoot while the mouse is captured (playing), never on menu clicks.
            if (Input.MouseMode != InputMouseModeEnum.Confined)
                return;

            // Ray from camera origin straight through screen center (= crosshair).
            _from = _camera.GlobalPosition;
            Vector3 forward = _camera.GlobalTransform.Basis.Z;
            _to.X = _from.X - forward.X * RayLength;
            _to.Y = _from.Y - forward.Y * RayLength;
            _to.Z = _from.Z - forward.Z * RayLength; // camera looks down -Z, so negate

            _query.From = _from;
            _query.To = _to;

            Godot.Collections.Dictionary result = _camera.GetWorld3D().PhysicsSpace.IntersectRay(_query);

            if (result.Count > 0 &&
                result["collider"].AsGodotObject() is Target target)
            {
                HitOccurred?.Invoke(target);
            }
            else
            {
                MissOccurred?.Invoke();
            }
        }
    }
}
