shader_type spatial;

group_uniforms SetupTheseValues;
uniform sampler2D viewport_texture; // Set it as: "New ViewportTexture" and select your SubViewport Node in the scene tree
uniform sampler2D floor_texture:hint_default_white;
uniform sampler2D floor_heightmap:hint_default_white;
uniform vec3 trail_color:source_color = vec3(0.0);
uniform vec3 ground_modulate:source_color = vec3(1.0); // Same as it usually works with modulate property in Sprites and such
uniform float ditch_height:hint_range(0.0, 1.0) = 0.2;
group_uniforms;


void fragment() {
	float trail = texture(viewport_texture, UV).r;
	vec3 ground = texture(floor_texture, UV).rgb;

	// This changes all blacks and grays into a darkened trail_color-scale but leaves whites as whites
	vec3 trail_recolored = 1.0 - (1.0-trail) * (1.0-trail_color);

	// Multiplying the base colors with the darker colors of the trail will darken the resulting ditch smoothly
	ALBEDO = ground*ground_modulate * trail_recolored;
}

void vertex() {
	// If heightmap is gray in average, multiplying it by a black trail will create ditches in the ground
	// So it might not look right with mostly-black heightmaps
	float trail = texture(viewport_texture, UV).r;
	float minY = VERTEX.y + 0.25;
	VERTEX.y = texture(floor_heightmap, UV).r * ditch_height * trail;
	if (VERTEX.y < minY) {VERTEX.y = minY;}
	if (UV.x <= 0.0 || UV.x >= 1.0 || UV.y <= 0.0 || UV.y >= 1.0) {VERTEX.y = minY;}

	// Honestly I don't know about normals nearly enough to know if I'm doing it right...
	NORMAL.y = VERTEX.y;
}