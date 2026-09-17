using Godot;

namespace ValeDasFlores;

// Photo-based blockout; distances remain estimates until a real measure is supplied.
public static class FarmWorld
{
    public static readonly Vector3 LakeCenter = new(30, 0, -43);
    public static void Build(Node3D root)
    {
        root.AddChild(new WorldEnvironment { Environment = new Godot.Environment {
            BackgroundMode = Godot.Environment.BGMode.Sky,
            Sky = new Sky { SkyMaterial = new ProceduralSkyMaterial {
                SkyTopColor = new Color("4386bb"), SkyHorizonColor = new Color("d1dedc"),
                GroundBottomColor = new Color("8e8760"), GroundHorizonColor = new Color("d1c8a7") } },
            AmbientLightSource = Godot.Environment.AmbientSource.Color,
            AmbientLightColor = new Color("e0e2cd"), AmbientLightEnergy = .35f,
            TonemapMode = Godot.Environment.ToneMapper.Linear,
            FogEnabled = true, FogLightColor = new Color("c5cbb3"), FogDensity = .0012f, FogSkyAffect = .1f
        } });
        root.AddChild(new DirectionalLight3D { RotationDegrees = new(-52, -35, 0), LightColor = new Color("fff0d2"),
            LightEnergy = .85f, ShadowEnabled = true, DirectionalShadowMaxDistance = 120 });
        var ground = Models.Box(root, new(0, -9, 0), new(800, .1f, 800), "a89972");
        ground.Name = "DryGround";
        ground.MaterialOverride = new ShaderMaterial { Shader = GD.Load<Shader>("res://Shaders/dry_ground.gdshader") };
        root.AddChild(new FarmTerrain());
        Models.Box(root, new(-47, .016f, 0), new(6, .025f, 216), "9e7658");
        Models.Box(root, new(-20, .02f, 12), new(54, .025f, 5), "ab8560");
        Models.Box(root, new(-47, .034f, 0), new(1.3f, .015f, 210), "8d9564");
        Models.Box(root, new(-23, .033f, 12), new(45, .015f, .7f), "8d9564");
        House(root); WaterTower(root); Lake(root); Implements(root); Corral(root);
        WireFence(root, new(-105, 0, -104), new(105, 0, -104), true);
        WireFence(root, new(-105, 0, 104), new(105, 0, 104), true);
        WireFence(root, new(-105, 0, -104), new(-105, 0, 104), true);
        WireFence(root, new(105, 0, -104), new(105, 0, 104), true);
        WireFence(root, new(-40, 0, 30), new(85, 0, 30), true);
        var rng = new RandomNumberGenerator { Seed = 2105 };
        CerradoTree(root, new(8, 0, 8), 1.25f, 3);
        CerradoTree(root, new(27, 0, -17), 1.05f, 9);
        CerradoTree(root, new(48, 0, -12), 1.3f, 2);
        CerradoTree(root, new(-14, 0, -16), 1.1f, 7);
        for (int i = 0; i < 170; i++)
        {
            var p = new Vector3(rng.RandfRange(-101, 101), 0, rng.RandfRange(-101, 101));
            if (Mathf.Abs(p.X + 47) < 6 || (p.X > -42 && p.X < 60 && p.Z > -90 && p.Z < 29) || NearLake(p, 1.35f)) continue;
            CerradoTree(root, p, rng.RandfRange(.6f, 1.25f), i);
        }
        ScatterGroundCover(root, rng);
        Models.Sign(root, new(-39, 1.7f, 16), "VALE DAS FLORES", 40);
        Models.Solid(root, new(-40.7f, .8f, 16), new(.16f, 1.6f, .16f), "736956");
        Models.Solid(root, new(-37.3f, .8f, 16), new(.16f, 1.6f, .16f), "736956");
        foreach (var child in root.GetChildren())
            if (child is Node3D n && child is not FarmTerrain && child is not DirectionalLight3D && n.Name != "DryGround")
                n.SetMeta("base_y", n.Position.Y);
        RefreshDecorationHeights(root, root.GetNode<FarmTerrain>("Terrain"));
    }
    public static void RefreshDecorationHeights(Node3D root, FarmTerrain terrain)
    {
        foreach (var child in root.GetChildren())
        {
            if (child is not Node3D node || !node.HasMeta("base_y")) continue;
            var p = node.Position;
            // Instance origins are already local ground coordinates; do not offset twice.
            p.Y = (float)node.GetMeta("base_y") + (node is MultiMeshInstance3D || node.Name == "Lago" ? 0 : terrain.HeightAt(p.X, p.Z));
            node.Position = p;
            if (node is not MultiMeshInstance3D cover) continue;
            for (int i = 0; i < cover.Multimesh.VisibleInstanceCount; i++)
            {
                var transform = cover.Multimesh.GetInstanceTransform(i);
                var point = transform.Origin;
                point.Y = terrain.HeightAt(point.X, point.Z) + transform.Basis.Scale.Y * .16f;
                transform.Origin = point;
                cover.Multimesh.SetInstanceTransform(i, transform);
            }
        }
    }
    private static bool NearLake(Vector3 p, float margin = 1) =>
        new Vector2((p.X - LakeCenter.X) / 22, (p.Z - LakeCenter.Z) / 14).Length() < margin;
    private static void House(Node3D root)
    {
        var house = new Node3D { Name = "Sede", Position = new(23, 0, 9) }; root.AddChild(house);
        Models.Solid(house, new(0, .08f, 0), new(15, .16f, 10), "968b76");
        Models.Solid(house, new(0, 1.7f, 4), new(14, 3.3f, .25f), "e6e1ce");
        Models.Solid(house, new(-7, 1.7f, 0), new(.25f, 3.3f, 8), "e6e1ce");
        Models.Solid(house, new(7, 1.7f, 0), new(.25f, 3.3f, 8), "e6e1ce");
        // Real doorway, without collision across the opening.
        Models.Solid(house, new(-5, 1.7f, -4), new(4, 3.3f, .25f), "eee9d8");
        Models.Solid(house, new(3, 1.7f, -4), new(8, 3.3f, .25f), "eee9d8");
        Models.Solid(house, new(-2, 2.95f, -4), new(2, .8f, .25f), "eee9d8");
        Models.Box(house, new(0, 3.32f, 0), new(14, .12f, 8), "514c40");
        foreach (float side in new[] { -1f, 1f })
        {
            Models.Box(house, new(0, 4, side * 2.25f), new(15.8f, .18f, 5.15f), "514d40").RotationDegrees = new(side * 18, 0, 0);
            for (int i = 0; i < 40; i++)
                Models.Box(house, new(-7.6f + i * .39f, 4.1f, side * 2.25f), new(.035f, .025f, 5.15f), "69604d").RotationDegrees = new(side * 18, 0, 0);
        }
        foreach (float x in new[] { 1.4f, 4.7f })
        {
            Models.Box(house, new(x, 1.9f, -4.15f), new(2, 1.25f, .09f), "535e58");
            for (int i = 0; i < 9; i++) Models.Box(house, new(x, 1.36f + i * .135f, -4.23f), new(1.94f, .045f, .07f), "a8ada0");
        }
        Models.Box(house, new(0, .22f, -4.15f), new(14, .15f, .07f), "88755c");
        Models.Box(house, new(0, 4.78f, 0), new(15.8f, .12f, .19f), "6a6250");
        Models.Box(house, new(-3.02f, 1.3f, -4.17f), new(.12f, 2.5f, .09f), "74694f");
        Models.Box(house, new(-.98f, 1.3f, -4.17f), new(.12f, 2.5f, .09f), "74694f");
        // Slight earth staining beneath the old limewashed walls.
        for (int i = 0; i < 11; i++)
            Models.Box(house, new(.1f + i * .6f, .25f, -4.137f), new(.36f, .18f + i % 3 * .09f, .018f), "c1b59a");
        Tire(house, new(5.2f, .65f, -4.7f), .62f, true);
        // Side veranda seen in 14.14.23 (1); placement remains an estimated blockout.
        Models.Solid(house, new(-9, .1f, 0), new(4, .2f, 8), "9a9686");
        foreach (float z in new[] { -3.7f, 0f, 3.7f })
        {
            Models.Solid(house, new(-10.65f, 1.55f, z), new(.2f, 3.1f, .2f), "d2cbb8");
            Models.Box(house, new(-8.9f, 3.12f, z), new(4.2f, .12f, .12f), "71624b").RotationDegrees = new(0, 0, 7);
        }
        Models.Box(house, new(-9, 3.26f, 0), new(4.6f, .1f, 8.6f), "6c685c").RotationDegrees = new(0, 0, 7);
        for (int rib = 0; rib < 28; rib++)
            Models.Box(house, new(-9, 3.32f, -4.1f + rib * .3f), new(4.6f, .045f, .045f), "878172").RotationDegrees = new(0, 0, 7);
        Models.Box(house, new(-7.35f, .85f, 2.7f), new(.55f, .12f, 1.8f), "877454");
        foreach (float z in new[] { 2f, 3.4f }) Models.Box(house, new(-7.35f, .43f, z), new(.12f, .85f, .12f), "736248");
    }
    private static void WaterTower(Node3D root)
    {
        var tower = new Node3D { Name = "CaixaDAgua", Position = new(8, 0, 1) }; root.AddChild(tower);
        Models.Solid(tower, new(0, .1f, 0), new(3, .2f, 3), "a59e8b");
        Models.Solid(tower, new(0, 2.8f, 0), new(.65f, 5.6f, .65f), "827b66");
        Models.Solid(tower, new(0, 5.5f, 0), new(2.5f, .22f, 2.5f), "766c52");
        for (int board = -2; board <= 2; board++)
            Models.Box(tower, new(board * .44f, 5.64f, 0), new(.4f, .06f, 2.45f), board % 2 == 0 ? "938369" : "746a55");
        foreach (float side in new[] { -1f, 1f })
        {
            Models.Rod(tower, new(side * .22f, 4.5f, 0), new(side * 1.05f, 5.45f, 0), .075f, "6f6450");
            Models.Box(tower, new(side * .335f, 2.8f, .12f), new(.02f, 5.5f, .045f), "a2977f");
        }
        Models.Cylinder(tower, new(0, 6.35f, 0), 1.08f, 1.55f, "184975", .97f);
        Models.Cylinder(tower, new(0, 7.15f, 0), 1.14f, .15f, "133e65");
        for (int i = 0; i < 4; i++) Models.Cylinder(tower, new(0, 5.73f + i * .35f, 0), 1.095f - i * .024f, .045f, "275c86");
        Models.Cylinder(tower, new(.65f, 3, .65f), .055f, 5.7f, "c2c1ad");
        Models.Box(tower, new(-.7f, .42f, 2.4f), new(2, .8f, 1.1f), "78a8ae");
    }
    private static void Lake(Node3D root)
    {
        var lake = new Node3D { Name = "Lago", Position = LakeCenter }; root.AddChild(lake);
        var bank = Models.Cylinder(lake, new(0, .01f, 0), 1, .04f, "7a8960"); bank.Scale = new(25, 1, 17);
        var water = Models.Cylinder(lake, new(0, .045f, 0), 1, .025f, "4c6d72"); water.Scale = new(22, 1, 14);
        water.MaterialOverride = new ShaderMaterial { Shader = GD.Load<Shader>("res://Shaders/pond.gdshader") };
        // Low water exposes pale sand, with irregular grassy fingers around the shore.
        for (int i = 0; i < 18; i++)
        {
            float angle = i * Mathf.Tau / 18;
            var patch = Models.Cylinder(lake, new(Mathf.Cos(angle) * 22.2f, .028f, Mathf.Sin(angle) * 14.1f), 1, .015f, i % 3 == 0 ? "a9aa82" : "8f9968");
            patch.Scale = new(2.1f + i % 3 * .4f, 1, 1.1f + i % 2 * .3f);
            patch.Rotation = new(0, -angle, 0);
        }
        // Open shore. New terrain profiles provide a shallow, walkable basin.
    }
    private static void WireFence(Node3D root, Vector3 a, Vector3 b, bool collision)
    {
        var fence = new StaticBody3D { Position = (a + b) * .5f }; root.AddChild(fence);
        float length = a.DistanceTo(b);
        fence.Rotation = new(0, -Mathf.Atan2(b.Z - a.Z, b.X - a.X), 0);
        int count = Mathf.Max(1, Mathf.CeilToInt(length / 5));
        for (int i = 0; i <= count; i++) Models.Box(fence, new(-length / 2 + length * i / count, .72f, 0), new(.12f, 1.44f, .12f), "a6a596");
        foreach (float y in new[] { .4f, .8f, 1.2f }) Models.Box(fence, new(0, y, 0), new(length, .018f, .018f), "655f50");
        if (collision) Models.Collider(fence, new(0, .7f, 0), new(length, 1.4f, .12f));
    }
    public static void CerradoTree(Node3D root, Vector3 position, float scale, int seed)
    {
        var tree = new StaticBody3D { Position = position, Scale = Vector3.One * scale }; root.AddChild(tree);
        Models.Cylinder(tree, new(.16f, 1.5f, 0), .22f, 3, "777366", .13f).RotationDegrees = new(0, 0, -7);
        Models.Collider(tree, new(0, 1.5f, 0), new(.5f, 3, .5f));
        var rng = new RandomNumberGenerator { Seed = (ulong)(seed + 40) };
        for (int j = 0; j < 5; j++)
        {
            float angle = j * Mathf.Tau / 5 + seed;
            float reach = rng.RandfRange(1.15f, 2.1f);
            var tip = new Vector3(Mathf.Cos(angle) * reach, 3.5f + rng.RandfRange(-.6f, .9f), Mathf.Sin(angle) * reach);
            var elbow = new Vector3(tip.X * .55f, 2.7f + rng.RandfRange(-.4f, .35f), tip.Z * .4f);
            Models.Rod(tree, new(.15f, 1.7f, 0), elbow, .12f, "777366", .075f);
            Models.Rod(tree, elbow, tip, .075f, "777366", .025f);
            var twigTip = tip + new Vector3(Mathf.Sin(angle) * .65f, .65f, Mathf.Cos(angle) * .5f);
            Models.Rod(tree, elbow.Lerp(tip, .65f), twigTip, .04f, "817c6a", .008f);
            if (seed % 5 == 0 && j % 2 == 0) continue;
            // Uneven overlapping lobes leave gaps and exposed crooked branches.
            for (int lobe = 0; lobe < 2; lobe++)
            {
                string[] leafColors = { "747847", "657044", "858253", "596640" };
                tree.AddChild(new MeshInstance3D {
                    Position = tip + new Vector3(rng.RandfRange(-.55f, .55f), lobe * .42f, rng.RandfRange(-.5f, .5f)),
                    RotationDegrees = new(rng.RandfRange(-15, 15), rng.RandfRange(0, 360), rng.RandfRange(-12, 12)),
                    Scale = new(rng.RandfRange(.95f, 1.5f), rng.RandfRange(.5f, .85f), rng.RandfRange(.9f, 1.35f)),
                    Mesh = new SphereMesh { Radius = 1.05f, Height = 2.1f, RadialSegments = 7, Rings = 3 },
                    MaterialOverride = Models.Material(leafColors[(seed + j + lobe) % leafColors.Length]) });
            }
        }
    }
    private static void Tire(Node3D root, Vector3 p, float radius, bool upright)
    {
        var mesh = new MeshInstance3D { Position = p, Mesh = new TorusMesh { InnerRadius = radius * .55f, OuterRadius = radius, Rings = 16, RingSegments = 8 }, MaterialOverride = Models.Material("30332d") };
        if (upright) mesh.RotationDegrees = new(75, 0, 0);
        root.AddChild(mesh);
    }
    private static void Implements(Node3D root)
    {
        var yard = new Node3D { Name = "Implementos", Position = new(9, 0, -14) }; root.AddChild(yard);
        Models.Solid(yard, new(0, .7f, 0), new(3.8f, .22f, 1.7f), "8a6639");
        foreach (float x in new[] { -1.5f, -.75f, 0, .75f, 1.5f })
            foreach (float z in new[] { -.65f, .65f })
                Models.Cylinder(yard, new(x, .5f, z), .48f, .09f, "665342").RotationDegrees = new(0, 0, 78);
        Models.Solid(yard, new(0, .4f, 2), new(.15f, .16f, 3.2f), "78603d");
        Models.Cylinder(yard, new(0, .22f, 3.4f), .075f, .44f, "78603d");
        Models.Solid(yard, new(-6, .8f, 0), new(3, .16f, .2f), "665342");
        foreach (float side in new[] { -1f, 1f })
        {
            yard.AddChild(new MeshInstance3D { Position = new(-6 + side * 1.5f, 1, 0), RotationDegrees = new(0, 0, 90),
                Mesh = new TorusMesh { InnerRadius = .86f, OuterRadius = 1, Rings = 16, RingSegments = 6 }, MaterialOverride = Models.Material("75583d") });
            for (int i = 0; i < 8; i++)
                Models.Box(yard, new(-6 + side * 1.5f, 1, 0), new(.09f, 1.8f, .06f), "75583d").RotationDegrees = new(i * 22.5f, 0, 0);
        }
        for (int i = 0; i < 3; i++) Tire(yard, new(7, .18f + i * .3f, 2), .7f, false);
    }
    private static void ScatterGroundCover(Node3D root, RandomNumberGenerator rng)
    {
        var material = new StandardMaterial3D { AlbedoColor = new Color("a29a6d"), VertexColorUseAsAlbedo = true, CullMode = BaseMaterial3D.CullModeEnum.Disabled };
        var grass = new MultiMesh { TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            UseColors = true, Mesh = Models.GrassTuft(material), InstanceCount = 6500 };
        root.AddChild(new MultiMeshInstance3D { Multimesh = grass, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off });
        int placed = 0;
        for (int i = 0; i < 10000 && placed < grass.InstanceCount; i++)
        {
            var p = new Vector3(rng.RandfRange(-103, 103), .13f, rng.RandfRange(-103, 103));
            if (NearLake(p, 1.2f) || Mathf.Abs(p.X + 47) < 4 || (p.X > -48 && p.X < 32 && Mathf.Abs(p.Z - 12) < 3) || (p.X > 15 && p.X < 32 && p.Z > 4 && p.Z < 15)) continue;
            float scale = rng.RandfRange(.5f, 1.7f);
            float tall = (p.X < -55 || p.Z > 38) ? 2.5f : 1;
            p.Y = .16f * scale * tall;
            grass.SetInstanceTransform(placed, new Transform3D(Basis.FromEuler(new(0, rng.RandfRange(0, Mathf.Tau), 0)).Scaled(new Vector3(scale, scale * tall, scale)), p));
            grass.SetInstanceColor(placed++, new Color(rng.RandfRange(.8f, 1), rng.RandfRange(.85f, 1), rng.RandfRange(.7f, .95f)));
        }
        grass.VisibleInstanceCount = placed;
    }
    private static void Corral(Node3D root)
    {
        // Photo 14.14.35: open timber shelter. Coordinates are provisional.
        var corral = new Node3D { Name = "CurralCoberto", Position = new(56, 0, 12) }; root.AddChild(corral);
        foreach (float x in new[] { -4.5f, 0, 4.5f }) foreach (float z in new[] { -3f, 3f })
            Models.Solid(corral, new(x, 1.7f, z), new(.18f, 3.4f, .18f), "786b53");
        foreach (float side in new[] { -1f, 1f })
        {
            Models.Box(corral, new(0, 3.55f, side * 1.65f), new(10.2f, .12f, 3.6f), "706b5a").RotationDegrees = new(side * 12, 0, 0);
            foreach (float y in new[] { .35f, .8f, 1.25f })
                Models.Solid(corral, new(side * 4.5f, y, 0), new(.1f, .18f, 6), "827a66");
        }
        foreach (float y in new[] { .35f, .8f, 1.25f })
        {
            Models.Solid(corral, new(0, y, 3), new(9, .18f, .1f), "8f8874");
            Models.Solid(corral, new(2.25f, y, -3), new(4.5f, .18f, .1f), "8f8874");
        }
        Models.Box(corral, new(0, 3.96f, 0), new(10.2f, .1f, .2f), "8e8671");
        Tire(corral, new(-4.2f, .8f, .9f), .6f, true);
        Tire(corral, new(3, .8f, 2.8f), .6f, true);
    }
}
