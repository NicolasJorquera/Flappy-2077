using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum GameState { Menu, Ready, Playing, Paused, Over }

/// <summary>
/// Gestor de la partida: estados, puntaje, récord, dificultad, pausa y fin de partida.
/// Arma en tiempo de ejecución el fondo de ciudad, los efectos y la interfaz (<see cref="GameUI"/>).
/// El nombre de la clase y sus campos se mantienen para no romper las referencias de la escena.
/// </summary>
public class LogicScript : MonoBehaviour
{
    public int playerScore;
    public Text scoreText;              // UI antigua: se oculta
    public GameObject startScreen;      // UI antigua: se oculta
    public GameObject gameOverScreen;   // UI antigua: se oculta
    public GameObject car;
    public GameObject buildingsSpawner;
    public CarScript carScript;
    public AudioClip onePoint;
    public AudioClip tenPoints;
    public AudioClip hundredPoints;
    public AudioClip thousandPoints;
    public AudioClip gameOverSound;
    public GameObject backgroundImage;  // fondo antiguo de estrellas: lo reemplaza CityBackdrop

    public const float BaseWorldSpeed = 5f;

    // Rangos por puntaje (medallas). Índice 0 = sin medalla.
    public static readonly int[] RankThresholds = { 0, 10, 25, 50, 100 };
    public static readonly string[] RankKeys = { "rank_none", "rank_bronze", "rank_silver", "rank_gold", "rank_neon" };

    /// <summary>Al recargar la escena desde "Otra vez" se salta el menú.</summary>
    private static bool _skipMenuOnLoad;

    /// <summary>Multiplicador por dificultad (1 → 1,4 según el puntaje).</summary>
    public static float DifficultySpeed { get; private set; } = 1f;
    /// <summary>Multiplicador efectivo para todo lo que se desplaza: dificultad × frenado al chocar.</summary>
    public static float SpeedFactor { get; private set; } = 1f;
    /// <summary>Velocidad del mundo en unidades/s (fondo, estela).</summary>
    public static float WorldSpeed => BaseWorldSpeed * SpeedFactor;

    public GameState State { get; private set; }
    /// <summary>Frame en que se reanudó la pausa: esa misma pulsación no debe impulsar el auto.</summary>
    public int ResumeFrame { get; private set; } = -1;
    /// <summary>True si la escena que carga ahora va directo a "listo" (salta el menú).</summary>
    public static bool WillSkipMenu => _skipMenuOnLoad;
    public bool IsNewBest { get; private set; }

    private Rigidbody2D _rb;
    private BuildingsSpawnScript _spawner;
    private AudioSource _audio;
    private GameUI _ui;
    private float _brake = 1f;
    private bool _runActive;   // hay una partida empezada que aún no se contó como terminada
    private bool _continued;   // ya usó su "continuar" en esta partida

    /// <summary>¿Se ofrece "continuar viendo un anuncio"? Una vez por partida y con anuncio listo.</summary>
    public bool CanContinue => State == GameState.Over && !_continued && playerScore > 0 && Interstitial.RewardedReady;

    private void Awake()
    {
        DifficultySpeed = 1f;
        SpeedFactor = 1f;
    }

    private void Start()
    {
        Application.targetFrameRate = Mathf.Max(60, Mathf.RoundToInt((float)Screen.currentResolution.refreshRateRatio.value));
        Time.timeScale = 1f;

        _rb = car.GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0;
        _spawner = buildingsSpawner.GetComponent<BuildingsSpawnScript>();
        _spawner.spawn = false;
        _audio = GetComponent<AudioSource>();
        _audio.playOnAwake = false;

        // Reemplazo de la UI y el fondo antiguos.
        if (startScreen != null) startScreen.SetActive(false);
        if (gameOverScreen != null) gameOverScreen.SetActive(false);
        if (scoreText != null) scoreText.gameObject.SetActive(false);
        if (backgroundImage != null) backgroundImage.SetActive(false);

        NoAdsStore.EnsureCreated();
        Leaderboard.Init();

        var cam = Camera.main;
        CityBackdrop.Create(cam);
        Fx.Create(cam);
        _ui = GameUI.Create(this);

        pickMusic.Instance?.SetMuffled(false);
        if (_skipMenuOnLoad)
        {
            _skipMenuOnLoad = false;
            EnterReady();
        }
        else
        {
            State = GameState.Menu;
            _ui.ShowMenu();
        }
    }

    private void Update()
    {
        // Frenado del mundo tras chocar (tiempo real para que no dependa de timeScale).
        float target = State == GameState.Over ? 0f : 1f;
        _brake = Mathf.MoveTowards(_brake, target, Time.deltaTime * 2.2f);
        SpeedFactor = DifficultySpeed * _brake;
        DebugAutopilot.Update();

        if (Input.GetKeyDown(KeyCode.Escape)) OnBackPressed();
        if (State == GameState.Paused && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.P))) Resume();
        else if (State == GameState.Playing && Input.GetKeyDown(KeyCode.P)) Pause();
    }

    // ------------------------------------------------------------------ flujo

    /// <summary>Menú → pantalla "toca para volar".</summary>
    public void Play()
    {
        if (State != GameState.Menu) return;
        EnterReady();
    }

    private void EnterReady()
    {
        State = GameState.Ready;
        playerScore = 0;
        _ui.ShowReady(playerScore);
    }

    /// <summary>Primer toque en "listo": empieza la partida.</summary>
    public void BeginRun()
    {
        if (State != GameState.Ready) return;
        State = GameState.Playing;
        _rb.gravityScale = 1;
        _spawner.spawn = true;
        _spawner.Prime(0.55f);
        carScript.carIsAlive = true;
        _runActive = true;
        _ui.ShowHud();
    }

    public void addScore(int scoreToAdd)
    {
        if (!carScript.carIsAlive) return;
        playerScore += scoreToAdd;
        DifficultySpeed = 1f + Mathf.Min(0.4f, playerScore * 0.006f);

        Color spark = Fx.Cyan;
        int sparks = 5;
        if (playerScore % 1000 == 0) { Sfx.Play(_audio, thousandPoints, 1f); spark = Fx.Amber; sparks = 30; }
        else if (playerScore % 100 == 0) { Sfx.Play(_audio, hundredPoints, 1f); spark = Fx.Amber; sparks = 22; }
        else if (playerScore % 10 == 0) { Sfx.Play(_audio, tenPoints, 0.8f); spark = Fx.Pink; sparks = 12; }
        else Sfx.Play(_audio, onePoint, 0.8f);

        Fx.Instance?.ScoreSparks(car.transform.position + Vector3.right * 0.4f, spark, sparks);
        _ui.SetScore(playerScore, playerScore % 10 == 0);
    }

    public void Pause()
    {
        if (State != GameState.Playing) return;
        State = GameState.Paused;
        Time.timeScale = 0f;
        pickMusic.Instance?.SetMuffled(true);
        _ui.ShowPause();
    }

    public void Resume()
    {
        if (State != GameState.Paused) return;
        State = GameState.Playing;
        Time.timeScale = 1f;
        ResumeFrame = Time.frameCount;
        pickMusic.Instance?.SetMuffled(false);
        _ui.ShowHud();
    }

    public void gameOver()
    {
        // Solo una partida en curso puede terminar (no al abandonar desde la pausa ni al recargar).
        if (State != GameState.Playing) return;
        Time.timeScale = 1f;
        State = GameState.Over;
        _spawner.spawn = false;
        Sfx.Play(_audio, gameOverSound, 0.35f);
        IsNewBest = GameSettings.SubmitScore(playerScore) && playerScore > 0;
        Leaderboard.Submit(playerScore);
        pickMusic.Instance?.SetMuffled(true);
        _ui.Flash(new Color(1f, 0.35f, 0.75f, 0.55f));
        StartCoroutine(ShowGameOverAfter(0.85f));
    }

    private IEnumerator ShowGameOverAfter(float seconds)
    {
        _ui.HideHud();
        yield return new WaitForSecondsRealtime(seconds);
        _ui.ShowGameOver(playerScore, GameSettings.BestScore, IsNewBest);
    }

    /// <summary>"Continuar": muestra el anuncio recompensado y, si se completa, revive el auto.</summary>
    public void ContinueWithAd()
    {
        if (!CanContinue) return;
        _ui.SetContinueBusy(true);
        Interstitial.ShowRewarded(earned =>
        {
            if (earned && State == GameState.Over) Revive();
            else _ui.ShowContinueUnavailable();
        });
    }

    private void Revive()
    {
        _continued = true;
        // Despeja los edificios cercanos para que no choque apenas reaparezca.
        float clearUntil = car.transform.position.x + 9f;
        foreach (var b in FindObjectsByType<BuildingsMoveScript>())
            if (b.transform.position.x < clearUntil) Destroy(b.gameObject);
        carScript.Revive();
        State = GameState.Ready; // "toca para volar" de nuevo, conservando el puntaje
        pickMusic.Instance?.SetMuffled(false);
        _ui.ShowReady(playerScore);
    }

    /// <summary>Cierra la partida para estadísticas y ritmo de anuncios (una sola vez).</summary>
    private void FinishRun()
    {
        if (!_runActive) return;
        _runActive = false;
        GameSettings.CountGame();
        Interstitial.NotifyGameFinished();
    }

    /// <summary>"Otra vez": nueva partida directa (salta el menú).</summary>
    public void restartGame()
    {
        FinishRun();
        _skipMenuOnLoad = true;
        FreezeCar();
        ReloadAtBreak();
    }

    /// <summary>Volver al menú principal.</summary>
    public void GoToMenu()
    {
        _skipMenuOnLoad = false;
        Time.timeScale = 1f;
        bool afterGame = State == GameState.Over;
        FinishRun(); // también una partida abandonada en pausa o tras "continuar"
        FreezeCar();
        State = GameState.Menu;
        if (afterGame) ReloadAtBreak();
        else Reload(); // desde "listo" o la pausa: sin anuncio, como el original
    }

    /// <summary>Deja el auto quieto y "muerto" para que nada dispare otro fin de partida al recargar.</summary>
    private void FreezeCar()
    {
        carScript.carIsAlive = false;
        _rb.linearVelocity = Vector2.zero;
        _rb.simulated = false;
    }

    private static void Reload() => SceneManager.LoadScene(SceneManager.GetActiveScene().name);

    private void ReloadAtBreak()
    {
        // Pausa natural: si toca (cada varias partidas y con tiempo mínimo), el intersticial
        // se muestra aquí, antes de cargar la siguiente partida; nunca durante el juego.
        Interstitial.ShowAtBreakThen(Reload);
    }

    /// <summary>Botón atrás de Android / Escape.</summary>
    private void OnBackPressed()
    {
        if (_ui.HandleBack()) return; // cerró un submenú (opciones)
        switch (State)
        {
            case GameState.Playing: Pause(); break;
            case GameState.Paused: Resume(); break;
            case GameState.Ready:
            case GameState.Over: GoToMenu(); break;
            case GameState.Menu: Application.Quit(); break;
        }
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused && State == GameState.Playing) Pause();
    }

    private void OnApplicationFocus(bool focus)
    {
        if (!focus && State == GameState.Playing) Pause();
    }

    public static int RankFor(int score)
    {
        int rank = 0;
        for (int i = 1; i < RankThresholds.Length; i++)
            if (score >= RankThresholds[i]) rank = i;
        return rank;
    }
}
