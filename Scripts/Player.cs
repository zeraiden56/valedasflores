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
    private float _step;

    public override void _Ready()
    {
        Name = "Player";
        FloorSnapLength = .3f;
        AddChild(new CollisionShape3D { Position = new(0, .9f, 0), Shape = new CapsuleShape3D { Radius = .32f, Height = 1.8f } });
        _model = Models.Farmer(this);
        _head = _model.GetNode<Node3D>("Head");
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
        MoveAndSlide();
        Rotation = new(0, Yaw, 0);
        _head.Visible = ThirdPerson;
        _step += new Vector2(Velocity.X, Velocity.Z).Length() * dt;
        _model.Position = new(0, IsOnFloor() ? Mathf.Sin(_step * 3) * .015f : 0, 0);
    }

    public void SetDriving(bool driving)
    {
        Driving = driving;
        Visible = !driving;
        CollisionLayer = driving ? 0u : 1u;
        CollisionMask = driving ? 0u : 1u;
        Velocity = Vector3.Zero;
    }
}
