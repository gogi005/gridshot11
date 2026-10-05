using Godot;

namespace AimTrainer.UI
{
    /// <summary>
    /// Classic CS/Valorant crosshair: 4 lines + center dot.
    /// Drawn at the CENTER of its own rect — and the node itself is anchored to
    /// screen center. It NEVER moves. Mouse motion rotates the camera instead.
    /// </summary>
    public partial class Crosshair : Control
    {
        [Export] public float CrosshairSize = 10f;
        [Export] public float CrosshairGap = 4f;
        [Export] public float CrosshairThickness = 2f;
        [Export] public Color CrosshairColor = new Color(0.1f, 1f, 0.35f);
        [Export] public bool ShowCenterDot = true;

        public override void _Ready()
        {
            MouseFilter = MouseFilterEnum.Ignore; // never eat shots/menu clicks
            // Keep it perfectly centered regardless of window size.
            SetAnchorsAndOffsetsPreset(LayoutPreset.Center);
        }

        public override void _Draw()
        {
            Vector2 c = Size * 0.5f; // node is centered on screen → draw around middle
            float half = CrosshairThickness * 0.5f;
            float inner = CrosshairGap;
            float outer = CrosshairGap + CrosshairSize;

            // Right line
            DrawRect(new Rect2(c.X + inner, c.Y - half, outer - inner, CrosshairThickness), CrosshairColor);
            // Left line
            DrawRect(new Rect2(c.X - outer, c.Y - half, outer - inner, CrosshairThickness), CrosshairColor);
            // Bottom line
            DrawRect(new Rect2(c.X - half, c.Y + inner, CrosshairThickness, outer - inner), CrosshairColor);
            // Top line
            DrawRect(new Rect2(c.X - half, c.Y - outer, CrosshairThickness, outer - inner), CrosshairColor);

            if (ShowCenterDot)
                DrawRect(new Rect2(c.X - half, c.Y - half, CrosshairThickness, CrosshairThickness), CrosshairColor);
        }
    }
}
