# Vale das Flores

Protótipo 3D de simulador de fazenda em **C# / Godot 4.5.1 .NET**, com personagem em primeira pessoa e trator dirigível inspirado no **CBT 2105**. Projeto independente de Distribuidora Simulator.

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

| Controle | A pé | No trator |
| --- | --- | --- |
| WASD | Caminhar | W acelera, S freia e engata ré; A/D esterçam |
| Mouse | Olhar | Girar câmera externa ou olhar do assento |
| Shift | Correr | — |
| Espaço | Pular | Frear |
| E | Entrar, próximo do trator | Descer, com o trator parado e saída livre |
| V | Primeira/terceira pessoa | Terceira/primeira pessoa no assento |
| F | — | Ligar/desligar faróis |
| Roda do mouse | — | Aproximar/afastar câmera externa |
| Esc | Pausar/continuar | Pausar/continuar |

Você começa a pé, próximo ao trator amarelo. Aproxime-se até aparecer a indicação **E — entrar**. Ao entrar, a câmera muda automaticamente para terceira pessoa. Segure Espaço até parar e pressione E para descer. A pausa também é acionada quando a janela perde o foco.

## Incluído

- Personagem procedural com chapéu, corpo visível ao olhar para baixo e vista externa opcional.
- CBT 2105 estilizado: capô amarelo, grade, escapamento, assento aberto, para-lamas, identificação lateral, rodas traseiras grandes, rodas dianteiras esterçáveis, pneus animados, volante e faróis funcionais.
- Motorista sentado visível na câmera externa.
- Aceleração, desaceleração, ré, freio e colisão com cenário. A direção inverte naturalmente em ré.
- Entrada por proximidade e linha de visão; saída bloqueada em movimento e busca de espaço livre para desembarcar.
- Câmera externa com aproximação ao encontrar obstáculos.
- Fazenda com sede, galpão acessível, árvores, flores, estrada, talhões decorativos, cercas e limites físicos.
- HUD com velocidade, faróis, estado e comandos contextuais; menu de pausa.

## Modelos e reaproveitamento

O Distribuidora não contém arquivos FBX/GLB: seus modelos são desenhados em C++ com primitivas. `Models.cs` adapta a construção do personagem de `src/world_render.cpp` (`renderManager`) e `src/customers_render.cpp`, e a ideia de rodas segmentadas de `src/world_render.cpp`. As cores, proporções e roupas foram adaptadas para o ambiente rural. Não há dependência de execução do projeto antigo.

O trator e a fazenda são novos modelos procedurais. O CBT é uma **aproximação visual de poucos polígonos**, não uma réplica dimensional nem um modelo licenciado pelo fabricante. Não usa texturas ou modelos baixados. Referência de identificação/cor do CBT 2105: https://www.agrofinder.com.br/produto/19102/trator-cbt-2105.

## Estrutura

- `Scenes/Farm.tscn`: cena de entrada.
- `Scripts/Farm.cs`: interação, câmera, input, pausa e HUD.
- `Scripts/Player.cs`: movimento, corrida, pulo e corpo do jogador.
- `Scripts/Tractor.cs`: modelo do CBT, condução, rodas, motorista e faróis.
- `Scripts/Models.cs`: primitivas reutilizáveis e modelo do fazendeiro.
- `Scripts/FarmWorld.cs`: cenário procedural.
- `Scripts/SmokeTest.cs`: testes integrados usando o mundo físico real.

Os modelos são construídos ao executar a cena; a cena no editor começa apenas com o nó raiz. Para alterar o visual, edite os componentes acima. Modelos GLB/FBX poderão substituir os nós visuais mantendo os controladores.

## Validação

```sh
./jogar.sh --headless -- --smoke
```

O teste verifica apoio no chão, caminhada, pulo, alternância de câmera, entrada por proximidade, condução, direção, ré, freio, faróis, pausa, saída e colisão com obstáculos. Encerra com código diferente de zero se alguma verificação falhar.

Validação executada nesta entrega: compilação sem erros e sem avisos; **25 verificações integradas passaram** no Godot 4.5.1 .NET em modo headless.

O teste sem janela não substitui a conferência visual e dos controles na sessão gráfica; essa conferência ainda não foi realizada.

## Escopo desta primeira versão

A condução usa `CharacterBody3D` com comportamento simplificado, sem suspensão independente ou transmissão mecânica. O solo jogável é plano. Ainda não há cultivo interativo, implementos, economia, som, salvamento ou exportação pronta para Windows. A intenção desta base é validar exploração e condução antes de acrescentar os sistemas de fazenda.

`.tools/`, `.godot/`, `bin/` e `obj/` ficam fora do Git. O código usa .NET 8 conforme a base adotada pelo Godot desde 4.4: https://godotengine.org/article/godotsharp-packages-net8/.
