using Godot;
using System;
using System.Threading.Tasks;

namespace ValeDasFlores;

// Executado somente com -- --smoke; usa o mundo físico e os controles reais.
public partial class SmokeTest : Node
{
    public Farm Farm = null!;
    private int _checks;
    private void Check(bool condition, string description)
    {
        if (!condition) throw new InvalidOperationException(description);
        _checks++; GD.Print($"PASS: {description}");
    }
    private async Task Frames(int count)
    {
        for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
    }
    public override async void _Ready()
    {
        try
        {
            Farm.SetPaused(false);
            await Frames(12);
            var player = Farm.Player; var tractor = Farm.Tractor;
            Check(tractor.EngineAudio.Stream.GetLength() > 5 && tractor.EngineAudio.Playing, "Gravação do CBT carregada e motor em marcha lenta");
            int idleSteps = player.FootstepsPlayed;
            await Frames(12);
            Check(player.FootstepsPlayed == idleSteps, "Parado não produz passos");
            Check(player.IsOnFloor() && tractor.IsOnFloor(), "Personagem e trator apoiam no solo");
            Check(!player.Driving && !player.ThirdPerson, "Início em primeira pessoa");
            var start = player.GlobalPosition;
            Input.ActionPress("forward"); await Frames(35); Input.ActionRelease("forward");
            Check(player.GlobalPosition.DistanceTo(start) > 1, "Caminhada movimenta o personagem");
            Check(player.FootstepsPlayed > idleSteps, "Caminhada dispara som dos passos");
            Input.ActionPress("jump"); await Frames(6); Input.ActionRelease("jump");
            Check(player.GlobalPosition.Y > .3f, "Pulo afasta o personagem do solo");
            int airborneSteps = player.FootstepsPlayed;
            await Frames(8);
            Check(player.FootstepsPlayed == airborneSteps, "No ar não produz passos");
            await Frames(65);
            Farm.ToggleCamera(); Check(player.ThirdPerson, "Alterna para terceira pessoa a pé");
            Farm.ToggleCamera(); Check(!player.ThirdPerson, "Retorna à primeira pessoa a pé");
            player.GlobalPosition = new(10, .05f, -5); await Frames(2);
            Check(!Farm.Interact(), "Não entra no trator à distância");
            player.GlobalPosition = tractor.ToGlobal(new Vector3(2.4f, .1f, .3f)); await Frames(3);
            Check(Farm.Interact() && player.Driving && tractor.Occupied, "Entrada próxima assume o trator");
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Check(!player.Visible && player.CollisionLayer == 0, "Personagem externo desativado ao dirigir");
            Farm.ToggleCamera(); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Check(Farm.Camera.GlobalPosition.DistanceTo(tractor.Seat) < .2f, "Câmera em primeira pessoa no assento");
            Farm.ToggleCamera(); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Check(Farm.Camera.GlobalPosition.DistanceTo(tractor.Seat) > 4, "Câmera externa do trator");
            var tractorStart = tractor.GlobalPosition;
            int drivingSteps = player.FootstepsPlayed;
            float idlePitch = tractor.EngineAudio.PitchScale;
            Input.ActionPress("accelerate"); await Frames(80);
            Check(tractor.Speed > 2 && tractor.GlobalPosition.DistanceTo(tractorStart) > 1, "Aceleração desloca o trator");
            Check(tractor.EngineAudio.PitchScale > idlePitch + .1f, "Som do motor acompanha a aceleração");
            Check(player.FootstepsPlayed == drivingSteps, "Dirigir não produz passos");
            Check(!Farm.Interact() && player.Driving, "Impede saída com trator em movimento");
            float angle = tractor.Rotation.Y;
            Input.ActionPress("left"); await Frames(25); Input.ActionRelease("left"); Input.ActionRelease("accelerate");
            Check(Mathf.Abs(tractor.Rotation.Y - angle) > .1f, "Direção altera a trajetória");
            Input.ActionPress("brake"); await Frames(50); Input.ActionRelease("brake");
            Check(Mathf.Abs(tractor.Speed) < .01f, "Freio para o trator");
            Input.ActionPress("reverse"); await Frames(30); Input.ActionRelease("reverse");
            Check(tractor.Speed < -.5f, "Ré funciona");
            Input.ActionPress("brake"); await Frames(30); Input.ActionRelease("brake");
            tractor.ToggleLights(); Check(tractor.LightsOn, "Liga faróis");
            tractor.ToggleLights(); Check(!tractor.LightsOn, "Desliga faróis");
            Farm.SetPaused(true); var paused = tractor.GlobalPosition;
            Input.ActionPress("accelerate"); await Frames(10); Input.ActionRelease("accelerate");
            Check(tractor.GlobalPosition.IsEqualApprox(paused), "Pausa congela a física");
            Farm.SetPaused(false);
            Check(tractor.EngineAudio.Playing, "Motor continua após retomar o jogo");
            Check(Farm.Interact() && !player.Driving && !tractor.Occupied, "Saída segura devolve o controle a pé");
            Check(player.Visible && player.CollisionLayer == 1, "Restaura corpo e colisão do personagem");
            await Frames(5);
            // Parede real em frente ao veículo: deve bloquear o movimento.
            tractor.GlobalPosition = new(0, .1f, -4); tractor.Rotation = Vector3.Zero;
            var wall = Models.Solid(Farm, new(0, 1.5f, -9), new(8, 3, .5f), "776655");
            player.GlobalPosition = tractor.ToGlobal(new Vector3(2.4f, .1f, 0)); await Frames(3);
            Check(Farm.Interact(), "Reentrada no veículo");
            Input.ActionPress("accelerate"); await Frames(150); Input.ActionRelease("accelerate");
            Check(tractor.GlobalPosition.Z > -6.8f, "Trator não atravessa obstáculos");
            Input.ActionPress("brake"); await Frames(15); Input.ActionRelease("brake");
            var blockers = new Node3D(); Farm.AddChild(blockers);
            foreach (var local in new[] { new Vector3(2.1f, 1, .5f), new Vector3(-2.1f, 1, .5f), new Vector3(0, 1, 3.1f) })
                Models.Solid(blockers, tractor.ToGlobal(local), new(1, 2, 1), "776655");
            await Frames(4);
            Check(!Farm.Interact() && player.Driving, "Não desce dentro de obstáculos");
            blockers.QueueFree(); wall.QueueFree(); await Frames(4);
            Check(Farm.Interact(), "Libera a saída após remover obstáculos");
            player.GlobalPosition = new(30, .1f, -18); player.Yaw = 0;
            Input.ActionPress("forward"); await Frames(90); Input.ActionRelease("forward");
            Check(player.GlobalPosition.Z < -21, "Margem do lago acessível sem cerca bloqueando a passagem");
            GD.Print($"SMOKE OK: {_checks} verificações"); Farm.QuitGame(0);
        }
        catch (Exception exception)
        {
            GD.PushError($"SMOKE FAILED: {exception}"); Farm.QuitGame(1);
        }
    }
}
