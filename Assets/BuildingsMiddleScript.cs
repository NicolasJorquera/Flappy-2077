using UnityEngine;

/// <summary>Trigger del hueco entre edificios: al cruzarlo, el auto supera el obstáculo.</summary>
public class BuildingsMiddleScript : MonoBehaviour
{
    public LogicScript Logic;
    private Collider2D _collider;
    private bool _passed;

    private void Start()
    {
        Logic = GameObject.FindGameObjectWithTag("Logic").GetComponent<LogicScript>();
        _collider = GetComponent<Collider2D>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_passed || collision.gameObject.layer != 3) return; // capa 3 = Car
        _passed = true;
        Logic.OnGapPassed(_collider);
    }
}
