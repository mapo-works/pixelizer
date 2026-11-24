# Pixelizer

The studio's 3D pixel-art look as one reusable Godot addon, `addons/pixelizer/`: a real 3D scene rendered small and
upscaled with hard pixels (640×360-ish, whole-number scale), a camera that moves in whole pixels so nothing
shimmers, toon light in flat bands and one-pixel outlines. 16 px per metre.

This repo is the **master copy**. Every studio project carries a copy and never edits it:

| Project | Uses it for |
|---|---|
| [Landlord](../landlord) | the land / map builder and the map runtime games use |
| [Dudemaker](../dudemaker) | the character maker (pose, key, later model) |
| games | made from Landlord's game template, which brings the addon along |

Godot **4.7.2 .NET** (C#), Forward+.

## Run the demo

```sh
dotnet build
/path/to/Godot_v4.7.2-stable_mono_linux.x86_64 --path .
```

`demo/demo.tscn` puts a few code-built shapes through the pipeline: textured ground, crates, a pillar, a ball, a
faceted rock without crease lines, a cube bobbing in whole pixels, and a walker the camera follows. Q / E turn the
camera; F1–F4 switch snapping, sub-pixel smoothing, outlines and low-res off and on; F5 shows the frame stats.
`-- --shot=<file.png>` saves the window after half a second and quits.

## Change the look

1. Edit `addons/pixelizer/` here and check it in the demo.
2. Refresh the projects:

   ```sh
   python3 tools/install.py ../landlord ../dudemaker
   ```

   It replaces each project's `addons/pixelizer` and adds the project settings the look needs when the project
   doesn't set them (hard shadows, nearest decals).
3. Build and look at each project. Landlord's game template carries the addon on to games
   (`tools/new_game.py --update <game>` there).

What's in the addon and how to use it in a project: [addons/pixelizer/README.md](addons/pixelizer/README.md).
