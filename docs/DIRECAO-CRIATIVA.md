# Vale das Flores — direção visual e divisão de trabalho

## Direção

Referência de época: jogos 3D de console da geração PS2. Formas de poucos polígonos, silhuetas reconhecíveis, detalhes econômicos e paleta rural: terra ocre, capim seco, vegetação oliva, azul da caixa-d'água e amarelo envelhecido do CBT. O cenário é uma interpretação estilizada das fotos; dimensões não medidas continuam estimativas.

O mundo e os menus recebem pixelização suave por padrão (blocos de 2 pixels), em janela 1920 × 1080. Os painéis têm bordas retas, contraste claro e foco de navegação visível. F8 alterna suave, clássico e nítido durante a sessão. O HUD mostra ações próximas, sem lista permanente de comandos; as instruções completas ficam em CONTROLES. Os avisos alternam entre teclado e símbolos de PlayStation conforme o último dispositivo usado.

## Frentes dos agentes

| Frente | Responsabilidade | Critério de entrega |
| --- | --- | --- |
| Modelos por fotos | CBT, veículos, construções, árvores e objetos rurais | Comparar silhueta, proporções, materiais e detalhes visíveis; preservar pontos de interação e colisões |
| Lógica | Diesel, atividades, economia, construção e três saves | Evitar duplicação de recompensas, travamentos e perda de dados; testar os fluxos alterados |
| UX/UI | Menu inicial/pausa, loja, saves, HUD e comandos | Explicar ações no contexto, reduzir excesso de texto e manter leitura/foco claros |
| Integração | Efeito visual, compilação, testes e capturas | Conferir as alterações em conjunto e gerar o executável Windows |

Essas frentes são tarefas coordenadas na conversa. Não são personagens do jogo nem processos permanentes executados dentro dele.

## Novas fotos

Ao enviar uma referência, indique qual objeto deve ser alterado e, se possível, uma medida aproximada. Fotos de frente, lado e traseira ajudam a reconstruir a forma. Para o terreno, uma distância conhecida e indicação de subida/descida ajudam mais que uma imagem aérea sozinha. Pessoas e animais presentes por acaso nas fotos não fazem parte dos modelos pedidos.

## Integração

Cada frente deve ter arquivos sob sua responsabilidade durante o trabalho simultâneo. Mudanças nas interfaces entre sistemas são combinadas antes de editar arquivos de outra frente. O resultado só é considerado pronto após compilação, testes relevantes e conferência visual no executável exportado.
