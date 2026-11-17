using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Build de release para Google Play (.aab firmado).
///
/// La clave de subida NO está en el repo. Se lee de fuera:
///   FLAPPY_KEYSTORE   ruta del keystore (por defecto ~/keys/flappy2077-upload.keystore)
///   FLAPPY_KEY_ALIAS  alias (por defecto "upload")
///   FLAPPY_KS_PASS    contraseña del keystore
///   FLAPPY_KEY_PASS   contraseña de la clave (por defecto la del keystore)
///
/// Uso:
///   Unity -batchmode -quit -projectPath . -buildTarget Android \
///         -executeMethod BuildAndroid.Release -logFile -
/// </summary>
public static class BuildAndroid
{
    public static void Release()
    {
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string keystore = Env("FLAPPY_KEYSTORE", Path.Combine(home, "keys", "flappy2077-upload.keystore"));
        string storePass = Env("FLAPPY_KS_PASS", null);
        if (!File.Exists(keystore) || string.IsNullOrEmpty(storePass))
        {
            Fail("Falta el keystore (" + keystore + ") o FLAPPY_KS_PASS.");
        }

        var android = NamedBuildTarget.Android;
        PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
        PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)36;
        EditorUserBuildSettings.buildAppBundle = true;

        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.keystoreName = keystore;
        PlayerSettings.Android.keystorePass = storePass;
        PlayerSettings.Android.keyaliasName = Env("FLAPPY_KEY_ALIAS", "upload");
        PlayerSettings.Android.keyaliasPass = Env("FLAPPY_KEY_PASS", storePass);

        Directory.CreateDirectory("Builds");
        string output = Path.Combine("Builds",
            "Flappy2077-" + PlayerSettings.bundleVersion + "-" + PlayerSettings.Android.bundleVersionCode + ".aab");

        BuildReport report;
        try
        {
            report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = output,
                target = BuildTarget.Android,
                options = BuildOptions.None,
            });
        }
        finally
        {
            // Que la ruta local del keystore no acabe guardada en ProjectSettings.asset.
            PlayerSettings.Android.useCustomKeystore = false;
            PlayerSettings.Android.keystoreName = string.Empty;
            PlayerSettings.Android.keystorePass = string.Empty;
            PlayerSettings.Android.keyaliasPass = string.Empty;
        }

        if (report.summary.result != BuildResult.Succeeded)
        {
            Fail("Build fallido: " + report.summary.result);
        }
        Debug.Log("AAB listo: " + output);
    }

    private static string Env(string name, string fallback)
    {
        string value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrEmpty(value) ? fallback : value;
    }

    private static void Fail(string message)
    {
        Debug.LogError(message);
        if (Application.isBatchMode) EditorApplication.Exit(1);
        throw new BuildFailedException(message);
    }
}
