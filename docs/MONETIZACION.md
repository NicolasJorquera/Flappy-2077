# Flappy 2077 — monetización y tablero mundial

Decidido por Nicolás (2026-11-20): intersticial (ya existía) + **anuncio recompensado**
("continuar"), compra única **"Sin anuncios" a US$1.99** y **tablero mundial con Google Play
Games Services**.

## Qué hace el juego

| Pieza | Comportamiento | Archivo |
|---|---|---|
| Intersticial | Igual que antes: solo al salir de la pantalla de choque, cada ≥3 partidas y ≥3 min. Nunca si compró "Sin anuncios". | `Assets/Interstitial.cs` |
| Recompensado | Al chocar con al menos 1 punto aparece "CONTINUAR - VER ANUNCIO", **una vez por partida**. Si lo ve completo, despeja los edificios cercanos y vuelve a "toca para volar" con el mismo puntaje. Tras verlo, el próximo intersticial espera el ciclo completo. Se mantiene aunque compre "Sin anuncios" porque es opcional. | `Assets/Interstitial.cs`, `LogicScript.ContinueWithAd` |
| Sin anuncios | Producto no consumible `remove_ads`. Botón en el menú (con el precio local que entrega Google) y fila en Opciones. Se restaura sola al reinstalar y se revoca si Google la reembolsa. | `Assets/Scripts/Monetization/NoAdsStore.cs` (Unity IAP 5.5.1) |
| Tablero | Botón de trofeo en el menú y en el choque; abre el ranking de Google (global, semanal, diario). Sube el puntaje al chocar y el récord local al iniciar sesión. **Inactivo hasta configurarlo** (ver abajo). | `Assets/Scripts/Monetization/Leaderboard.cs` |

Mientras falten los IDs, nada se rompe: sin ID de recompensado no se ofrece "continuar" (en
builds de desarrollo se usa el anuncio de prueba de Google), sin producto en Play no aparece
"Sin anuncios", y sin Play Games no aparece el trofeo.

## Lo que tiene que hacer Nicolás (cuentas externas)

### 1. AdMob — anuncio recompensado
1. AdMob → Apps → Flappy 2077 → Ad units → **Add ad unit → Rewarded**. Nombre: `continue`.
   Recompensa: `1` `continue` (el valor no se usa).
2. Pasarme el ID (`ca-app-pub-3038495459046159/…`) → va en `RewardedUnitId` de `Interstitial.cs`.

### 2. Play Console — perfil de comerciante y producto
1. **Perfil de pagos (comerciante)**: Play Console → Setup → Payments profile. Chile figura
   como ubicación admitida para registro de comerciante, con pagos por transferencia en USD
   ([ubicaciones](https://support.google.com/googleplay/android-developer/answer/9306917?hl=en),
   [cuenta bancaria](https://support.google.com/googleplay/android-developer/answer/7161440?hl=en),
   consultado 2026-11-20). **El vínculo Play Console ↔ perfil de pagos no se puede cambiar después.**
2. Monetize → Products → In-app products → **Create product**:
   - ID: `remove_ads` (exacto), nombre "Remove ads", descripción "Removes ads between runs.
     Optional rewarded ads stay available." Precio US$1.99 (Google calcula los demás países).
   - Activar.
3. La compra solo funciona con la app instalada **desde Play** (pista de prueba interna) y con
   tu cuenta como **license tester** (Setup → License testing) para probar sin cobro real.
   En el APK de desarrollo instalado por cable la tienda no carga y el botón no aparece.

### 3. Play Games Services (tablero) — configurado 2026-11-20

- Proyecto de Play Games creado en Play Console, vinculado al proyecto de Google Cloud
  **SmartMirror** (`smartmirror-1612697544963`, reutilizado porque la cuenta llegó al límite de
  proyectos; elegido por Nicolás). Nombre visible: "Flappy 2077". **App ID: `217847290651`**.
- Credenciales Android (clientes OAuth en ese proyecto):
  - "Flappy 2077": SHA-1 de la firma de Google Play (`73:86:87:…:FB:21`), usada para instalaciones
    nuevas.
  - "Flappy 2077 (debug)": SHA-1 de la clave de depuración (`0A:5D:2C:…:41:9E`), para el APK de
    prueba instalado por cable.
- Tablero **"Best score"** `CgkIm7bXxasGEAIQAQ`: numérico, mayor primero, protección contra
  manipulación activa, límites 0–100000.
- En el juego: plugin `com.google.play.games` 2.3.0 incluido en `Packages/` (Apache-2.0; su
  dependencia apunta al gestor de Unity), setup con `PlayGamesSetup.Run` (escribe el App ID en
  `Assets/Plugins/Android/GooglePlayGamesManifest.androidlib`) y `FLAPPY_PLAY_GAMES` activo en
  Android.
- **Play Games publicado 2026-11-20** (ícono = logo de la app, gráfico destacado de
  `Tools/art/store_graphics.py`, categoría Arcade). Google indica que puede tardar unas horas en
  estar disponible para todos. Los jugadores solo verán el ranking cuando se publique en Play una
  versión del juego que incluya este código (la 1.1.0 actual no lo tiene).

### 4. Ficha de Play
- **Seguridad de los datos**: declarar compras dentro de la app y, con el tablero, que se usa
  el perfil de Play Games (nombre/ID de jugador, puntajes).
- La ficha mostrará "Contiene anuncios · Ofrece compras en la app".

## Dinero e impuestos (verificar antes de publicar)

- **Comisión de Google**: en 2026 Google cambió el esquema (comisión de servicio + comisión de
  facturación, por región, con despliegue hasta septiembre de 2027). Las cifras varían entre el
  esquema antiguo (15 % el primer US$1M anual y 30 % sobre eso) y el nuevo (p. ej. 20 % estándar
  o 10 % sobre el primer US$1M en las regiones ya migradas). Hay que revisar en Play Console qué
  aplica a Chile y desde cuándo
  ([tarifas nuevas](https://support.google.com/googleplay/android-developer/answer/16954621?hl=en),
  [blog jun-2026](https://android-developers.googleblog.com/2026/06/play-expanded-billing.html),
  consultado 2026-11-20). No hay cifras de ingresos: dependen de descargas y retención.
- **IVA en Chile**: según la ayuda de Google, si el comerciante está en Chile, **es responsable
  de determinar, cobrar y enterar el IVA** de las compras hechas por clientes en Chile; si no está
  registrado como contribuyente de IVA, Google aplica un 19 % de IVA sobre su comisión
  ([impuestos](https://support.google.com/googleplay/android-developer/answer/138000?hl=en),
  consultado 2026-11-20). **Consultar con un contador** (inicio de actividades / SII) antes de
  activar la compra.

## Bug corregido en los anuncios (afecta a la versión publicada 1.1.0)

En un Pixel 7 (Android 17) el log mostró
`IllegalStateException: MobileAds.initialize() must be called prior to setting the app volume`.
El código de `main` llamaba `MobileAds.SetApplicationVolume` **antes** de `MobileAds.Initialize`;
la excepción cortaba `InitializeIfAllowed` y el SDK nunca se iniciaba, así que probablemente la
1.1.0 no muestra anuncios. En esta rama el volumen se fija dentro del callback de inicialización.
Conviene confirmarlo en AdMob (impresiones de la app ≈ 0 desde la 1.1.0).

## Pendiente técnico

- Validación local de recibos (Google Play): opcional; requiere la clave de licencia de Play
  Console para generar `GooglePlayTangle` (Services → In-App Purchasing → Receipt Validation
  Obfuscator). Sin ella, una compra falsificada en un teléfono rooteado podría quitar anuncios;
  el riesgo económico es bajo.
- Probar en teléfono real: anuncio recompensado de prueba, compra con license tester, tablero.
