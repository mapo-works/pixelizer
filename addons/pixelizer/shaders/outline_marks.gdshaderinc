// Marks the surface shaders leave for the outline pass (outline_post.gdshaderinc) in the roughness they write: Godot
// hands the normal-roughness buffer's alpha to the pass as plain roughness. A mark is a set of bits:
//   OUTLINE_WHOLE        outlined as one piece (toon_whole_outline.gdshader, Landlord's stairs.gdshader): between two such
//                        pixels only a big depth jump is a line;
//   OUTLINE_NO_CREASES   no crease highlight on this pixel, nor on its neighbour across the edge (a ground texture
//                        whose JSON says "creases": false, Landlord's GroundTextures.cs);
//   OUTLINE_NO_OUTLINES  no dark silhouette line on this pixel ("outlines": false).
// Landlord's ground textures' variant table keeps the last two in the same bits (ground_texture_lines()).
// A mark is written as roughness 0.95 - 0.05 * bits: 0.9 for OUTLINE_WHOLE alone, down to 0.6. No mark is Godot's
// default 1.0. The toon light() ignores roughness. An opaque material that sets its own roughness to one of these
// values would read as marked, so code-built materials that the outline pass sees keep the default.
#ifndef OUTLINE_MARKS_INCLUDED
#define OUTLINE_MARKS_INCLUDED

const int OUTLINE_WHOLE = 1;
const int OUTLINE_NO_CREASES = 2;
const int OUTLINE_NO_OUTLINES = 4;

// The roughness that carries mark `bits` (0: no mark).
float outline_mark(int bits) {
	return bits == 0 ? 1.0 : 0.95 - 0.05 * float(bits);
}

// The mark a roughness carries; 0 for any roughness that isn't one (the buffer keeps it to about 1/127).
int outline_mark_bits(float roughness) {
	float k = (0.95 - roughness) / 0.05;
	float bits = round(k);
	return bits >= 1.0 && bits <= 7.0 && abs(k - bits) < 0.4 ? int(bits) : 0;
}

#endif
