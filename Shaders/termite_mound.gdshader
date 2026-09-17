shader_type spatial;
varying vec3 local_position;
float grain(vec3 p) { return fract(sin(dot(p, vec3(12.9898, 78.233, 43.12))) * 43758.5453); }
void vertex() {
    VERTEX += NORMAL * (sin(VERTEX.x * 16.0 + VERTEX.y * 11.0) * cos(VERTEX.z * 17.0) * .06);
    local_position = VERTEX;
}
void fragment() {
    float n = grain(floor(local_position * 100.0));
    float cracks = smoothstep(.90, .98, sin(local_position.x * 25.0 + sin(local_position.y * 12.0)) * cos(local_position.z * 19.0));
    ALBEDO = mix(vec3(.51, .34, .18), vec3(.71, .53, .32), n) * (1.0 - cracks * .35);
    ROUGHNESS = 1.0;
}
