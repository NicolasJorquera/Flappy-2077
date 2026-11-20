using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Auto volador del jugador: impulso al tocar/pulsar, inclinación según la velocidad,
/// estela de propulsor, flotación en la pantalla "listo" y choque con explosión.
/// </summary>
public class CarScript : MonoBehaviour
{
    public Rigidbody2D myRigidbody;
    public float flapStrength = 5f;

    public SpriteRenderer spriteRenderer;
    public Sprite carDown;
    public Sprite carUp;
    public LogicScript Logic;
    public bool carIsAlive;
    public AudioClip boostSound;

    /// <summary>Posición de vuelo: algo a la izquierda del centro para ver venir los edificios.</summary>
    public Vector2 flightPosition = new Vector2(-2.6f, 0.6f);
    /// <summary>Posición en el menú: abajo a la izquierda, sin tapar el título ni los botones.</summary>
    public Vector2 menuPosition = new Vector2(-5.4f, -1.4f);

    private AudioSource _audio;
    private Vector3 _baseScale;
    private float _squash;
    private float _trailTimer;
    private bool _crashed;
    private Collider2D _collider;
    private Vector2 _hover;

    private void Start()
    {
        _audio = GetComponent<AudioSource>();
        _audio.playOnAwake = false;
        _collider = GetComponent<Collider2D>();
        Logic = GameObject.FindGameObjectWithTag("Logic").GetComponent<LogicScript>();
        spriteRenderer.sprite = carDown;
        // La escena guarda flapStrength = 0; el script original lo fijaba aquí en 5.
        if (flapStrength <= 0f) flapStrength = 5f;
        _baseScale = transform.localScale;
        carIsAlive = false;
        _hover = LogicScript.WillSkipMenu ? flightPosition : menuPosition;
        transform.position = new Vector3(_hover.x, _hover.y, transform.position.z);
    }

    private void Update()
    {
        var state = Logic.State;
        bool pressed = FlapPressed();

        if (state == GameState.Ready || state == GameState.Menu)
        {
            // Flota suave mientras espera; al pasar del menú a "listo" se desliza a su sitio.
            var home = state == GameState.Menu ? menuPosition : flightPosition;
            _hover = Vector2.Lerp(_hover, home, 1f - Mathf.Exp(-4f * Time.deltaTime));
            float y = _hover.y + Mathf.Sin(Time.time * 3f) * 0.22f;
            transform.position = new Vector3(_hover.x, y, transform.position.z);
            transform.rotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 3f + 1f) * 3f);
            spriteRenderer.sprite = Mathf.Cos(Time.time * 3f) > 0 ? carUp : carDown;
            EmitTrail(0.09f, 0.6f);
            if (state == GameState.Ready && pressed)
            {
                // Arranca exactamente en la posición de vuelo y sin deriva horizontal.
                _hover = flightPosition;
                transform.position = new Vector3(flightPosition.x, transform.position.y, transform.position.z);
                myRigidbody.constraints = RigidbodyConstraints2D.FreezePositionX;
                Logic.BeginRun();
                Flap();
            }
        }
        else if (state == GameState.Playing && carIsAlive)
        {
            if (pressed) Flap();
            else if (myRigidbody.linearVelocity.y < 0) spriteRenderer.sprite = carDown;

            // Inclinación: nariz arriba al subir, en picada al caer (suavizado).
            float target = Mathf.Clamp(myRigidbody.linearVelocity.y * 5f, -55f, 22f);
            float z = Mathf.LerpAngle(transform.eulerAngles.z, target, 1f - Mathf.Exp(-10f * Time.deltaTime));
            transform.rotation = Quaternion.Euler(0, 0, z);
            EmitTrail(0.035f, 1f);
        }
        else if (_crashed)
        {
            transform.Rotate(0, 0, -420f * Time.deltaTime);
        }

        // "Squash & stretch" al impulsar.
        _squash = Mathf.MoveTowards(_squash, 0f, Time.deltaTime * 5f);
        transform.localScale = new Vector3(_baseScale.x * (1f - _squash * 0.12f), _baseScale.y * (1f + _squash * 0.18f), _baseScale.z);
    }

    private bool FlapPressed()
    {
        if (Time.timeScale == 0f || Time.frameCount == Logic.ResumeFrame) return false;
        if (DebugAutopilot.Active) return DebugAutopilot.ShouldFlap(this);
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            return true;

        for (int i = 0; i < Input.touchCount; i++)
        {
            var t = Input.GetTouch(i);
            if (t.phase != TouchPhase.Began) continue;
            if (IsOverUI(t.position)) continue;
            return true;
        }
        // Ratón (navegador/editor). En móvil los toques ya se procesaron arriba.
        if (Input.touchCount == 0 && Input.GetMouseButtonDown(0))
        {
            return !IsOverUI(Input.mousePosition);
        }
        return false;
    }

    private static readonly List<RaycastResult> UiHits = new List<RaycastResult>();

    /// <summary>
    /// Raycast propio contra la UI: no depende de que el EventSystem ya haya procesado este
    /// toque (el orden de Update entre scripts no está garantizado).
    /// </summary>
    private static bool IsOverUI(Vector2 screenPos)
    {
        var events = EventSystem.current;
        if (events == null) return false;
        UiHits.Clear();
        events.RaycastAll(new PointerEventData(events) { position = screenPos }, UiHits);
        return UiHits.Count > 0;
    }

    private void Flap()
    {
        spriteRenderer.sprite = carUp;
        myRigidbody.linearVelocity = Vector2.up * flapStrength;
        _squash = 1f;
        Sfx.Play(_audio, boostSound, 0.5f, Random.Range(0.95f, 1.06f));

        if (Fx.Instance != null)
        {
            Vector2 rear = RearPoint();
            for (int i = 0; i < 7; i++)
            {
                var c = i % 2 == 0 ? Fx.Pink : Fx.Cyan;
                Fx.Instance.Emit(rear + Random.insideUnitCircle * 0.08f,
                    new Vector2(Random.Range(-4f, -2f), Random.Range(-2.5f, -0.5f)), c, Random.Range(0.07f, 0.13f), Random.Range(0.25f, 0.45f));
            }
        }
    }

    private void EmitTrail(float interval, float speedScale)
    {
        if (Fx.Instance == null) return;
        _trailTimer -= Time.deltaTime;
        if (_trailTimer > 0f) return;
        _trailTimer = interval;
        Vector2 rear = RearPoint();
        var c = Random.value < 0.5f ? Fx.Pink : Fx.Lilac;
        c.a = 0.85f;
        Fx.Instance.Emit(rear + new Vector2(0f, Random.Range(-0.05f, 0.05f)),
            new Vector2(-LogicScript.WorldSpeed * speedScale - 1f, Random.Range(-0.2f, 0.2f)), c, Random.Range(0.05f, 0.09f), 0.35f, order: -1);
    }

    private Vector2 RearPoint()
    {
        // El sprite del auto tiene márgenes transparentes: la cola está a ~20 % del ancho.
        var b = spriteRenderer.sprite.bounds;
        var local = new Vector3(b.min.x + b.size.x * 0.2f, b.center.y - b.size.y * 0.02f, 0f);
        return spriteRenderer.transform.TransformPoint(local);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (carIsAlive) Crash("choque con " + collision.collider.name);
    }

    private void OnBecameInvisible()
    {
        if (carIsAlive) Crash("salió de la pantalla");
    }

    /// <summary>Tras "continuar": vuelve a flotar en su posición, listo para otro primer toque.</summary>
    public void Revive()
    {
        _crashed = false;
        carIsAlive = false;
        myRigidbody.simulated = true;
        myRigidbody.gravityScale = 0f;
        myRigidbody.linearVelocity = Vector2.zero;
        myRigidbody.angularVelocity = 0f;
        myRigidbody.constraints = RigidbodyConstraints2D.None;
        if (_collider != null) _collider.enabled = true;
        transform.rotation = Quaternion.identity;
        _hover = flightPosition;
        transform.position = new Vector3(flightPosition.x, flightPosition.y, transform.position.z);
        spriteRenderer.sprite = carDown;
    }

    private void Crash(string reason)
    {
        if (Debug.isDebugBuild) Debug.Log($"Fin de partida: {reason} en {transform.position} (vel {myRigidbody.linearVelocity})");
        carIsAlive = false;
        _crashed = true;
        spriteRenderer.sprite = carDown;
        // Rebote hacia atrás y caída girando, sin volver a chocar.
        if (_collider != null) _collider.enabled = false;
        myRigidbody.constraints = RigidbodyConstraints2D.None;
        myRigidbody.linearVelocity = new Vector2(-1.5f, 3.5f);
        myRigidbody.gravityScale = 1.4f;
        if (Fx.Instance != null)
        {
            Fx.Instance.Explosion(transform.position);
            Fx.Instance.Shake(0.35f, 0.45f);
        }
        Haptics.Pulse(60);
        Logic.gameOver();
    }
}
