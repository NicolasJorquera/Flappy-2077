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

    private int _played;
    private float _gameTime;
    private float _logTimer;
    private int _best;
    private bool _triedContinue;
    private LogicScript _logic;
    private GameState _lastState = (GameState)(-1);

    private void Awake()
    {
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

        if (state == GameState.Menu) _logic.Play();

        _gameTime += Time.deltaTime;
        _logTimer -= Time.deltaTime;
        if (state == GameState.Playing && _logTimer <= 0f)
        {
            _logTimer = 1f;
            var rb = _logic.car.GetComponent<Rigidbody2D>();
            Debug.Log($"[Playtest] t={_gameTime:0.0} y={_logic.car.transform.position.y:0.00} vy={rb.linearVelocity.y:0.00} puntos={_logic.playerScore} vel={LogicScript.SpeedFactor:0.00} dt={Time.deltaTime:0.000} gs={rb.gravityScale} tipo={rb.bodyType} sim={rb.simulated} lin={rb.linearDamping} misma={rb == _logic.carScript.myRigidbody} escala={_logic.car.transform.localScale} hijos={_logic.car.transform.childCount} padre={_logic.car.transform.parent?.name}");
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

        bool timeout = state == GameState.Playing && _gameTime > MaxSecondsPerGame;
        if (state == GameState.Over || timeout)
        {
            _played++;
            _best = Mathf.Max(_best, _logic.playerScore);
            Debug.Log($"[Playtest] partida {_played}: {_logic.playerScore} puntos{(timeout ? " (tiempo límite)" : "")}");
            Debug.Log($"[Playtest] ¿ofrece continuar? {_logic.CanContinue} · tienda disponible {NoAdsStore.Available} ({NoAdsStore.PriceText}) · ranking {Leaderboard.Available}");
            if (_played >= GamesToPlay)
            {
                Debug.Log($"[Playtest] FIN mejor={_best}");
                UnityEditor.EditorApplication.Exit(0);
                enabled = false;
                return;
            }
            _logic.restartGame();
            _logic = null;
            _lastState = (GameState)(-1);
        }
    }
}
#endif
