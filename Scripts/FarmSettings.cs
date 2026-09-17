using Godot;
using System;
using System.Linq;
using System.Globalization;

namespace ValeDasFlores;

public partial class Farm
{
    private VBoxContainer _mainMenu = null!, _slotsMenu = null!, _settingsMenu = null!;
    private Label _menuHeading = null!, _videoFeedback = null!;
    private Button _newButton = null!, _loadButton = null!, _saveButton = null!, _rescueButton = null!, _titleButton = null!;
    public bool TitleMenuActive { get; private set; } = true;
    private bool _hasSession;
    private string _slotMode = "load";
    private OptionButton _resolution = null!, _windowMode = null!, _frameLimit = null!;
    private CheckButton _vsync = null!;
    private ConfirmationDialog _videoConfirm = null!;
    private int _videoGeneration;
    private bool _videoPending;
    private VideoOptions _video = new(1, 0, 1, true), _previousVideo = new(1, 0, 1, true);
    private record VideoOptions(int Resolution, int Mode, int Fps, bool Vsync);
    private static readonly Vector2I[] Resolutions = { new(1280, 720), new(1920, 1080), new(2560, 1440), new(3840, 2160) };
    private static readonly int[] FrameLimits = { 30, 60, 120, 144, 165, 0 };

    public void ShowTitleMenu()
    {
        TitleMenuActive = true;
        SetPaused(true); RefreshMenuPresentation(true);
    }
    public void OnSessionStarted()
    {
        _hasSession = true; TitleMenuActive = false;
        _mainMenu.Visible = true; _slotsMenu.Visible = false; _settingsMenu.Visible = false;
    }
    public void RefreshMenuPresentation(bool paused)
    {
        if (_mainMenu == null) return;
        if (!paused) { OnSessionStarted(); return; }
        _mainMenu.Visible = true; _slotsMenu.Visible = false; _settingsMenu.Visible = false;
        _menuHeading.Text = TitleMenuActive ? "VALE DAS FLORES" : "UMA PAUSA NO CAMPO";
        _resumeButton.Text = TitleMenuActive ? "CONTINUAR" : "VOLTAR AO CAMPO";
        _resumeButton.Disabled = TitleMenuActive && !_hasSession && !Enumerable.Range(1, 3).Any(Saves.Exists);
        _newButton.Visible = TitleMenuActive; _saveButton.Visible = !TitleMenuActive;
        _rescueButton.Visible = !TitleMenuActive; _titleButton.Visible = !TitleMenuActive;
        if (_resumeButton.Disabled) _newButton.GrabFocus(); else _resumeButton.GrabFocus();
    }
    private void ContinueSession()
    {
        if (_hasSession) { OnSessionStarted(); SetPaused(false); return; }
        int newest = 0; DateTime saved = DateTime.MinValue;
        for (int slot = 1; slot <= 3; slot++)
        {
            try { if (Saves.Exists(slot)) { var state = Saves.Load(slot); DateTime.TryParseExact(state.SavedAt, "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var timestamp); if (newest == 0 || timestamp > saved) { newest = slot; saved = timestamp; } } }
            catch { }
        }
        if (newest != 0) { if (LoadSlot(newest)) OnSessionStarted(); }
        else ShowSlots("load");
    }
    private void ShowSlots(string mode)
    {
        _slotMode = mode; _mainMenu.Visible = false; _settingsMenu.Visible = false; _slotsMenu.Visible = true;
        _menuHeading.Text = mode == "new" ? "NOVO JOGO" : mode == "save" ? "SALVAR PARTIDA" : "CARREGAR PARTIDA";
        RefreshSlots();
        foreach (var button in _slotsMenu.FindChildren("*", "Button", true, false).OfType<Button>())
        {
            button.Visible = button.Text == "VOLTAR" || (mode == "new" && button.Text == "NOVA PARTIDA") || (mode == "load" && button.Text == "CARREGAR") || (mode == "save" && button.Text == "SALVAR AQUI");
        }
        foreach (var button in _slotsMenu.FindChildren("*", "Button", true, false).OfType<Button>()) if (button.IsVisibleInTree()) { button.GrabFocus(); break; }
    }
    public bool BackMenu()
    {
        if (_videoPending) { RevertVideo(); return true; }
        if (_settingsMenu.Visible || _slotsMenu.Visible) { RefreshMenuPresentation(true); return true; }
        return TitleMenuActive;
    }
    private void ShowSettings()
    {
        _mainMenu.Visible = false; _slotsMenu.Visible = false; _settingsMenu.Visible = true;
        _menuHeading.Text = "OPÇÕES DE VÍDEO"; UpdateVideoSelectors(); _resolution.GrabFocus();
    }
    private OptionButton VideoChoice(VBoxContainer parent, string label, string[] entries)
    {
        var row = new HBoxContainer(); parent.AddChild(row);
        Text(row, label, 22, new Color("e8c478")).CustomMinimumSize = new(380, 0);
        var option = new OptionButton { CustomMinimumSize = new(520, 48), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        foreach (var entry in entries) option.AddItem(entry); row.AddChild(option); return option;
    }
    private void CreateSettingsMenu(VBoxContainer menu)
    {
        _settingsMenu = new VBoxContainer { Visible = false }; menu.AddChild(_settingsMenu); _settingsMenu.AddThemeConstantOverride("separation", 18);
        _resolution = VideoChoice(_settingsMenu, "Resolução", new[] { "1280 × 720 / HD", "1920 × 1080 / Full HD", "2560 × 1440 / QHD", "3840 × 2160 / 4K" });
        _windowMode = VideoChoice(_settingsMenu, "Modo de exibição", new[] { "Janela", "Tela cheia", "Tela cheia sem bordas" });
        _frameLimit = VideoChoice(_settingsMenu, "Limite de quadros", new[] { "30 FPS", "60 FPS", "120 FPS", "144 FPS", "165 FPS", "Ilimitado" });
        _vsync = new CheckButton { Text = "Sincronização vertical (VSync)", CustomMinimumSize = new(0, 48) }; _settingsMenu.AddChild(_vsync);
        Text(_settingsMenu, "FPS limita os quadros do jogo. Hz depende do monitor e da configuração do Windows.\nEm tela cheia, a resolução também depende dos modos disponíveis no monitor.", 18, new Color("c7c9ae"));
        _videoFeedback = Text(_settingsMenu, "Mudanças precisam ser confirmadas em 15 segundos.", 18, new Color("e8c478"));
        MenuButton(_settingsMenu, "APLICAR", PreviewVideo);
        MenuButton(_settingsMenu, "VOLTAR", () => BackMenu());
        _videoConfirm = new ConfirmationDialog { Theme = RetroTheme(), Title = "Manter configuração?", OkButtonText = "MANTER", CancelButtonText = "REVERTER" };
        AddChild(_videoConfirm); RetroLook.DecoratePopup(_videoConfirm);
        _videoConfirm.Confirmed += KeepVideo; _videoConfirm.Canceled += RevertVideo;
        var config = new ConfigFile();
        if (config.Load("user://video.cfg") == Error.Ok)
            _video = new(Math.Clamp(config.GetValue("video", "resolution", 1).AsInt32(), 0, 3), Math.Clamp(config.GetValue("video", "mode", 0).AsInt32(), 0, 2), Math.Clamp(config.GetValue("video", "fps", 1).AsInt32(), 0, 5), config.GetValue("video", "vsync", true).AsBool());
        ApplyVideo(_video); UpdateVideoSelectors();
    }
    private void UpdateVideoSelectors()
    {
        _resolution.Select(_video.Resolution); _windowMode.Select(_video.Mode); _frameLimit.Select(_video.Fps); _vsync.ButtonPressed = _video.Vsync;
    }
    private void ApplyVideo(VideoOptions options)
    {
        Engine.MaxFps = FrameLimits[options.Fps];
        DisplayServer.WindowSetVsyncMode(options.Vsync ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled);
        var window = GetWindow(); window.Mode = Window.ModeEnum.Windowed; window.Borderless = options.Mode == 2;
        window.Size = Resolutions[options.Resolution];
        if (options.Mode != 0) window.Mode = options.Mode == 1 ? Window.ModeEnum.ExclusiveFullscreen : Window.ModeEnum.Fullscreen;
        else { var usable = DisplayServer.ScreenGetUsableRect(); window.Position = usable.Position + (usable.Size - window.Size) / 2; }
    }
    private async void PreviewVideo()
    {
        if (_videoPending) return;
        _previousVideo = _video; _video = new(_resolution.Selected, _windowMode.Selected, _frameLimit.Selected, _vsync.ButtonPressed);
        _videoPending = true; int generation = ++_videoGeneration;
        ApplyVideo(_video); _videoConfirm.DialogText = "Manter estas opções? Revertendo em 15 segundos."; _videoConfirm.PopupCentered(new(650, 180));
        for (int seconds = 14; seconds >= 0; seconds--)
        {
            await ToSignal(GetTree().CreateTimer(1, true), SceneTreeTimer.SignalName.Timeout);
            if (!IsInsideTree() || generation != _videoGeneration || !_videoPending) return;
            _videoConfirm.DialogText = $"Manter estas opções? Revertendo em {seconds} segundos.";
        }
        RevertVideo();
    }
    private void KeepVideo()
    {
        if (!_videoPending) return;
        _videoPending = false; ++_videoGeneration;
        var config = new ConfigFile(); config.SetValue("video", "resolution", _video.Resolution); config.SetValue("video", "mode", _video.Mode); config.SetValue("video", "fps", _video.Fps); config.SetValue("video", "vsync", _video.Vsync);
        var result = config.Save("user://video.cfg"); _videoFeedback.Text = result == Error.Ok ? "Opções salvas." : "Opções aplicadas, mas não foi possível gravar as preferências.";
        _resolution.GrabFocus();
    }
    private void RevertVideo()
    {
        if (!_videoPending) return;
        _videoPending = false; ++_videoGeneration; _video = _previousVideo; ApplyVideo(_video); _videoConfirm.Hide(); UpdateVideoSelectors(); _videoFeedback.Text = "Configuração anterior restaurada."; _resolution.GrabFocus();
    }
}

