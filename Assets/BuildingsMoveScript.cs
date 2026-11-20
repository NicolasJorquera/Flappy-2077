using UnityEngine;

/// <summary>
/// Desplaza un par de edificios hacia la izquierda. La velocidad efectiva es
/// <see cref="moveSpeed"/> × <see cref="LogicScript.SpeedFactor"/>: sube con la dificultad
/// y se frena hasta cero al chocar.
/// </summary>
public class BuildingsMoveScript : MonoBehaviour
{
    public float moveSpeed = 5;
    private const float DeadZone = -24f;

    private void Update()
    {
        transform.position += Vector3.left * (moveSpeed * LogicScript.SpeedFactor * Time.deltaTime);
        if (transform.position.x < DeadZone) Destroy(gameObject);
    }
}
