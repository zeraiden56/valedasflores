using Godot;

namespace ValeDasFlores;

public partial class Farm
{
    private Control _hudGroup = null!;
    private Button _resumeButton = null!;
    private Label _menuFooter = null!;
    private void RefreshInputHints()
    {
        if (_menuFooter != null) _menuFooter.Text = UsingGamepad
            ? "Direcional / analógico navegar   /   × confirmar   /   ○ voltar   /   Options menu   /   Share salvar"
            : "F5 salvar   /   F2 construir   /   F8 visual retrô   /   TAB ou setas navegar   /   ENTER escolher";
    }
    private static StyleBoxFlat RetroBox(string background, string border, int width = 2, int padding = 14) => new()
    {
        BgColor = new Color(background), BorderColor = new Color(border),
        BorderWidthLeft = width, BorderWidthTop = width, BorderWidthRight = width, BorderWidthBottom = width,
        ContentMarginLeft = padding, ContentMarginRight = padding, ContentMarginTop = padding, ContentMarginBottom = padding
    };

    private static Theme RetroTheme()
    {
        var theme = new Theme { DefaultFontSize = 20 };
        theme.SetStylebox("normal", "Button", RetroBox("34372b", "897a50"));
        theme.SetStylebox("hover", "Button", RetroBox("585436", "dfbd72"));
        theme.SetStylebox("pressed", "Button", RetroBox("8b713f", "f8dc96"));
        theme.SetStylebox("focus", "Button", new StyleBoxFlat {
            BgColor = Colors.Transparent, BorderColor = new Color("f8d680"),
            BorderWidthLeft = 3, BorderWidthTop = 3, BorderWidthRight = 3, BorderWidthBottom = 3 });
        theme.SetStylebox("disabled", "Button", RetroBox("282a23", "4a4c3d"));
        theme.SetColor("font_color", "Button", new Color("eee4c6"));
        theme.SetColor("font_hover_color", "Button", new Color("fff4d4"));
        theme.SetColor("font_focus_color", "Button", new Color("ffe4a2"));
        theme.SetColor("font_disabled_color", "Button", new Color("888974"));
        return theme;
    }

    private void CreateRetroHud()
    {
        var layer = new CanvasLayer { Layer = 1 }; AddChild(layer);
        var root = new Control { Theme = RetroTheme(), MouseFilter = Control.MouseFilterEnum.Ignore };
        layer.AddChild(root); root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _hudGroup = new Control { MouseFilter = Control.MouseFilterEnum.Ignore }; root.AddChild(_hudGroup);
        _hudGroup.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var title = Panel(_hudGroup, new(32, 28), new(500, 0));
        var column = new VBoxContainer(); title.AddChild(column);
        Text(column, "VALE DAS FLORES", 24, new Color("e8c478"));
        _status = Text(column, "", 14, new Color("b6b79b"));
        _progress = Text(column, "", 18, new Color("eee4c6"));
        _progress.CustomMinimumSize = new(520, 0); _progress.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        var info = Panel(_hudGroup, Vector2.Zero, new(300, 0));
        info.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopRight);
        info.OffsetLeft = -342; info.OffsetRight = -32; info.OffsetTop = 28; info.OffsetBottom = 190;
        _speed = Text(info, "", 18, new Color("eee4c6"));
        var hints = Panel(_hudGroup, Vector2.Zero, new(1050, 0));
        hints.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomLeft);
        hints.OffsetLeft = 32; hints.OffsetRight = 1150; hints.OffsetTop = -138; hints.OffsetBottom = -28;
        _hint = Text(hints, "", 20, new Color("eee4c6"));
        hints.Visible = false;
        var crosshair = Text(_hudGroup, "+", 18, new Color(1, 1, 1, .5f));
        crosshair.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
        crosshair.OffsetLeft = -6; crosshair.OffsetTop = -14;
        _toast = Text(_hudGroup, "", 22, new Color("ffe2a0"));
        _toast.HorizontalAlignment = HorizontalAlignment.Center;
        _toast.AddThemeColorOverride("font_shadow_color", new Color("171a15"));
        _toast.AddThemeConstantOverride("shadow_offset_x", 2); _toast.AddThemeConstantOverride("shadow_offset_y", 2);
        _toast.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterTop);
        _toast.OffsetLeft = -620; _toast.OffsetRight = 620; _toast.OffsetTop = 220; _toast.OffsetBottom = 280;
        _pausePanel = new ColorRect { Color = new Color("171c19ed"), Visible = false };
        root.AddChild(_pausePanel); _pausePanel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var center = new CenterContainer(); _pausePanel.AddChild(center); center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var menu = new VBoxContainer { CustomMinimumSize = new(1140, 0) }; center.AddChild(menu);
        menu.AddThemeConstantOverride("separation", 18);
        Text(menu, "VIDA NO CERRADO / SEU LUGAR NO CAMPO", 16, new Color("bca676"));
        _menuHeading = Text(menu, "VALE DAS FLORES", 56, new Color("f1d491"));
        _mainMenu = new VBoxContainer(); menu.AddChild(_mainMenu); _mainMenu.AddThemeConstantOverride("separation", 12);
        _resumeButton = MenuButton(_mainMenu, "CONTINUAR", ContinueSession);
        _newButton = MenuButton(_mainMenu, "NOVO JOGO", () => ShowSlots("new"));
        _loadButton = MenuButton(_mainMenu, "CARREGAR", () => ShowSlots("load"));
        _saveButton = MenuButton(_mainMenu, "SALVAR", () => ShowSlots("save"));
        MenuButton(_mainMenu, "OPÇÕES", ShowSettings);
        MenuButton(_mainMenu, "CONTROLES", ShowControls);
        _rescueButton = MenuButton(_mainMenu, "REBOCAR TRATOR", () => Confirm("Rebocar à sede e garantir 5 L de diesel? Custa até R$ 50.", () => Activities.RescueTractor()));
        _titleButton = MenuButton(_mainMenu, "MENU INICIAL", () => Confirm("Voltar ao início? Salve sua partida antes de continuar.", ShowTitleMenu));
        MenuButton(_mainMenu, "SAIR", () => Confirm("Sair do jogo? O progresso não salvo será perdido.", () => QuitGame()));
        _slotsMenu = new VBoxContainer(); menu.AddChild(_slotsMenu); _slotsMenu.AddThemeConstantOverride("separation", 14);
        AddSaveMenu(_slotsMenu);
        MenuButton(_slotsMenu, "VOLTAR", () => BackMenu()); _slotsMenu.Visible = false;
        CreateSettingsMenu(menu);
        _menuFooter = Text(menu, "", 16, new Color("b2b49b"));
        RefreshInputHints();
        CreateShop(root);
    }

    private void ShowControls()
    {
        var dialog = new AcceptDialog { Theme = RetroTheme(), Title = "CONTROLES", DialogText =
            "CONTROLE PS4\nAnalógico esquerdo andar/dirigir / direito câmera\nA pé: X pular / L3 correr / triângulo entrar ou descer\nVeículos: X acelerar / círculo ré / L2 frear\nQuadrado interagir: loja, diesel ou pesca\nL1 trocar ferramenta (parado) / R1 baixar ou levantar\nR3 mudar câmera / direcional cima faróis\nJeep: direcional baixo abrir/fechar capota\nOptions menu / Share salvar / círculo voltar / X confirmar\n\nTECLADO E MOUSE\nWASD andar/dirigir / SHIFT correr / ESPAÇO pular ou frear\nE entrar/descer / V câmera / F faróis / roda do mouse distância\n1 pá / 2 grade / 3 sem ferramenta / R baixar ou levantar\nC plantar/colher / G diesel / B loja / P pescar / F5 salvar / ESC menu\nH capota do Jeep / F8 alternar visual retrô\n\nCONSTRUIR\nF2 abrir editor / edição com teclado e mouse", OkButtonText = "VOLTAR" };
        AddChild(dialog); RetroLook.DecoratePopup(dialog); dialog.PopupCentered(new Vector2I(1000, 800));
        dialog.VisibilityChanged += () => { if (!dialog.Visible) { dialog.QueueFree(); _resumeButton.GrabFocus(); } };
    }
}





