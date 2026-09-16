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
            Check(player.IsOnFloor() && tractor.IsOnFloor(), "Personagem e trator apoiam no solo");
            Check(!player.Driving && !player.ThirdPerson, "Início em primeira pessoa");
            var start = player.GlobalPosition;
            Input.ActionPress("forward"); await Frames(35); Input.ActionRelease("forward");
            Check(player.GlobalPosition.DistanceTo(start) > 1, "Caminhada movimenta o personagem");
            Input.ActionPress("jump"); await Frames(6); Input.ActionRelease("jump");
            Check(player.GlobalPosition.Y > .3f, "Pulo afasta o personagem do solo");
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
            Input.ActionPress("forward"); await Frames(80);
            Check(tractor.Speed > 2 && tractor.GlobalPosition.DistanceTo(tractorStart) > 1, "Aceleração desloca o trator");
            Check(!Farm.Interact() && player.Driving, "Impede saída com trator em movimento");
            float angle = tractor.Rotation.Y;
            Input.ActionPress("left"); await Frames(25); Input.ActionRelease("left"); Input.ActionRelease("forward");
            Check(Mathf.Abs(tractor.Rotation.Y - angle) > .1f, "Direção altera a trajetória");
            Input.ActionPress("jump"); await Frames(50); Input.ActionRelease("jump");
            Check(Mathf.Abs(tractor.Speed) < .01f, "Freio para o trator");
            Input.ActionPress("back"); await Frames(30); Input.ActionRelease("back");
            Check(tractor.Speed < -.5f, "Ré funciona");
            Input.ActionPress("jump"); await Frames(30); Input.ActionRelease("jump");
            tractor.ToggleLights(); Check(tractor.LightsOn, "Liga faróis");
            tractor.ToggleLights(); Check(!tractor.LightsOn, "Desliga faróis");
            Farm.SetPaused(true); var paused = tractor.GlobalPosition;
            Input.ActionPress("forward"); await Frames(10); Input.ActionRelease("forward");
            Check(tractor.GlobalPosition.IsEqualApprox(paused), "Pausa congela a física");
            Farm.SetPaused(false);
            Check(Farm.Interact() && !player.Driving && !tractor.Occupied, "Saída segura devolve o controle a pé");
            Check(player.Visible && player.CollisionLayer == 1, "Restaura corpo e colisão do personagem");
            await Frames(5);
            // Parede real em frente ao veículo: deve bloquear o movimento.
            tractor.GlobalPosition = new(0, .1f, -4); tractor.Rotation = Vector3.Zero;
            var wall = Models.Solid(Farm, new(0, 1.5f, -9), new(8, 3, .5f), "776655");
            player.GlobalPosition = tractor.ToGlobal(new Vector3(2.4f, .1f, 0)); await Frames(3);
            Check(Farm.Interact(), "Reentrada no veículo");
            Input.ActionPress("forward"); await Frames(150); Input.ActionRelease("forward");
            Check(tractor.GlobalPosition.Z > -6.8f, "Trator não atravessa obstáculos");
            Input.ActionPress("jump"); await Frames(15); Input.ActionRelease("jump");
            var blockers = new Node3D(); Farm.AddChild(blockers);
            foreach (var local in new[] { new Vector3(2.1f, 1, .5f), new Vector3(-2.1f, 1, .5f), new Vector3(0, 1, 3.1f) })
                Models.Solid(blockers, tractor.ToGlobal(local), new(1, 2, 1), "776655");
            await Frames(4);
            Check(!Farm.Interact() && player.Driving, "Não desce dentro de obstáculos");
            blockers.QueueFree(); wall.QueueFree(); await Frames(4);
            Check(Farm.Interact(), "Libera a saída após remover obstáculos");
            GD.Print($"SMOKE OK: {_checks} verificações"); GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError($"SMOKE FAILED: {exception}"); GetTree().Quit(1);
        }
    }
}
