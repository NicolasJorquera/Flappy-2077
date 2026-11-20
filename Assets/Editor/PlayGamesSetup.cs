using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

/// <summary>
/// Configura Google Play Games en modo batch (equivale a Window → Google Play Games → Setup →
/// Android setup) y activa FLAPPY_PLAY_GAMES. Los recursos vienen de Play Console →
/// Play Games Services → Tablas de clasificación → Obtener recursos.
///
///   Unity -batchmode -quit -projectPath . -buildTarget Android -executeMethod PlayGamesSetup.Run
/// </summary>
public static class PlayGamesSetup
{
    private const string ResourcesXml =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<resources>
  <string name=""app_id"" translatable=""false"">217847290651</string>
  <string name=""package_name"" translatable=""false"">com.gonisoma.Flappy2077</string>
  <string name=""leaderboard_best_score"" translatable=""false"">CgkIm7bXxasGEAIQAQ</string>
</resources>";

    public const string Define = "FLAPPY_PLAY_GAMES";

    public static void Run()
    {
        bool ok = GooglePlayGames.Editor.GPGSAndroidSetupUI.PerformSetup(
            null, "Assets/Scripts/Monetization", "GPGSIds", ResourcesXml, null);
        if (!ok)
        {
            Debug.LogError("Play Games: setup fallido");
            if (Application.isBatchMode) EditorApplication.Exit(1);
            return;
        }

        var target = NamedBuildTarget.Android;
        string defines = PlayerSettings.GetScriptingDefineSymbols(target);
        if (!(";" + defines + ";").Contains(";" + Define + ";"))
        {
            PlayerSettings.SetScriptingDefineSymbols(target, string.IsNullOrEmpty(defines) ? Define : defines + ";" + Define);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Play Games: setup listo");
    }
}
