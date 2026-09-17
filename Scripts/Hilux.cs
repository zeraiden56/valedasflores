using Godot;
using System.Collections.Generic;

namespace ValeDasFlores;

// Stylized late-1990s pickup, built with the same low-poly vocabulary as the CBT.
public partial class Hilux : CharacterBody3D
{
    public bool Occupied;
    public float Speed { get; private set; }
    public bool LightsOn { get; private set; }
    public Node3D Driver = null!;
    public virtual Vector3 Seat => ToGlobal(new(-.43f, 1.8f, -.25f));
    protected virtual float TopSpeed => 17;
    protected virtual float Wheelbase => 3.2f;
    private readonly List<(Node3D Steer, Node3D Spin, bool Front)> _wheels = new();
    protected readonly List<SpotLight3D> _lights = new();
    private float _steering;

    public override void _Ready()
    {
        Name = "Hilux1999"; FloorSnapLength = .5f;
        Models.Collider(this, new(0, .98f, 0), new(1.95f, 1.96f, 5.15f));
        Models.Box(this, new(0, .62f, 0), new(1.7f, .2f, 4.8f), "34352f");
        Models.Box(this, new(0, .95f, -.45f), new(1.9f, .55f, 4.05f), "171c1c");
        Models.Box(this, new(0, 1.3f, -1.65f), new(1.88f, .2f, 1.55f), "252a29");
        // Cab pillars surround open windows so the animated seated driver remains visible.
        Models.Box(this, new(0, 2.03f, -.18f), new(1.86f, .12f, 1.95f), "202726");
        Models.Box(this, new(0, 1.64f, .71f), new(1.8f, .72f, .1f), "1b2221");
        Models.Box(this, new(0, 1.75f, .775f), new(1.35f, .35f, .015f), "647c7c");
        Models.Box(this, new(0, 1.42f, -.85f), new(1.65f, .13f, .4f), "484b43");
        foreach (float side in new[] { -1f, 1f })
        {
            Models.Box(this, new(side * .86f, 1.64f, -1.03f), new(.1f, .8f, .12f), "262c2a").RotationDegrees = new(-13, 0, 0);
            Models.Box(this, new(side * .87f, 1.67f, .66f), new(.12f, .7f, .12f), "262c2a");
            Models.Box(this, new(side * .87f, 1.67f, -.02f), new(.08f, .7f, .07f), "262c2a");
            Models.Box(this, new(side * .95f, 1.18f, -.14f), new(.06f, .34f, 1.6f), "202725");
            Models.Box(this, new(side * .99f, 1.26f, .22f), new(.04f, .055f, .18f), "9a9b8e");
            Models.Box(this, new(side * 1.07f, 1.53f, -.87f), new(.25f, .17f, .14f), "323a35");
            Models.Box(this, new(side * 1.08f, .59f, -.12f), new(.2f, .07f, 1.9f), "8e9185");
            Models.Box(this, new(side * .91f, 1.1f, 1.57f), new(.15f, .62f, 1.85f), "1b2322");
            Models.Box(this, new(side * .91f, 1.44f, 1.57f), new(.18f, .055f, 1.92f), "4c5048");
            Models.Box(this, new(side * .68f, 1.06f, -2.49f), new(.43f, .27f, .035f), "f2e8bd");
            Models.Box(this, new(side * .92f, 1.06f, -2.45f), new(.08f, .22f, .08f), "ba813c");
            Models.Box(this, new(side * .82f, 1.11f, 2.52f), new(.2f, .37f, .04f), "a94332");
            Models.Box(this, new(side * .82f, 1.22f, 2.55f), new(.18f, .08f, .02f), "ccaa68");
            var lamp = new SpotLight3D { Position = new(side * .68f, 1.08f, -2.53f), LightColor = new Color("ffe9b8"), LightEnergy = 2.1f, SpotRange = 36, SpotAngle = 34, Visible = false };
            AddChild(lamp); _lights.Add(lamp);
            Wheel(side * .98f, -1.6f, true); Wheel(side * .98f, 1.6f, false);
            Models.Box(this, new(side * .45f, 1.03f, -.12f), new(.65f, .18f, .62f), "4b5148");
            Models.Box(this, new(side * .45f, 1.4f, .12f), new(.65f, .65f, .14f), "454c43");
        }
        Models.Box(this, new(0, .79f, 1.55f), new(1.62f, .1f, 1.85f), "45493e");
        for (int rib = -3; rib <= 3; rib++) Models.Box(this, new(rib * .22f, .855f, 1.55f), new(.04f, .025f, 1.75f), "5b5b4d");
        Models.Box(this, new(0, 1.12f, 2.48f), new(1.9f, .57f, .1f), "202724");
        Models.Box(this, new(0, 1.25f, 2.545f), new(.28f, .06f, .025f), "777d70");
        Models.Box(this, new(0, .7f, -2.53f), new(2, .18f, .15f), "9a9b8b");
        Models.Box(this, new(0, .65f, 2.6f), new(2, .17f, .18f), "848c7e");
        Models.Box(this, new(0, 1.08f, -2.5f), new(.83f, .28f, .04f), "565c51");
        for (int slat = 0; slat < 3; slat++) Models.Box(this, new(0, .99f + slat * .09f, -2.53f), new(.8f, .035f, .025f), "b1b3a2");
        Models.Box(this, new(0, .73f, -2.63f), new(.46f, .16f, .025f), "d6d5bd");
        Driver = Models.Farmer(this, true); Driver.Position = new(-.43f, .32f, -.28f); Driver.Visible = false;
    }

    protected void Wheel(float x, float z, bool front)
    {
        var steer = new Node3D { Position = new(x, .48f, z) }; AddChild(steer);
        var spin = new Node3D(); steer.AddChild(spin);
        Models.Cylinder(spin, Vector3.Zero, .47f, .32f, "242a25").RotationDegrees = new(0, 0, 90);
        float side = Mathf.Sign(x);
        Models.Cylinder(spin, new(side * .17f, 0, 0), .27f, .04f, "a5a896").RotationDegrees = new(0, 0, 90);
        Models.Cylinder(spin, new(side * .2f, 0, 0), .095f, .055f, "686e60").RotationDegrees = new(0, 0, 90);
        for (int i = 0; i < 6; i++)
        {
            float a = i * Mathf.Tau / 6;
            Models.Box(spin, new(side * .2f, Mathf.Cos(a) * .18f, Mathf.Sin(a) * .18f), new(.035f, .06f, .06f), "424a40");
        }
        for (int i = 0; i < 16; i++)
        {
            float a = i * Mathf.Tau / 16;
            Models.Box(spin, new(0, Mathf.Cos(a) * .47f, Mathf.Sin(a) * .47f), new(.34f, .04f, .09f), "343a30").Rotation = new(a, 0, 0);
        }
        _wheels.Add((steer, spin, front));
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        float throttle = Occupied ? Farm.VehicleThrottle() : 0;
        bool brake = !Occupied || Farm.VehicleBrake();
        float steer = Occupied ? Input.GetAxis("right", "left") : 0;
        Speed = Mathf.MoveToward(Speed, brake ? 0 : throttle >= 0 ? throttle * TopSpeed : throttle * 5, dt * (brake ? 18 : Mathf.Abs(throttle) > .01f ? 5.5f : 2.8f));
        _steering = Mathf.MoveToward(_steering, steer * .46f, dt * 2.4f);
        RotateY(Speed / Wheelbase * Mathf.Tan(_steering) * dt);
        var forward = -GlobalBasis.Z;
        Velocity = forward * Speed + Vector3.Up * (IsOnFloor() ? -.6f : Velocity.Y - 22 * dt);
        var before = GlobalPosition; MoveAndSlide();
        float travelled = (GlobalPosition - before).Dot(forward);
        if (IsOnWall()) Speed = 0;
        foreach (var wheel in _wheels)
        {
            wheel.Steer.Rotation = new(0, wheel.Front ? _steering : 0, 0);
            wheel.Spin.RotateX(-travelled / .47f);
        }
    }
    public void StopMotion() { Speed = 0; Velocity = Vector3.Zero; }
    public void ToggleLights()
    {
        LightsOn = !LightsOn;
        foreach (var lamp in _lights) lamp.Visible = LightsOn;
    }
}
