// Full-screen post-process for pixel-art outlines. Put it on a MeshInstance3D with a
// 2x2 QuadMesh (Flip Faces on) that is a child of the camera *inside the low-res SubViewport*,
// so every line is exactly one low-res pixel wide.
//
//  - Silhouettes (depth edges): darken the pixel on the NEAR side of a depth jump.
//  - Creases (normal edges): lighten convex edges where depth is continuous.
// Requires the Forward+ renderer (normal-roughness buffer).
//
// Stencil marks (written by the surface shaders):
//   0 = normal: silhouettes and creases (this shader). toon_whole_outline.gdshader is one of these, and its roughness
//       mark keeps dark lines off its own smaller edges.
//   1 = no outlines at all (Landlord's grass and cave caps): a dark line around every blade is noise.
//   2 = silhouettes only, no creases (toon_no_creases.gdshader): handled by outline_post_silhouette.gdshader
//       on a second quad, as this shader only runs where the stencil is 0.
// Finer, per pixel: the roughness marks of outline_marks.gdshaderinc (one piece; a Landlord ground texture without creases
// or silhouettes).
shader_type spatial;
render_mode unshaded, fog_disabled;
stencil_mode read, compare_equal, 0;

#include "res://addons/pixelizer/shaders/outline_post.gdshaderinc"
