using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ValeDasFlores;

public partial class FarmActivities
{
    public const float CropGrowthSeconds = 180, MoundRespawnSeconds = 600;
    private readonly Dictionary<int, Node3D> _crops = new();
    public static Vector3 CellPosition(int id) => FieldStart + new Vector3(id / 64 * 26 + id % 8 * 2.5f, 0, id % 64 / 8 * 2.5f);
    // An expanded water ellipse includes the shore and the original signed spot.
    public static bool CanFishAt(Vector3 position)
    {
        var local = position - FarmWorld.LakeCenter;
        return new Vector2(local.X / 26, local.Z / 24).LengthSquared() <= 1 && position.Y < 4;
    }
    public int NearbyCell()
    {
        if (Farm.Player.Driving) return -1;
        int nearest = -1; float distance = 2.2f;
        foreach (var cell in _cells)
        {
            float candidate = cell.Value.GlobalPosition.DistanceTo(Farm.Player.GlobalPosition);
            if (candidate < distance) { nearest = cell.Key; distance = candidate; }
        }
        return nearest;
    }
    private string CropHint()
    {
        int id = NearbyCell();
        if (id < 0) return "";
        string key = Farm.Prompt("C", "\u25a1");
        if (State.CropAges.TryGetValue(id, out float age))
            return age >= CropGrowthSeconds ? $"{key}  colher milho  +R$12 / +8 XP" : $"Milho crescendo: {Mathf.CeilToInt(CropGrowthSeconds - age)} s";
        return State.TilledCells.Contains(id) ? $"{key}  plantar milho / 1 semente ({State.Seeds} disponiveis)" : "Prepare esta terra com a grade do trator antes de plantar.";
    }
    public bool CropAction()
    {
        int id = NearbyCell();
        if (id < 0) { Farm.Message("Aproxime-se a pe de um talhao para plantar ou colher."); return false; }
        if (State.CropAges.TryGetValue(id, out float age))
        {
            if (age < CropGrowthSeconds) { Farm.Message(CropHint()); return false; }
            State.CropAges.Remove(id); State.TilledCells.Remove(id);
            if (_crops.Remove(id, out var crop)) { crop.GetParent().RemoveChild(crop); crop.QueueFree(); }
            _cells[id].MaterialOverride = Models.Material("a29962");
            Reward(12, 8, "Milho colhido e vendido! Prepare a terra para plantar novamente."); return true;
        }
        if (!State.TilledCells.Contains(id)) { Farm.Message("Passe a grade neste pedaco de terra antes de plantar."); return false; }
        if (State.Seeds <= 0) { Farm.Message("Compre sementes na loja: 20 por R$ 40."); return false; }
        State.Seeds--; State.CropAges[id] = 0; BuildCrop(id);
        Farm.Message("Milho plantado! Cresce em 3 minutos de jogo ativo."); return true;
    }
    public void AdvanceFarmTime(float dt)
    {
        if (!float.IsFinite(dt) || dt <= 0 || Farm.Builder.Active || GetTree().Paused) return;
        foreach (int id in State.CropAges.Keys.ToArray())
        {
            State.CropAges[id] = Mathf.Min(CropGrowthSeconds, State.CropAges[id] + dt);
            UpdateCrop(id);
        }
        bool respawned = false;
        foreach (int id in State.ClearedMounds.ToArray())
        {
            State.MoundAges[id] = Mathf.Min(MoundRespawnSeconds, State.MoundAges[id] + dt);
            if (State.MoundAges[id] < MoundRespawnSeconds) continue;
            var p = Ground(MoundPosition(id));
            if (p.DistanceTo(Farm.Player.GlobalPosition) < 5 || p.DistanceTo(Farm.Tractor.GlobalPosition) < 6
                || (Farm.Hilux != null && p.DistanceTo(Farm.Hilux.GlobalPosition) < 6)
                || (Farm.Jeep != null && p.DistanceTo(Farm.Jeep.GlobalPosition) < 6)) continue;
            State.ClearedMounds.Remove(id); State.MoundAges[id] = 0; respawned = true;
        }
        // Rebuild only on the rare respawn, never on every growth tick.
        if (respawned) RebuildWork();
    }
    private void RebuildCrops()
    {
        _crops.Clear();
        foreach (int id in State.CropAges.Keys) BuildCrop(id);
    }
    private void BuildCrop(int id)
    {
        var crop = new Node3D { Name = $"Milho{id}", Position = Ground(CellPosition(id)) }; _fieldRoot.AddChild(crop); _crops[id] = crop;
        foreach (float x in new[] { -.6f, .6f }) foreach (float z in new[] { -.6f, .6f })
        {
            Models.Box(crop, new(x, .65f, z), new(.06f, 1.3f, .06f), "648042");
            Models.Box(crop, new(x, .58f, z), new(.55f, .055f, .18f), "749447").RotationDegrees = new(0, 0, 25);
            Models.Box(crop, new(x, .95f, z), new(.18f, .055f, .5f), "7f9b4d").RotationDegrees = new(25, 0, 0);
            Models.Box(crop, new(x + .1f, .9f, z), new(.12f, .32f, .13f), "c7aa50");
        }
        UpdateCrop(id);
    }
    private void UpdateCrop(int id)
    {
        if (!_crops.TryGetValue(id, out var crop)) return;
        float growth = .15f + .85f * State.CropAges[id] / CropGrowthSeconds;
        crop.Scale = new(1, growth, 1);
    }
    private void RefreshCropHeights()
    {
        foreach (var crop in _crops) crop.Value.Position = Ground(CellPosition(crop.Key));
    }
}
