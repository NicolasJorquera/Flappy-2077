using UnityEngine;

/// <summary>
/// Piloto automático para pruebas: solo en builds de desarrollo (nunca en la versión de Play).
/// F9 lo activa/desactiva. Apunta al centro del próximo hueco entre edificios.
/// Sirve para probar partidas largas (dificultad, rangos, récord) y capturar pantallas.
/// </summary>
public static class DebugAutopilot
{
    private static bool _active;
    private static float _cooldown;

    public static bool Active => _active && Debug.isDebugBuild;

    public static void SetActive(bool active) => _active = active;

    public static void Update()
    {
        if (!Debug.isDebugBuild) return;
        if (Input.GetKeyDown(KeyCode.F9))
        {
            _active = !_active;
            Debug.Log("Autopiloto " + (_active ? "activado" : "desactivado"));
        }
        _cooldown -= Time.deltaTime;
    }

    public static bool ShouldFlap(CarScript car)
    {
        if (_cooldown > 0f) return false;
        if (car.Logic != null && car.Logic.State == GameState.Ready)
        {
            _cooldown = 0.16f;
            return true; // "toca para volar": arrancar
        }
        float carX = car.transform.position.x;
        float targetY = 0.5f;
        float best = float.MaxValue;
        foreach (var gap in Object.FindObjectsByType<BuildingsMiddleScript>())
        {
            var col = gap.GetComponent<Collider2D>();
            if (col == null) continue;
            var b = col.bounds;
            if (b.max.x < carX - 0.4f) continue;
            if (b.center.x < best)
            {
                best = b.center.x;
                targetY = b.center.y;
            }
        }
        var rb = car.myRigidbody;
        // Un impulso eleva ~v²/2g ≈ 1,3 unidades: impulsar cayendo y bajo el centro evita pasarse.
        if (car.transform.position.y < targetY - 0.75f && rb.linearVelocity.y <= 0f)
        {
            _cooldown = 0.16f;
            return true;
        }
        return false;
    }
}
