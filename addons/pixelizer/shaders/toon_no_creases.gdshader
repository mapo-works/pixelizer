// toon.gdshader that marks its pixels with stencil 2: the outline pass draws their silhouette but no crease
// highlights (see outline_post.gdshader). For models whose faceting reads as noise when every fold is lit up.
shader_type spatial;
render_mode cull_back;
stencil_mode write, compare_always, 2;

#include "res://addons/pixelizer/shaders/toon_surface.gdshaderinc"
