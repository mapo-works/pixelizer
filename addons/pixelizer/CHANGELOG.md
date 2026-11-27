# Changelog

## 1.0.0 (2026-11-27)

First public release.

- `PixelViewport`: low-res SubViewport, whole-number upscale, 1 px hidden margin, sub-pixel shift, mouse mapping.
- `PixelCameraRig`: orthographic, 16 px per metre, snapped in camera space; yaw steps, pitch, follow, cutaway,
  shake in whole pixels, planet mode, `SunTurned`.
- Toon shaders (`toon`, `toon_no_creases`, `toon_whole_outline`) on the shared `toon_common` include.
- Outline pass: silhouettes and creases, stencils 0 / 1 / 2, per-pixel roughness marks.
- `pixel_stage.tscn`, `PixelBob`, `DebugOverlay`, `EditorTheme`.
- Builds in projects that leave C# nullable annotations off.
