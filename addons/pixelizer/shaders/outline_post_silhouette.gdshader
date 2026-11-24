// Outline pass for pixels marked with stencil 2 (toon_no_creases.gdshader): same as outline_post.gdshader, but
// meant to run with crease_strength = 0, so those surfaces get their silhouette and no crease highlights.
// Put it on a second full-screen quad next to the outline_post one (e.g. a child of it, so toggling that one
// toggles both). The two never touch the same pixel.
shader_type spatial;
render_mode unshaded, fog_disabled;
stencil_mode read, compare_equal, 2;

#include "res://addons/pixelizer/shaders/outline_post.gdshaderinc"
