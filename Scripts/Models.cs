using Godot;
using System.Collections.Generic;

namespace ValeDasFlores;

// Shared low-poly pieces for the farm's photo-inspired models.
public static class Models
{
    private static readonly Dictionary<string, StandardMaterial3D> Materials = new();
    public static StandardMaterial3D Material(string hex)
    {
        if (!Materials.TryGetValue(hex, out var material))
        {
            material = new StandardMaterial3D { AlbedoColor = new Color(hex), Roughness = .88f };
            Materials[hex] = material;
        }
        return material;
    }

    public static MeshInstance3D Box(Node3D parent, Vector3 position, Vector3 size, string color)
    {
        var mesh = new MeshInstance3D { Mesh = new BoxMesh { Size = size }, MaterialOverride = Material(color), Position = position };
        parent.AddChild(mesh);
        return mesh;
    }

    public static MeshInstance3D Cylinder(Node3D parent, Vector3 position, float radius, float height, string color, float? top = null)
    {
        var mesh = new MeshInstance3D {
            Mesh = new CylinderMesh { BottomRadius = radius, TopRadius = top ?? radius, Height = height, RadialSegments = 16 },
            MaterialOverride = Material(color), Position = position
        };
        parent.AddChild(mesh);
        return mesh;
    }

    public static void Collider(Node3D parent, Vector3 position, Vector3 size)
    {
        parent.AddChild(new CollisionShape3D { Position = position, Shape = new BoxShape3D { Size = size } });
    }

    public static MeshInstance3D Rod(Node3D parent, Vector3 from, Vector3 to, float radius, string color, float? tip = null)
    {
        var rod = Cylinder(parent, (from + to) * .5f, radius, from.DistanceTo(to), color, tip);
        rod.Quaternion = new Quaternion(Vector3.Up, (to - from).Normalized());
        return rod;
    }

    public static Mesh GrassTuft(Material material)
    {
        var surface = new SurfaceTool(); surface.Begin(Mesh.PrimitiveType.Triangles);
        // Six leaning blades share one instanced mesh, like the dry clumps in the photos.
        for (int i = 0; i < 6; i++)
        {
            float a = i * Mathf.Tau / 6;
            var direction = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
            var across = new Vector3(-direction.Z, 0, direction.X) * .075f;
            var foot = direction * .06f - Vector3.Up * .16f;
            surface.AddVertex(foot - across);
            surface.AddVertex(foot + across);
            surface.AddVertex(direction * .2f + Vector3.Up * (.16f - i % 2 * .05f));
        }
        surface.GenerateNormals(); surface.SetMaterial(material);
        var mesh = surface.Commit(); surface.Dispose(); return mesh;
    }

    public static StaticBody3D Solid(Node3D parent, Vector3 position, Vector3 size, string color)
    {
        var body = new StaticBody3D { Position = position };
        parent.AddChild(body);
        Box(body, Vector3.Zero, size, color);
        Collider(body, Vector3.Zero, size);
        return body;
    }

    public static Label3D Sign(Node3D parent, Vector3 position, string text, int size = 64)
    {
        var label = new Label3D { Position = position, Text = text, FontSize = size, PixelSize = .008f,
            Modulate = new Color("fff2cf"), OutlineSize = 8, OutlineModulate = new Color("263a2c") };
        parent.AddChild(label);
        return label;
    }

    public static Node3D Farmer(Node3D parent, bool seated = false)
    {
        var model = new Node3D { Name = "FarmerModel" };
        parent.AddChild(model);
        Box(model, new(0, 1.12f, 0), new(.5f, .64f, .28f), "496a50");
        foreach (float side in new[] { -1f, 1f })
        {
            if (!seated)
            {
                var leg = new Node3D { Name = side < 0 ? "LeftLeg" : "RightLeg", Position = new(side * .16f, .83f, 0) };
                model.AddChild(leg);
                Box(leg, new(0, -.4f, 0), new(.22f, .8f, .26f), "354554");
                Box(leg, new(0, -.73f, -.08f), new(.24f, .18f, .4f), "473c32");
                var shoulder = new Node3D { Name = side < 0 ? "LeftArm" : "RightArm", Position = new(side * .34f, 1.34f, 0) };
                model.AddChild(shoulder);
                Box(shoulder, new(0, -.3f, 0), new(.16f, .6f, .18f), "c38e63");
                Box(shoulder, new(0, -.1f, 0), new(.18f, .23f, .2f), "496a50");
                continue;
            }
            Box(model, new(side * .16f, seated ? .72f : .43f, seated ? -.23f : 0),
                seated ? new(.22f, .22f, .64f) : new(.22f, .8f, .26f), "354554");
            if (seated) Box(model, new(side * .16f, .43f, -.5f), new(.22f, .55f, .23f), "354554");
            Box(model, new(side * .16f, .1f, seated ? -.57f : -.08f), new(.24f, .18f, .4f), "473c32");
            var arm = Box(model, new(side * .34f, 1.04f, seated ? -.2f : 0), new(.16f, .6f, .18f), "c38e63");
            if (seated) arm.RotationDegrees = new(-48, 0, 0);
        }
        var head = new Node3D { Name = "Head" };
        model.AddChild(head);
        Box(head, new(0, 1.66f, 0), new(.34f, .38f, .32f), "c38e63");
        Cylinder(head, new(0, 1.87f, 0), .32f, .045f, "d7b87b");
        Cylinder(head, new(0, 1.94f, 0), .2f, .15f, "b69256");
        return model;
    }

    public static void Tree(Node3D parent, Vector3 position, float size)
    {
        var tree = new StaticBody3D { Position = position };
        parent.AddChild(tree);
        Rod(tree, Vector3.Zero, new(.18f, size * 2, 0), .24f, "796044", .12f);
        Collider(tree, new(0, size, 0), new(.5f, size * 2, .5f));
        for (int i = 0; i < 4; i++)
        {
            float angle = i * Mathf.Tau / 4 + position.X;
            var tip = new Vector3(Mathf.Cos(angle) * size * .75f, size * (2.1f + i * .12f), Mathf.Sin(angle) * size * .75f);
            Rod(tree, new(.12f, size * 1.4f, 0), tip, .09f, "796044", .025f);
            tree.AddChild(new MeshInstance3D { Position = tip, Scale = new(1, .65f, .85f),
                Mesh = new SphereMesh { Radius = size * .8f, Height = size * 1.6f, RadialSegments = 7, Rings = 3 },
                MaterialOverride = Material(i % 2 == 0 ? "657044" : "858253") });
        }
    }
}
