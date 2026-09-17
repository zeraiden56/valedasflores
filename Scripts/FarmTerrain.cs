using Godot;
using System;

namespace ValeDasFlores;

public partial class FarmTerrain : StaticBody3D
{
    public const int Count = 65;
    public const float Step = 3.5f, Half = (Count - 1) * Step / 2;
    public float[] Heights { get; private set; } = CreateInitialHeights();
    public static float[] CreateInitialHeights()
    {
        // Artistic blockout from ground photos, not surveyed elevation data.
        var heights = new float[Count * Count];
        for (int z = 0; z < Count; z++) for (int x = 0; x < Count; x++)
        {
            float wx = x * Step - Half, wz = z * Step - Half;
            float lakeRadius = new Vector2((wx - FarmWorld.LakeCenter.X) / 21, (wz - FarmWorld.LakeCenter.Z) / 13).Length();
            if (lakeRadius < 1)
            {
                heights[z * Count + x] = -.65f * Mathf.SmoothStep(0, 1, (1 - lakeRadius) * 2);
                continue;
            }
            if (!Editable(wx, wz)) continue;
            float west = Mathf.SmoothStep(0, 1, (-wx - 55) / 27);
            float south = Mathf.SmoothStep(0, 1, (wz - 38) / 35);
            float east = Mathf.SmoothStep(0, 1, (wx - 68) / 30);
            float edge = Mathf.SmoothStep(0, 1, (94 - Mathf.Max(Mathf.Abs(wx), Mathf.Abs(wz))) / 12);
            float paths = Mathf.SmoothStep(0, 1, (Mathf.Min(Mathf.Abs(wz - 12), Mathf.Abs(wz - 30)) - 6) / 9);
            float rolling = 6 + 2.2f * Mathf.Sin(wx * .055f + wz * .033f) + 1.4f * Mathf.Cos(wz * .091f - wx * .017f);
            heights[z * Count + x] = Mathf.Max(west, Mathf.Max(south, east)) * edge * paths * rolling;
        }
        return heights;
    }
    private MeshInstance3D _visual = null!;
    private CollisionShape3D _collision = null!;
    public override void _Ready()
    {
        Name = "Terrain";
        _visual = new MeshInstance3D { MaterialOverride = new ShaderMaterial { Shader = GD.Load<Shader>("res://Shaders/dry_ground.gdshader") } };
        AddChild(_visual);
        _collision = new CollisionShape3D(); AddChild(_collision);
        Rebuild();
    }
    public static bool Editable(float x, float z) => Math.Abs(x) < 94 && Math.Abs(z) < 94
        && !(x > -19 && x < 65 && z > -65 && z < 28) // Buildings, pond and service area.
        && Math.Abs(x + 47) > 6 && Math.Abs(z - 12) > 5 && Math.Abs(z - 30) > 5;
    public float HeightAt(float x, float z)
    {
        float gx = Mathf.Clamp((x + Half) / Step, 0, Count - 1.001f), gz = Mathf.Clamp((z + Half) / Step, 0, Count - 1.001f);
        int ix = (int)gx, iz = (int)gz;
        float a = Heights[iz * Count + ix], b = Heights[iz * Count + ix + 1], c = Heights[(iz + 1) * Count + ix], d = Heights[(iz + 1) * Count + ix + 1];
        float u = gx - ix, v = gz - iz;
        return u + v <= 1 ? a + (b - a) * u + (c - a) * v : d + (c - d) * (1 - u) + (b - d) * (1 - v);
    }
    public void SetHeights(float[] values)
    {
        if (values == null || values.Length != Heights.Length || Array.Exists(values, h => !float.IsFinite(h) || h < -8 || h > 12)) throw new ArgumentException("Dimensões de terreno inválidas.");
        Heights = (float[])values.Clone(); Rebuild();
    }
    public bool Brush(Vector3 center, float radius, int mode, float strength = .45f)
    {
        if (!float.IsFinite(center.X) || !float.IsFinite(center.Z) || !float.IsFinite(radius) || radius <= 0
            || !float.IsFinite(strength) || strength < 0 || mode < 1 || mode > 4) return false;
        var source = (float[])Heights.Clone(); bool changed = false;
        float level = HeightAt(center.X, center.Z);
        for (int z = 1; z < Count - 1; z++) for (int x = 1; x < Count - 1; x++)
        {
            float wx = x * Step - Half, wz = z * Step - Half;
            float distance = new Vector2(wx - center.X, wz - center.Z).Length();
            if (distance >= radius || !Editable(wx, wz)) continue;
            int i = z * Count + x; float weight = 1 - distance / radius;
            float value = mode switch {
                1 => source[i] + strength * weight,
                2 => source[i] - strength * weight,
                3 => Mathf.Lerp(source[i], (source[i - 1] + source[i + 1] + source[i - Count] + source[i + Count]) / 4, weight * .5f),
                _ => Mathf.Lerp(source[i], level, weight * .6f)
            };
            Heights[i] = Mathf.Clamp(value, -8, 12); changed |= !Mathf.IsEqualApprox(source[i], Heights[i]);
        }
        if (changed) Rebuild();
        return changed;
    }
    private void Rebuild()
    {
        var surface = new SurfaceTool(); surface.Begin(Mesh.PrimitiveType.Triangles);
        var faces = new Vector3[(Count - 1) * (Count - 1) * 6]; int index = 0;
        Vector3 Point(int x, int z) => new(x * Step - Half, Heights[z * Count + x], z * Step - Half);
        void Triangle(Vector3 a, Vector3 b, Vector3 c)
        {
            foreach (var p in new[] { a, b, c }) { surface.SetUV(new(p.X / 10, p.Z / 10)); surface.AddVertex(p); faces[index++] = p; }
        }
        for (int z = 0; z < Count - 1; z++) for (int x = 0; x < Count - 1; x++)
        {
            var a = Point(x, z); var b = Point(x + 1, z); var c = Point(x, z + 1); var d = Point(x + 1, z + 1);
            Triangle(a, b, c); Triangle(b, d, c);
        }
        surface.GenerateNormals(); _visual.Mesh = surface.Commit(); surface.Dispose();
        // Use the exact triangles of the rendered terrain for collision and sampling.
        _collision.Shape = new ConcavePolygonShape3D { Data = faces, BackfaceCollision = true };
    }
}
