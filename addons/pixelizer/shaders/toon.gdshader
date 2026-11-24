// Toon (cel) shading for low-res 3D pixel art.
// - Lighting is quantised into a few flat bands instead of a smooth gradient (toon_common.gdshaderinc).
// - Optional world-aligned texture projection: texels sit on a fixed world grid
//   (pixels_per_unit), so every surface has the same pixel density regardless of mesh UVs.
shader_type spatial;
render_mode cull_back;

#include "res://addons/pixelizer/shaders/toon_surface.gdshaderinc"
