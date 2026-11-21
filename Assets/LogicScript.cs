using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum GameState { Menu, Ready, Playing, Paused, Over }

/// <summary>
/// Gestor de la partida: modo, estados, puntaje, récord, dificultad, pausa y fin de partida.
/// Arma en tiempo de ejecución el fondo de ciudad, los efectos, la interfaz (<see cref="GameUI"/>)
/// y, en Arcade/Diario, el director de chips, power-ups y jefes (<see cref="ArcadeDirector"/>).
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

    public static readonly string[] RankKeys = { "rank_none", "rank_bronze", "rank_silver", "rank_gold", "rank_neon" };

    /// <summary>Al recargar la escena desde "Otra vez" se salta el menú.</summary>
    private static bool _skipMenuOnLoad;

    /// <summary>Modo de la partida actual (sobrevive a la recarga de escena de "Otra vez").</summary>
    public static GameMode Mode { get; private set; } = GameMode.Classic;
    /// <summary>Práctica de jefes: cuál se practica (0 dron, 1 cañonera, 2 gusano).</summary>
    public static int PracticeBoss { get; private set; }
    /// <summary>La práctica terminó con el jefe derrotado.</summary>
    public bool PracticeWon { get; private set; }

    /// <summary>Multiplicador por dificultad (1 → 1,4 según los edificios superados).</summary>
    public static float DifficultySpeed { get; private set; } = 1f;
    /// <summary>Multiplicador de power-ups (ciudad lenta). Lo fija el director de arcade.</summary>
    public static float PowerSpeedFactor = 1f;
    /// <summary>Multiplicador efectivo para todo lo que se desplaza.</summary>
    public static float SpeedFactor { get; private set; } = 1f;
    /// <summary>Velocidad del mundo en unidades/s (fondo, estela, generación de obstáculos).</summary>
    public static float WorldSpeed => BaseWorldSpeed * SpeedFactor;

    public static LogicScript Instance { get; private set; }

    public GameState State { get; private set; }
    /// <summary>Frame en que se reanudó la pausa: esa misma pulsación no debe impulsar el auto.</summary>
    public int ResumeFrame { get; private set; } = -1;
    /// <summary>True si la escena que carga ahora va directo a "listo" (salta el menú).</summary>
    public static bool WillSkipMenu => _skipMenuOnLoad;
    public bool IsNewBest { get; private set; }
    public RunStats Stats { get; private set; } = new RunStats();
    public ArcadeDirector Arcade { get; private set; }
    public GameUI UI => _ui;

    private Rigidbody2D _rb;
    private BuildingsSpawnScript _spawner;
    private AudioSource _audio;
    private GameUI _ui;
    private float _brake = 1f;
    private bool _runActive;   // hay una partida empezada que aún no se contó como terminada
    private bool _continued;   // ya usó su "continuar" en esta partida

    /// <summary>¿Se ofrece "continuar viendo un anuncio"? Una vez por partida, no en el diario.</summary>
    public bool CanContinue => State == GameState.Over && !_continued && playerScore > 0
                               && Mode != GameMode.Daily && Mode != GameMode.Bosses && Interstitial.RewardedReady;

    private void Awake()
    {
        Instance = this;
        DifficultySpeed = 1f;
        SpeedFactor = 1f;
        PowerSpeedFactor = 1f;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
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
        carScript.ApplySkin(CarSkins.Selected);

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
        SpeedFactor = DifficultySpeed * _brake * PowerSpeedFactor;
        DebugAutopilot.Update();

        if (Input.GetKeyDown(KeyCode.Escape)) OnBackPressed();
        if (State == GameState.Paused && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.P))) Resume();
        else if (State == GameState.Playing && Input.GetKeyDown(KeyCode.P)) Pause();
    }

    // ------------------------------------------------------------------ flujo

    /// <summary>Menú → pantalla "toca para volar" en el modo indicado.</summary>
    public void Play(GameMode mode)
    {
        if (State != GameState.Menu) return;
        Mode = mode;
        EnterReady();
    }

    public void Play() => Play(Mode);

    /// <summary>Galería: practicar contra un jefe ya encontrado.</summary>
    public void PlayBossPractice(int kind)
    {
        if (!GameSettings.BossSeen(kind)) return;
        PracticeBoss = Mathf.Clamp(kind, 0, 2);
        Play(GameMode.Bosses);
    }

    private void EnterReady()
    {
        State = GameState.Ready;
        playerScore = 0;
        Stats = new RunStats { Mode = Mode };
        GameRandom.Reset(Mode == GameMode.Daily ? GameModes.DailySeed : (int?)null);
        if (GameModes.HasArcadeRules(Mode) && Arcade == null) Arcade = ArcadeDirector.Create(this, _spawner);
        _ui.ShowReady(playerScore);
    }

    /// <summary>Primer toque en "listo": empieza (o retoma, tras continuar) la partida.</summary>
    public void BeginRun()
    {
        if (State != GameState.Ready) return;
        State = GameState.Playing;
        _rb.gravityScale = 1;
        if (Mode == GameMode.Bosses)
        {
            Arcade.StartPractice(PracticeBoss); // sin edificios: solo el jefe elegido
        }
        else if (Arcade == null || !Arcade.BossActive)
        {
            _spawner.spawn = true;
            _spawner.Prime(0.55f);
        }
        carScript.carIsAlive = true;
        _runActive = true;
        _ui.ShowHud();
    }

    /// <summary>El auto cruzó el hueco de un par de edificios.</summary>
    public void OnGapPassed(Collider2D gap)
    {
        if (!carScript.carIsAlive) return;
        Stats.Gaps++;
        DifficultySpeed = 1f + Mathf.Min(0.4f, Stats.Gaps * 0.006f);
        int points = 1;
        if (Arcade != null) points = Arcade.OnGapPassed(gap);
        AddPoints(points, car.transform.position, popup: points > 1);
    }

    /// <summary>Compatibilidad con la escena original: suma puntos sin multiplicadores.</summary>
    public void addScore(int scoreToAdd) => AddPoints(scoreToAdd, car.transform.position, popup: false);

    /// <summary>Suma puntos (ya multiplicados) con sonido, chispas y, si se pide, un "+N" flotante.</summary>
    public void AddPoints(int points, Vector3 worldPos, bool popup)
    {
        if (!carScript.carIsAlive || points <= 0) return;
        int before = playerScore;
        playerScore += points;
        Stats.Score = playerScore;

        // Hitos: se celebra al cruzar cada decena/centena/millar (los chips saltan varios puntos).
        bool k = playerScore / 1000 > before / 1000, c = playerScore / 100 > before / 100, d = playerScore / 10 > before / 10;
        Color spark = Fx.Cyan;
        int sparks = 5;
        if (k) { Sfx.Play(_audio, thousandPoints, 1f); spark = Fx.Amber; sparks = 30; }
        else if (c) { Sfx.Play(_audio, hundredPoints, 1f); spark = Fx.Amber; sparks = 22; }
        else if (d) { Sfx.Play(_audio, tenPoints, 0.8f); spark = Fx.Pink; sparks = 12; }
        else if (!popup) Sfx.Play(_audio, onePoint, 0.8f);

        Fx.Instance?.ScoreSparks(worldPos + Vector3.right * 0.4f, spark, sparks);
        _ui.SetScore(playerScore, d);
        if (popup) _ui.PopPoints(worldPos, "+" + points, spark);
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
        Arcade?.OnCrash();
        Sfx.Play(_audio, gameOverSound, 0.35f);
        var done = new List<Mission>();
        IsNewBest = false;
        if (Mode != GameMode.Bosses) // la práctica no cuenta para récords, rankings ni misiones
        {
            IsNewBest = GameSettings.SubmitScore(Mode, playerScore) && playerScore > 0;
            Leaderboard.Submit(Mode, playerScore);
            Stats.Score = playerScore;
            Stats.Continued = _continued;
            done = Missions.Report(Stats);
        }
        pickMusic.Instance?.SetMuffled(true);
        _ui.Flash(new Color(1f, 0.35f, 0.75f, 0.55f));
        StartCoroutine(ShowGameOverAfter(0.85f, done));
    }

    /// <summary>Práctica: el jefe fue derrotado. El auto queda flotando y aparece la victoria.</summary>
    public void WinPractice()
    {
        if (State != GameState.Playing) return;
        State = GameState.Over;
        PracticeWon = true;
        carScript.carIsAlive = false;
        _rb.gravityScale = 0f;
        _rb.linearVelocity = Vector2.zero;
        Sfx.NewBest();
        StartCoroutine(ShowGameOverAfter(0.4f, new List<Mission>()));
    }

    private IEnumerator ShowGameOverAfter(float seconds, List<Mission> done)
    {
        _ui.HideHud();
        yield return new WaitForSecondsRealtime(seconds);
        _ui.ShowGameOver(playerScore, GameSettings.BestFor(Mode), IsNewBest, done);
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
        // Despeja los edificios y proyectiles cercanos para que no choque apenas reaparezca.
        float clearUntil = car.transform.position.x + 9f;
        foreach (var b in FindObjectsByType<BuildingsMoveScript>())
            if (b.transform.position.x < clearUntil) Destroy(b.gameObject);
        Arcade?.OnRevive();
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

    /// <summary>"Otra vez": nueva partida directa en el mismo modo (salta el menú).</summary>
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
        if (_ui.HandleBack()) return; // cerró un submenú (opciones, piloto)
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

    public static int RankFor(int score, GameMode mode)
    {
        var t = GameModes.RankThresholds(mode);
        int rank = 0;
        for (int i = 1; i < t.Length; i++)
            if (score >= t[i]) rank = i;
        return rank;
    }
}
