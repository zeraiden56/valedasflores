shader_type canvas_item;
render_mode unshaded;
uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_nearest;
uniform float pixel_size = 2.0;
uniform float color_steps = 64.0;
void fragment() {
    vec2 cell = SCREEN_PIXEL_SIZE * pixel_size;
    vec2 uv = (floor(SCREEN_UV / cell) + .5) * cell;
    vec3 color = textureLod(screen_texture, uv, 0.0).rgb;
    vec2 p = floor(SCREEN_UV / cell);
    float dither = (mod(p.x + p.y * 2.0, 4.0) - 1.5) / (color_steps * 4.0);
    color = floor(clamp(color + dither, 0.0, 1.0) * color_steps + .5) / color_steps;
    COLOR = vec4(color, 1.0);
}

