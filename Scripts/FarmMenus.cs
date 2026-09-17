using Godot;
using System;
using System.Linq;

namespace ValeDasFlores;

public partial class Farm
{
    public SaveStore Saves = new();
    public int SelectedSlot { get; private set; }
    private readonly Label[] _slotLabels = new Label[3];
    private ConfirmationDialog _confirm = null!;
    private Action? _confirmed;
    private Control _shopPanel = null!;
    private Label _shopBalance = null!, _menuMessage = null!;
    private Label _shopFeedback = null!;
    private bool _shopOpen;
    private Button _shopFirstButton = null!;
    private bool _quitting;
    public async void QuitGame(int code = 0)
    {
        if (_quitting) return;
        _quitting = true;
        Tractor.SetPhysicsProcess(false); Player.SetPhysicsProcess(false); Activities.SetPhysicsProcess(false);
        Hilux.SetPhysicsProcess(false); Jeep.SetPhysicsProcess(false);
        Tractor.ReleaseAudio(); Player.ReleaseAudio();
        // Allow the audio mixer to release its playback before destroying the scene.
        await ToSignal(GetTree().CreateTimer(.15, true), SceneTreeTimer.SignalName.Timeout);
        GetTree().Quit(code);
    }
    private void Confirm(string message, Action action)
    {
        _confirmed = action; _confirm.DialogText = message; _confirm.PopupCentered(new(560, 180));
    }
    private static Button MenuButton(Control parent, string title, Action action)
    {
        var button = new Button { Text = title, CustomMinimumSize = new(150, 48), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; parent.AddChild(button); button.Pressed += action; return button;
    }
    private void AddSaveMenu(VBoxContainer menu)
    {
        _confirm = new ConfirmationDialog { Theme = RetroTheme(), Title = "Confirmar", OkButtonText = "Confirmar", CancelButtonText = "Cancelar" }; AddChild(_confirm);
        RetroLook.DecoratePopup(_confirm);
        _confirm.Confirmed += () => { var action = _confirmed; _confirmed = null; action?.Invoke(); };
        Text(menu, "SUAS PARTIDAS", 18, new Color("dfbd72"));
        var slots = new HBoxContainer(); menu.AddChild(slots); slots.AddThemeConstantOverride("separation", 16);
        for (int i = 1; i <= 3; i++)
        {
            int slot = i;
            var card = new PanelContainer { CustomMinimumSize = new(366, 0), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            card.AddThemeStyleboxOverride("panel", RetroBox("252b22", "756c47", 2, 18)); slots.AddChild(card);
            var row = new VBoxContainer(); row.AddThemeConstantOverride("separation", 10); card.AddChild(row);
            Text(row, $"PARTIDA 0{slot}", 22, new Color("e8c478"));
            _slotLabels[i - 1] = Text(row, "", 17, new Color("c7c9ae"));
            _slotLabels[i - 1].CustomMinimumSize = new(320, 72);
            _slotLabels[i - 1].AutowrapMode = TextServer.AutowrapMode.WordSmart;
            MenuButton(row, "CARREGAR", () => {
                if (!Saves.Exists(slot)) { _menuMessage.Text = "Espaço vazio. Comece uma nova partida."; return; }
                Confirm("Carregar esta partida? O progresso não salvo será perdido.", () => LoadSlot(slot));
            });
            MenuButton(row, "SALVAR AQUI", () => {
                if (Saves.Exists(slot)) Confirm($"Substituir a partida {slot} pelo progresso atual?", () => SaveSlot(slot));
                else SaveSlot(slot);
            });
            MenuButton(row, "NOVA PARTIDA", () => {
                if (Saves.Exists(slot)) Confirm($"Iniciar novamente no espaço {slot}? A partida existente será substituída.", () => NewSlot(slot));
                else Confirm("Começar uma nova partida? O progresso não salvo da sessão atual será perdido.", () => NewSlot(slot));
            });
        }
        _menuMessage = Text(menu, "Escolha onde começar. Lembre-se de salvar antes de sair.", 18, new Color("eed59d"));
        RefreshSlots();
    }
    public SaveData CaptureState()
    {
        var state = Activities.State;
        state.Fuel = Tractor.Fuel; state.Tool = Tractor.Tool; state.Lights = Tractor.LightsOn; state.Driving = Player.Driving;
        state.PlayerPosition = new[] { Player.Position.X, Player.Position.Y, Player.Position.Z }; state.PlayerYaw = Player.Yaw;
        state.TractorPosition = new[] { Tractor.Position.X, Tractor.Position.Y, Tractor.Position.Z }; state.TractorYaw = Tractor.Rotation.Y;
        state.DrivenVehicle = DrivingJeep ? 2 : DrivingHilux ? 1 : 0;
        state.HiluxPosition = new[] { Hilux.Position.X, Hilux.Position.Y, Hilux.Position.Z }; state.HiluxYaw = Hilux.Rotation.Y; state.HiluxLights = Hilux.LightsOn;
        state.JeepPosition = new[] { Jeep.Position.X, Jeep.Position.Y, Jeep.Position.Z }; state.JeepYaw = Jeep.Rotation.Y; state.JeepLights = Jeep.LightsOn; state.JeepRoof = Jeep.RoofOn;
        state.Heights = (float[])Terrain.Heights.Clone();
        return state;
    }
    public void ApplyState(SaveData state)
    {
        state.Validate();
        if (Builder.Active) Builder.Toggle();
        Activities.CancelFishing(); Activities.State = state;
        Terrain.SetHeights(state.Heights); Builder.ClearHistory(); Builder.RefreshWorld();
        Player.Position = new(state.PlayerPosition[0], state.PlayerPosition[1], state.PlayerPosition[2]); Player.Yaw = state.PlayerYaw;
        Tractor.Position = new(state.TractorPosition[0], state.TractorPosition[1], state.TractorPosition[2]); Tractor.Rotation = new(0, state.TractorYaw, 0);
        Tractor.StopMotion(); Tractor.Fuel = state.Fuel; Tractor.Equip(state.Tool);
        if (Tractor.LightsOn != state.Lights) Tractor.ToggleLights();
        Hilux.Position = new(state.HiluxPosition[0], state.HiluxPosition[1], state.HiluxPosition[2]); Hilux.Rotation = new(0, state.HiluxYaw, 0); Hilux.StopMotion();
        Jeep.Position = new(state.JeepPosition[0], state.JeepPosition[1], state.JeepPosition[2]); Jeep.Rotation = new(0, state.JeepYaw, 0); Jeep.StopMotion();
        if (Hilux.LightsOn != state.HiluxLights) Hilux.ToggleLights();
        if (Jeep.LightsOn != state.JeepLights) Jeep.ToggleLights();
        if (Jeep.RoofOn != state.JeepRoof) Jeep.ToggleRoof();
        Player.SetDriving(state.Driving); Tractor.Occupied = state.Driving && state.DrivenVehicle == 0;
        Hilux.Occupied = state.Driving && state.DrivenVehicle == 1; Jeep.Occupied = state.Driving && state.DrivenVehicle == 2;
        Builder.SnapActors(); SnapCamera();
    }
    public bool SaveSlot(int slot)
    {
        try { Saves.Save(slot, CaptureState()); SelectedSlot = slot; RefreshSlots(); Message($"Partida salva no espaço {slot}."); _menuMessage.Text = $"Save {slot} atualizado com sucesso."; return true; }
        catch (Exception ex) { _menuMessage.Text = "Não foi possível salvar: " + ex.Message; Message(_menuMessage.Text); return false; }
    }
    public bool LoadSlot(int slot)
    {
        try { var state = Saves.Load(slot); ApplyState(state); SelectedSlot = slot; SetPaused(false); Message($"Partida {slot} carregada."); return true; }
        catch (Exception ex) { _menuMessage.Text = "Não foi possível carregar: " + ex.Message; return false; }
    }
    private void NewSlot(int slot)
    {
        try { var state = new SaveData(); Saves.Save(slot, state); ApplyState(state); SelectedSlot = slot; RefreshSlots(); SetPaused(false); Message("Bem-vindo! A pá é gratuita. Limpe os cupinzeiros para comprar a grade."); }
        catch (Exception ex) { _menuMessage.Text = "Não foi possível iniciar: " + ex.Message; }
    }
    private void RefreshSlots()
    {
        for (int slot = 1; slot <= 3; slot++)
        {
            if (_slotLabels[slot - 1] == null) continue;
            try {
                var state = Saves.Exists(slot) ? Saves.Load(slot) : null;
                _slotLabels[slot - 1].Text = state == null ? "VAZIO\nUma nova história espera por você." : $"{(SelectedSlot == slot ? "EM USO / " : "")}R$ {state.Money}  /  {state.Xp} XP\n{state.SavedAt}";
            } catch { _slotLabels[slot - 1].Text = $"{slot} — Save inválido (backup .bak preservado)"; }
        }
    }
    private void QuickSave()
    {
        if (TitleMenuActive && !_hasSession) return;
        if (SelectedSlot == 0) { SetPaused(true); ShowSlots("save"); _menuMessage.Text = "Escolha um dos três espaços em Salvar aqui."; }
        else SaveSlot(SelectedSlot);
    }
    private void CreateShop(Control root)
    {
        _shopPanel = new ColorRect { Color = new Color("171c19f5"), Visible = false }; root.AddChild(_shopPanel); _shopPanel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var center = new CenterContainer(); _shopPanel.AddChild(center); center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var column = new VBoxContainer { CustomMinimumSize = new(720, 0) }; center.AddChild(column); column.AddThemeConstantOverride("separation", 15);
        Text(column, "SUPRIMENTOS / VALE DAS FLORES", 16, new Color("bca676"));
        Text(column, "ARMAZÉM DA FAZENDA", 40, new Color("f1d491"));
        _shopBalance = Text(column, "", 20, new Color("d2ddba"));
        _shopFeedback = Text(column, "Escolha um item.", 17, new Color("f0d79b"));
        _shopFirstButton = MenuButton(column, "Grade de discos — R$ 220", () => { Activities.Buy(0); RefreshShop(); });
        MenuButton(column, "Vara de pesca — R$ 100", () => { Activities.Buy(1); RefreshShop(); });
        MenuButton(column, "5 iscas — R$ 20", () => { Activities.Buy(2); RefreshShop(); });
        MenuButton(column, "20 sementes de milho — R$ 40", () => { Activities.Buy(3); RefreshShop(); });
        MenuButton(column, "Vender todos os peixes — R$ 35 cada", () => { Activities.SellFish(); RefreshShop(); });
        MenuButton(column, "Voltar", () => SetPaused(false));
    }
    private void RefreshShop() => _shopBalance.Text = $"R$ {Activities.State.Money}  •  {Activities.State.Seeds} sementes / {Activities.State.Bait} iscas  •  {Activities.State.Fish} peixes\nGrade: {(Activities.State.HarrowOwned ? "comprada" : "não comprada")}  •  Vara: {(Activities.State.RodOwned ? "comprada" : "não comprada")}";
    private void OpenShop()
    {
        if (Player.Driving || Player.Position.DistanceTo(FarmActivities.Shop) > 4) { Message("Vá a pé ao balcão LOJA, perto do diesel."); return; }
        SetPaused(true); _pausePanel.Visible = false; _shopPanel.Visible = true; _shopOpen = true; RefreshShop();
        _shopFirstButton.GrabFocus();
    }
}



