using Godot;

namespace ValeDasFlores;

public partial class Farm
{
    public CharacterBody3D[] Vehicles => new CharacterBody3D[] { Tractor, Hilux, Jeep };
    private CharacterBody3D? NearbyVehicle()
    {
        CharacterBody3D? nearest = null;
        float distance = 3.7f;
        foreach (var vehicle in Vehicles)
        {
            float d = Player.GlobalPosition.DistanceTo(vehicle.GlobalPosition);
            if (d > distance) continue;
            using var ray = PhysicsRayQueryParameters3D.Create(Player.GlobalPosition + Vector3.Up * 1.4f, vehicle.GlobalPosition + Vector3.Up * 1.2f);
            ray.Exclude = new Godot.Collections.Array<Rid> { Player.GetRid() };
            var hit = GetWorld3D().DirectSpaceState.IntersectRay(ray);
            if (hit.Count == 0 || hit["collider"].AsGodotObject() != vehicle) continue;
            nearest = vehicle; distance = d;
        }
        return nearest;
    }
    public bool CanEnter() => !Player.Driving && NearbyVehicle() != null;
    public string NearbyVehicleName() => NearbyVehicle() is Jeep ? "WILLYS CJ5" : NearbyVehicle() is Hilux ? "HILUX 1999" : "CBT 2105";
    public bool Interact()
    {
        if (!Player.Driving)
        {
            var vehicle = NearbyVehicle(); if (vehicle == null) return false;
            Activities.CancelFishing();
            Tractor.Occupied = vehicle == Tractor; Hilux.Occupied = vehicle == Hilux; Jeep.Occupied = vehicle == Jeep;
            Player.SetDriving(true); _vehicleThird = true; _orbitYaw = 0; _orbitPitch = -.3f; SnapCamera();
            return true;
        }
        if (Mathf.Abs(VehicleSpeed) > .4f) { Message($"Pare antes de descer. {Prompt("ESPAÇO", "L2")} para frear."); return false; }
        var active = ActiveVehicle;
        foreach (var local in new[] { new Vector3(2.1f, .12f, .5f), new Vector3(-2.1f, .12f, .5f), new Vector3(0, .12f, 3.3f) })
        {
            var candidate = active.ToGlobal(local);
            candidate.Y = Mathf.Max(candidate.Y, Terrain.HeightAt(candidate.X, candidate.Z) + .12f);
            using var supportRay = PhysicsRayQueryParameters3D.Create(candidate + Vector3.Up * 2, candidate - Vector3.Up * 5);
            supportRay.Exclude = new Godot.Collections.Array<Rid> { Player.GetRid(), active.GetRid() };
            var support = GetWorld3D().DirectSpaceState.IntersectRay(supportRay);
            if (support.Count > 0)
            {
                float groundY = support["position"].AsVector3().Y;
                if (groundY > active.GlobalPosition.Y + 1.2f) continue;
                candidate.Y = groundY + .12f;
            }
            using var shape = new CapsuleShape3D { Radius = .36f, Height = 1.8f };
            using var query = new PhysicsShapeQueryParameters3D { Shape = shape,
                Transform = new Transform3D(Basis.Identity, candidate + Vector3.Up * .92f), CollisionMask = 1,
                Exclude = new Godot.Collections.Array<Rid> { Player.GetRid() } };
            if (GetWorld3D().DirectSpaceState.IntersectShape(query).Count > 0) continue;
            Player.GlobalPosition = candidate; Player.Yaw = active.Rotation.Y; Player.Pitch = -.08f;
            Player.SetDriving(false); Tractor.Occupied = Hilux.Occupied = Jeep.Occupied = false; SnapCamera(); return true;
        }
        Message("Saída bloqueada. Procure um lugar mais aberto."); return false;
    }
    public bool ChangeJeepRoof()
    {
        if (Mathf.Abs(Jeep.Speed) > .1f || (!DrivingJeep && (Player.Driving || NearbyVehicle() != Jeep)))
        { Message("Aproxime-se do Willys parado para trocar a capota."); return false; }
        Jeep.ToggleRoof(); Message(Jeep.RoofOn ? "Capota colocada." : "Capota retirada."); return true;
    }
}
