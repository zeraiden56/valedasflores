# Áudio do CBT 2105

Mixagem atual: motor entre −22 dB em marcha lenta e −15 dB sob carga (redução de 10 dB); passos em −18 dB (redução de 8 dB). A atenuação espacial continua ativa.

`cbt2105-loop.wav` deriva do arquivo fornecido pelo usuário:
`src/trator CBT 2105 4x2 gradeando terra.mp3`.

Preparação: trecho de 12 a 26 segundos, mono, 44.100 Hz, filtro passa-altas em 45 Hz e passa-baixas em 6.500 Hz. Transição linear de 400 ms entre a cauda e o início, formando um loop de 13,6 segundos, com pico normalizado a 24000/32768. O original permanece intacto e não é incluído na exportação.

O motor começa em marcha lenta e permanece ligado ao desembarcar enquanto houver diesel. Para quando o tanque esvazia, retoma após abastecer e fica suspenso no editor. Volume e tonalidade respondem suavemente ao acelerador e à velocidade; não há simulação mecânica de RPM nem amostras específicas de partida/desligamento. A gravação é de trabalho no campo, portanto também pode conter ruídos do ambiente e do implemento. A qualidade da escuta ainda deve ser conferida pelo usuário.

Os passos são amostras provisórias de solo seco sintetizadas em `Scripts/FarmAudio.cs`, sem arquivos de terceiros. São disparados por distância realmente percorrida no chão, param ao dirigir e respeitam a pausa. Ainda não há troca de som por material de piso.

Os emissores usam [AudioStreamPlayer3D](https://docs.godotengine.org/en/4.5/classes/class_audiostreamplayer3d.html); o loop usa [AudioStreamWAV](https://docs.godotengine.org/en/4.5/classes/class_audiostreamwav.html).
