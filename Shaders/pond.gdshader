shader_type spatial;
varying vec3 world_position;
void vertex() { world_position = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz; }
void fragment() {
    float ripple = sin(world_position.x * 3.5 + TIME * 1.2) * sin(world_position.z * 4.3 + TIME * .8);
    ALBEDO = mix(vec3(.19, .29, .28), vec3(.33, .44, .43), ripple * .5 + .5);
    ROUGHNESS = .28;
    SPECULAR = .65;
    NORMAL = normalize(vec3(ripple * .05, 0.0, 1.0));
}
