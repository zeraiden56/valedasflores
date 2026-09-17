# Vale das Flores

Protótipo 3D de simulador de fazenda em **C# / Godot 4.5.1 .NET**, com personagem em primeira pessoa e trator dirigível inspirado no **CBT 2105**. Projeto independente de Distribuidora Simulator.

Direção visual: 3D de poucos polígonos inspirado na geração PS2, com pixelização suave de 2 pixels no mundo e nos menus em Full HD. **F8** alterna suave, clássico e nítido. Veja a [direção criativa e as frentes de trabalho](docs/DIRECAO-CRIATIVA.md).

## Menu e vídeo

O início oferece **Continuar** (save mais recente, ou sessão em andamento), **Novo jogo**, **Carregar**, **Opções** e **Sair**. Durante a partida, o menu de pausa oferece **Salvar** e retorno ao campo. As opções incluem 720p, 1080p, 1440p e 4K; janela, tela cheia exclusiva ou sem bordas; limite de 30/60/120/144/165 FPS ou ilimitado, além de VSync. Confirme alterações em 15 segundos ou a configuração anterior retorna. As preferências ficam em `user://video.cfg`. O limite de FPS não muda os Hz do monitor; VSync pode limitar os quadros à taxa da tela.

As dicas aparecem perto dos objetos; o clique esquerdo usa o serviço próximo ou entra no veículo. Pausa e editor suspendem o crescimento das plantações e o retorno dos cupinzeiros. Os novos morros e a bacia do lago aparecem em novas partidas; saves antigos preservam seu relevo.

## Jogar no Windows

O executável Windows x64 desta cópia está em `build/windows/`. No PowerShell, a partir da pasta que contém este README:

```powershell
.\build\windows\ValeDasFlores.exe
```

Na primeira execução, abre em **janela Full HD (1920 × 1080)**; depois respeita as opções de vídeo salvas. No menu inicial, escolha **Novo jogo** e um dos três espaços ou **Carregar** para continuar. ESC abre a pausa durante a partida.

Mantenha `ValeDasFlores.exe`, `ValeDasFlores.pck` e a pasta `data_ValeDasFlores_windows_x86_64` juntos. O pacote inclui o runtime .NET; não é necessário instalar Godot para jogar. Para ver os logs no terminal, use `ValeDasFlores.console.exe`.

Para recompilar e executar as verificações integradas no executável exportado:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\compilar-windows.ps1 -Testar
```

O script usa o SDK .NET instalado, importa os recursos e exporta em Release. As ferramentas locais já estão preparadas nesta cópia. Em um clone novo:

1. Instale o SDK .NET 8 ou superior, x64.
2. Baixe `Godot_v4.5.1-stable_mono_win64.zip` da [versão oficial 4.5.1](https://github.com/godotengine/godot/releases/tag/4.5.1-stable) e extraia em `.tools/godot/`, preservando a pasta interna. Alternativamente, defina `$env:GODOT_BIN` com o caminho completo do executável Godot .NET.
3. Baixe `Godot_v4.5.1-stable_mono_export_templates.tpz` da mesma versão (arquivo ZIP) e extraia os quatro arquivos `windows_debug_x86_64.exe`, `windows_debug_x86_64_console.exe`, `windows_release_x86_64.exe` e `windows_release_x86_64_console.exe` da pasta `templates/` para `.tools/templates/`.
4. Execute o comando de compilação acima. A primeira execução requer internet para restaurar os pacotes NuGet.

A exportação segue o [fluxo oficial do Godot para Windows](https://docs.godotengine.org/en/4.5/tutorials/export/exporting_for_windows.html). `ValeDasFlores.sln` acompanha o projeto porque o exportador C# exige esse arquivo. O script permite usar runtimes superiores ao .NET 8 no editor, mantendo o jogo exportado com seu próprio runtime .NET 8.

## Jogar no Linux

Nesta máquina, as ferramentas usadas na validação ficam em `.tools/` e o script as encontra automaticamente:

```sh
cd "/home/arthurdias/Documentos/Faculdade - SI/Projetos/JOGOS/Vale das flores"
./jogar.sh
```

O script compila o C# antes de abrir o jogo. A primeira compilação requer internet para restaurar os pacotes NuGet. Execute em uma sessão gráfica para jogar.

Em outra máquina, instale o **SDK .NET 8** (ou compatível superior) e **Godot 4.5.1 na edição .NET**, que inclui C#. A edição comum do Godot não executa estes scripts. Abra `project.godot`, compile pelo botão **Build** e pressione **F6/F5** para executar a cena/projeto. No Linux também pode usar:

```sh
GODOT_BIN="/caminho/para/Godot_mono" ./jogar.sh
```

- Godot .NET: https://godotengine.org/download/archive/4.5.1-stable/
- SDK .NET: https://dotnet.microsoft.com/download/dotnet/8.0

## Controles

No controle PS4, o analógico esquerdo move o personagem e vira os veículos; o direito controla a câmera. Nos veículos, **X acelera, círculo dá ré, L2 freia e triângulo entra/sai**. A pé, X pula e L3 corre. Quadrado usa a ação próxima: loja, diesel, pesca ou plantio/colheita. L1 troca o implemento com o trator parado; R1 baixa/levanta; R3 alterna a câmera; direcional para cima aciona os faróis. No Willys parado, **direcional para baixo ou H** abre/fecha a capota. Options abre o menu; Share salva. Nos menus, X confirma e círculo volta. O editor F2 usa teclado e mouse.

As dicas próximas alternam automaticamente entre teclado e símbolos de PlayStation pelo último dispositivo usado. O HUD não exibe a lista permanente de comandos; consulte **CONTROLES** no menu. A integração foi testada com eventos simulados; a conexão de um DualShock físico ainda precisa ser conferida ao jogar.

| Controle | A pé | Nos veículos |
| --- | --- | --- |
| WASD | Caminhar | W acelera, S freia e engata ré; A/D esterçam |
| Mouse | Olhar | Girar câmera externa ou olhar do assento |
| Shift | Correr | — |
| Espaço | Pular | Frear |
| E ou clique esquerdo | Entrar no veículo próximo | E desce, com o veículo parado e saída livre |
| V | Primeira/terceira pessoa | Terceira/primeira pessoa no assento |
| F | — | Ligar/desligar faróis |
| Roda do mouse | — | Aproximar/afastar câmera externa |
| 1 / 2 / 3 | — | Pá / grade / sem ferramenta (trator parado) |
| R | — | Baixar/levantar a ferramenta selecionada |
| G | Abastecer perto do tambor e do trator | Abastecer parado ao lado do tambor |
| B | Abrir loja perto do balcão | — |
| P | Lançar/recolher linha no ponto de pesca | — |
| C | Plantar/colher no talhão próximo | — |
| H | — | Abrir/fechar capota do Jeep parado |
| F2 | Entrar/sair do modo de construção | Desça antes de construir |
| F5 | Salvar no espaço selecionado | Salvar no espaço selecionado |
| Esc | Pausar/continuar | Pausar/continuar |

Você começa a pé, próximo ao trator amarelo. Aproxime-se até aparecer a indicação **E — entrar**. Ao entrar, a câmera muda automaticamente para terceira pessoa. Segure Espaço até parar e pressione E para descer. A pausa também é acionada quando a janela perde o foco.

## Atividades e progressão

- **Cupinzeiros:** ficam a oeste da sede, junto à estrada. Entre no CBT, pare, aperte **1** para selecionar a pá e **R** para baixá-la. Avance sobre os cupinzeiros: cada um paga R$ 40 e 25 XP. Os 12 rendem ainda um bônus de R$ 200 e 150 XP. O bônus é único por partida. Cada cupinzeiro reaparece após 600 segundos de jogo ativo, quando a área está livre; derrubá-lo novamente rende a recompensa individual.
- **Loja:** o balcão fica ao lado do tambor de diesel. Vá a pé e aperte **B**. Grade: R$ 220; vara: R$ 100; cinco iscas: R$ 20; 20 sementes: R$ 40. A pá já está disponível desde o início.
- **Gradear:** compre a grade, selecione **2** com o trator parado e baixe com **R**. Os três talhões ficam além dos cupinzeiros, com 64 células cada. Passe pela área com a grade traseira: cada célula preparada paga R$ 4 e 3 XP; completar as 64 células do primeiro talhão paga uma única vez um bônus de R$ 250 e 150 XP. O solo preparado fica escuro e com sulcos. Depois de preparar a terra, desça para plantar.
- **Milho:** compre sementes na loja e aproxime-se a pé de uma célula preparada. Aperte **C** (ou quadrado no PS4) para plantar uma semente. Ela cresce em 180 segundos de jogo ativo. Use a mesma ação para colher: cada célula rende R$ 12 e 8 XP, com venda automática. Prepare novamente o solo com a grade antes de replantar.
- **Diesel:** o tanque comporta 80 L e começa com 30 L. Consome combustível em marcha lenta e trabalhando. Pare junto ao tambor e aperte **G** para comprar até 10 L por vez, a R$ 6/L, limitado ao tanque e ao saldo. Sem combustível o motor para.
- **Socorro:** no menu ESC, traz o trator à sede e garante ao menos 5 L. Custa R$ 50, limitado ao saldo disponível, para evitar bloquear uma partida sem dinheiro.
- **Pesca:** compre vara e iscas e vá à margem do lago; também há um ponto sinalizado. **P** lança a linha e consome uma isca. Espere a mensagem **FISGOU** e aperte **P** em até 2,5 segundos. Recolher cedo, tarde ou se afastar perde a tentativa. Cada peixe dá 20 XP; venda na loja por R$ 35 cada.
- O nível aumenta a cada 200 XP. Dinheiro, XP, compras, combustível, peixe, iscas e missões são persistidos. A Hilux preta de 1999 e o Jeep Willys são aproximações estilizadas dirigíveis, com colisão. O Jeep tem capota conversível; pare antes de abrir ou fechar.

## Construção e relevo — F2

O World Builder é integrado ao jogo para testar as mudanças imediatamente com o trator. Entre a pé com **F2**. Use **WASD** para voar, **Q/E** para descer/subir, **Shift** para acelerar e o mouse para olhar. A mira projeta o pincel no terreno.

| Tecla | Ferramenta |
| --- | --- |
| 1 | Elevar terreno |
| 2 | Baixar terreno |
| 3 | Suavizar |
| 4 | Nivelar em relação ao centro do pincel |
| 5 / 6 / 7 | Colocar árvore / cerca / pedra |
| 8 | Remover um objeto colocado por você |
| Clique esquerdo | Aplicar; segure para esculpir |
| Roda do mouse | Alterar raio de 4 a 20 metros |
| R | Girar a cerca ou pedra antes de colocar |
| Z | Desfazer (até 20 operações nesta sessão) |
| F2 | Voltar a jogar |
| F5 | Salvar terreno e construções no espaço selecionado |

O relevo usa uma malha com espaçamento de 3,5 m e colisão correspondente, com alturas de -8 a 12 m. A sede, o lago, a estrada e os limites ficam protegidos para preservar as atividades; edite os pastos ao redor. Até 200 objetos adicionais por partida. Construções originais não podem ser removidas pelo pincel. Cercas são segmentos rígidos; evite declives muito acentuados. A suspensão do trator ainda é simplificada e o veículo não inclina visualmente para acompanhar o solo.

## Três saves

O menu inicial oferece **Novo jogo** e **Carregar**; a pausa oferece **Salvar** e **Carregar**. Cada ação abre os três espaços, com confirmação antes de substituir ou descartar progresso. **F5** atualiza o espaço selecionado durante uma partida; sem seleção, abre a escolha de save. Não há autosave: salve antes de sair.

No Windows, os arquivos ficam em `%APPDATA%\Godot\app_userdata\Vale das Flores\saves\slot-1.json` (e 2/3). Cada substituição guarda o anterior como `.bak`. Para recuperar manualmente, feche o jogo e copie o backup para o respectivo `.json`. Saves inválidos são recusados sem substituir a partida atual.

Também são guardados posições, ocupação dos veículos, faróis, ferramenta selecionada, capota do Jeep, relevo, objetos construídos, sementes, crescimento do milho e tempo de retorno dos cupinzeiros. Ao carregar, o trator fica parado e a ferramenta levantada; uma pescaria em andamento é cancelada. Cada slot tem seu próprio mundo e progresso.

## Incluído

- Personagem procedural com chapéu, corpo visível ao olhar para baixo e vista externa opcional.
- CBT 2105 estilizado com teto baseado na foto, pá frontal selecionável, grade comprável, diesel, rodas animadas e faróis funcionais.
- Motorista sentado visível na câmera externa.
- Aceleração, desaceleração, ré, freio e colisão com cenário. A direção inverte naturalmente em ré.
- Entrada por proximidade e linha de visão; saída bloqueada em movimento e busca de espaço livre para desembarcar.
- Câmera externa com aproximação ao encontrar obstáculos.
- Cenário de cerrado inspirado nas fotos fornecidas: sede clara com entrada acessível, caixa azul elevada, lago acessível, árvores espaçadas, solo seco, caminhos de terra e implementos decorativos.
- Áudio espacial do CBT a partir da gravação fornecida, variando com a aceleração, e passos provisórios de solo seco.
- HUD com velocidade, faróis, estado e comandos contextuais; menu de pausa.

## Modelos e reaproveitamento

A sede e o entorno são uma aproximação visual das fotos e da imagem aérea, com proporções estimadas. A área cercada mede aproximadamente 210 × 208 metros no jogo; isso não representa uma medição da propriedade real. Novas partidas começam com ondulações suaves nos campos externos; saves antigos mantêm as alturas gravadas. O editor permite esculpir o relevo. As 60 fotos de `.tools/mapeamento` orientaram varanda, curral, caixa, margem e vegetação; veja [referências e limites do mapeamento](docs/MAPEAMENTO.md). O lago tem margem acessível e água rasa, sem sistema de natação. É possível pescar na margem; o ponto sinalizado continua disponível. A troca de ferramentas usa teclado ou controle, sem engate físico. Os equipamentos antigos no terreiro continuam decorativos.

O Distribuidora não contém arquivos FBX/GLB: seus modelos são desenhados em C++ com primitivas. `Models.cs` adapta a construção do personagem de `src/world_render.cpp` (`renderManager`) e `src/customers_render.cpp`, e a ideia de rodas segmentadas de `src/world_render.cpp`. As cores, proporções e roupas foram adaptadas para o ambiente rural. Não há dependência de execução do projeto antigo.

O trator e a fazenda são novos modelos procedurais. O CBT é uma **aproximação visual de poucos polígonos**, não uma réplica dimensional nem um modelo licenciado pelo fabricante. Não usa texturas ou modelos baixados. Referência de identificação/cor do CBT 2105: https://www.agrofinder.com.br/produto/19102/trator-cbt-2105.

## Estrutura

- `Scenes/Farm.tscn`: cena de entrada.
- `Scripts/Farm.cs`: interação, câmera, input, pausa e HUD.
- `Scripts/Player.cs`: movimento, corrida, pulo e corpo do jogador.
- `Scripts/Tractor.cs`: modelo do CBT, condução, rodas, motorista e faróis.
- `Scripts/Models.cs`: primitivas reutilizáveis e modelo do fazendeiro.
- `Scripts/FarmWorld.cs`: cenário procedural.
- `Scripts/FarmTerrain.cs` e `Scripts/WorldBuilder.cs`: terreno com colisão e editor integrado.
- `Scripts/FarmActivities.cs`: diesel, cupinzeiros, grade, pesca e loja; `Scripts/FarmCrops.cs` implementa plantio e retorno dos cupinzeiros.
- `Scripts/Hilux.cs`, `Scripts/Jeep.cs` e `Scripts/FarmVehicles.cs`: veículos e interação.
- `Scripts/FarmHud.cs` e `Scripts/FarmSettings.cs`: interface, menus e opções de vídeo.
- `Scripts/SaveData.cs` e `Scripts/FarmMenus.cs`: três saves, validação, backup e menus.
- `Scripts/FarmAudio.cs`: geração das amostras provisórias de passos.
- `Audio/cbt2105-loop.wav`: trecho tratado da gravação do trator; detalhes em `Audio/README.md`.
- `Shaders/`: materiais procedurais do solo seco e da água.
- `Scripts/SmokeTest.cs`: testes integrados usando o mundo físico real.
- `Scripts/GameplayTest.cs`: atividades, economia, relevo e saves isolados dos slots do jogador.

Os modelos são construídos ao executar a cena; a cena no editor começa apenas com o nó raiz. Para alterar o visual, edite os componentes acima. Modelos GLB/FBX poderão substituir os nós visuais mantendo os controladores.

## Validação

```sh
./jogar.sh --headless -- --smoke
./jogar.sh --headless -- --gameplay-test
./jogar.sh --headless -- --input-test
./jogar.sh --headless -- --vehicle-test
```

O teste verifica apoio no chão, caminhada, pulo, alternância de câmera, entrada por proximidade, condução, direção, ré, freio, faróis, pausa, saída e colisão com obstáculos. Encerra com código diferente de zero se alguma verificação falhar.

O script Windows com `-Testar` executa quatro baterias no executável exportado: condução/áudio (`--smoke`), atividades/persistência (`--gameplay-test`), controle/interface (`--input-test`) e veículos (`--vehicle-test`). Os testes de save usam uma pasta única em `user://test-saves/`, sem gravar nos três espaços reais do jogador.

Validação desta versão: **157 verificações aprovadas** (33 de condução/áudio, 60 de atividades/editor/persistência, 30 de controle/interface e 34 de veículos). Executável exportado e menus/efeito retrô conferidos visualmente. O teste de atividades ainda registra um recurso em uso ao encerrar; as verificações funcionais passam.

O cenário também foi conferido por capturas renderizadas em OpenGL no Windows. Os testes automatizados não substituem a avaliação auditiva e dos controles ao jogar.

## Escopo desta primeira versão

A condução usa `CharacterBody3D` com comportamento simplificado, sem suspensão independente ou transmissão mecânica. Há plantio e colheita de milho, Hilux e Jeep dirigíveis. Ainda não há multiplayer, natação ou reprodução dimensional da propriedade. A pesca e o trabalho com implementos são mecânicas iniciais; os modelos são procedurais e estilizados.

`.tools/`, `.godot/`, `bin/` e `obj/` ficam fora do Git. O código usa .NET 8 conforme a base adotada pelo Godot desde 4.4: https://godotengine.org/article/godotsharp-packages-net8/.

