// Body of the outline post-process, shared by outline_post.gdshader and outline_post_silhouette.gdshader.
// Those two differ only in which stencil-marked pixels they run on. Include it after the render_mode and
// stencil_mode lines. Within those, the marks the surface shaders write as roughness (outline_marks.gdshaderinc) keep
// lines off single pixels: models outlined as one piece, and ground textures without creases or silhouettes.

#include "res://addons/pixelizer/shaders/outline_marks.gdshaderinc"

uniform sampler2D screen_texture : hint_screen_texture, filter_nearest;
uniform sampler2D depth_texture : hint_depth_texture, filter_nearest;
uniform sampler2D normal_texture : hint_normal_roughness_texture, filter_nearest;

uniform bool enabled = true;
// Depth difference (world units) that counts as a silhouette.
uniform float depth_threshold = 0.25;
// Between two pixels of a model outlined as one piece (toon_whole_outline.gdshader), only a jump this big is a line.
uniform float whole_depth_threshold = 1.2;
// 1 - dot(n, neighbour_n) above this counts as a crease (0.1 ≈ 25°).
uniform float normal_threshold = 0.1;
// Ignore surfaces turned further away than this (view-space normal z). Stops false creases
// on curved silhouettes. 0.3 ≈ surfaces within ~72° of facing the camera.
uniform float min_facing : hint_range(0.0, 1.0) = 0.3;
uniform float outline_strength : hint_range(0.0, 1.0) = 0.6;
uniform float crease_strength : hint_range(0.0, 1.0) = 0.8;
uniform vec4 outline_color : source_color = vec4(0.05, 0.03, 0.08, 1.0);

void vertex() {
	// Cover the whole screen regardless of the camera (Godot's documented post-process trick).
	POSITION = vec4(VERTEX.xy, 1.0, 1.0);
}

float linear_depth(vec2 uv, mat4 inv_projection) {
	float raw = texture(depth_texture, uv).x;
	vec3 ndc = vec3(uv * 2.0 - 1.0, raw);
	vec4 view = inv_projection * vec4(ndc, 1.0);
	return -view.z / view.w;
}

// The outline marks of the pixel at uv (outline_marks.gdshaderinc).
int marks(vec2 uv) {
	return outline_mark_bits(texture(normal_texture, uv).a);
}

bool outlined_whole(vec2 uv) {
	return (marks(uv) & OUTLINE_WHOLE) != 0;
}

vec3 view_normal(vec2 uv) {
	return normalize(texture(normal_texture, uv).xyz * 2.0 - 1.0);
}

void fragment() {
	vec3 color = texture(screen_texture, SCREEN_UV).rgb;
	if (!enabled) {
		ALBEDO = color;
	} else {
		vec2 px = 1.0 / VIEWPORT_SIZE;
		float d = linear_depth(SCREEN_UV, INV_PROJECTION_MATRIX);
		vec3 n = view_normal(SCREEN_UV);
		int here = marks(SCREEN_UV);

		// Silhouettes: this pixel is the near side of a depth jump. A surface seen at a grazing angle
		// (terrain sloping away from the camera) changes depth a lot from one pixel to the next without
		// any edge, so "neighbour is farther than the threshold" would outline it. Instead, compare the
		// neighbours with what this pixel's own slope predicts: d_left + d_right - 2d is ~0 on any flat or
		// tilted surface and equals the jump at a real edge. It is positive only on the near side, so
		// lines stay 1 px thick.
		float d_right = linear_depth(SCREEN_UV + vec2(px.x, 0.0), INV_PROJECTION_MATRIX);
		float d_left  = linear_depth(SCREEN_UV - vec2(px.x, 0.0), INV_PROJECTION_MATRIX);
		float d_down  = linear_depth(SCREEN_UV + vec2(0.0, px.y), INV_PROJECTION_MATRIX);
		float d_up    = linear_depth(SCREEN_UV - vec2(0.0, px.y), INV_PROJECTION_MATRIX);
		float jump = max(d_left + d_right - 2.0 * d, d_up + d_down - 2.0 * d);
		float silhouette = step(depth_threshold, jump);
		// A model outlined as one piece: its own smaller edges don't count, what lies behind it does.
		if (silhouette > 0.0 && jump < whole_depth_threshold && (here & OUTLINE_WHOLE) != 0) {
			bool against_other =
				(d_right - d > depth_threshold && !outlined_whole(SCREEN_UV + vec2(px.x, 0.0))) ||
				(d_left - d > depth_threshold && !outlined_whole(SCREEN_UV - vec2(px.x, 0.0))) ||
				(d_down - d > depth_threshold && !outlined_whole(SCREEN_UV + vec2(0.0, px.y))) ||
				(d_up - d > depth_threshold && !outlined_whole(SCREEN_UV - vec2(0.0, px.y)));
			silhouette = against_other ? 1.0 : 0.0;
		}
		// A surface that wants no silhouettes (a ground texture's "outlines": false): none on its pixels. Lines that
		// something in front of it draws lie on that thing's pixels, so they stay.
		if ((here & OUTLINE_NO_OUTLINES) != 0) {
			silhouette = 0.0;
		}

		// Creases: only compare with the right and lower neighbour, so each edge is marked on
		// one side only. Convex test: crossing a convex edge, the normal turns towards the
		// direction we move in. (Screen-down is view-space -Y.)
		vec3 n_right = view_normal(SCREEN_UV + vec2(px.x, 0.0));
		vec3 n_down  = view_normal(SCREEN_UV + vec2(0.0, px.y));
		// Skip normals that are nearly edge-on to the camera (view-space z ≈ 0): on smooth,
		// round objects the normal swings fast right at the silhouette, which would otherwise
		// produce a false highlight just inside the outline.
		float facing = step(min_facing, n.z);
		float crease_r = step(normal_threshold, 1.0 - dot(n, n_right))
			* step(abs(d_right - d), depth_threshold)
			* step(0.0, n_right.x - n.x)
			* step(min_facing, n_right.z);
		float crease_d = step(normal_threshold, 1.0 - dot(n, n_down))
			* step(abs(d_down - d), depth_threshold)
			* step(0.0, -(n_down.y - n.y))
			* step(min_facing, n_down.z);
		// A surface that wants no creases (a ground texture's "creases": false): none on its pixels, nor on the pixel
		// beside it across the edge, so its normal map's bumps don't light up its border either.
		if (((here | marks(SCREEN_UV + vec2(px.x, 0.0))) & OUTLINE_NO_CREASES) != 0) {
			crease_r = 0.0;
		}
		if (((here | marks(SCREEN_UV + vec2(0.0, px.y))) & OUTLINE_NO_CREASES) != 0) {
			crease_d = 0.0;
		}
		float crease = max(crease_r, crease_d) * facing;

		vec3 outlined = mix(color, outline_color.rgb, outline_strength);
		vec3 highlighted = color + (vec3(1.0) - color) * crease_strength;
		ALBEDO = silhouette > 0.0 ? outlined : (crease > 0.0 ? highlighted : color);
	}
}
