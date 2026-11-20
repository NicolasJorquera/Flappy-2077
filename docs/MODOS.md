# Flappy 2077 — modos de juego, jefes y progreso (rama `feat/modo-arcade`)

Implementado el 2026-11-20 mientras Nicolás estaba fuera; **pendiente de su revisión**.
Todo el balance (números de abajo) es un primer ajuste para corregir jugando.

## Menú

`CLÁSICO` · `ARCADE` · `DIARIO` (cada uno con su récord debajo), `OPCIONES` y `PILOTO NV n`.
El auto del menú queda a la derecha del título (se ajusta en pantallas 4:3).

## Modos

| Modo | Reglas | Récord | Ranking (Play Games) | Continuar con anuncio |
|---|---|---|---|---|
| Clásico | El original: 1 punto por edificio. | `score.best` (el de siempre) | "Best score" `CgkIm7bXxasGEAIQAQ` (publicado) | Sí |
| Arcade | Chips, power-ups, combo y jefes. | `score.best.arcade` | "Arcade" `CgkIm7bXxasGEAIQAw` (**borrador**) | Sí |
| Diario | Reglas de Arcade con la **misma ciudad para todos** ese día (semilla = fecha UTC aaaammdd). | Récord del día | "Daily challenge" `CgkIm7bXxasGEAIQBA` (**borrador**) | No (justo para todos) |

Los tableros en borrador solo los ven los testers hasta que se publiquen en Play Console
(Play Games Services → Publicación). Ojo: una vez publicados no se pueden borrar.

## Arcade (`Assets/Scripts/Arcade/ArcadeDirector.cs`)

- **Chips** (se toman por cercanía; el imán los atrae):
  - +1 cian en el centro del hueco (55 % de los obstáculos),
  - +5 rosa cerca del borde (22 %),
  - +10 dorado pegado al borde (8 %),
  - arcos de 4–6 chips entre edificios (45 %).
- **Power-ups** cada 6–9 obstáculos: escudo (aguanta 1 choque, luego 1,4 s de invulnerabilidad),
  imán 8 s, ciudad lenta 5 s (mundo al 60 %), puntos dobles 10 s. Al continuar con anuncio se
  regala un escudo.
- **Combo "perfecto"**: cruzar a ≤ 0,32 del centro del hueco. Cada 3 seguidos sube el
  multiplicador (×2, ×3 máx.); un cruce normal lo reinicia. Afecta edificios, chips y jefes.
- La dificultad sube por edificios superados (no por puntos), igual que en el clásico.
- Los obstáculos se generan **por distancia** (no por tiempo): la separación es la misma aunque el
  mundo vaya más rápido, lento o se detenga.

## Jefes (`Assets/Scripts/Arcade/BossFight.cs`)

Cada **25 edificios**: alarma, la ciudad se despeja y entra un jefe por la derecha. El auto
**dispara solo** (5 tiros/s); el jugador apunta con la altura y esquiva con el mismo toque.

| # | Jefe | Vida | Ataques |
|---|---|---|---|
| 1 | Dron patrulla | 18 | Abanicos de 3 disparos (cada 3.ª ráfaga, 5) |
| 2 | Cañonera corporativa | 30 | Sigue tu altura; ráfagas de 3 rectos; láser horizontal con aviso de 0,9 s |
| 3 | Gusano del metro | 40 | Recorre la derecha en "ocho"; suelta minas que flotan hacia ti |

Se repiten en ciclo; cada vuelta +35 % de vida y +20 % de velocidad. Derrotarlo da
25 × (vuelta) puntos × multiplicadores y suelta un power-up. Si te golpean: escudo o choque.
El disparo usa el `shoot.wav` original del juego (estaba sin usar).

## Galería de jefes (práctica)

Botón **JEFES** en el menú, **bloqueado hasta que llegues a pelear con un jefe en Arcade o
Diario** (aviso: "Llega a un jefe en ARCADE para desbloquear"). Cada tarjeta se desbloquea al
encontrar a ese jefe; antes se ve como silueta con "???".

**Pelear** = práctica contra ese único jefe, sin edificios. Si lo vences aparece
"¡JEFE DERROTADO!"; si te golpea, "¡CHOCASTE!". En ambos casos: otra vez o menú.
La práctica **no** da récord, ranking, misiones ni "continuar" con anuncio. La tarjeta muestra
si ese jefe ya fue derrotado alguna vez (en cualquier modo).

Prueba automática: en Arcade el piloto llegó al jefe, quedó marcado como visto y lo derrotó
(tras continuar); la práctica no movió la XP.

## Misiones, niveles y colores (`Assets/Scripts/Progression/`)

- **3 misiones activas** (se guardan en el dispositivo); al cumplir una se gana XP y aparece otra.
  Tipos: edificios/puntos/racha en una partida; chips, power-ups, jefes y perfectos acumulados;
  jugar N partidas; jugar el diario. Las metas crecen con el nivel.
- **Nivel de piloto**: cada nivel cuesta 200 XP más que el anterior (200, 400, 600…).
- **8 colores del auto** (`Tools/art/skins.py`): Original (1), Neón rosa (2), Cian hielo (3),
  Tóxico (4), Oro (6), Medianoche (8), Carmesí (10), Holograma (12, cambia de color en vivo).
  Cada color tiene su estela. Se eligen en la pantalla **PILOTO**.
- La pantalla de choque muestra la misión cumplida (+XP) y avisa la subida de nivel y el color
  desbloqueado.

## Arte nuevo (`Tools/art/generate.py`)

- 4 edificios inferiores: pagoda neón, torre corporativa de vidrio, bloque de tiendas, torre de
  antenas. 3 superiores: pantalla colgante "2077", contenedor de carga, monorriel.
- Fondo: dirigible con pantalla y drones de reparto.
- Chips, cápsulas de power-up, 3 jefes, proyectiles, minas y sonidos (chips, power-up, escudo,
  perfecto, alarma, impacto, explosión).

## Cómo se probó

- Prueba automática (`PLAYTEST_MODES=Classic,Arcade,Daily ... -executeMethod Playtest.Run`):
  - Arcade: 26 edificios, 30–57 chips, 9–15 perfectos, 2–4 power-ups.
  - El jefe aparece a los 25 edificios.
  - "Continuar" conserva el puntaje.
  - Las misiones suben de nivel.
  - Diario: dos partidas con la misma ciudad.
- El piloto automático **no apunta** a los jefes: no se verificó derrotar uno jugando.
  Revisar en el teléfono la dificultad de cada jefe.
- Revisión visual en WebGL: menú, pantalla Piloto, pantalla de choque del Arcade.
