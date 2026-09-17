shader_type spatial;
varying vec3 world_position;
void vertex() { world_position = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz; }
void fragment() {
    float furrow = sin(world_position.x * 24.0) * .5 + .5;
    float grain = fract(sin(dot(floor(world_position.xz * 65.0), vec2(12.98, 78.23))) * 43758.54);
    ALBEDO = mix(vec3(.20, .13, .075), vec3(.36, .25, .14), furrow) * (.8 + grain * .3);
    ROUGHNESS = 1.0;
}
