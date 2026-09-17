using Godot;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ValeDasFlores;

// Synthetic controller events exercise the same bindings and UI route as a pad.
// No save action is sent; player save slots are never read or written here.
public partial class InputTest : Node
{
    public Farm Farm = null!;
    private int _checks;
    private void Check(bool ok, string text)
    {
        if (!ok) throw new InvalidOperationException(text);
        _checks++; GD.Print("PASS: " + text);
    }
    private async Task Frames(int count = 2)
    {
        for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
    }
    private static bool HasButton(string action, JoyButton button) => InputMap.ActionGetEvents(action)
        .Any(e => e is InputEventJoypadButton b && b.ButtonIndex == button);
    private static bool HasAxis(string action, JoyAxis axis, float value) => InputMap.ActionGetEvents(action)
        .Any(e => e is InputEventJoypadMotion m && m.Axis == axis && m.AxisValue == value);
    private void PadMode()
    {
        using var ev = new InputEventJoypadButton { Device = 0, ButtonIndex = JoyButton.X, Pressed = true };
        Farm._Input(ev);
    }
    private static void SendButton(JoyButton button, bool pressed)
    {
        using var ev = new InputEventJoypadButton { Device = 0, ButtonIndex = button, Pressed = pressed };
        Input.ParseInputEvent(ev);
    }
    public override async void _Ready()
    {
        try
        {
            Farm.SetPaused(false); await Frames(12);
            var a = Farm.Activities; var player = Farm.Player; var tractor = Farm.Tractor;
            Check(HasAxis("forward", JoyAxis.LeftY, -1) && HasAxis("right", JoyAxis.LeftX, 1), "Analogico esquerdo ligado ao movimento");
            Check(HasAxis("look_up", JoyAxis.RightY, -1) && HasAxis("look_right", JoyAxis.RightX, 1), "Analogico direito ligado a camera");
            Check(HasButton("interact", JoyButton.Y) && HasButton("context", JoyButton.X), "Triangulo interage e quadrado executa servico");
            Check(HasButton("cycle_tool", JoyButton.LeftShoulder) && HasButton("lower_tool", JoyButton.RightShoulder), "L1 e R1 ligados aos implementos");
            Check(HasButton("pause", JoyButton.Start) && HasButton("quick_save", JoyButton.Back), "Options e Share ligados a pausa e save");
            Check(HasButton("ui_accept", JoyButton.A) && HasButton("ui_cancel", JoyButton.B), "Cruz confirma e circulo volta nos menus");
            Check(InputMap.ActionGetDeadzone("look_right") >= .2f, "Zona morta evita deriva na camera");
            using (var key = new InputEventKey { PhysicalKeycode = Key.W, Pressed = true }) Farm._Input(key);
            Check(!Farm.UsingGamepad && Farm.Prompt("B", "\u25a1") == "B", "Teclado seleciona dicas de teclado");
            using (var drift = new InputEventJoypadMotion { Device = 0, Axis = JoyAxis.RightX, AxisValue = .1f }) Farm._Input(drift);
            Check(!Farm.UsingGamepad, "Pequena deriva nao troca dispositivo das dicas");
            PadMode();
            Check(Farm.UsingGamepad && Farm.Prompt("B", "\u25a1") == "\u25a1", "Botao seleciona dicas do controle");
            using (var mouse = new InputEventMouseMotion { Relative = new Vector2(5, 0) }) Farm._Input(mouse);
            Check(!Farm.UsingGamepad, "Movimento do mouse restaura dicas de teclado");
            PadMode();
            player.Position = FarmActivities.Shop + new Vector3(0, .1f, 2);
            Check(a.NearbyService() == "shop" && a.ContextHint().StartsWith("\u25a1  abrir"), "Quadrado oferece loja no balcao mesmo perto do diesel");
            Farm.ContextAction();
            Check(GetTree().Paused && GetViewport().GuiGetFocusOwner() is Button, "Loja abre pausada e com foco navegavel no controle");
            var firstFocus = GetViewport().GuiGetFocusOwner();
            SendButton(JoyButton.DpadDown, true); await Frames(); SendButton(JoyButton.DpadDown, false);
            Check(GetViewport().GuiGetFocusOwner() is Button && GetViewport().GuiGetFocusOwner() != firstFocus, "Direcional navega opcoes da loja");
            SendButton(JoyButton.B, true); await Frames(); SendButton(JoyButton.B, false);
            Check(!GetTree().Paused, "Circulo fecha loja e retoma partida");
            player.Position = FarmActivities.Shop + new Vector3(0, 0, -4.1f);
            Check(a.NearbyService() == "", "Loja nao e oferecida alem de quatro metros");
            player.Position = FarmActivities.Pump + new Vector3(3.5f, 0, 0);
            tractor.Position = new(70, .1f, 70); tractor.StopMotion();
            Check(a.NearbyService() == "", "Diesel nao e oferecido quando trator esta distante");
            tractor.Position = FarmActivities.Pump + new Vector3(3, .1f, 0);
            Check(a.NearbyService() == "fuel" && a.ContextHint().StartsWith("\u25a1"), "Quadrado oferece diesel com jogador e trator proximos");
            player.Position = FarmActivities.Pump + new Vector3(4.1f, 0, 0);
            Check(a.NearbyService() == "", "Abastecimento a pe respeita alcance de quatro metros");
            player.SetDriving(true); tractor.Occupied = true;
            Check(a.NearbyService() == "fuel", "Motorista abastece pelo alcance de sete metros do trator");
            tractor.Position = FarmActivities.Pump + new Vector3(7.1f, 0, 0);
            Check(a.NearbyService() == "", "Motorista distante nao recebe acao de abastecer");
            player.SetDriving(false); tractor.Occupied = false;
            player.Position = FarmActivities.FishingSpot + new Vector3(0, 0, 5.1f);
            Check(a.NearbyService() == "", "Pesca nao e oferecida fora de cinco metros");
            player.Position = FarmActivities.FishingSpot + new Vector3(0, .1f, 1);
            a.State.RodOwned = true; a.State.Bait = 2;
            Check(a.NearbyService() == "fish" && a.ContextHint().StartsWith("\u25a1"), "Quadrado oferece pesca no lago");
            Farm.ContextAction();
            Check(a.Fishing && a.State.Bait == 1 && a.ContextHint().Contains("\u25a1"), "Acao contextual inicia pesca com dica do controle");
            player.Position = FarmActivities.Shop;
            Check(a.NearbyService() == "fish", "Linha lancada tem prioridade sobre outros servicos");
            Farm.ContextAction(); Check(!a.Fishing, "Acao contextual recolhe a linha ativa");
            Farm.SetPaused(true);
            Check(GetViewport().GuiGetFocusOwner() is Button, "Menu de pausa recebe foco para o controle");
            SendButton(JoyButton.B, true); await Frames(); SendButton(JoyButton.B, false);
            Check(!GetTree().Paused, "Circulo fecha menu de pausa");
            Check(player.GetNode<AudioStreamPlayer3D>("Footsteps").VolumeDb == -18, "Passos usam volume reduzido de -18 dB");
            tractor.Position = new(-70, .2f, 60); tractor.Rotation = Vector3.Zero;
            tractor.Occupied = true; player.SetDriving(true); tractor.Fuel = 30;
            Input.ActionPress("accelerate"); await Frames(90); Input.ActionRelease("accelerate");
            Check(tractor.EngineAudio.VolumeDb <= -15 && tractor.EngineAudio.VolumeDb > -22, "Motor acelerado preserva reducao de volume");
            tractor.StopMotion(); tractor.Occupied = false; player.SetDriving(false);
            GD.Print($"INPUT OK: {_checks} verificacoes"); Farm.QuitGame();
        }
        catch (Exception ex)
        {
            Input.ActionRelease("accelerate");
            GD.PushError("INPUT FAILED: " + ex); Farm.QuitGame(1);
        }
    }
}
