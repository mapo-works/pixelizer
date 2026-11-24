using Godot;

namespace Pixelizer.Demo;

/// <summary>
/// The look on a few code-built shapes, to check a change to the addon before it goes out to the projects: textured
/// ground at 16 texels per metre, crates, a pillar, a ball, a faceted rock without creases, a cube that bobs in whole
/// pixels, and a walker going round that the camera follows (F1 / F2 show what snapping and sub-pixel smoothing do).
/// <c>-- --shot=&lt;file.png&gt;</c> saves the window after a moment and quits.
/// </summary>
public partial class Demo : Node
{
    private const string Shaders = "res://addons/pixelizer/shaders/";

    private Node3D _walker = null!;
    private float _clock;
    private string _shot = "";
    private int _frames;

    public override void _Ready()
    {
        foreach (string arg in OS.GetCmdlineUserArgs())
            if (arg.StartsWith("--shot=")) _shot = arg["--shot=".Length..];

        var view = GetNode<PixelViewport>("PixelStage");
        var stage = view.GetNode<Node3D>("SubViewport/Stage");
        var rig = view.GetNode<PixelCameraRig>("SubViewport/CameraRig");

        var toon = GD.Load<Shader>(Shaders + "toon.gdshader");
        var noCreases = GD.Load<Shader>(Shaders + "toon_no_creases.gdshader");

        Add(stage, new BoxMesh { Size = new Vector3(24f, 1f, 24f) }, new Vector3(0f, -0.5f, 0f),
            Material(toon, new Color(0.55f, 0.72f, 0.38f), Speckle(new Color(1f, 1f, 1f), new Color(0.86f, 0.9f, 0.8f), 7)));
        Material crate = Material(toon, new Color(0.78f, 0.56f, 0.34f), Speckle(new Color(1f, 1f, 1f), new Color(0.8f, 0.74f, 0.66f), 3));
        Add(stage, new BoxMesh { Size = Vector3.One }, new Vector3(-2f, 0.5f, -1f), crate);
        Add(stage, new BoxMesh { Size = Vector3.One }, new Vector3(-1f, 0.5f, -1.5f), crate);
        Add(stage, new BoxMesh { Size = Vector3.One }, new Vector3(-1.5f, 1.5f, -1.25f), crate);
        Add(stage, new CylinderMesh { TopRadius = 0.5f, BottomRadius = 0.6f, Height = 4f, RadialSegments = 12 },
            new Vector3(3f, 2f, -3f), Material(toon, new Color(0.74f, 0.72f, 0.7f)));
        Add(stage, new SphereMesh { Radius = 0.75f, Height = 1.5f }, new Vector3(2f, 0.75f, 2f),
            Material(toon, new Color(0.85f, 0.32f, 0.3f), worldTexels: false));
        Add(stage, new SphereMesh { Radius = 1f, Height = 1.6f, RadialSegments = 7, Rings = 4 }, new Vector3(-3f, 0.6f, 3f),
            Material(noCreases, new Color(0.5f, 0.5f, 0.56f), worldTexels: false));

        MeshInstance3D bobbing = Add(stage, new BoxMesh { Size = new Vector3(0.75f, 0.75f, 0.75f) }, new Vector3(0f, 2f, 0f),
            Material(toon, new Color(0.45f, 0.75f, 0.95f), worldTexels: false));
        bobbing.AddChild(new PixelBob { Pixels = 3f, Seconds = 3f, BaseY = 2f });

        _walker = new Node3D { Name = "Walker" };
        stage.AddChild(_walker);
        Add(_walker, new CapsuleMesh { Radius = 0.3f, Height = 1.6f }, new Vector3(0f, 0.8f, 0f),
            Material(toon, new Color(0.95f, 0.85f, 0.55f), worldTexels: false));
        rig.Target = _walker;
        rig.FollowTarget = true;

        var help = new CanvasLayer { Layer = 40 };
        AddChild(help);
        var label = new Label
        {
            Text = "Q / E turn   F1 snapping   F2 sub-pixel   F3 outlines   F4 low-res   F5 stats",
            Position = new Vector2(12f, 8f),
        };
        label.Theme = EditorTheme.Build(EditorTheme.ScaleFor(GetViewport().GetVisibleRect().Size));
        help.AddChild(label);
    }

    public override void _Process(double delta)
    {
        _clock += (float)delta;
        float a = _clock * 0.25f;
        _walker.Position = new Vector3(Mathf.Cos(a) * 5f, 0f, Mathf.Sin(a) * 5f);

        if (_shot == "" || ++_frames != 30) return;
        Error saved = GetViewport().GetTexture().GetImage().SavePng(_shot);
        GD.Print(saved == Error.Ok ? $"saved {_shot}" : $"could not save {_shot}: {saved}");
        GetTree().Quit(saved == Error.Ok ? 0 : 1);
    }

    private static MeshInstance3D Add(Node3D parent, Mesh mesh, Vector3 position, Material material)
    {
        var instance = new MeshInstance3D { Mesh = mesh, Position = position, MaterialOverride = material };
        parent.AddChild(instance);
        return instance;
    }

    private static ShaderMaterial Material(Shader shader, Color colour, Texture2D? texture = null, bool worldTexels = true)
    {
        var material = new ShaderMaterial { Shader = shader };
        material.SetShaderParameter("albedo", colour);
        if (texture is not null) material.SetShaderParameter("albedo_texture", texture);
        material.SetShaderParameter("world_pixels_per_unit", worldTexels ? 16f : 0f);
        return material;
    }

    /// <summary>A 16 × 16 tile (one metre at 16 texels per metre) of two shades, seeded so every run looks the same.</summary>
    private static ImageTexture Speckle(Color light, Color dark, ulong seed)
    {
        var rng = new RandomNumberGenerator { Seed = seed };
        Image image = Image.CreateEmpty(16, 16, false, Image.Format.Rgb8);
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
                image.SetPixel(x, y, rng.Randf() < 0.22f ? dark : light);
        return ImageTexture.CreateFromImage(image);
    }
}
