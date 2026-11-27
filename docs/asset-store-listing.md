# Asset Store listing

What to type into the Godot Asset Store (store.godotengine.org) for Pixelizer. The store asks for proper English in
full sentences, a description that is more than a link, tags that aren't the asset's name, and no singular + plural
pairs of the same tag.

## Upload form

- **Publisher**: mapo works
- **Asset name**: Pixelizer
- **Asset URL slug**: `pixelizer`

## Settings tab

- **Asset type**: Addon
- **License**: MIT
- **Link to source code**: <https://github.com/mapo-works/pixelizer>
- **Tags**: `3d`, `pixel-art`, `shader`, `post-processing`, `camera`, `toon`, `outline`, `csharp`
- **AI usage disclosure**: The code was written by mapo works together with Claude (Anthropic's AI model) used as a
  pair programmer. Art direction, testing and the decisions are ours. The demo uses no generated art: every shape
  and texture is built in code.

**Summary**

> Turns real 3D scenes into crisp pixel art: low-res whole-number upscale, a pixel-snapped camera, toon light and
> one-pixel outlines.

**Description**

> Pixelizer renders your 3D world into a small viewport (about 640×360) and shows it upscaled by a whole number with
> nearest filtering, so every pixel stays a hard square. The orthographic camera rig snaps to the pixel grid in
> camera space and hands its leftover sub-pixel offset to the upscaled image, so the view can glide smoothly while
> edges never shimmer.
>
> Surfaces are lit by toon shaders in a few flat bands with tinted shadows, and textured on a world-aligned grid of
> 16 texels per metre, so every object has the same pixel density. A full-screen outline pass draws dark,
> slope-aware silhouettes and light crease lines one low-res pixel wide; stencils and per-pixel roughness marks let
> each surface choose which lines it gets.
>
> Instance `pixel_stage.tscn`, put your world under its SubViewport, give your meshes the toon shader, and the look
> is done. The camera rig also does 90° turns, pitch, follow, cutaway, shake in whole pixels, and a planet mode
> where "up" points away from a centre. `DebugOverlay` toggles each stage with F1–F5 so you can see what it adds.
>
> Requires Godot 4.7 .NET (C#) and the Forward+ renderer. After installing, build the C# solution; there is no
> plugin to enable. The source repository has a demo scene and a script that sets the two project settings the look
> needs (hard shadows, nearest-filtered decals).
>
> Inspired by t3ssel8r's 3D pixel art; full credits are in `addons/pixelizer/CREDITS.md`.

## Media tab

- **Thumbnail** (16:9): `docs/screenshots/thumbnail.png` (960×540)
- **Screenshots**: `docs/screenshots/demo.png` (1280×720, the demo as it starts)

## Versions tab

- **Version name**: 1.0.0
- **File**: `dist/pixelizer-1.0.0.zip` from `python3 tools/package.py`
- **Minimum Godot version**: 4.7
- **Changelog**: First public release. (The full list is in `addons/pixelizer/CHANGELOG.md`.)
- **Additional info**: Needs the .NET (C#) build of Godot and the Forward+ renderer.
