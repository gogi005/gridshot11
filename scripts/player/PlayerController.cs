using System;
using Godot;

namespace AimTrainer.Player
{
    /// <summary>
    /// Mouse-look only. The player NEVER moves (gridshot is static).
    /// Mouse motion rotates this node's Y (yaw) and the Camera3D's X (pitch).
    /// THE CROSSHAIR NEVER MOVES — it stays fixed at screen center while the
    /// camera rotates. ESC toggles mouse capture (pause/resume). R restarts.
    /// </summary>
    public partial class PlayerController : Node3D
    {
        [Export] public float MouseSensitivity = 0.002f;
        [Export] public float MaxPitchDeg = 89.0f;

        public event Action RoundStartRequested;
        public event Action RestartRequested;
        public event Action MouseClicked;
        public event Action<bool> MouseCaptureToggled;

        private Camera3D _camera;
        private float _yaw;
        private float _pitch;
        private bool _captured;

        public bool IsCaptured => _captured;

        public override void _Ready()
        {
            _camera = GetNode<Camera3D>("Camera3D");

            // Start with the cursor captured so aim works immediately.
            SetCapture(true);
        }

        private void SetCapture(bool on)
        {
            if (_captured == on)
                return;
            _captured = on;
            Input.MouseMode = on ? InputMouseModeEnum.Confined : InputMouseModeEnum.Visible;
            MouseCaptureToggled?.Invoke(on);
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (@event is InputEventMouseMotion motion && _captured)
            {
                // Horizontal mouse → rotate whole player (yaw)
                _yaw -= motion.Relative.X * MouseSensitivity;
                RotationDegrees = new Vector3(0f, Mathf.RadToDeg(_yaw), 0f);

                // Vertical mouse → pitch the camera only, clamped
                _pitch -= motion.Relative.Y * MouseSensitivity;
                float limit = Mathf.DegToRad(MaxPitchDeg);
                _pitch = Mathf.Clamp(_pitch, -limit, limit);
                _camera.RotationDegrees = new Vector3(Mathf.RadToDeg(_pitch), 0f, 0f);
            }
            else if (@event.IsActionPressed("ui_cancel"))
            {
                // ESC: release the mouse (pause). Click "resume" to recapture.
                SetCapture(!_captured);
            }
            else if (@event.IsActionPressed("restart"))
            {
                RestartRequested?.Invoke();
            }
            else if (@event is InputEventMouseButton mb && mb.Pressed
                     && mb.ButtonIndex == MouseButton.Left)
            {
                // Any left click: re-capture the mouse if it was released (ESC/pause),
                // and notify GameManager (used to start the round on first click).
                if (!_captured)
                    SetCapture(true);
                MouseClicked?.Invoke();
            }
        }

        /// <summary>Called by GameManager once the round actually begins.</summary>
        public void NotifyRoundStarted()
        {
            RoundStartRequested?.Invoke();
        }
    }
}
