using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ValeDasFlores;

public partial class WorldBuilder : Node3D
{
    public Farm Farm = null!;
    public bool Active { get; private set; }
    public int Mode { get; private set; } = 1;
    public float Radius { get; private set; } = 8;
    public float PropYaw { get; private set; }
    private float _yaw, _pitch = -.65f, _timer;
    private MeshInstance3D _cursor = null!;
    private Node3D _props = null!;
    private readonly List<(float[] Heights, List<PlacedProp> Props)> _undo = new();
    private bool _stroke;
    public string Hint => $"CONSTRUIR  •  {new[] { "", "ELEVAR", "BAIXAR", "SUAVIZAR", "NIVELAR", "ÁRVORE", "CERCA", "PEDRA", "REMOVER OBJETO" }[Mode]}  •  raio {Radius:0} m\n"
        + "1–4 relevo  •  5 árvore / 6 cerca / 7 pedra / 8 remover  •  clique aplica\nWASD voar  •  Q/E descer/subir  •  roda: raio  •  R girar  •  Z desfazer  •  F2 voltar  •  F5 salvar";
    public override void _Ready()
    {
        _props = new Node3D { Name = "PlacedProps" }; Farm.AddChild(_props);
        _cursor = new MeshInstance3D { Mesh = new TorusMesh { InnerRadius = .96f, OuterRadius = 1, Rings = 48, RingSegments = 6 },
            MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color("e8d784"), ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded }, Visible = false };
        Farm.AddChild(_cursor);
    }
    public void Toggle()
    {
        if (!Active && (Farm.Player.Driving || Mathf.Abs(Farm.Tractor.Speed) > .1f)) { Farm.Message("Desça do trator para construir."); return; }
        Active = !Active; _cursor.Visible = Active; _stroke = false;
        Farm.Activities.CancelFishing();
        Farm.Player.SetPhysicsProcess(!Active); Farm.Tractor.SetPhysicsProcess(!Active); Farm.Hilux.SetPhysicsProcess(!Active); Farm.Jeep.SetPhysicsProcess(!Active);
        Farm.Tractor.EngineAudio.StreamPaused = Active;
        if (Active)
        {
            Farm.Camera.GlobalPosition = Farm.Player.GlobalPosition + new Vector3(0, 18, 10);
            _yaw = Farm.Player.Yaw; _pitch = -.65f;
        }
        else
        {
            SnapActors(); Farm.SnapCamera();
        }
    }
    public void SnapActors()
    {
        foreach (var actor in new CharacterBody3D[] { Farm.Player, Farm.Tractor, Farm.Hilux, Farm.Jeep })
        {
            var p = actor.GlobalPosition; p.Y = Mathf.Max(p.Y, Farm.Terrain.HeightAt(p.X, p.Z) + .2f); actor.GlobalPosition = p; actor.Velocity = Vector3.Zero;
        }
    }
    public void HandleInput(InputEvent ev)
    {
        if (ev is InputEventMouseMotion mouse) { _yaw -= mouse.Relative.X * .003f; _pitch = Mathf.Clamp(_pitch - mouse.Relative.Y * .003f, -1.5f, -.05f); }
        if (ev is InputEventMouseButton button && button.Pressed)
        {
            if (button.ButtonIndex == MouseButton.WheelUp) Radius = Mathf.Min(20, Radius + 1);
            if (button.ButtonIndex == MouseButton.WheelDown) Radius = Mathf.Max(4, Radius - 1);
        }
        if (ev is InputEventKey key && key.Pressed && !key.Echo)
        {
            int number = (int)key.PhysicalKeycode - (int)Key.Key0;
            if (number >= 1 && number <= 8) Mode = number;
            if (key.PhysicalKeycode == Key.R) PropYaw += Mathf.Pi / 4;
            if (key.PhysicalKeycode == Key.Z) Undo();
        }
    }
    public void Tick(float dt)
    {
        if (!Active) return;
        var camera = Farm.Camera; camera.Rotation = new(_pitch, _yaw, 0);
        var move = Input.GetVector("left", "right", "forward", "back");
        float vertical = (Input.IsPhysicalKeyPressed(Key.E) ? 1 : 0) - (Input.IsPhysicalKeyPressed(Key.Q) ? 1 : 0);
        var p = camera.Position + (camera.Basis * new Vector3(move.X, 0, move.Y) + Vector3.Up * vertical) * dt * (Input.IsKeyPressed(Key.Shift) ? 40 : 18);
        p.X = Mathf.Clamp(p.X, -105, 105); p.Z = Mathf.Clamp(p.Z, -105, 105); p.Y = Mathf.Clamp(p.Y, Farm.Terrain.HeightAt(p.X, p.Z) + 2, 90); camera.Position = p;
        Vector3? target = null;
        for (float distance = 1; distance < 220; distance += .5f)
        {
            var hit = camera.Position - camera.Basis.Z * distance;
            if (Mathf.Abs(hit.X) < 105 && Mathf.Abs(hit.Z) < 105 && hit.Y <= Farm.Terrain.HeightAt(hit.X, hit.Z)) { target = new(hit.X, Farm.Terrain.HeightAt(hit.X, hit.Z) + .12f, hit.Z); break; }
        }
        _cursor.Visible = target.HasValue;
        if (target.HasValue) { _cursor.Position = target.Value; _cursor.Scale = new(Radius, .1f, Radius); }
        bool down = Input.IsMouseButtonPressed(MouseButton.Left);
        _timer -= dt;
        if (down && target.HasValue && (!_stroke || (_timer <= 0 && Mode <= 4)))
        {
            _stroke |= Apply(target.Value, !_stroke); _timer = .16f;
        }
        if (!down) _stroke = false;
    }
    public bool Apply(Vector3 center, bool remember = true)
    {
        if (!FarmTerrain.Editable(center.X, center.Z)) { Farm.Message("Sede, lago, estrada e limites ficam protegidos. Edite os pastos ao redor."); return false; }
        // Keep history only for edits that actually succeed; rejected clicks must not
        // evict useful undo entries or split a held brush stroke.
        var snapshot = remember ? ((float[])Farm.Terrain.Heights.Clone(), Farm.Activities.State.Props.Select(p => new PlacedProp { Kind = p.Kind, X = p.X, Z = p.Z, Yaw = p.Yaw }).ToList()) : default;
        if (Mode <= 4)
        {
            if (!Farm.Terrain.Brush(center, Radius, Mode)) return false;
        }
        else if (Mode == 8)
        {
            var nearest = Farm.Activities.State.Props.OrderBy(p => new Vector2(p.X - center.X, p.Z - center.Z).LengthSquared()).FirstOrDefault();
            if (nearest == null || new Vector2(nearest.X - center.X, nearest.Z - center.Z).Length() >= Radius) return false;
            Farm.Activities.State.Props.Remove(nearest);
        }
        else
        {
            if (Farm.Activities.State.Props.Count >= 200) { Farm.Message("Limite de 200 objetos construídos."); return false; }
            if (center.DistanceTo(Farm.Player.GlobalPosition) < 4 || center.DistanceTo(Farm.Tractor.GlobalPosition) < 6 || center.DistanceTo(Farm.Hilux.GlobalPosition) < 6 || center.DistanceTo(Farm.Jeep.GlobalPosition) < 6) { Farm.Message("Deixe espaço livre ao redor do personagem e do trator."); return false; }
            Farm.Activities.State.Props.Add(new PlacedProp { Kind = Mode, X = center.X, Z = center.Z, Yaw = PropYaw });
        }
        if (remember)
        {
            _undo.Add(snapshot);
            if (_undo.Count > 20) _undo.RemoveAt(0);
        }
        RefreshWorld(false); return true;
    }
    public void Undo()
    {
        if (_undo.Count == 0) { Farm.Message("Nada para desfazer."); return; }
        var snapshot = _undo[^1]; _undo.RemoveAt(_undo.Count - 1);
        Farm.Activities.State.Props = snapshot.Props; Farm.Terrain.SetHeights(snapshot.Heights); RefreshWorld(false);
    }
    public void ClearHistory() => _undo.Clear();
    public void RefreshWorld(bool rebuildWork = true)
    {
        FarmWorld.RefreshDecorationHeights(Farm.World, Farm.Terrain);
        foreach (var child in _props.GetChildren()) { _props.RemoveChild(child); child.QueueFree(); }
        foreach (var prop in Farm.Activities.State.Props)
        {
            var p = new Vector3(prop.X, Farm.Terrain.HeightAt(prop.X, prop.Z), prop.Z);
            if (prop.Kind == 5) FarmWorld.CerradoTree(_props, p, .9f, 31);
            else if (prop.Kind == 6)
            {
                var fence = new StaticBody3D { Position = p, Rotation = new(0, prop.Yaw, 0) }; _props.AddChild(fence);
                foreach (float x in new[] { -2f, 2f }) Models.Box(fence, new(x, .7f, 0), new(.15f, 1.4f, .15f), "aaa38c");
                foreach (float y in new[] { .5f, 1f }) Models.Box(fence, new(0, y, 0), new(4, .05f, .05f), "75634a");
                Models.Collider(fence, new(0, .7f, 0), new(4, 1.4f, .15f));
            }
            else Models.Solid(_props, p + Vector3.Up * .5f, new(1.6f, 1, 1.3f), "807c6c").Rotation = new(0, prop.Yaw, 0);
        }
        if (rebuildWork) Farm.Activities.RebuildWork();
        else Farm.Activities.RefreshWorkHeights();
    }
}
