// Body of the toon surface shader, shared by toon.gdshader, toon_no_creases.gdshader and toon_whole_outline.gdshader
// (which differ only in how the outline pass treats them). Include it after the shader_type, render_mode and
// stencil_mode lines.

#include "res://addons/pixelizer/shaders/toon_common.gdshaderinc"
#include "res://addons/pixelizer/shaders/outline_marks.gdshaderinc"

uniform vec4 albedo : source_color = vec4(1.0);
uniform sampler2D albedo_texture : source_color, filter_nearest, repeat_enable, hint_default_white;

// 0 = use the mesh's own UVs. > 0 = project the texture in world space at this density
// (match PixelCameraRig.PixelsPerUnit to get 1 texel = 1 screen pixel on camera-facing surfaces).
uniform float world_pixels_per_unit = 0.0;

// Multiply by the mesh's vertex colours (imported models that are coloured that way, AssetImporter). Off by default.
uniform bool use_vertex_color = false;

varying vec3 v_world_pos;
varying vec3 v_world_normal;

void vertex() {
	v_world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	v_world_normal = normalize((MODEL_MATRIX * vec4(NORMAL, 0.0)).xyz);
}

void fragment() {
	vec2 uv = world_pixels_per_unit > 0.0
		? toon_world_uv(v_world_pos, v_world_normal, world_pixels_per_unit, vec2(textureSize(albedo_texture, 0)))
		: UV;
	ALBEDO = albedo.rgb * texture(albedo_texture, uv).rgb;
	if (use_vertex_color) {
		ALBEDO *= COLOR.rgb;
	}
#ifdef TOON_WHOLE_OUTLINE
	// Tells the outline pass this model is outlined as one piece (outline_marks.gdshaderinc). Everything else keeps 1.0.
	ROUGHNESS = outline_mark(OUTLINE_WHOLE);
#endif
}
