using Godot;

namespace ValeDasFlores;

public static class FarmWorld
{
    public static void Build(Node3D root)
    {
        var environment = new Godot.Environment {
            BackgroundMode = Godot.Environment.BGMode.Sky,
            Sky = new Sky { SkyMaterial = new ProceduralSkyMaterial {
                SkyTopColor = new Color("789dad"), SkyHorizonColor = new Color("e5dfbb"),
                GroundBottomColor = new Color("6c7652"), GroundHorizonColor = new Color("e5dfbb") } },
            AmbientLightSource = Godot.Environment.AmbientSource.Color,
            AmbientLightColor = new Color("c3d7df"), AmbientLightEnergy = .65f,
            TonemapMode = Godot.Environment.ToneMapper.Filmic,
            FogEnabled = true, FogLightColor = new Color("c5cbaa"), FogDensity = .0018f
        };
        root.AddChild(new WorldEnvironment { Environment = environment });
        root.AddChild(new DirectionalLight3D { RotationDegrees = new(-38, -35, 0), LightColor = new Color("fff0c9"), LightEnergy = 1.25f, ShadowEnabled = true, DirectionalShadowMaxDistance = 100 });
        Models.Solid(root, new(0, -.25f, 0), new(160, .5f, 160), "7b8950");
        Models.Box(root, new(0, .008f, 0), new(8, .015f, 154), "b89f73");
        Models.Box(root, new(-10, .019f, 5), new(28, .025f, 20), "b49a70");
        // Duas áreas de plantio, por enquanto decorativas.
        for (int field = 0; field < 2; field++)
        {
            float x = field == 0 ? 22 : -25;
            Models.Box(root, new(x, .013f, -30), new(28, .025f, 42), "806342");
            for (int row = 0; row < 12; row++)
            {
                Models.Box(root, new(x - 12 + row * 2.1f, .05f, -30), new(.7f, .08f, 40), "6c553c");
                for (int plant = 0; plant < 18; plant++)
                    Models.Cylinder(root, new(x - 12 + row * 2.1f, .22f, -49 + plant * 2.2f), .25f, .38f, field == 0 ? "75934b" : "acaa55", .045f);
            }
        }
        Barn(root);
        Models.Solid(root, new(20, 1.9f, 13), new(9, 3.8f, 7), "e1d1a2");
        Roof(root, new(20, 4.2f, 13), new(10.3f, .25f, 4.5f), "955c3d");
        Models.Box(root, new(20, 1.25f, 9.48f), new(1.25f, 2.5f, .06f), "56725d");
        foreach (float x in new[] { 17.3f, 22.7f })
        {
            Models.Box(root, new(x, 2, 9.44f), new(1.55f, 1.45f, .08f), "f2e5ba");
            Models.Box(root, new(x, 2, 9.38f), new(1.3f, 1.2f, .07f), "698b91");
            Models.Box(root, new(x, 2, 9.32f), new(.06f, 1.2f, .03f), "f2e5ba");
        }
        Models.Sign(root, new(20, 3.25f, 9.33f), "SEDE", 50).RotationDegrees = new(0, 180, 0);
        for (int i = -8; i <= 8; i++)
        {
            Fence(root, new(i * 9, 0, 73), false);
            Fence(root, new(i * 9, 0, -73), false);
            Fence(root, new(73, 0, i * 9), true);
            Fence(root, new(-73, 0, i * 9), true);
        }
        var rng = new RandomNumberGenerator { Seed = 2105 };
        for (int i = 0; i < 65; i++)
        {
            float x = rng.RandfRange(-68, 68), z = rng.RandfRange(-68, 68);
            if (Mathf.Abs(x) > 45 || z > 29 || z < -57)
                Models.Tree(root, new(x, 0, z), rng.RandfRange(1.5f, 2.7f));
        }
        for (int i = 0; i < 100; i++)
        {
            var pos = new Vector3(rng.RandfRange(7, 38), .18f, rng.RandfRange(22, 28));
            Models.Cylinder(root, pos, .025f, .35f, "576e3a");
            Models.Box(root, pos + new Vector3(0, .2f, 0), new(.2f, .08f, .2f), i % 3 == 0 ? "dabbd5" : i % 3 == 1 ? "eee1a4" : "c99691");
        }
        Models.Solid(root, new(5.5f, 1.25f, 14), new(.16f, 2.5f, .16f), "746144");
        Models.Box(root, new(5.5f, 2.2f, 14), new(4.1f, 1.02f, .14f), "3e5845");
        Models.Sign(root, new(5.5f, 2.24f, 14.09f), "VALE DAS FLORES", 48);
        // Limites físicos independentes da decoração da cerca.
        foreach (float side in new[] { -1f, 1f })
        {
            var wall = new StaticBody3D(); root.AddChild(wall);
            Models.Collider(wall, new(side * 76, 3, 0), new(1, 6, 153));
            Models.Collider(wall, new(0, 3, side * 76), new(153, 6, 1));
        }
        for (int i = 0; i < 12; i++)
        {
            var hill = Models.Cylinder(root, new(Mathf.Cos(i * Mathf.Tau / 12) * 130, 6, Mathf.Sin(i * Mathf.Tau / 12) * 130), 46, 30 + i % 3 * 8, i % 2 == 0 ? "7f9168" : "94a27e", 0);
            hill.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        }
    }

    private static void Roof(Node3D root, Vector3 center, Vector3 size, string color)
    {
        foreach (float side in new[] { -1f, 1f })
        {
            var roof = Models.Box(root, center + new Vector3(0, 0, side * size.Z * .44f), size, color);
            roof.RotationDegrees = new(side * 22, 0, 0);
        }
    }

    private static void Barn(Node3D root)
    {
        Models.Solid(root, new(-20, 2.5f, 8), new(12, 5, .3f), "985d43");
        Models.Solid(root, new(-26, 2.5f, 2), new(.3f, 5, 12), "985d43");
        Models.Solid(root, new(-14, 2.5f, 2), new(.3f, 5, 12), "985d43");
        foreach (float x in new[] { -24.4f, -15.6f })
            Models.Solid(root, new(x, 2.5f, -4), new(3.2f, 5, .3f), "a86a4b");
        Models.Solid(root, new(-20, 4.6f, -4), new(6, .8f, .3f), "a86a4b");
        Roof(root, new(-20, 5.8f, 2), new(13.6f, .22f, 7.3f), "645c4b");
        var sign = Models.Sign(root, new(-20, 4.55f, -4.2f), "OFICINA  •  01", 48);
        sign.RotationDegrees = new(0, 180, 0);
        for (int i = 0; i < 4; i++) Models.Solid(root, new(-24, .5f, i * 1.5f), new(1.4f, 1, 1.1f), "c6ac61");
    }

    private static void Fence(Node3D root, Vector3 position, bool rotated)
    {
        var fence = new Node3D { Position = position, RotationDegrees = new(0, rotated ? 90 : 0, 0) }; root.AddChild(fence);
        Models.Box(fence, new(0, .7f, 0), new(.17f, 1.4f, .17f), "827255");
        Models.Box(fence, new(4.5f, .55f, 0), new(9, .12f, .1f), "a58e64");
        Models.Box(fence, new(4.5f, 1.13f, 0), new(9, .12f, .1f), "a58e64");
    }
}
