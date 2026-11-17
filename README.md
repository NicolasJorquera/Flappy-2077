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
