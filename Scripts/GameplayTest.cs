using Godot;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ValeDasFlores;

// Run with -- --gameplay-test. Saves go to a unique test folder, never the player's slots.
public partial class GameplayTest : Node
{
    public Farm Farm = null!;
    private int _checks;
    private void Check(bool condition, string text) { if (!condition) throw new InvalidOperationException(text); _checks++; GD.Print("PASS: " + text); }
    private async Task Frames(int count) { for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame); }
    public override async void _Ready()
    {
        try
        {
            Farm.SetPaused(false); await Frames(20);
            var tractor = Farm.Tractor; var player = Farm.Player; var activities = Farm.Activities;
            Check(player.IsOnFloor() && tractor.IsOnFloor(), "Terreno editável sustenta personagem e trator");
            tractor.Fuel = 0; tractor.Occupied = true; player.SetDriving(true);
            var before = tractor.Position; Input.ActionPress("accelerate"); await Frames(45); Input.ActionRelease("accelerate");
            Check(tractor.Position.DistanceTo(before) < .2f && !tractor.EngineAudio.Playing, "Sem diesel, trator não anda e motor para");
            int money = activities.State.Money;
            Check(!activities.Refuel() && activities.State.Money == money, "Abastecimento remoto não cobra nem abastece");
            tractor.Position = FarmActivities.Pump + new Vector3(3, .2f, 0); tractor.StopMotion();
            Check(activities.Refuel() && tractor.Fuel == 10 && activities.State.Money == money - 60, "Abastecimento cobra só os litros fornecidos");
            Check(!Farm.SelectTool(2), "Grade bloqueada antes da compra");
            tractor.Occupied = false; player.SetDriving(false); player.Position = FarmActivities.Shop + new Vector3(0, .1f, 2);
            Check(activities.ContextHint().StartsWith("B  "), "Balcao prioriza dica da loja em vez do diesel proximo");
            Check(activities.Buy(0) && activities.State.HarrowOwned, "Compra libera grade");
            money = activities.State.Money;
            Check(!activities.Buy(-1) && !activities.Buy(4) && activities.State.Money == money, "Loja rejeita itens inexistentes sem cobrar");
            Check(!activities.Buy(0) && activities.State.Money == money, "Compra repetida não cobra novamente");
            Check(!activities.Buy(1), "Loja rejeita compra sem saldo");
            tractor.Position = FarmActivities.MoundPosition(0) + new Vector3(0, .2f, 6); tractor.Rotation = Vector3.Zero; tractor.StopMotion();
            tractor.Occupied = true; player.SetDriving(true); tractor.Equip(1); tractor.ToggleTool();
            money = activities.State.Money; Input.ActionPress("accelerate"); await Frames(90); Input.ActionRelease("accelerate");
            Check(activities.State.ClearedMounds.Contains(0) && activities.State.Money >= money + 40 && activities.State.Xp >= 25, "Pá baixa em movimento derruba cupinzeiro e recompensa");
            tractor.StopMotion(); money = activities.State.Money; activities.UpdateWork(); activities.UpdateWork();
            Check(activities.State.Money == money, "Cupinzeiro destruído não duplica recompensa");
            tractor.Position = FarmActivities.FieldStart + new Vector3(0, .2f, 8); tractor.Rotation = Vector3.Zero;
            tractor.Equip(2); tractor.ToggleTool(); Input.ActionPress("accelerate"); await Frames(90); Input.ActionRelease("accelerate"); tractor.StopMotion();
            Check(activities.State.TilledCells.Count > 0, "Grade baixa prepara células do talhão");
            int prepared = activities.State.TilledCells.Count; money = activities.State.Money; activities.UpdateWork();
            Check(activities.State.TilledCells.Count == prepared && activities.State.Money == money, "Células já preparadas não pagam novamente");
            tractor.Occupied = false; player.SetDriving(false); activities.State.Money = 1000; player.Position = FarmActivities.Shop + new Vector3(0, .1f, 2);
            Check(activities.Buy(1) && activities.Buy(2), "Vara e iscas compradas");
            player.Position = FarmActivities.FishingSpot + new Vector3(0, .1f, 1); await Frames(10);
            int bait = activities.State.Bait; activities.FishAction();
            Check(activities.Fishing && activities.State.Bait == bait - 1, "Lançar linha consome uma isca");
            activities.FishAction(); Check(!activities.Fishing && activities.State.Fish == 0, "Recolher cedo não concede peixe");
            activities.FishAction(); Farm.SetPaused(true); await Frames(20); Check(activities.Fishing && !activities.Bite, "Pausa mantém a pescaria suspensa"); Farm.SetPaused(false);
            for (int i = 0; i < 400 && !activities.Bite; i++) await Frames(1);
            Check(activities.Bite, "Peixe abre janela de fisgada"); activities.FishAction();
            Check(activities.State.Fish == 1, "Recolher na hora captura peixe");
            player.Position = FarmActivities.Shop + new Vector3(0, .1f, 2); money = activities.State.Money; activities.SellFish();
            Check(activities.State.Fish == 0 && activities.State.Money == money + 35, "Venda converte peixe em dinheiro uma única vez");
            var point = new Vector3(-73.5f, 0, -70);
            Check(!Farm.Terrain.Brush(point, float.NaN, 1) && !Farm.Terrain.Brush(point, 10, 9), "Pincel rejeita parametros invalidos sem modificar terreno");
            var invalidHeights = (float[])Farm.Terrain.Heights.Clone(); invalidHeights[0] = float.NaN;
            bool badTerrain = false;
            try { Farm.Terrain.SetHeights(invalidHeights); } catch (ArgumentException) { badTerrain = true; }
            Check(badTerrain && float.IsFinite(Farm.Terrain.Heights[0]), "Terreno rejeita alturas invalidas antes de alterar colisao");
            Check(Farm.Terrain.Brush(point, 10, 1, 2) && Farm.Terrain.HeightAt(point.X, point.Z) > 1, "Pincel eleva relevo");
            player.Position = activities.Ground(point) + Vector3.Up * 3; player.Velocity = Vector3.Zero; await Frames(100);
            Check(player.IsOnFloor() && Mathf.Abs(player.Position.Y - Farm.Terrain.HeightAt(player.Position.X, player.Position.Z)) < .3f, "Colisão acompanha relevo elevado");
            Check(!Farm.Terrain.Brush(new(23, 0, 9), 6, 1), "Área da sede protegida contra deformação");
            Farm.Builder.Toggle(); Check(Farm.Builder.Active && !player.IsPhysicsProcessing(), "Editor pausa controle do personagem");
            var buildPoint = new Vector3(-75, 0, 45);
            Farm.Builder.HandleInput(new InputEventKey { PhysicalKeycode = Key.Key6, Pressed = true });
            Check(Farm.Builder.Apply(buildPoint) && activities.State.Props.Count == 1, "Editor coloca cerca pela ferramenta selecionada");
            // An unsuccessful placement must not consume the next undo operation.
            var actorBuildPoint = new Vector3(player.Position.X, Farm.Terrain.HeightAt(player.Position.X, player.Position.Z), player.Position.Z);
            Check(!Farm.Builder.Apply(actorBuildPoint), "Editor impede cerca sobre personagem");
            Farm.Builder.Undo(); Check(activities.State.Props.Count == 0, "Desfazer remove a construção e restaura o estado anterior");
            Farm.Builder.HandleInput(new InputEventKey { PhysicalKeycode = Key.Key1, Pressed = true });
            var remainingMound = activities.Mounds[1];
            float originalBuildHeight = Farm.Terrain.HeightAt(-75, 45);
            Check(Farm.Builder.Apply(buildPoint) && Farm.Terrain.HeightAt(-75, 45) > originalBuildHeight, "Pincel do editor altera relevo");
            Check(ReferenceEquals(remainingMound, activities.Mounds[1]), "Esculpir preserva colisores dos cupinzeiros existentes");
            Farm.Builder.HandleInput(new InputEventKey { PhysicalKeycode = Key.Key8, Pressed = true });
            Check(!Farm.Builder.Apply(buildPoint), "Remover em terreno vazio nao cria historico");
            Farm.Builder.Undo(); Check(Mathf.Abs(Farm.Terrain.HeightAt(-75, 45) - originalBuildHeight) < .001f, "Desfazer restaura alturas do terreno");
            Farm.Builder.HandleInput(new InputEventKey { PhysicalKeycode = Key.Key5, Pressed = true }); Farm.Builder.Apply(buildPoint);
            Farm.Builder.Toggle();
            player.Position = FarmActivities.Shop + new Vector3(0, .1f, 2);
            activities.State.Money = 1000; money = activities.State.Money;
            Check(activities.Buy(3) && activities.State.Seeds == 20 && activities.State.Money == money - 40, "Loja vende vinte sementes por R$40");
            player.Position = activities.Ground(FarmActivities.CellPosition(64)) + Vector3.Up * .1f;
            Check(activities.NearbyService() == "crop" && !activities.CropAction() && activities.State.Seeds == 20, "Talhao adicional exige gradear antes de plantar");
            activities.State.TilledCells.Add(64); activities.RebuildWork();
            Check(activities.CropAction() && activities.State.Seeds == 19 && activities.State.CropAges[64] == 0, "Plantio consome uma semente na celula preparada");
            Check(!activities.CropAction() && activities.State.Seeds == 19, "Plantio repetido nao consome semente nem colhe cedo");
            player.Position = activities.Ground(FarmActivities.CellPosition(128)) + Vector3.Up * .1f;
            activities.State.TilledCells.Add(128); activities.RebuildWork();
            Check(activities.CropAction() && activities.State.CropAges.ContainsKey(128), "Terceiro talhao aceita plantio independente");
            activities.AdvanceFarmTime(60);
            Check(activities.State.CropAges[64] == 60 && activities.State.CropAges[128] == 60, "Milho cresce pelo tempo ativo da partida");
            Farm.SetPaused(true); activities.AdvanceFarmTime(60); Farm.SetPaused(false);
            Check(activities.State.CropAges[64] == 60, "Pausa nao avanca crescimento");
            Farm.Builder.Toggle(); activities.AdvanceFarmTime(60); Farm.Builder.Toggle();
            Check(activities.State.CropAges[64] == 60, "Editor nao avanca crescimento");
            activities.State.MoundAges[0] = 123;
            Check(FarmActivities.CanFishAt(FarmWorld.LakeCenter) && FarmActivities.CanFishAt(FarmWorld.LakeCenter + new Vector3(24, 0, 0))
                && !FarmActivities.CanFishAt(new(-70, 0, 50)), "Pesca aceita agua e margem oval mas rejeita pasto distante");
            var legacy = System.Text.Json.JsonSerializer.Deserialize<SaveData>("{}")!; legacy.Validate();
            Check(legacy.Seeds == 0 && legacy.CropAges.Count == 0 && legacy.MoundAges.Length == 12, "Saves antigos recebem agricultura vazia e timers novos");
            var badCrop = new SaveData(); badCrop.CropAges[64] = 10;
            bool invalidCrop = false; try { badCrop.Validate(); } catch (InvalidDataException) { invalidCrop = true; }
            Check(invalidCrop, "Save rejeita cultivo em celula sem preparo");
            var store = new SaveStore(ProjectSettings.GlobalizePath("user://test-saves/" + Guid.NewGuid().ToString("N"))); Farm.Saves = store;
            for (int slot = 1; slot <= 3; slot++) { activities.State.Money = slot * 111; Check(Farm.SaveSlot(slot), $"Grava espaço {slot}"); }
            Check(store.Load(1).Money == 111 && store.Load(2).Money == 222 && store.Load(3).Money == 333, "Três saves independentes");
            var saved = store.Load(2); Check(saved.Heights[12 * FarmTerrain.Count + 11] >= 0 && saved.Props.Count == 1 && saved.HarrowOwned && saved.ClearedMounds.Contains(0), "Save inclui relevo, construção, compras e missão");
            activities.State.Money = 999; Farm.Terrain.SetHeights(new float[FarmTerrain.Count * FarmTerrain.Count]);
            Check(Farm.LoadSlot(2) && Farm.Activities.State.Money == 222 && Farm.Terrain.HeightAt(point.X, point.Z) > 1, "Carregar restaura progresso e terreno");
            Check(activities.State.CropAges[64] == 60 && activities.State.CropAges[128] == 60 && activities.State.Seeds == 18
                && activities.State.MoundAges[0] == 123, "Save restaura sementes cultivos e timers de respawn");
            player.Position = activities.Ground(FarmActivities.CellPosition(64)) + Vector3.Up * .1f;
            activities.AdvanceFarmTime(120);
            money = activities.State.Money; int cropXp = activities.State.Xp;
            Check(activities.CropAction() && activities.State.Money == money + 12 && activities.State.Xp == cropXp + 8, "Colheita madura vende milho e concede experiencia");
            money = activities.State.Money;
            Check(!activities.CropAction() && activities.State.Money == money && !activities.State.TilledCells.Contains(64), "Colheita nao duplica recompensa e exige novo preparo");
            activities.State.MoundBonus = true; activities.State.MoundAges[0] = FarmActivities.MoundRespawnSeconds - 1;
            player.Position = activities.Ground(FarmActivities.MoundPosition(0));
            activities.AdvanceFarmTime(2);
            Check(activities.State.ClearedMounds.Contains(0) && !activities.Mounds.ContainsKey(0), "Cupinzeiro nao reaparece sobre o jogador");
            player.Position = new(70, .1f, 70); tractor.Position = new(60, .1f, 70);
            activities.AdvanceFarmTime(1);
            Check(!activities.State.ClearedMounds.Contains(0) && activities.Mounds.ContainsKey(0) && activities.State.MoundBonus
                && activities.State.Money == money, "Respawn restaura cupinzeiro sem pagar nem reiniciar bonus global");
            Check(Farm.SaveSlot(2) && File.Exists(store.SlotPath(2) + ".bak"), "Substituição mantém backup do save anterior");
            money = Farm.Activities.State.Money; File.WriteAllText(store.SlotPath(3), "{invalid");
            Check(!Farm.LoadSlot(3) && Farm.Activities.State.Money == money, "Save corrompido não altera sessão atual");
            bool rejected = false; try { store.SlotPath(4); } catch (ArgumentOutOfRangeException) { rejected = true; }
            Check(rejected, "Não permite quarto espaço de save");
            Farm.Activities.State.Money = 0; tractor.Fuel = 0; tractor.Position = new(-80, 1, 70);
            Farm.Activities.RescueTractor();
            Check(tractor.Fuel >= 5 && tractor.Position.DistanceTo(FarmActivities.Pump) < 6 && Farm.Activities.State.Money == 0, "Socorro recupera trator sem criar dívida ou bloquear partida sem dinheiro");
            GD.Print($"GAMEPLAY OK: {_checks} verificações"); Farm.QuitGame();
        }
        catch (Exception ex) { GD.PushError("GAMEPLAY FAILED: " + ex); Farm.QuitGame(1); }
    }
}
