shader_type spatial;
varying vec3 world_position;
float hash(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }
float noise(vec2 p) {
    vec2 i = floor(p), f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    return mix(mix(hash(i), hash(i + vec2(1, 0)), f.x), mix(hash(i + vec2(0, 1)), hash(i + vec2(1, 1)), f.x), f.y);
}
void vertex() { world_position = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz; }
void fragment() {
    vec2 p = world_position.xz;
    float patches = noise(p * .28) * .65 + noise(p * 1.8) * .35;
    vec3 dirt = mix(vec3(.40, .33, .23), vec3(.64, .55, .40), noise(p * 24.0));
    vec3 grass = mix(vec3(.39, .41, .24), vec3(.53, .51, .32), noise(p * 14.0));
    ALBEDO = mix(dirt, grass, smoothstep(.37, .66, patches));
    ROUGHNESS = .96;
}
