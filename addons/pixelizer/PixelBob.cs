using Godot;

namespace Pixelizer;

/// <summary>
/// Bobs its parent up and down in whole low-res pixels (a sub-pixel bob would make edges shimmer): the altar's
/// floating cube, say. Landlord's map export keeps it in exported scenes.
/// </summary>
[GlobalClass]
public partial class PixelBob : Node
{
    [Export] public float Pixels { get; set; } = 3f;
    [Export] public float Seconds { get; set; } = 3f;
    /// <summary>The parent's height (local Y) at rest.</summary>
    [Export] public float BaseY { get; set; }

    /// <summary>World density: <c>PixelCameraRig.PixelsPerUnit</c>.</summary>
    private const float PixelsPerMetre = 16f;

    private float _time;

    public override void _Process(double delta)
    {
        if (GetParent() is not Node3D target) return;
        _time += (float)delta;
        float pixels = Mathf.Round(Mathf.Sin(_time * Mathf.Tau / Seconds) * Pixels);
        Vector3 position = target.Position;
        position.Y = BaseY + pixels / PixelsPerMetre;
        target.Position = position;
    }
}
