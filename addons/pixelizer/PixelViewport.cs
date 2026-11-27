#nullable enable // the addon builds the same in projects that leave <Nullable> off

using Godot;

namespace Pixelizer;

/// <summary>
/// Renders the 3D world into a small SubViewport and shows it on screen upscaled by a
/// whole-number factor with nearest-neighbour filtering. The factor is window height / 360,
/// rounded; the low-res picture is then as wide and tall as needed to fill the whole window.
///
/// Expected layout (the SubViewport is authored in the scene, the display is created here):
///   PixelViewport (Control, full rect)
///   └── SubViewport            ← put your 3D world inside this
///
/// The SubViewport is rendered <see cref="Margin"/> pixels larger on every side. Those extra
/// pixels are hidden by clipping and let us shift the upscaled image by a fraction of a
/// low-res pixel (see <see cref="SetSubPixelOffset"/>) without exposing an empty edge.
/// </summary>
[GlobalClass]
public partial class PixelViewport : Control
{
    /// <summary>Hidden low-res pixels rendered around each edge for sub-pixel scrolling.</summary>
    public const int Margin = 1;

    /// <summary>The game's "virtual" resolution: its height picks the upscale (720p ×2, 1080p ×3, 1440p ×4, 4K ×6).</summary>
    [Export] public Vector2I BaseResolution { get; set; } = new(640, 360);

    /// <summary>Shift the upscaled image by the camera's leftover sub-pixel offset.</summary>
    [Export]
    public bool SubPixelSmoothing
    {
        get => _subPixelSmoothing;
        set { _subPixelSmoothing = value; ApplyDisplayPosition(); }
    }

    /// <summary>Debug switch: false renders at the window's native resolution instead.</summary>
    [Export]
    public bool LowResEnabled
    {
        get => _lowResEnabled;
        set { _lowResEnabled = value; if (IsNodeReady()) UpdateLayout(); }
    }

    public SubViewport SubViewport { get; private set; } = null!;

    /// <summary>Screen pixels per low-res pixel (1 when low-res mode is off).</summary>
    public int PixelScale { get; private set; } = 1;

    /// <summary>Visible render size in low-res pixels, excluding the hidden margin.</summary>
    public Vector2I RenderResolution { get; private set; }

    /// <summary>
    /// Rendered pixels per low-res pixel: 1 in low-res mode, the upscale low-res mode would use in the debug
    /// native-resolution mode. The camera divides by it so both modes show the same framing.
    /// </summary>
    public float ResolutionFactor => _lowResEnabled ? 1f : _virtualScale;

    private int _virtualScale = 1;
    private bool _subPixelSmoothing = true;
    private bool _lowResEnabled = true;
    private Vector2 _subPixelOffset;
    private Vector2 _displayOrigin;
    private Control _frame = null!;
    private TextureRect _display = null!;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;

        SubViewport = GetNode<SubViewport>("SubViewport");
        SubViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
        // Anything that smooths or blends pixels ruins the look, so turn it all off.
        SubViewport.Msaa3D = Viewport.Msaa.Disabled;
        SubViewport.ScreenSpaceAA = Viewport.ScreenSpaceAAEnum.Disabled;
        SubViewport.UseTaa = false;
        SubViewport.Scaling3DMode = Viewport.Scaling3DModeEnum.Bilinear;
        SubViewport.Scaling3DScale = 1f;

        // _frame is the visible window onto the upscaled image; it clips the hidden margin.
        _frame = new Control { Name = "Frame", ClipContents = true, MouseFilter = MouseFilterEnum.Ignore };
        AddChild(_frame);

        _display = new TextureRect
        {
            Name = "Display",
            Texture = SubViewport.GetTexture(),
            TextureFilter = TextureFilterEnum.Nearest,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _frame.AddChild(_display);

        Resized += UpdateLayout;
        UpdateLayout();
    }

    /// <summary>
    /// Called by the camera every frame with how far (in low-res pixels) the ideal camera
    /// position is from the snapped one. Applied immediately so it lands in the same frame.
    /// </summary>
    public void SetSubPixelOffset(Vector2 lowResPixels)
    {
        _subPixelOffset = lowResPixels;
        ApplyDisplayPosition();
    }

    /// <summary>
    /// Maps a point in window coordinates (e.g. the mouse) to pixel coordinates inside the SubViewport,
    /// which is what <see cref="Camera3D.ProjectRayOrigin"/> and <see cref="Camera3D.ProjectRayNormal"/>
    /// expect for the camera that renders into it. Undoes the hidden margin, the sub-pixel shift
    /// and the upscale. Returns false when the point is outside the visible game image.
    /// </summary>
    public bool TryWindowToViewport(Vector2 windowPosition, out Vector2 viewportPosition)
    {
        viewportPosition = default;
        if (_frame is null || _display is null) return false;

        Vector2 inFrame = windowPosition - _frame.GlobalPosition;
        if (inFrame.X < 0 || inFrame.Y < 0 || inFrame.X >= _frame.Size.X || inFrame.Y >= _frame.Size.Y)
            return false;

        viewportPosition = (inFrame - _display.Position) / PixelScale;
        return true;
    }

    /// <summary>The other way round: SubViewport pixels (e.g. <see cref="Camera3D.UnprojectPosition"/>) to window coordinates.</summary>
    public Vector2 ViewportToWindow(Vector2 viewportPosition)
    {
        if (_frame is null || _display is null) return viewportPosition;
        return _frame.GlobalPosition + _display.Position + viewportPosition * PixelScale;
    }

    /// <summary>The mouse position in SubViewport pixels; see <see cref="TryWindowToViewport"/>.</summary>
    public bool TryGetMouseViewportPosition(out Vector2 viewportPosition) =>
        TryWindowToViewport(GetGlobalMousePosition(), out viewportPosition);

    private void UpdateLayout()
    {
        if (SubViewport is null) return;

        Vector2 available = Size;
        if (available.X < 1 || available.Y < 1) return;

        // Whole-number upscale picked from the height (round, not floor, so a maximised ~1050 px window gets ×3),
        // then as many low-res pixels as it takes to cover the window. No letterbox: the last partial pixel on the
        // right/bottom edge is clipped by the frame.
        _virtualScale = Mathf.Max(1, Mathf.RoundToInt(available.Y / BaseResolution.Y));
        if (_lowResEnabled)
        {
            PixelScale = _virtualScale;
            RenderResolution = new Vector2I(
                Mathf.CeilToInt(available.X / PixelScale),
                Mathf.CeilToInt(available.Y / PixelScale));
        }
        else
        {
            PixelScale = 1;
            RenderResolution = new Vector2I(Mathf.FloorToInt(available.X), Mathf.FloorToInt(available.Y));
        }

        var margin = new Vector2I(Margin, Margin);
        SubViewport.Size = RenderResolution + margin * 2;

        _frame.Size = available;
        _frame.Position = Vector2.Zero;

        _display.Size = (Vector2)SubViewport.Size * PixelScale;
        _displayOrigin = -(Vector2)margin * PixelScale;
        ApplyDisplayPosition();
    }

    private void ApplyDisplayPosition()
    {
        if (_display is null) return;
        Vector2 shift = _subPixelSmoothing ? -_subPixelOffset * PixelScale : Vector2.Zero;
        // Round to whole *screen* pixels: we move in steps smaller than a low-res pixel,
        // but never between physical pixels.
        _display.Position = (_displayOrigin + shift).Round();
    }
}
