#nullable enable // the addon builds the same in projects that leave <Nullable> off

using Godot;

namespace Pixelizer;

/// <summary>
/// Native-resolution UI layer with toggles for each pipeline stage, so you can see what each
/// one contributes. Lives outside the low-res SubViewport, so its text stays crisp.
/// Keys come from the input actions below; a project that doesn't declare them gets the defaults
/// (F1–F5, Q / E) when this node is ready.
/// </summary>
public partial class DebugOverlay : CanvasLayer
{
    [Export] public PixelViewport? PixelViewport { get; set; }
    [Export] public PixelCameraRig? CameraRig { get; set; }
    [Export] public GeometryInstance3D? OutlinePass { get; set; }

    /// <summary>Extra lines for scene-specific controls, shown under the standard ones.</summary>
    [Export(PropertyHint.MultilineText)] public string ExtraHelp { get; set; } = "";

    private Label _label = null!;

    /// <summary>Input actions this overlay reads, with the key each gets when the project has no such action.</summary>
    public static readonly (string Action, Key Key)[] Actions =
    {
        ("debug_toggle_snapping", Key.F1),
        ("debug_toggle_subpixel", Key.F2),
        ("debug_toggle_outlines", Key.F3),
        ("debug_toggle_lowres", Key.F4),
        ("debug_toggle_overlay", Key.F5),
        ("camera_rotate_left", Key.Q),
        ("camera_rotate_right", Key.E),
    };

    /// <summary>Adds the <see cref="Actions"/> the project's input map lacks.</summary>
    public static void EnsureActions()
    {
        foreach ((string action, Key key) in Actions)
        {
            if (InputMap.HasAction(action)) continue;
            InputMap.AddAction(action);
            InputMap.ActionAddEvent(action, new InputEventKey { PhysicalKeycode = key });
        }
    }

    public override void _Ready()
    {
        EnsureActions();
        // Top right, clear of the editor's side panel. Hidden until F5.
        _label = new Label { Visible = false, HorizontalAlignment = HorizontalAlignment.Right };
        _label.SetAnchorsPreset(Control.LayoutPreset.TopRight);
        _label.GrowHorizontal = Control.GrowDirection.Begin;
        _label.OffsetLeft = _label.OffsetRight = -12;
        _label.OffsetTop = 56;
        _label.AddThemeColorOverride("font_color", Colors.White);
        _label.AddThemeColorOverride("font_outline_color", Colors.Black);
        _label.AddThemeConstantOverride("outline_size", 6);
        AddChild(_label);
    }

    private bool _measuring;

    private double _worst, _worstShown, _worstClock;

    /// <summary>Frame cost of the 3D view: GPU milliseconds, draw calls, objects and triangles.</summary>
    private string FrameStats()
    {
        Rid world = PixelViewport!.SubViewport.GetViewportRid();
        if (!_measuring)
        {
            RenderingServer.ViewportSetMeasureRenderTime(world, true);
            _measuring = true;
        }
        double gpu = RenderingServer.ViewportGetMeasuredRenderTimeGpu(world);
        double draws = Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame);
        double tris = Performance.GetMonitor(Performance.Monitor.RenderTotalPrimitivesInFrame);
        double objects = Performance.GetMonitor(Performance.Monitor.RenderTotalObjectsInFrame);
        return $"gpu {gpu:0.0} ms (3D view)\n" +
               $"{draws:0} draws  {objects:0} objects  {tris / 1000.0:0}k tris\n";
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (PixelViewport is null || CameraRig is null) return;

        if (e.IsActionPressed("debug_toggle_snapping"))
            CameraRig.Snapping = !CameraRig.Snapping;
        else if (e.IsActionPressed("debug_toggle_subpixel"))
            PixelViewport.SubPixelSmoothing = !PixelViewport.SubPixelSmoothing;
        else if (e.IsActionPressed("debug_toggle_outlines") && OutlinePass is not null)
            OutlinePass.Visible = !OutlinePass.Visible;
        else if (e.IsActionPressed("debug_toggle_lowres"))
            PixelViewport.LowResEnabled = !PixelViewport.LowResEnabled;
        else if (e.IsActionPressed("debug_toggle_overlay"))
            _label.Visible = !_label.Visible;
        else if (e.IsActionPressed("camera_rotate_left"))
            CameraRig.RotateSteps(-1);
        else if (e.IsActionPressed("camera_rotate_right"))
            CameraRig.RotateSteps(1);
        else return;

        GetViewport().SetInputAsHandled();
    }

    public override void _Process(double delta)
    {
        if (!_label.Visible || PixelViewport is null || CameraRig is null) return;

        // Longest frame of the last second: hitches hide in the fps average.
        _worst = System.Math.Max(_worst, delta);
        _worstClock += delta;
        if (_worstClock >= 1.0)
        {
            _worstShown = _worst;
            _worst = 0;
            _worstClock = 0;
        }

        static string OnOff(bool v) => v ? "ON " : "off";
        Vector2I r = PixelViewport.RenderResolution;
        _label.Text =
            $"{Engine.GetFramesPerSecond()} fps  worst {_worstShown * 1000:0} ms   render {r.X}x{r.Y}  x{PixelViewport.PixelScale}\n" +
            FrameStats() +
            $"Q / E: rotate camera\n" +
            $"F1 snapping   {OnOff(CameraRig.Snapping)}\n" +
            $"F2 sub-pixel  {OnOff(PixelViewport.SubPixelSmoothing)}\n" +
            $"F3 outlines   {OnOff(OutlinePass?.Visible ?? false)}\n" +
            $"F4 low-res    {OnOff(PixelViewport.LowResEnabled)}\n" +
            $"F5 show / hide this text" +
            (ExtraHelp.Length > 0 ? "\n" + ExtraHelp : "");
    }
}
