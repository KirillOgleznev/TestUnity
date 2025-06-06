using UnityEngine;
using System.Collections.Generic;
using Unity.FPS.Game;

public class FlameDamage : MonoBehaviour
{
    [Header("Урон")]
    public float damagePerSecond = 20f;
    public float range = 8f;

    [Header("Настройки конуса")]
    public float coneAngle = 45f; // Угол конуса в градусах (полный угол)
    public Transform fireDirection; // Направление огня (обычно это сам префаб)

    public LayerMask enemyLayers = -1;

    private float damageInterval = 0.1f;
    private float nextDamageTime = 0f;
    private HashSet<GameObject> enemiesInRange = new HashSet<GameObject>();

    void Start()
    {
        // Если направление не задано, используем собственный transform
        if (fireDirection == null)
            fireDirection = transform;
    }

    void Update()
    {
        if (Time.time >= nextDamageTime)
        {
            FindEnemiesInCone();
            DamageEnemiesInRange();
            nextDamageTime = Time.time + damageInterval;
        }
    }

    void FindEnemiesInCone()
    {
        // Сначала находим всех в радиусе
        Collider[] colliders = Physics.OverlapSphere(transform.position, range);

        enemiesInRange.Clear();

        foreach (Collider col in colliders)
        {
            Damageable damageable = col.GetComponent<Damageable>();
            if (damageable != null)
            {
                // Проверяем попадает ли в конус
                if (IsInCone(col.transform.position))
                {
                    enemiesInRange.Add(col.gameObject);
                }
            }
        }
    }

    bool IsInCone(Vector3 targetPosition)
    {
        Vector3 directionToTarget = (targetPosition - transform.position).normalized;
        Vector3 fireForward = fireDirection.forward;

        // Вычисляем угол между направлением огня и направлением к цели
        float angle = Vector3.Angle(fireForward, directionToTarget);

        // Проверяем попадает ли в половину угла конуса
        return angle <= coneAngle / 2f;
    }

    void DamageEnemiesInRange()
    {
        List<GameObject> enemiesToRemove = new List<GameObject>();

        foreach (GameObject enemy in enemiesInRange)
        {
            if (enemy == null)
            {
                enemiesToRemove.Add(enemy);
                continue;
            }

            Damageable damageable = enemy.GetComponent<Damageable>();
            if (damageable != null)
            {
                float damage = damagePerSecond * damageInterval;
                damageable.InflictDamage(damage, false, gameObject);
            }
        }

        foreach (GameObject enemy in enemiesToRemove)
        {
            enemiesInRange.Remove(enemy);
        }
    }

    // Визуализация конуса в Scene View
    void OnDrawGizmos()
    {
        if (fireDirection == null) return;

        Gizmos.color = Color.red;

        // Рисуем линию направления
        Vector3 forward = fireDirection.forward * range;
        Gizmos.DrawLine(transform.position, transform.position + forward);

        // Рисуем границы конуса
        float halfAngle = coneAngle / 2f;

        Vector3 rightBoundary = Quaternion.AngleAxis(halfAngle, fireDirection.up) * forward;
        Vector3 leftBoundary = Quaternion.AngleAxis(-halfAngle, fireDirection.up) * forward;

        Gizmos.DrawLine(transform.position, transform.position + rightBoundary);
        Gizmos.DrawLine(transform.position, transform.position + leftBoundary);

        // Рисуем дугу
        Gizmos.color = Color.yellow;
        Vector3 previousPoint = transform.position + rightBoundary;
        for (int i = 1; i <= 20; i++)
        {
            float currentAngle = Mathf.Lerp(halfAngle, -halfAngle, i / 20f);
            Vector3 currentPoint = transform.position +
                Quaternion.AngleAxis(currentAngle, fireDirection.up) * forward;
            Gizmos.DrawLine(previousPoint, currentPoint);
            previousPoint = currentPoint;
        }
    }
}