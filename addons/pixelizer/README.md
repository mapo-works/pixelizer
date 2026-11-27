# Pixelizer

A 3D pixel-art look as one Godot addon: a real 3D scene rendered small and upscaled with hard pixels, toon-lit in
flat bands, outlined one low-res pixel wide, through a camera that moves in whole pixels so nothing shimmers. Give
every project the same copy and they all look alike.

Godot **4.7 .NET** (C#), **Forward+** (the outline pass reads the normal-roughness buffer). MIT license
(`LICENSE`), © 2026 mapo works; who helped: `CREDITS.md`; versions: `CHANGELOG.md`.
Source, demo and issues: <https://github.com/mapo-works/pixelizer>.

## What's in it

| File | Does |
|---|---|
| `pixel_stage.tscn` | Drop-in view: everything below wired together. Put your 3D world under `SubViewport/Stage` |
| `PixelViewport.cs` | Renders the SubViewport at window ÷ N (whole N from the height: 720p ×2, 1080p ×3) plus a 1 px hidden margin, shows it upscaled with nearest filtering, shifts it by the camera's sub-pixel leftover. Maps mouse ↔ SubViewport pixels |
| `PixelCameraRig.cs` | Orthographic rig, 16 px per metre, snapped to the pixel grid in camera space; yaw steps (Q/E), pitch, follow, cutaway, shake in whole pixels, planet mode (`GravityCenter`), turns the sun with it (`SunTurned` event) |
| `shaders/toon_common.gdshaderinc` | The banded `light()` and world-aligned texel projection; include it in any toon shader so all of them light alike |
| `shaders/toon.gdshader` | Toon surface (colour, texture, world texels at `world_pixels_per_unit` = 16, vertex colours) |
| `shaders/toon_no_creases.gdshader` | Same, stencil 2: silhouette but no crease highlights (faceted models) |
| `shaders/toon_whole_outline.gdshader` | Same, outlined as one piece (stairs-like shapes) |
| `shaders/outline_post.gdshader(inc)`, `outline_post_silhouette.gdshader` | Full-screen outline pass: dark slope-aware silhouettes, light convex creases |
| `shaders/outline_marks.gdshaderinc` | Per-pixel marks a surface leaves for the outline pass in its roughness |
| `PixelBob.cs` | Bobs its parent up and down in whole low-res pixels |
| `DebugOverlay.cs` | F1 snapping, F2 sub-pixel, F3 outlines, F4 low-res, F5 stats; Q / E turn the camera. Adds those keys itself if the project has no such input actions |
| `EditorTheme.cs` | Code-built UI theme for tools: flat dark panels, 1 px rims, sizes in art pixels so they scale with the 3D view |

## Use it

1. Put the addon in `addons/pixelizer/` (Asset Store, by hand, or `python3 tools/install.py <project>` from the
   source repository, which also sets the project settings below) and build the C# solution. Nothing to enable in
   Project Settings → Plugins: these are plain C# nodes and shaders.
2. Instance `res://addons/pixelizer/pixel_stage.tscn` as the main view (full rect), or build the same tree yourself:
   `PixelViewport (Control) → SubViewport → CameraRig (PixelCameraRig) → Camera3D → OutlinePass → SilhouettePass`.
3. Give meshes a `ShaderMaterial` with `toon.gdshader` (or one of its variants). Author textures at 16 px per metre.
4. Input does not reach nodes inside the SubViewport (it's shown through a `TextureRect`): poll `Input`, or read
   events outside and turn the mouse into a ray with `PixelCameraRig.TryGetMouseRay`.

Project settings the look needs (`install.py` adds them when missing):

```ini
[rendering]
lights_and_shadows/directional_shadow/soft_shadow_filter_quality=0   ; hard shadows
textures/decals/filter=0                                             ; nearest-filtered decals
```

`PixelViewport` switches off MSAA, screen-space AA, TAA and 3D scaling on its SubViewport by itself. Leave
`display/window/stretch/mode` disabled: integer viewport stretch would lock the UI to 640×360 and rule out sub-pixel
scrolling.

## Outline stencils and marks

| Stencil | Surface | Outline pass |
|---|---|---|
| 0 | `toon.gdshader`, `toon_whole_outline.gdshader`, anything else | silhouettes and creases |
| 1 | a surface that wants no lines at all (Landlord's grass, cave caps) | none |
| 2 | `toon_no_creases.gdshader` | silhouettes only (`outline_post_silhouette.gdshader`) |

Finer, per pixel: `outline_mark(bits)` written as `ROUGHNESS` (`OUTLINE_WHOLE`, `OUTLINE_NO_CREASES`,
`OUTLINE_NO_OUTLINES`). An opaque material that sets its own roughness to 0.6–0.9 reads as marked, so keep the
default 1.0 on code-built materials the outline pass sees.

## Rules that keep it pixel art

- **16 px per metre** (`PixelCameraRig.PixelsPerUnit`). Zoom only in whole steps.
- **Move in whole low-res pixels** anything that should stay crisp (`PixelBob`, the camera's shake); smooth
  sub-pixel motion re-rasterises edges and they shimmer.
- **No smoothing**: nearest filtering, no AA, no soft shadows, no 3D scaling.
- **Flat light**: few bands (`bands` = 3), ambient fills the lowest, so shadows are tinted, not black.

## Where it lives

The master copy is the `pixelizer` repository, with a demo scene. Projects carry copies and never edit them: change
the look there, check it in the demo, then refresh each project with `tools/install.py`.
