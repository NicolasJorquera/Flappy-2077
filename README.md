# Flappy 2077

Unity game based on Flappy Bird.

## Build de release (Google Play)

Requiere Unity 6 con Android Build Support. La clave de subida **no** está en el repo: vive en `~/keys/flappy2077-upload.keystore` y su contraseña en el Llavero (`flappy2077-keystore`).

```bash
export FLAPPY_KS_PASS="$(security find-generic-password -s flappy2077-keystore -w)"
/Applications/Unity/Hub/Editor/<versión>/Unity.app/Contents/MacOS/Unity -batchmode -quit -nographics \
  -projectPath . -buildTarget Android -executeMethod BuildAndroid.Release -logFile -
```

El .aab queda en `Builds/`.

## Publicación automática (GitHub Actions)

`.github/workflows/play-store.yml` compila el .aab con GameCI y lo sube a Play con la cuenta de servicio:

- **push a `main`** → pista *internal* (completed).
- **tag `vX.Y.Z`** (debe coincidir con `bundleVersion`) → *production* al 20 %.
- **Run workflow** manual → pista y estado a elegir.

El `versionCode` es el número de ejecución + `VERSION_CODE_OFFSET` (100 por defecto). Antes de subir, el job comprueba que el .aab no tenga firma de depuración, que el paquete sea `com.gonisoma.Flappy2077`, que el target sea 36 y que las `.so` estén alineadas a 16 KB.

Configuración en GitHub → Settings → Environments → `play-store`:

- `ANDROID_KEYSTORE_BASE64`, `ANDROID_KEYSTORE_PASSWORD`, `ANDROID_KEY_ALIAS` (`upload`), `ANDROID_KEY_PASSWORD`.
- `PLAY_SERVICE_ACCOUNT_JSON`: la misma cuenta de servicio de las otras apps, con acceso a Flappy 2077 en Play Console.
- `UNITY_EMAIL`, `UNITY_PASSWORD` y `UNITY_LICENSE` (contenido del `.ulf` de la licencia Personal; guía de GameCI: https://game-ci.com/docs/github/activation).
