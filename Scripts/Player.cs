using Godot;

namespace ValeDasFlores;

public partial class Player : CharacterBody3D
{
    public float Yaw;
    public float Pitch = -.08f;
    public bool Driving;
    public bool ThirdPerson;
    private Node3D _model = null!;
    private Node3D _head = null!;
    private Node3D _leftLeg = null!, _rightLeg = null!, _leftArm = null!, _rightArm = null!;
    private float _gaitWeight;
    private float _step;
    private float _stepDistance;
    private AudioStreamPlayer3D _footsteps = null!;
    private readonly AudioStreamWav[] _stepSounds = new AudioStreamWav[4];
    public int FootstepsPlayed { get; private set; }

    public override void _Ready()
    {
        Name = "Player";
        FloorSnapLength = .3f;
        AddChild(new CollisionShape3D { Position = new(0, .9f, 0), Shape = new CapsuleShape3D { Radius = .32f, Height = 1.8f } });
        _model = Models.Farmer(this);
        _head = _model.GetNode<Node3D>("Head");
        _leftLeg = _model.GetNode<Node3D>("LeftLeg"); _rightLeg = _model.GetNode<Node3D>("RightLeg");
        _leftArm = _model.GetNode<Node3D>("LeftArm"); _rightArm = _model.GetNode<Node3D>("RightArm");
        for (int i = 0; i < _stepSounds.Length; i++) _stepSounds[i] = FarmAudio.Footstep(2105 + i);
        _footsteps = new AudioStreamPlayer3D { Name = "Footsteps", UnitSize = 3, MaxDistance = 22, VolumeDb = FarmAudio.FootstepDb, MaxPolyphony = 2 };
        AddChild(_footsteps);
    }

    public override void _ExitTree() => ReleaseAudio();

    public void ReleaseAudio()
    {
        if (GodotObject.IsInstanceValid(_footsteps))
        {
            _footsteps.Stop();
            _footsteps.Stream = null;
        }
        // These WAVs are generated and owned by this player. Release their managed
        // references explicitly so shutdown does not depend on a later GC cycle.
        for (int i = 0; i < _stepSounds.Length; i++)
        {
            _stepSounds[i]?.Dispose();
            _stepSounds[i] = null!;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Driving) return;
        float dt = (float)delta;
        var input = Input.GetVector("left", "right", "forward", "back");
        var direction = new Vector3(input.X, 0, input.Y).Rotated(Vector3.Up, Yaw);
        float speed = Input.IsActionPressed("run") ? 6.5f : 3.7f;
        var velocity = Velocity;
        velocity.X = Mathf.MoveToward(velocity.X, direction.X * speed, 24 * dt);
        velocity.Z = Mathf.MoveToward(velocity.Z, direction.Z * speed, 24 * dt);
        velocity.Y = IsOnFloor() ? -.5f : velocity.Y - 22 * dt;
        if (IsOnFloor() && Input.IsActionJustPressed("jump")) velocity.Y = 7;
        Velocity = velocity;
        var before = GlobalPosition;
        MoveAndSlide();
        var travelled = GlobalPosition - before;
        if (IsOnFloor())
        {
            _stepDistance += new Vector2(travelled.X, travelled.Z).Length();
            if (_stepDistance >= .95f)
            {
                _stepDistance %= .95f;
                _footsteps.Stream = _stepSounds[FootstepsPlayed % _stepSounds.Length];
                _footsteps.PitchScale = 1 + (FootstepsPlayed % 3 - 1) * .055f;
                _footsteps.Play();
                FootstepsPlayed++;
            }
        }
        else _stepDistance = 0;
        Rotation = new(0, Yaw, 0);
        _head.Visible = ThirdPerson;
        float distance = new Vector2(travelled.X, travelled.Z).Length();
        _step += distance;
        _gaitWeight = Mathf.MoveToward(_gaitWeight, IsOnFloor() && distance > .001f ? Mathf.Min(1, distance / Mathf.Max(dt, .001f) / 5) : 0, dt * 7);
        float swing = Mathf.Sin(_step * 5.4f) * .7f * _gaitWeight;
        _leftLeg.Rotation = new(swing, 0, 0); _rightLeg.Rotation = new(-swing, 0, 0);
        _leftArm.Rotation = new(-swing * .75f, 0, 0); _rightArm.Rotation = new(swing * .75f, 0, 0);
        _model.Position = new(0, Mathf.Abs(Mathf.Sin(_step * 5.4f)) * .035f * _gaitWeight, 0);
    }

    public void SetDriving(bool driving)
    {
        Driving = driving;
        Visible = !driving;
        CollisionLayer = driving ? 0u : 1u;
        CollisionMask = driving ? 0u : 1u;
        Velocity = Vector3.Zero;
        _stepDistance = 0;
        _footsteps.Stop();
    }
}
