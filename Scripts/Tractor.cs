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
    public float Fuel { get; set; } = 30;
    public int Tool { get; private set; }
    public bool ToolLowered { get; private set; }
    private Node3D _blade = null!, _harrow = null!;
    public string ToolName => Tool == 1 ? "PÁ FRONTAL" : Tool == 2 ? "GRADE" : "SEM IMPLEMENTO";
    public void Equip(int tool) { Tool = tool; ToolLowered = false; UpdateTools(); }
    public void ToggleTool() { if (Tool != 0) ToolLowered = !ToolLowered; UpdateTools(); }
    public void StopMotion() { Speed = 0; Velocity = Vector3.Zero; }
    private void UpdateTools()
    {
        _blade.Visible = Tool == 1; _harrow.Visible = Tool == 2;
        _blade.Position = new(0, ToolLowered ? 0 : .8f, 0);
        _harrow.Position = new(0, ToolLowered ? 0 : .5f, 0);
    }
    private AudioStreamWav? _engineRecording;
    public AudioStreamPlayer3D EngineAudio { get; private set; } = null!;
    public Vector3 Seat => ToGlobal(new(0, 2.35f, .72f));

    public override void _Ready()
    {
        Name = "CBT2105";
        FloorSnapLength = .4f;
        Models.Collider(this, new(0, 1.18f, 0), new(2.25f, 2.25f, 4.35f));
        Models.Box(this, new(0, .85f, 0), new(.85f, .35f, 3.8f), "47473d");
        Models.Box(this, new(0, 1.47f, -.94f), new(1.13f, .82f, 2.12f), "b88b43");
        Models.Box(this, new(0, 1.92f, -.96f), new(1.18f, .12f, 2.18f), "ceaa63");
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
            Models.Box(this, new(side * .95f, 2.07f, .98f), new(.53f, .13f, 1.86f), "c8a15b");
            Models.Box(this, new(side * .68f, 1.61f, .95f), new(.08f, .9f, 1.65f), "a78142");
            // Exposed engine, paint wear and panel seams read clearly at low resolution.
            Models.Box(this, new(side * .59f, 1.34f, -.75f), new(.04f, .1f, 1.8f), "6d6247");
            for (int vent = 0; vent < 6; vent++)
                Models.Box(this, new(side * .592f, 1.56f, -1.72f + vent * .1f), new(.022f, .22f, .025f), "645e48");
            Models.Rod(this, new(side * .45f, 1.05f, -.1f), new(side * .45f, 1.1f, -1.5f), .08f, "504c3d");
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
        // Canopy and loader inspired by the user's CBT reference.
        foreach (float side in new[] { -1f, 1f })
        {
            Models.Box(this, new(side * .79f, 2.85f, 1.15f), new(.09f, 1.7f, .09f), "665d46");
            Models.Rod(this, new(side * .79f, 2.9f, 1.15f), new(side * .79f, 3.66f, -.42f), .035f, "625b48");
            Models.Box(this, new(side * 1.11f, 3.7f, .5f), new(.055f, .16f, 2.45f), "78684a");
        }
        Models.Box(this, new(0, 3.74f, .5f), new(2.25f, .12f, 2.45f), "bfa568");
        Models.Box(this, new(0, 3.67f, .5f), new(2.05f, .04f, 2.25f), "625b43");
        for (int rib = -2; rib <= 2; rib++)
            Models.Box(this, new(rib * .38f, 3.812f, .5f), new(.035f, .025f, 2.3f), "d0b77e");
        _blade = new Node3D { Name = "FrontBlade" }; AddChild(_blade);
        foreach (float side in new[] { -1f, 1f })
        {
            Models.Box(_blade, new(side * .86f, .7f, -1.1f), new(.16f, .22f, 4.2f), "b58b3e");
            Models.Rod(_blade, new(side * .77f, 1.55f, .28f), new(side * .89f, .78f, -1.45f), .075f, "967242");
            Models.Rod(_blade, new(side * .89f, .78f, -1.45f), new(side * .95f, .5f, -2.45f), .036f, "b7b8a6");
            Models.Rod(_blade, new(side * .92f, .64f, -2.68f), new(side * .92f, 2.64f, -2.32f), .075f, "a78145");
            Models.Rod(_blade, new(side * .78f, 1.6f, .2f), new(side * .8f, 1.32f, -.65f), .035f, "35372e");
        }
        Models.Box(_blade, new(0, 2.61f, -2.32f), new(2, .12f, .12f), "b18a48");
        // Open grille guard, characteristic of the photographed loader.
        for (int rail = -4; rail <= 4; rail++)
            Models.Box(_blade, new(rail * .2f, 1.35f, -2.62f), new(.025f, .9f, .035f), "655b40");
        for (int rail = 0; rail < 5; rail++)
            Models.Box(_blade, new(0, .94f + rail * .2f, -2.64f), new(1.75f, .025f, .035f), "655b40");
        Models.Box(_blade, new(0, .47f, -3.25f), new(2.8f, .85f, .18f), "b59450").RotationDegrees = new(-12, 0, 0);
        Models.Box(_blade, new(0, .09f, -3.45f), new(2.85f, .1f, .5f), "716b59");
        _harrow = new Node3D { Name = "Harrow" }; AddChild(_harrow);
        Models.Box(_harrow, new(0, .5f, 3.0f), new(.16f, .16f, 2.5f), "a78345");
        Models.Box(_harrow, new(0, .6f, 4), new(3.4f, .18f, 1.5f), "b19043");
        foreach (float x in new[] { -1.4f, -.7f, 0, .7f, 1.4f }) foreach (float z in new[] { 3.5f, 4.5f })
            Models.Cylinder(_harrow, new(x, .37f, z), .36f, .07f, "62594a").RotationDegrees = new(0, 15, 80);
        Equip(0);
        var recording = GD.Load<AudioStreamWav>("res://Audio/cbt2105-loop.wav");
        _engineRecording = recording;
        recording.LoopMode = AudioStreamWav.LoopModeEnum.Forward;
        recording.LoopBegin = 0;
        recording.LoopEnd = Mathf.RoundToInt((float)recording.GetLength() * recording.MixRate);
        EngineAudio = new AudioStreamPlayer3D {
            Name = "EngineAudio", Stream = recording, Position = new(0, 1.5f, -1),
            UnitSize = 7, MaxDistance = 100, VolumeDb = FarmAudio.EngineIdleDb, PitchScale = .78f
        };
        AddChild(EngineAudio);
        EngineAudio.Play();
    }

    public override void _ExitTree() => ReleaseAudio();

    public void ReleaseAudio()
    {
        if (GodotObject.IsInstanceValid(EngineAudio))
        {
            EngineAudio.Stop();
            EngineAudio.Stream = null;
        }
        _engineRecording?.Dispose();
        _engineRecording = null;
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
            var axle = Models.Cylinder(spin, new(side * (width / 2 + .04f), 0, 0), radius * .18f, .09f, "80775c");
            axle.RotationDegrees = new(0, 0, 90);
            for (int bolt = 0; bolt < 6; bolt++)
            {
                float a = bolt * Mathf.Tau / 6;
                Models.Box(spin, new(side * (width / 2 + .035f), Mathf.Cos(a) * radius * .31f, Mathf.Sin(a) * radius * .31f), new(.04f, .055f, .055f), "625e4d");
            }
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
        float throttle = Occupied && Fuel > 0 ? Farm.VehicleThrottle() : 0;
        Fuel = Mathf.Max(0, Fuel - dt * (.002f + Mathf.Abs(throttle) * .035f + (ToolLowered ? .02f : 0)));
        if (Fuel <= 0 && EngineAudio.Playing) EngineAudio.Stop();
        else if (Fuel > 0 && !EngineAudio.Playing) EngineAudio.Play();
        float load = Mathf.Clamp(Mathf.Abs(throttle) * .7f + Mathf.Abs(Speed) / 8 * .3f, 0, 1);
        EngineAudio.PitchScale = Mathf.Lerp(EngineAudio.PitchScale, .78f + load * .28f, 1 - Mathf.Exp(-3 * dt));
        EngineAudio.VolumeDb = Mathf.Lerp(EngineAudio.VolumeDb, FarmAudio.EngineIdleDb + load * 7, 1 - Mathf.Exp(-3 * dt));
        float steering = Occupied ? Input.GetAxis("right", "left") : 0;
        bool brake = !Occupied || Fuel <= 0 || Farm.VehicleBrake();
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
