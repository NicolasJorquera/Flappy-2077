using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Build WebGL de previsualización (no se publica): permite probar el juego en un navegador
/// sin dispositivo Android.
///
///   Unity -batchmode -quit -projectPath . -buildTarget WebGL \
///         -executeMethod BuildPreview.WebGL -logFile -
///
/// Carpeta de salida: variable FLAPPY_PREVIEW_OUT o build/webgl.
/// </summary>
public static class BuildPreview
{
    public static void WebGL()
    {
        string output = Environment.GetEnvironmentVariable("FLAPPY_PREVIEW_OUT");
        if (string.IsNullOrEmpty(output)) output = Path.Combine("build", "webgl");
        Directory.CreateDirectory(output);

        // Sin compresión: así cualquier servidor estático sirve los archivos sin cabeceras especiales.
        // Se restaura al terminar para no ensuciar ProjectSettings.asset.
        var previousCompression = PlayerSettings.WebGL.compressionFormat;
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;

        BuildReport report;
        try
        {
            report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = output,
                target = BuildTarget.WebGL,
                options = BuildOptions.Development,
            });
        }
        finally
        {
            PlayerSettings.WebGL.compressionFormat = previousCompression;
            AssetDatabase.SaveAssets();
        }

        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError("Build WebGL fallido: " + report.summary.result);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            return;
        }
        Debug.Log("WebGL listo: " + output);
    }

    /// <summary>
    /// APK de desarrollo firmado con la clave de depuración, para instalar en un teléfono de prueba
    /// (adb install -r build/Flappy2077-dev.apk). No sirve para Play: allí va BuildAndroid.Release.
    /// Incluye el piloto automático (F9 con teclado) y los anuncios de prueba de Google.
    /// </summary>
    public static void AndroidDev()
    {
        string output = Environment.GetEnvironmentVariable("FLAPPY_PREVIEW_OUT");
        if (string.IsNullOrEmpty(output)) output = Path.Combine("build", "Flappy2077-dev.apk");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));

        bool previousBundle = EditorUserBuildSettings.buildAppBundle;
        EditorUserBuildSettings.buildAppBundle = false;
        PlayerSettings.Android.useCustomKeystore = false;
        BuildReport report;
        try
        {
            report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = output,
                target = BuildTarget.Android,
                options = BuildOptions.Development,
            });
        }
        finally
        {
            EditorUserBuildSettings.buildAppBundle = previousBundle;
        }

        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError("Build Android de desarrollo fallido: " + report.summary.result);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            return;
        }
        Debug.Log("APK listo: " + output);
    }
}
