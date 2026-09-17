using Godot;
using System;
using System.Threading.Tasks;

namespace ValeDasFlores;

// Uses a temporary raised arena and an isolated save directory, never user slots.
public partial class VehicleTest : Node
{
    public Farm Farm = null!;
    private int _checks;
    private void Check(bool ok, string text) { if (!ok) throw new InvalidOperationException(text); _checks++; GD.Print("PASS: " + text); }
    private async Task Frames(int n = 2) { for (int i = 0; i < n; i++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame); }
    private static void Button(JoyButton button, bool pressed)
    {
        using var ev = new InputEventJoypadButton { Device = 0, ButtonIndex = button, Pressed = pressed };
        Input.ParseInputEvent(ev);
    }
    private static void Brake(float value)
    {
        using var ev = new InputEventJoypadMotion { Device = 0, Axis = JoyAxis.TriggerLeft, AxisValue = value };
        Input.ParseInputEvent(ev);
    }
    private async Task Tap(JoyButton button) { Button(button, true); await Frames(); Button(button, false); await Frames(); }
    public override async void _Ready()
    {
        try
        {
            Farm.Saves = new SaveStore(ProjectSettings.GlobalizePath("user://vehicle-tests/" + Guid.NewGuid().ToString("N")));
            Farm.SetPaused(false);
            var arena = new StaticBody3D { Name = "TestArena", Position = new(0, 19.5f, 55) }; Farm.AddChild(arena);
            Models.Collider(arena, Vector3.Zero, new(70, 1, 70));
            Farm.Hilux.Position = new(-12, 20.2f, 60); Farm.Hilux.Rotation = Vector3.Zero;
            Farm.Jeep.Position = new(12, 20.2f, 60); Farm.Jeep.Rotation = Vector3.Zero;
            Farm.Player.Position = new(-8, 20.1f, 60);
            await Frames(30);
            Check(Farm.Hilux.IsOnFloor() && Farm.Jeep.IsOnFloor(), "Hilux e Willys apoiados no mundo fisico");
            int slot = 0;
            foreach (var vehicle in new Hilux[] { Farm.Hilux, Farm.Jeep })
            {
                slot++; string name = vehicle == Farm.Jeep ? "Willys" : "Hilux";
                Farm.Player.Position = vehicle.ToGlobal(new(2.7f, .3f, 0)); Farm.Player.Velocity = Vector3.Zero;
                await Frames(10);
                Check(Farm.CanEnter(), name + " acessivel pela lateral");
                var blocker = new StaticBody3D { Position = vehicle.ToGlobal(new(1.55f, 1, 0)) }; Farm.AddChild(blocker);
                Models.Collider(blocker, Vector3.Zero, new(.15f, 3, 2)); await Frames();
                Check(!Farm.CanEnter(), name + " nao permite entrar atraves de parede");
                blocker.QueueFree(); await Frames();
                if (vehicle == Farm.Jeep)
                    Check(Farm.ChangeJeepRoof() && !Farm.Jeep.RoofOn, "Willys permite retirar capota parado a pe");
                await Tap(JoyButton.Y);
                Check(Farm.Player.Driving && Farm.ActiveVehicle == vehicle && vehicle.Occupied && !Farm.Tractor.Occupied, name + " recebe motorista via triangulo");
                await Tap(JoyButton.RightStick);
                Check(Farm.Camera.GlobalPosition.DistanceTo(vehicle.Seat) < .1f, name + " camera interna usa o banco correto");
                await Tap(JoyButton.DpadUp);
                Check(vehicle.LightsOn, name + " direcional aciona farois");
                await Tap(JoyButton.RightStick);
                Check(vehicle.Driver.Visible, name + " motorista aparece na camera externa");
                var initial = vehicle.Position;
                Button(JoyButton.A, true); await Frames(90); Button(JoyButton.A, false);
                Check(vehicle.Speed > 3 && vehicle.Position.Z < initial.Z - 2, name + " acelera com X");
                Check(!Farm.Interact() && Farm.Player.Driving, name + " impede desembarque em movimento");
                if (vehicle == Farm.Jeep)
                    Check(!Farm.ChangeJeepRoof() && !Farm.Jeep.RoofOn, "Willys bloqueia troca de capota em movimento");
                Brake(1); await Frames(45);
                Check(Mathf.Abs(vehicle.Speed) < .05f, name + " L2 freia ate parar");
                Brake(0);
                Button(JoyButton.B, true); await Frames(50); Button(JoyButton.B, false);
                Check(vehicle.Speed < -2 && !GetTree().Paused, name + " circulo engata re sem abrir menu");
                Brake(1); await Frames(40); Brake(0);
                var hiluxPosition = Farm.Hilux.Position; var jeepPosition = Farm.Jeep.Position;
                Check(Farm.SaveSlot(slot), name + " grava save isolado ao volante");
                var saved = Farm.Saves.Load(slot);
                Check(saved.Driving && saved.DrivenVehicle == slot && saved.JeepRoof == Farm.Jeep.RoofOn, name + " save identifica motorista e capota");
                Farm.Hilux.Position += Vector3.Right * 5; Farm.Jeep.Position += Vector3.Left * 5;
                Farm.Jeep.ToggleRoof(); vehicle.ToggleLights();
                Check(Farm.LoadSlot(slot) && Farm.ActiveVehicle == vehicle && vehicle.Occupied && vehicle.LightsOn
                    && Farm.Jeep.RoofOn == saved.JeepRoof, name + " carregar restaura motorista farois e capota");
                Check(Farm.Hilux.Position.DistanceTo(hiluxPosition) < .01f && Farm.Jeep.Position.DistanceTo(jeepPosition) < .01f,
                    name + " save restaura posicoes independentes dos dois veiculos");
                await Frames(); await Tap(JoyButton.Y);
                Check(!Farm.Player.Driving && !vehicle.Occupied, name + " triangulo desembarca quando parado");
            }
            var legacy = System.Text.Json.JsonSerializer.Deserialize<SaveData>("{\"Driving\":true}")!; legacy.Validate();
            Check(legacy.DrivenVehicle == 0 && legacy.JeepRoof && legacy.HiluxPosition[0] == 34 && legacy.JeepPosition[0] == 40,
                "Save legado conserva CBT e recebe defaults dos novos veiculos");
            GD.Print($"VEHICLE OK: {_checks} verificacoes"); Farm.QuitGame();
        }
        catch (Exception ex)
        {
            Button(JoyButton.A, false); Button(JoyButton.B, false); Brake(0);
            GD.PushError("VEHICLE FAILED: " + ex); Farm.QuitGame(1);
        }
    }
}
