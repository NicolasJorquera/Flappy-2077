#if UNITY_EDITOR
using UnityEngine;

/// <summary>
/// Conductor de la prueba automática (solo editor; ver Assets/Editor/Playtest.cs).
/// Juega N partidas con el piloto automático, registra puntaje y estado, y cierra Unity.
/// </summary>
public class PlaytestDriver : MonoBehaviour
{
    public static int GamesToPlay = 2;
    public static float MaxSecondsPerGame = 45f;
    /// <summary>Modo de cada partida (se repite el último si hay más partidas).</summary>
    public static GameMode[] Modes = { GameMode.Classic, GameMode.Arcade };
    public static float MaxSecondsArcade = 85f;

    private int _played;
    private float _gameTime;
    private float _logTimer;
    private int _best;
    private bool _triedContinue;
    private LogicScript _logic;
    private GameState _lastState = (GameState)(-1);

    private void Awake()
    {
        // PLAYTEST_MODES=Arcade,Arcade permite elegir los modos desde la línea de comandos.
        string env = System.Environment.GetEnvironmentVariable("PLAYTEST_MODES");
        if (!string.IsNullOrEmpty(env))
        {
            var parts = env.Split(',');
            Modes = new GameMode[parts.Length];
            for (int i = 0; i < parts.Length; i++) Modes[i] = (GameMode)System.Enum.Parse(typeof(GameMode), parts[i].Trim());
            GamesToPlay = parts.Length;
        }
        DontDestroyOnLoad(gameObject);
        DebugAutopilot.SetActive(true);
    }

    private void Update()
    {
        if (_logic == null)
        {
            _logic = FindAnyObjectByType<LogicScript>();
            if (_logic == null) return;
            _gameTime = 0f;
        }

        var state = _logic.State;
        if (state != _lastState)
        {
            Debug.Log($"[Playtest] estado {state} puntaje {_logic.playerScore}");
            _lastState = state;
        }

        if (state == GameState.Menu) _logic.Play(Modes[Mathf.Min(_played, Modes.Length - 1)]);

        _gameTime += Time.deltaTime;
        _logTimer -= Time.deltaTime;
        if (state == GameState.Playing && _logTimer <= 0f)
        {
            _logTimer = 1f;
            var rb = _logic.car.GetComponent<Rigidbody2D>();
            Debug.Log($"[Playtest] t={_gameTime:0.0} x={_logic.car.transform.position.x:0.00} y={_logic.car.transform.position.y:0.00} boss={(_logic.Arcade != null && _logic.Arcade.BossActive)} escudo={(_logic.Arcade != null && _logic.Arcade.HasShield)} vy={rb.linearVelocity.y:0.00} puntos={_logic.playerScore} vel={LogicScript.SpeedFactor:0.00} dt={Time.deltaTime:0.000} gs={rb.gravityScale} tipo={rb.bodyType} sim={rb.simulated} lin={rb.linearDamping} misma={rb == _logic.carScript.myRigidbody} escala={_logic.car.transform.localScale} hijos={_logic.car.transform.childCount} padre={_logic.car.transform.parent?.name}");
        }

        // Primera vez que choca con puntos: prueba "continuar" (en WebGL/escritorio el anuncio se simula).
        if (state == GameState.Over && !_triedContinue && _logic.CanContinue)
        {
            _triedContinue = true;
            int before = _logic.playerScore;
            _logic.ContinueWithAd();
            Debug.Log($"[Playtest] continuar con {before} puntos → estado {_logic.State}, puntaje {_logic.playerScore}");
            return;
        }

        float limit = LogicScript.Mode == GameMode.Classic ? MaxSecondsPerGame : MaxSecondsArcade;
        bool timeout = state == GameState.Playing && _gameTime > limit;
        if (state == GameState.Over || timeout)
        {
            _played++;
            _best = Mathf.Max(_best, _logic.playerScore);
            var st = _logic.Stats;
            var bossNow = _logic.Arcade != null ? _logic.Arcade.Boss : null;
            if (bossNow != null) Debug.Log($"[Playtest] vida del jefe al terminar: {bossNow.HealthFraction:P0}");
            Debug.Log($"[Playtest] partida {_played} ({LogicScript.Mode}): {_logic.playerScore} puntos{(timeout ? " (tiempo límite)" : "")} · edificios {st.Gaps} · chips {st.Chips} · perfectos {st.Perfects} (racha {st.BestStreak}) · power-ups {st.PowerUps} · jefes {st.Bosses} · nivel {GameSettings.Level} xp {GameSettings.Xp}");
            Debug.Log($"[Playtest] ¿ofrece continuar? {_logic.CanContinue} · tienda disponible {NoAdsStore.Available} ({NoAdsStore.PriceText}) · ranking {Leaderboard.Available}");
            if (_played >= GamesToPlay)
            {
                Debug.Log($"[Playtest] FIN mejor={_best}");
                UnityEditor.EditorApplication.Exit(0);
                enabled = false;
                return;
            }
            // Siguiente partida: si cambia el modo, por el menú; si no, "otra vez".
            if (Modes[Mathf.Min(_played, Modes.Length - 1)] != LogicScript.Mode) _logic.GoToMenu();
            else _logic.restartGame();
            _logic = null;
            _lastState = (GameState)(-1);
        }
    }
}
#endif
