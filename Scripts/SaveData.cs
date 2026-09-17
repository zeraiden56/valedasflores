using Godot;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;

namespace ValeDasFlores;

public sealed class PlacedProp
{
    public int Kind { get; set; }
    public float X { get; set; }
    public float Z { get; set; }
    public float Yaw { get; set; }
}

public sealed class SaveData
{
    public int Version { get; set; } = 1;
    public string SavedAt { get; set; } = "";
    public int Money { get; set; } = 300;
    public int Xp { get; set; }
    public float Fuel { get; set; } = 30;
    public int Tool { get; set; }
    public bool HarrowOwned { get; set; }
    public bool RodOwned { get; set; }
    public int Bait { get; set; }
    public int Seeds { get; set; }
    public Dictionary<int, float> CropAges { get; set; } = new();
    public float[] MoundAges { get; set; } = new float[12];
    public int Fish { get; set; }
    public bool MoundBonus { get; set; }
    public bool FieldBonus { get; set; }
    public List<int> ClearedMounds { get; set; } = new();
    public List<int> TilledCells { get; set; } = new();
    public float[] PlayerPosition { get; set; } = { -10, .1f, 18 };
    public float PlayerYaw { get; set; } = -.5f;
    public float[] TractorPosition { get; set; } = { -4, .15f, 2 };
    public float TractorYaw { get; set; }
    public bool Driving { get; set; }
    public int DrivenVehicle { get; set; } // 0: CBT (legacy), 1: Hilux, 2: Willys.
    public float[] HiluxPosition { get; set; } = { 34, .2f, 18 };
    public float HiluxYaw { get; set; } = Mathf.Pi / 2;
    public bool HiluxLights { get; set; }
    public float[] JeepPosition { get; set; } = { 40, .2f, 20 };
    public float JeepYaw { get; set; } = Mathf.Pi / 2;
    public bool JeepLights { get; set; }
    public bool JeepRoof { get; set; } = true;
    public bool Lights { get; set; }
    public float[] Heights { get; set; } = FarmTerrain.CreateInitialHeights();
    public List<PlacedProp> Props { get; set; } = new();

    public void Validate()
    {
        static bool PositionValid(float[]? p) => p is { Length: 3 } && p.All(float.IsFinite) && Math.Abs(p[0]) <= 108 && Math.Abs(p[2]) <= 108 && p[1] >= -20 && p[1] <= 100;
        if (Version != 1 || Money < 0 || Money > 10000000 || Xp < 0 || Xp > 10000000 || !float.IsFinite(Fuel) || Fuel < 0 || Fuel > 80 || Tool < 0 || Tool > 2 || (Tool == 2 && !HarrowOwned)
            || Bait < 0 || Bait > 100000 || Fish < 0 || Fish > 100000 || !PositionValid(PlayerPosition) || !PositionValid(TractorPosition)
            || !float.IsFinite(PlayerYaw) || !float.IsFinite(TractorYaw)
            || DrivenVehicle < 0 || DrivenVehicle > 2 || !PositionValid(HiluxPosition) || !PositionValid(JeepPosition)
            || !float.IsFinite(HiluxYaw) || !float.IsFinite(JeepYaw)
            || Heights == null || Heights.Length != FarmTerrain.Count * FarmTerrain.Count || Heights.Any(h => !float.IsFinite(h) || h < -8 || h > 12)
            || ClearedMounds == null || ClearedMounds.Count > 12 || ClearedMounds.Any(i => i < 0 || i >= 12) || ClearedMounds.Distinct().Count() != ClearedMounds.Count
            || TilledCells == null || TilledCells.Count > 192 || TilledCells.Any(i => i < 0 || i >= 192) || TilledCells.Distinct().Count() != TilledCells.Count
            || Seeds < 0 || Seeds > 100000 || CropAges == null || CropAges.Count > 192
            || CropAges.Any(c => c.Key < 0 || c.Key >= 192 || !TilledCells.Contains(c.Key) || !float.IsFinite(c.Value) || c.Value < 0 || c.Value > FarmActivities.CropGrowthSeconds)
            || MoundAges == null || MoundAges.Length != 12 || MoundAges.Any(a => !float.IsFinite(a) || a < 0 || a > FarmActivities.MoundRespawnSeconds)
            || Props == null || Props.Count > 200 || Props.Any(p => p == null || p.Kind < 5 || p.Kind > 7 || !float.IsFinite(p.X) || !float.IsFinite(p.Z) || !float.IsFinite(p.Yaw) || Math.Abs(p.X) > 95 || Math.Abs(p.Z) > 95))
            throw new InvalidDataException("Save inválido ou de uma versão incompatível.");
    }
}

public sealed class SaveStore
{
    private readonly string _directory;
    public SaveStore(string? directory = null) => _directory = directory ?? ProjectSettings.GlobalizePath("user://saves");
    public string SlotPath(int slot)
    {
        if (slot < 1 || slot > 3) throw new ArgumentOutOfRangeException(nameof(slot));
        return Path.Combine(_directory, $"slot-{slot}.json");
    }
    public bool Exists(int slot) => File.Exists(SlotPath(slot));
    public SaveData Load(int slot)
    {
        string path = SlotPath(slot);
        if (new FileInfo(path).Length > 2000000) throw new InvalidDataException("Arquivo de save muito grande.");
        var data = JsonSerializer.Deserialize<SaveData>(File.ReadAllText(path)) ?? throw new InvalidDataException("Save vazio.");
        data.Validate();
        return data;
    }
    public void Save(int slot, SaveData data)
    {
        data.Validate();
        Directory.CreateDirectory(_directory);
        string path = SlotPath(slot), temporary = path + ".tmp";
        data.SavedAt = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
        File.WriteAllText(temporary, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
        // Replace atomically on the same volume, keeping the previous successful save.
        if (File.Exists(path)) File.Replace(temporary, path, path + ".bak", true);
        else File.Move(temporary, path);
    }
}
