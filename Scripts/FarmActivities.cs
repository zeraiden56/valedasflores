using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ValeDasFlores;

public partial class FarmActivities : Node3D
{
    public Farm Farm = null!;
    public SaveData State = new();
    public static readonly Vector3 Pump = new(-15, 0, 10), Shop = new(-19, 0, 7), FishingSpot = new(30, 0, -20);
    public static readonly Vector3 FieldStart = new(-38, 0, -81);
    public readonly Dictionary<int, StaticBody3D> Mounds = new();
    private readonly Dictionary<int, MeshInstance3D> _cells = new();
    private ShaderMaterial _tilledMaterial = null!;
    private Node3D _moundRoot = null!, _fieldRoot = null!, _rod = null!;
    private float _fishingTime = -1, _biteAt;
    private Vector3 _castPosition;
    private readonly RandomNumberGenerator _random = new();
    public bool Fishing => _fishingTime >= 0;
    public bool Bite => Fishing && _fishingTime >= _biteAt && _fishingTime <= _biteAt + 2.5f;
    public int Level => 1 + State.Xp / 200;
    public string Objective => !State.MoundBonus ? $"Limpar cupinzeiros: {State.ClearedMounds.Count}/12  +R$ 200 ao concluir"
        : !State.FieldBonus ? $"Preparar talhao 1: {State.TilledCells.Count(i => i < 64)}/64  +R$ 250 ao concluir"
        : $"3 talhoes / {State.CropAges.Count} plantados / {State.Seeds} sementes";
    public override void _Ready()
    {
        _random.Randomize();
        _tilledMaterial = new ShaderMaterial { Shader = GD.Load<Shader>("res://Shaders/tilled_ground.gdshader") };
        _moundRoot = new Node3D { Name = "Cupinzeiros" }; AddChild(_moundRoot);
        _fieldRoot = new Node3D { Name = "Talhao" }; AddChild(_fieldRoot);
        BuildServices(); RebuildWork();
        _rod = new Node3D { Name = "FishingRod", Visible = false }; Farm.Camera.AddChild(_rod);
        Models.Cylinder(_rod, new(.4f, -.25f, -1), .015f, 1.6f, "8e7146").RotationDegrees = new(-65, 0, -10);
        Models.Box(_rod, new(.4f, -.7f, -1.7f), new(.005f, 1.6f, .005f), "b8bba7");
    }
    public static Vector3 MoundPosition(int i) => new(-35 + i % 3 * 6, 0, -17 - i / 3 * 9);
    public Vector3 Ground(Vector3 p) => new(p.X, Farm.Terrain.HeightAt(p.X, p.Z), p.Z);
    public void RebuildWork()
    {
        foreach (var child in _moundRoot.GetChildren()) { _moundRoot.RemoveChild(child); child.QueueFree(); }
        foreach (var child in _fieldRoot.GetChildren()) { _fieldRoot.RemoveChild(child); child.QueueFree(); }
        Mounds.Clear(); _cells.Clear();
        for (int i = 0; i < 12; i++)
        {
            if (State.ClearedMounds.Contains(i)) continue;
            var body = new StaticBody3D { Position = Ground(MoundPosition(i)) }; _moundRoot.AddChild(body);
            float height = 1.4f + i % 3 * .35f;
            var material = new ShaderMaterial { Shader = GD.Load<Shader>("res://Shaders/termite_mound.gdshader") };
            body.AddChild(new MeshInstance3D { Position = new(0, height * .43f, 0),
                Mesh = new SphereMesh { Radius = .85f, Height = height * 1.12f, RadialSegments = 16, Rings = 10 }, MaterialOverride = material });
            body.AddChild(new MeshInstance3D { Position = new(.4f, .42f, .15f),
                Mesh = new SphereMesh { Radius = .5f, Height = 1.1f, RadialSegments = 12, Rings = 8 }, MaterialOverride = material });
            Models.Collider(body, new(0, height / 2, 0), new(1.35f, height, 1.35f));
            Mounds[i] = body;
        }
        for (int id = 0; id < 192; id++)
        {
            var p = Ground(CellPosition(id));
            var cell = Models.Box(_fieldRoot, p + Vector3.Up * .025f, new(2.45f, .04f, 2.45f), State.TilledCells.Contains(id) ? "675039" : "a29962");
            if (State.TilledCells.Contains(id)) cell.MaterialOverride = _tilledMaterial;
            _cells[id] = cell;
        }
        RebuildCrops();
        for (int field = 1; field < 3; field++) Models.Sign(_fieldRoot, Ground(CellPosition(field * 64) + new Vector3(8, 0, 21)) + Vector3.Up * 2, $"TALHAO {field + 1}", 38);
        Models.Sign(_fieldRoot, Ground(new(-29, 0, -57)) + Vector3.Up * 2, "TALHÃO • GRADE", 42);
        Models.Sign(_moundRoot, Ground(new(-29, 0, -10)) + Vector3.Up * 2.4f, "CUPINZEIROS • PÁ FRONTAL", 35);
    }
    // Sculpting changes elevations, not mission state. Preserve colliders and materials
    // instead of destroying and recreating every mound and field cell each brush tick.
    public void RefreshWorkHeights()
    {
        foreach (var pair in Mounds) pair.Value.Position = Ground(MoundPosition(pair.Key));
        foreach (var pair in _cells)
            pair.Value.Position = Ground(CellPosition(pair.Key)) + Vector3.Up * .025f;
        RefreshCropHeights();
        foreach (var root in new[] { _moundRoot, _fieldRoot })
            foreach (var child in root.GetChildren())
                if (child is Label3D sign)
                {
                    var p = sign.Position;
                    p.Y = Farm.Terrain.HeightAt(p.X, p.Z) + (root == _moundRoot ? 2.4f : 2);
                    sign.Position = p;
                }
    }
    public override void _PhysicsProcess(double delta)
    {
        if (Farm.Builder.Active) return;
        AdvanceFarmTime((float)delta);
        UpdateWork();
        if (!Fishing) return;
        if (Farm.Player.Driving || Farm.Player.GlobalPosition.DistanceTo(_castPosition) > 1.5f) { CancelFishing(); Farm.Message("Pescaria cancelada ao se afastar."); return; }
        bool wasBiting = Bite; _fishingTime += (float)delta;
        if (!wasBiting && Bite) Farm.Message($"FISGOU! Aperte {Farm.Prompt("P", "\u25a1")} agora para recolher!");
        if (_fishingTime > _biteAt + 2.5f) { CancelFishing(); Farm.Message($"O peixe escapou. Aperte {Farm.Prompt("P", "\u25a1")} para tentar novamente."); }
    }
    public void UpdateWork()
    {
        var tractor = Farm.Tractor;
        if (!tractor.Occupied || !tractor.IsOnFloor() || tractor.Fuel <= 0 || !tractor.ToolLowered || tractor.Speed < .35f) return;
        if (tractor.Tool == 1)
        {
            foreach (var pair in new List<KeyValuePair<int, StaticBody3D>>(Mounds))
            {
                var p = tractor.ToLocal(pair.Value.GlobalPosition);
                if (Mathf.Abs(p.X) < 2 && p.Z < -2 && p.Z > -4.6f && Mathf.Abs(p.Y) < 1.5f) ClearMound(pair.Key);
            }
        }
        if (tractor.Tool == 2 && State.HarrowOwned)
        {
            var p = tractor.ToGlobal(new(0, 0, 4));
            foreach (var pair in _cells)
                if (new Vector2(pair.Value.GlobalPosition.X - p.X, pair.Value.GlobalPosition.Z - p.Z).Length() < 2.2f && Mathf.Abs(pair.Value.GlobalPosition.Y - p.Y) < 1.5f && !State.TilledCells.Contains(pair.Key))
                {
                    State.TilledCells.Add(pair.Key); pair.Value.MaterialOverride = _tilledMaterial;
                    State.Money += 4; State.Xp += 3;
                }
            if (State.TilledCells.Count(i => i < 64) == 64 && !State.FieldBonus) { State.FieldBonus = true; Reward(250, 150, "Talhão preparado!"); }
        }
    }
    private void ClearMound(int id)
    {
        if (!Mounds.Remove(id, out var mound) || State.ClearedMounds.Contains(id)) return;
        mound.CollisionLayer = 0; mound.QueueFree(); State.ClearedMounds.Add(id); State.MoundAges[id] = 0;
        Reward(40, 25, "Cupinzeiro derrubado!");
        if (State.ClearedMounds.Count == 12 && !State.MoundBonus) { State.MoundBonus = true; Reward(200, 150, "Área livre de cupinzeiros!"); }
    }
    private void Reward(int money, int xp, string message) { State.Money += money; State.Xp += xp; Farm.Message($"{message}  +R$ {money}  +{xp} XP"); }
    public bool Refuel()
    {
        var tractor = Farm.Tractor;
        if ((Farm.Player.Driving && !Farm.DrivingTractor) || tractor.GlobalPosition.DistanceTo(Pump) > 7 || Mathf.Abs(tractor.Speed) > .1f || (!Farm.Player.Driving && Farm.Player.GlobalPosition.DistanceTo(Pump) > 4))
        { Farm.Message($"Leve o trator ao tambor de diesel e pare. {Farm.Prompt("G", "\u25a1")} para abastecer."); return false; }
        int liters = Math.Min(10, Math.Min((int)Math.Floor(80 - tractor.Fuel), State.Money / 6));
        if (liters < 1) { Farm.Message("Tanque cheio ou saldo insuficiente (R$ 6/L)."); return false; }
        State.Money -= liters * 6; tractor.Fuel += liters; Farm.Message($"Abastecido: +{liters} L  •  R$ {liters * 6}"); return true;
    }
    public void RescueTractor()
    {
        if (Farm.Builder.Active) Farm.Builder.Toggle();
        CancelFishing();
        int fee = Math.Min(State.Money, 50); State.Money -= fee;
        Farm.Hilux.StopMotion(); Farm.Hilux.Occupied = false;
        Farm.Jeep.StopMotion(); Farm.Jeep.Occupied = false;
        Farm.Tractor.StopMotion(); Farm.Tractor.Occupied = false; Farm.Player.SetDriving(false);
        Farm.Tractor.Position = Pump + new Vector3(3, .2f, 0); Farm.Tractor.Rotation = Vector3.Zero;
        Farm.Tractor.Fuel = Mathf.Max(Farm.Tractor.Fuel, 5);
        Farm.Player.Position = Pump + new Vector3(5.5f, .2f, 1); Farm.Player.Yaw = .8f;
        Farm.SnapCamera(); Farm.SetPaused(false); Farm.Message($"Socorro concluído: trator na sede e ao menos 5 L. Custo R$ {fee}.");
    }
    public bool Buy(int item)
    {
        if (item < 0 || item > 3) return false;
        if (Farm.Player.Driving || Farm.Player.GlobalPosition.DistanceTo(Shop) > 4) { Farm.Message("Vá a pé ao balcão LOJA, ao lado do diesel."); return false; }
        int price = item == 0 ? 220 : item == 1 ? 100 : item == 3 ? 40 : 20;
        if ((item == 0 && State.HarrowOwned) || (item == 1 && State.RodOwned)) { Farm.Message("Você já possui esse equipamento."); return false; }
        if (State.Money < price) { Farm.Message("Saldo insuficiente."); return false; }
        State.Money -= price;
        if (item == 0) State.HarrowOwned = true; else if (item == 1) State.RodOwned = true; else if (item == 2) State.Bait += 5; else State.Seeds += 20;
        Farm.Message("Compra concluída!"); return true;
    }
    public void SellFish()
    {
        if (Farm.Player.Driving || Farm.Player.GlobalPosition.DistanceTo(Shop) > 4 || State.Fish == 0) { Farm.Message("Leve seus peixes à loja para vender."); return; }
        int total = State.Fish * 35; State.Money += total; State.Fish = 0; Farm.Message($"Peixes vendidos por R$ {total}.");
    }
    public void FishAction()
    {
        if (Fishing)
        {
            bool success = Bite; CancelFishing();
            if (success) { State.Fish++; State.Xp += 20; Farm.Message("Peixe pescado! +20 XP. Venda na loja por R$ 35."); }
            else Farm.Message("Recolheu cedo demais. Espere a indicação FISGOU.");
            return;
        }
        if (Farm.Player.Driving || !CanFishAt(Farm.Player.GlobalPosition)) { Farm.Message("Vá a pé ao lago para pescar."); return; }
        if (!State.RodOwned || State.Bait <= 0) { Farm.Message("Compre vara e iscas na loja."); return; }
        State.Bait--; _fishingTime = 0; _biteAt = _random.RandfRange(3, 6); _castPosition = Farm.Player.GlobalPosition;
        _rod.Visible = true; Farm.Message($"Linha lan\u00e7ada. Espere FISGOU e aperte {Farm.Prompt("P", "\u25a1")}.");
    }
    public void CancelFishing() { _fishingTime = -1; if (_rod != null) _rod.Visible = false; }
    public string NearbyService()
    {
        if (Fishing) return "fish";
        var player = Farm.Player;
        if (!player.Driving && player.GlobalPosition.DistanceTo(Shop) <= 4) return "shop";
        if (!player.Driving && CanFishAt(player.GlobalPosition)) return "fish";
        if (Farm.Tractor.GlobalPosition.DistanceTo(Pump) <= 7 &&
            (Farm.DrivingTractor || (!player.Driving && player.GlobalPosition.DistanceTo(Pump) <= 4))) return "fuel";
        if (!player.Driving && NearbyCell() >= 0) return "crop";
        return "";
    }
    public string ContextHint()
    {
        string fishKey = Farm.Prompt("P", "\u25a1");
        if (Fishing) return Bite ? $"{fishKey}  RECOLHER AGORA!" : $"Pescando... espere o peixe fisgar. {fishKey} recolhe a linha.";
        return NearbyService() switch
        {
            "shop" => $"{Farm.Prompt("B", "\u25a1")}  abrir loja  \u2022  grade, vara, iscas e venda de peixes",
            "fuel" => $"{Farm.Prompt("G", "\u25a1")}  abastecer at\u00e9 10 L  \u2022  R$ 6/L  \u2022  trator parado",
            "fish" => $"{fishKey}  pescar  \u2022  precisa de vara e isca",
            "crop" => CropHint(),
            _ => ""
        };
    }
    private void BuildServices()
    {
        var fuel = new Node3D { Position = Pump }; AddChild(fuel);
        Models.Cylinder(fuel, new(0, .8f, 0), .65f, 1.6f, "ad713c");
        Models.Solid(fuel, new(0, .8f, 0), new(1, 1.6f, 1), "ad713c");
        Models.Box(fuel, new(.7f, 1.1f, 0), new(.12f, .7f, .12f), "343a31");
        Models.Sign(fuel, new(0, 2.5f, 0), "DIESEL\nR$ 6 / litro", 38);
        Models.Solid(this, Shop + new Vector3(0, .65f, 0), new(2.5f, 1.3f, 1), "7c6342");
        Models.Sign(this, Shop + Vector3.Up * 2.5f, "LOJA", 45);
        Models.Sign(this, FishingSpot + new Vector3(3, 1.7f, 0), "PESCA", 42);

    }
}
