using Godot;

namespace ValeDasFlores;

public partial class Jeep : Hilux
{
    public bool RoofOn { get; private set; } = true;
    private Node3D _roof = null!;
    public override Vector3 Seat => ToGlobal(new(-.4f, 1.84f, .12f));
    protected override float TopSpeed => 13;
    protected override float Wheelbase => 2.3f;

    public override void _Ready()
    {
        Name = "WillysCJ5"; FloorSnapLength = .5f;
        Models.Collider(this, new(0, .91f, 0), new(1.85f, 1.82f, 3.7f));
        Models.Box(this, new(0, .61f, 0), new(1.45f, .2f, 3.5f), "333a2d");
        Models.Box(this, new(0, .87f, .22f), new(1.65f, .24f, 3.1f), "354936");
        Models.Box(this, new(0, 1.18f, -1.05f), new(1.13f, .55f, 1.6f), "304b35");
        Models.Box(this, new(0, 1.48f, -1.02f), new(1.2f, .1f, 1.65f), "405d40");
        // Seven narrow grille slots and round lamps define the CJ silhouette.
        Models.Box(this, new(0, 1.1f, -1.86f), new(1.55f, .75f, .12f), "3b5338");
        for (int i = -3; i <= 3; i++) Models.Box(this, new(i * .13f, 1.1f, -1.935f), new(.065f, .48f, .02f), "20291f");
        foreach (float side in new[] { -1f, 1f })
        {
            Models.Cylinder(this, new(side * .61f, 1.28f, -1.95f), .17f, .045f, "aaa98b").RotationDegrees = new(90, 0, 0);
            Models.Cylinder(this, new(side * .61f, 1.28f, -1.98f), .135f, .025f, "eee0b0").RotationDegrees = new(90, 0, 0);
            Models.Box(this, new(side * .8f, 1.05f, -1.25f), new(.46f, .1f, 1.45f), "3d5539");
            Models.Box(this, new(side * .78f, 1.06f, 1.05f), new(.16f, .55f, 1.22f), "30472f");
            Models.Box(this, new(side * .78f, 1.39f, 1.05f), new(.21f, .1f, 1.3f), "455e40");
            Models.Box(this, new(side * .85f, .75f, .04f), new(.33f, .07f, .85f), "48543c");
            Models.Rod(this, new(side * .7f, 1.4f, -.3f), new(side * .74f, 2.15f, -.5f), .04f, "40593b");
            Models.Rod(this, new(side * .72f, 1.62f, -.39f), new(side * 1.01f, 1.83f, -.41f), .018f, "596049");
            Models.Cylinder(this, new(side * 1.01f, 1.87f, -.42f), .09f, .03f, "9aa291").RotationDegrees = new(90, 0, 0);
            Models.Box(this, new(side * .4f, .99f, .14f), new(.6f, .15f, .62f), "343b2c");
            Models.Box(this, new(side * .4f, 1.34f, .43f), new(.6f, .66f, .13f), "39412f");
            Models.Box(this, new(side * .64f, 1.12f, 1.83f), new(.14f, .2f, .03f), "a24130");
            var lamp = new SpotLight3D { Position = new(side * .61f, 1.28f, -2.01f), LightColor = new Color("ffe6a9"), LightEnergy = 1.9f, SpotRange = 30, SpotAngle = 33, Visible = false };
            AddChild(lamp); _lights.Add(lamp);
            Wheel(side * .94f, -1.15f, true); Wheel(side * .94f, 1.15f, false);
        }
        Models.Rod(this, new(-.74f, 2.15f, -.5f), new(.74f, 2.15f, -.5f), .04f, "40593b");
        Models.Rod(this, new(-.7f, 1.48f, -.32f), new(.7f, 1.48f, -.32f), .045f, "40593b");
        Models.Rod(this, new(0, 1.48f, -.32f), new(0, 2.15f, -.5f), .02f, "566a49");
        Models.Box(this, new(0, 1.15f, 1.78f), new(1.65f, .62f, .1f), "354d32");
        Models.Box(this, new(0, .7f, -2.01f), new(1.92f, .15f, .17f), "565c46");
        Models.Box(this, new(0, .68f, 1.93f), new(1.9f, .14f, .16f), "565c46");
        Models.Cylinder(this, new(.35f, 1.3f, 1.99f), .46f, .24f, "272f24").RotationDegrees = new(90, 0, 0);
        Models.Cylinder(this, new(.35f, 1.3f, 2.13f), .24f, .025f, "6f795a").RotationDegrees = new(90, 0, 0);
        Driver = Models.Farmer(this, true); Driver.Position = new(-.4f, .28f, .06f); Driver.Visible = false;
        _roof = new Node3D { Name = "CapotaRemovivel" }; AddChild(_roof);
        Models.Box(_roof, new(0, 2.21f, .62f), new(1.78f, .09f, 2.34f), "262c27");
        Models.Box(_roof, new(0, 1.83f, 1.73f), new(1.75f, .7f, .055f), "252c25");
        Models.Box(_roof, new(0, 1.88f, 1.765f), new(1.2f, .36f, .01f), "68776b");
        foreach (float side in new[] { -1f, 1f })
        {
            Models.Rod(_roof, new(side * .8f, 1.37f, .68f), new(side * .8f, 2.17f, .68f), .035f, "4d5945");
            Models.Box(_roof, new(side * .86f, 1.84f, 1.12f), new(.045f, .69f, 1.17f), "2c322b");
            Models.Box(_roof, new(side * .89f, 1.89f, 1.1f), new(.01f, .37f, .8f), "657469");
        }
    }
    public void ToggleRoof() { RoofOn = !RoofOn; _roof.Visible = RoofOn; }
}
