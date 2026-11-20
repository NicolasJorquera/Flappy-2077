using UnityEngine;

/// <summary>
/// Efectos de sonido. Usa PlayOneShot (los sonidos se superponen en vez de cortarse) y aplica
/// el volumen de efectos de Opciones. Los sonidos de UI se generan con Tools/art/generate.py.
/// </summary>
public static class Sfx
{
    private static AudioSource _ui;
    private static AudioClip _click, _back, _toggle, _newBest;

    public static void Play(AudioSource source, AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (source == null || clip == null) return;
        float v = volume * GameSettings.SfxVolume;
        if (v <= 0f) return;
        source.pitch = pitch;
        source.PlayOneShot(clip, v);
    }

    public static void Click() => PlayUi(ref _click, "Audio/ui_click", 0.8f);
    public static void Back() => PlayUi(ref _back, "Audio/ui_back", 0.8f);
    public static void Toggle() => PlayUi(ref _toggle, "Audio/ui_toggle", 0.8f);
    public static void NewBest() => PlayUi(ref _newBest, "Audio/new_best", 0.9f);

    private static void PlayUi(ref AudioClip clip, string path, float volume)
    {
        if (clip == null) clip = Resources.Load<AudioClip>(path);
        if (_ui == null)
        {
            var go = new GameObject("UI Audio");
            Object.DontDestroyOnLoad(go);
            _ui = go.AddComponent<AudioSource>();
            _ui.playOnAwake = false;
            _ui.ignoreListenerPause = true;
        }
        Play(_ui, clip, volume);
    }
}
