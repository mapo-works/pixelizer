// Shared by every toon shader (toon.gdshader and its variants here, and a project's own toon shaders, such as Landlord's
// terrain), so they light and texture identically.
// Include it right after `shader_type spatial;`. It declares the band uniforms and the light() function.

// Number of light bands. 2-4 reads well at low resolution.
uniform int bands : hint_range(1, 8) = 3;
// Pushes band boundaries; > 0 makes more of the surface fall into the lit band.
uniform float band_bias : hint_range(-0.5, 0.5) = 0.1;

// World-aligned texture coordinates: texels sit on a fixed world grid (pixels_per_unit), so every surface has
// the same pixel density regardless of mesh UVs. Picks the projection plane from the dominant normal axis.
vec2 toon_world_uv(vec3 world_pos, vec3 world_normal, float pixels_per_unit, vec2 tex_size) {
	vec3 n = abs(world_normal);
	vec2 p;
	if (n.y >= n.x && n.y >= n.z) {
		p = world_pos.xz;
	} else if (n.x >= n.z) {
		p = vec2(world_pos.z, -world_pos.y);
	} else {
		p = vec2(world_pos.x, -world_pos.y);
	}
	return p * pixels_per_unit / tex_size;
}

void light() {
	// ATTENUATION includes shadows, so shadow edges get banded too.
	float lambert = clamp(dot(NORMAL, LIGHT), 0.0, 1.0) * ATTENUATION;
	float b = float(bands);
	float stepped = clamp(floor((lambert + band_bias) * b) / (b - 1.0 + 0.0001), 0.0, 1.0);
	if (bands == 1) {
		stepped = lambert + band_bias > 0.5 ? 1.0 : 0.0;
	}
	// LIGHT_COLOR is pre-multiplied by PI in Godot 4, divide to match a standard Lambert.
	DIFFUSE_LIGHT += stepped * LIGHT_COLOR / PI;
}
