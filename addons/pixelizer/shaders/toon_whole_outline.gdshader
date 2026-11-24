// toon.gdshader for models outlined as one piece, like stairs. It keeps crease highlights, and writes the roughness mark
// below, which tells the outline pass that a depth jump between two of these pixels is usually no outline
// (outline_post.gdshaderinc): stairs seen from behind have an edge every pixel or two, and a dark line on each reads as a
// checkerboard.
shader_type spatial;
render_mode cull_back;

#define TOON_WHOLE_OUTLINE

#include "res://addons/pixelizer/shaders/toon_surface.gdshaderinc"
