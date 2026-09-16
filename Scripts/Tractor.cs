using Godot;
using System.Collections.Generic;

namespace ValeDasFlores;

public partial class Tractor : CharacterBody3D
{
    public bool Occupied;
    public float Speed { get; private set; }
    public bool LightsOn { get; private set; }
    public Node3D Driver = null!;
    private readonly List<(Node3D Steer, Node3D Spin, float Radius, bool Front)> _wheels = new();
    private readonly List<SpotLight3D> _lights = new();
    private Node3D _steeringWheel = null!;
    private float _steer;
    public Vector3 Seat => ToGlobal(new(0, 2.35f, .72f));

    public override void _Ready()
    {
        Name = "CBT2105";
        FloorSnapLength = .4f;
        Models.Collider(this, new(0, 1.18f, 0), new(2.25f, 2.25f, 4.35f));
        Models.Box(this, new(0, .85f, 0), new(.85f, .35f, 3.8f), "47473d");
        Models.Box(this, new(0, 1.47f, -.94f), new(1.13f, .82f, 2.12f), "dcae27");
        Models.Box(this, new(0, 1.92f, -.96f), new(1.18f, .12f, 2.18f), "edc345");
        Models.Box(this, new(0, 1.46f, -2.02f), new(.99f, .69f, .06f), "343a34");
        for (int i = 0; i < 12; i++)
            Models.Box(this, new(-.45f + i * .082f, 1.46f, -2.06f), new(.026f, .63f, .035f), "a9a28a");
        Models.Box(this, new(0, .86f, -2.21f), new(1.65f, .22f, .24f), "55564a");
        Models.Cylinder(this, new(.4f, 2.34f, -1.3f), .075f, 1.55f, "41423a");
        Models.Cylinder(this, new(.4f, 3.13f, -1.3f), .105f, .05f, "292f2c");
        Models.Cylinder(this, new(-.4f, 2.12f, -1), .095f, .65f, "42473b");
        Models.Box(this, new(0, .97f, .75f), new(1.5f, .12f, 1.4f), "797650");
        Models.Box(this, new(0, 1.37f, .82f), new(.63f, .16f, .57f), "342f28");
        Models.Box(this, new(0, 1.67f, 1.09f), new(.64f, .57f, .13f), "342f28");
        Models.Box(this, new(0, 1.5f, .04f), new(.55f, .42f, .2f), "5b604b");
        _steeringWheel = new Node3D { Position = new(0, 1.95f, .19f), RotationDegrees = new(32, 0, 0) };
        AddChild(_steeringWheel);
        var rim = new MeshInstance3D { Mesh = new TorusMesh { InnerRadius = .19f, OuterRadius = .23f, Rings = 16, RingSegments = 8 }, MaterialOverride = Models.Material("282d29") };
        _steeringWheel.AddChild(rim);
        Models.Box(_steeringWheel, Vector3.Zero, new(.4f, .035f, .035f), "595b4e");
        Models.Box(this, new(0, .58f, 1.97f), new(.28f, .16f, .64f), "45483c");
        foreach (float side in new[] { -1f, 1f })
        {
            Models.Box(this, new(side * .95f, 2.07f, .98f), new(.53f, .13f, 1.86f), "dfb32f");
            Models.Box(this, new(side * .68f, 1.61f, .95f), new(.08f, .9f, 1.65f), "c39829");
            Models.Box(this, new(side * .92f, .63f, -.02f), new(.5f, .09f, .49f), "5d6050");
            Models.Box(this, new(side * .94f, .38f, .01f), new(.47f, .09f, .4f), "5d6050");
            Models.Box(this, new(side * .92f, 2.17f, 1.56f), new(.2f, .14f, .09f), "ba422a");
            Models.Box(this, new(side * .4f, 1.82f, -2.07f), new(.23f, .18f, .08f), "fff1ba");
            var lamp = new SpotLight3D { Position = new(side * .4f, 1.82f, -2.13f), LightColor = new Color("ffedbd"), LightEnergy = 2, SpotRange = 28, SpotAngle = 32, Visible = false };
            AddChild(lamp); _lights.Add(lamp);
            Wheel(side * 1.03f, .97f, 1.03f, .95f, .5f, false);
            Wheel(side * .89f, .55f, -1.49f, .54f, .3f, true);
            var decal = Models.Sign(this, new(side * .574f, 1.6f, -.9f), "CBT 2105", 40);
            decal.RotationDegrees = new(0, side * 90, 0);
            decal.PixelSize = .004f;
        }
        Driver = Models.Farmer(this, true);
        Driver.Position = new(0, .66f, .64f);
        Driver.Visible = false;
    }

    private void Wheel(float x, float y, float z, float radius, float width, bool front)
    {
        var steer = new Node3D { Position = new(x, y, z) }; AddChild(steer);
        var spin = new Node3D(); steer.AddChild(spin);
        var tire = Models.Cylinder(spin, Vector3.Zero, radius, width, "282d29");
        tire.RotationDegrees = new(0, 0, 90);
        foreach (float side in new[] { -1f, 1f })
        {
            var hub = Models.Cylinder(spin, new(side * (width / 2 + .012f), 0, 0), radius * .52f, .03f, "cbb985");
            hub.RotationDegrees = new(0, 0, 90);
        }
        for (int i = 0; i < 18; i++)
        {
            float angle = i * Mathf.Tau / 18;
            var tread = Models.Box(spin, new(0, Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius), new(width * 1.04f, .075f, front ? .11f : .17f), "363a31");
            tread.Rotation = new(angle, 0, .18f);
        }
        _wheels.Add((steer, spin, radius, front));
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        float throttle = Occupied ? Input.GetAxis("back", "forward") : 0;
        float steering = Occupied ? Input.GetAxis("right", "left") : 0;
        bool brake = !Occupied || Input.IsActionPressed("jump");
        float target = throttle >= 0 ? throttle * 8 : throttle * 3;
        Speed = Mathf.MoveToward(Speed, brake ? 0 : target, (brake ? 10 : throttle == 0 ? 1.8f : 2.7f) * dt);
        _steer = Mathf.MoveToward(_steer, steering * .5f, dt * 1.7f);
        RotateY(Speed / 2.52f * Mathf.Tan(_steer) * dt);
        var forward = -GlobalBasis.Z;
        Velocity = forward * Speed + Vector3.Up * (IsOnFloor() ? -.6f : Velocity.Y - 22 * dt);
        var before = GlobalPosition;
        MoveAndSlide();
        float travelled = (GlobalPosition - before).Dot(forward);
        if (IsOnWall()) Speed = 0;
        foreach (var wheel in _wheels)
        {
            wheel.Steer.Rotation = new(0, wheel.Front ? _steer : 0, 0);
            wheel.Spin.RotateX(-travelled / wheel.Radius);
        }
        _steeringWheel.Rotation = new(Mathf.DegToRad(32), _steer * 2, 0);
    }

    public void ToggleLights()
    {
        LightsOn = !LightsOn;
        foreach (var lamp in _lights) lamp.Visible = LightsOn;
    }
}
