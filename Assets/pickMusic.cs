using UnityEngine;

/// <summary>
/// Reproductor de música de fondo. Sobrevive a la recarga de escena (la canción no se corta
/// al reintentar), respeta el volumen y la canción elegidos en Opciones, y suena "apagada"
/// (filtro paso bajo + menos volumen) en pausa y en game over.
/// El nombre de la clase se mantiene por compatibilidad con la escena.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class pickMusic : MonoBehaviour
{
    public AudioClip song1;
    public AudioClip song2;
    public AudioClip song3;
    public AudioClip song4;
    public AudioClip song5;
    public AudioClip song6;
    public AudioClip song7;
    public AudioClip song8;
    public AudioClip song9;
    public AudioClip song10;

    private const float BaseVolume = 0.55f; // con el volumen por defecto (6/10) suena como antes (0.2)
    private const float MuffledCutoff = 900f;

    public static pickMusic Instance { get; private set; }

    private AudioClip[] _playlist;
    private AudioSource _source;
    private AudioLowPassFilter _lowPass;
    private int _current = -1;
    private bool _muffled;
    private float _duck = 1f;

    public int TrackCount => _playlist?.Length ?? 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _playlist = new[] { song1, song2, song3, song4, song5, song6, song7, song8, song9, song10 };
        _source = GetComponent<AudioSource>();
        _source.playOnAwake = false;
        _source.loop = false;
        _source.ignoreListenerPause = true;
        _lowPass = GetComponent<AudioLowPassFilter>();
        if (_lowPass == null) _lowPass = gameObject.AddComponent<AudioLowPassFilter>();
        _lowPass.cutoffFrequency = 22000f;

        GameSettings.Changed += ApplyVolume;
        ApplyVolume();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            GameSettings.Changed -= ApplyVolume;
        }
    }

    private void Update()
    {
        if (Instance != this) return; // copia de la escena recargada, a punto de destruirse
        // Suaviza filtro y volumen con tiempo real: también funciona en pausa (timeScale = 0).
        float targetCut = _muffled ? MuffledCutoff : 22000f;
        _lowPass.cutoffFrequency = Mathf.Lerp(_lowPass.cutoffFrequency, targetCut, 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime));
        _duck = Mathf.MoveTowards(_duck, _muffled ? 0.55f : 1f, Time.unscaledDeltaTime * 1.5f);
        ApplyVolume();

        if (!_source.isPlaying && Application.isFocused) PlayNext();
    }

    /// <summary>Pausa / game over: suena apagada. Jugando: normal.</summary>
    public void SetMuffled(bool muffled) => _muffled = muffled;

    /// <summary>Aplica la canción elegida en Opciones (si es fija, empieza a sonar ya).</summary>
    public void ApplyTrackChoice()
    {
        int track = GameSettings.MusicTrack;
        if (track == GameSettings.Shuffle || track >= _playlist.Length)
        {
            if (!_source.isPlaying) PlayNext();
            return;
        }
        if (track != _current || !_source.isPlaying) Play(track);
    }

    private void PlayNext()
    {
        if (_playlist.Length == 0) return;
        int track = GameSettings.MusicTrack;
        if (track != GameSettings.Shuffle && track < _playlist.Length)
        {
            Play(track); // canción fija: se repite
            return;
        }
        int next = 0;
        if (_playlist.Length > 1)
        {
            do next = Random.Range(0, _playlist.Length); while (next == _current);
        }
        Play(next);
    }

    private void Play(int index)
    {
        if (_playlist[index] == null) return;
        _current = index;
        _source.clip = _playlist[index];
        _source.Play();
    }

    private void ApplyVolume()
    {
        if (_source != null) _source.volume = BaseVolume * GameSettings.MusicVolume * _duck;
    }
}
