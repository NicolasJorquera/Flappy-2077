# Flappy 2077 — diseño (v1.2, rama `feat/opciones-y-arte`)

Documento corto de diseño: qué es el juego, qué se agregó en esta versión y cómo se mantiene.

## Concepto

Un "flappy" retro-futurista: un auto volador cruza Neo City de noche esquivando rascacielos.
Un toque = impulso. Cada hueco superado = 1 punto.

**Estética (se mantiene la original):** cielo negro estrellado, edificios pixel-art morado/azul
con ventanas cian y coral, auto flat-illustration, iluminación 2D tenue (luz global 0,5) con
luces rosadas por edificio. Paleta base: `#9b21d9` `#1129a6` `#40f4d2` `#ff5c5c` `#ff32a1`.

## Flujo de pantallas

```
Menú ──JUGAR──▶ Listo ("toca para volar", el auto flota) ──toque──▶ Jugando
  │                                                         │  ▲
  └─OPCIONES                                       pausa/atrás  │ seguir
                                                            ▼  │
                                                          Pausa ── OPCIONES / MENÚ
Jugando ──choque──▶ Fin (puntaje, récord, rango) ──OTRA VEZ──▶ Listo
                                                └──MENÚ──▶ Menú
```

- "Otra vez" salta el menú y va directo a "Listo".
- La app se pausa sola si pierde el foco (llamada, notificación, cambio de app).
- Botón atrás de Android / Escape: cierra opciones → pausa → menú → salir.
- Intersticial: igual que antes, solo en la pausa natural (al pulsar "Otra vez" o "Menú"
  tras terminar), con los mismos límites de frecuencia.

## Opciones (guardadas en el dispositivo, PlayerPrefs)

| Opción | Valores | Por defecto |
|---|---|---|
| Música | 0–10 | 6 (suena como el volumen original) |
| Efectos | 0–10 | 8 |
| Canción | Aleatoria, Pista 01–10 (se escucha al elegirla) | Aleatoria |
| Idioma | English / Español | idioma del sistema |
| Vibración (solo móvil) | Sí / No | Sí |
| Sacudida de pantalla | Sí / No | Sí |
| Privacidad | botón visible solo si UMP lo exige | — |

## Progresión y game feel

- **Dificultad:** la velocidad sube 0,6 % por punto hasta +40 % (≈ 67 puntos). El intervalo
  entre obstáculos se acorta en proporción, así la distancia entre ellos se mantiene.
- **Rangos / medallas:** 10 bronce (Corredor), 25 plata (Piloto), 50 oro (As de neón),
  100 neón (Leyenda 2077). Récord y partidas jugadas se guardan localmente.
- **Feedback:** inclinación del auto según la velocidad, "squash" al impulsar, estela de
  propulsor, chispas al puntuar (más grandes cada 10/100), explosión de píxeles, sacudida,
  destello y frenado del mundo al chocar; la música se "apaga" (filtro) en pausa y fin.
- **Posición del auto:** a la izquierda del centro (x = −2,6) para ver venir los edificios.

## Mundo

- **Fondo:** cielo con degradado tramado, luna sintética, dos capas de skyline con parallax
  (0,12× y 0,3×), tráfico aéreo lejano en dos profundidades, estrellas que titilan y bruma
  de neón al pie de la ciudad.
- **Obstáculos:** 50 % las 10 combinaciones originales, 50 % pares generados con 3 torres
  nuevas (neón, holográfica, cápsulas) abajo y 2 estructuras colgantes (plataforma, puente)
  arriba, hueco de 2,5 unidades a altura aleatoria. Colisión con la silueta real del sprite y
  la misma luz rosada de los originales.

## Arquitectura

| Archivo | Rol |
|---|---|
| `Assets/LogicScript.cs` | Estados, puntaje, dificultad, pausa, fin. Arma fondo, efectos e interfaz. |
| `Assets/CarScript.cs` | Entrada, física, inclinación, estela, choque. |
| `Assets/BuildingsSpawnScript.cs` | Mezcla prefabs originales y pares generados. |
| `Assets/pickMusic.cs` | Música persistente entre escenas, volumen, canción elegida, filtro. |
| `Assets/Interstitial.cs` | Anuncios (solo Android/iOS) y botón de privacidad. |
| `Assets/Scripts/UI/GameUI.cs` | Toda la interfaz, construida por código. |
| `Assets/Scripts/UI/PixelFont.cs`, `PixelText.cs` | Fuente pixel-art propia (con tildes, Ñ, ¿, ¡). |
| `Assets/Scripts/World/CityBackdrop.cs`, `Fx.cs` | Fondo con parallax; partículas y sacudida. |
| `Assets/Scripts/Core/*` | Opciones, textos ES/EN, efectos de sonido, vibración. |

Los nombres de clase y campos originales se mantienen para no romper las referencias de la
escena y los prefabs. La interfaz antigua (`Start Screen`, `Game Over Screen`, texto de
puntaje) y el fondo de estrellas siguen en la escena pero se ocultan al iniciar.

## Arte y sonido generados

Todo el arte nuevo sale de `Tools/art/generate.py` (Pillow, semilla fija → reproducible):

```bash
python3 -m venv Tools/art/.venv && Tools/art/.venv/bin/pip install pillow==11.3.0
Tools/art/.venv/bin/python Tools/art/generate.py
```

Escribe en `Assets/Resources/Pixel/` (sprites) y `Assets/Resources/Audio/` (sonidos de UI).
`Assets/Editor/PixelArtImporter.cs` los importa con filtro Point, 20 px/unidad y 9-slice.
La fuente pixel-art está definida en código; no hay fuentes de terceros.

## Probar sin teléfono (WebGL)

```bash
/Applications/Unity/Hub/Editor/6000.6.9f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -nographics \
  -projectPath . -buildTarget WebGL -executeMethod BuildPreview.WebGL -logFile -
python3 -m http.server 8077 -d build/webgl
```

En WebGL no hay anuncios ni vibración (el SDK de anuncios solo existe en móvil).
En builds de desarrollo, **F9** activa un piloto automático (útil para partidas largas y capturas).

**Prueba automática sin pantalla** (juega 2 partidas con el piloto y cierra Unity):

```bash
/Applications/Unity/Hub/Editor/6000.6.9f1/Unity.app/Contents/MacOS/Unity -batchmode \
  -projectPath . -executeMethod Playtest.Run -logFile - | grep Playtest
```

**APK de desarrollo** para instalar en un teléfono (`adb install -r build/Flappy2077-dev.apk`):
`-buildTarget Android -executeMethod BuildPreview.AndroidDev`.

## Monetización y tablero

Anuncio recompensado para continuar, compra "Sin anuncios" y ranking de Play Games:
ver [MONETIZACION.md](MONETIZACION.md) (incluye los pasos en AdMob y Play Console).

## Pendiente / ideas siguientes

- Probar en un Android real (vibración, botón atrás, área segura con notch, rendimiento).
- Subir `bundleVersion` a 1.2.0 antes de etiquetar la publicación.
- Nombres reales para las 10 canciones (hoy "Pista 01–10" porque los MP3 no traen título).
- Ideas: coleccionables entre edificios, skins del auto desbloqueables por rango,
  tabla de récords locales, modo daltónico.
