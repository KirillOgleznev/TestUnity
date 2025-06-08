using UnityEngine;
using System.Collections.Generic;
using Unity.FPS.Game; // ОСТАВЛЯЕМ для Damageable

public class FlameDamage : MonoBehaviour
{
    [Header("Урон")]
    public float damagePerSecond = 20f;
    public float range = 8f;

    [Header("Настройки конуса")]
    public float coneAngle = 45f;
    public Transform fireDirection;
        
    public LayerMask enemyLayers = -1;

    private float damageInterval = 0.1f;
    private float nextDamageTime = 0f;
    private HashSet<GameObject> enemiesInRange = new HashSet<GameObject>();

    void Start()
    {
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
        Collider[] colliders = Physics.OverlapSphere(transform.position, range, enemyLayers);
        enemiesInRange.Clear();

        foreach (Collider col in colliders)
        {
            Damageable damageable = col.GetComponent<Damageable>(); // Используем Unity.FPS.Game
            if (damageable != null && IsInCone(col.transform.position))
            {
                enemiesInRange.Add(col.gameObject);
            }
        }
    }

    bool IsInCone(Vector3 targetPosition)
    {
        Vector3 directionToTarget = (targetPosition - transform.position).normalized;
        Vector3 fireForward = fireDirection.forward;
        float angle = Vector3.Angle(fireForward, directionToTarget);
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

    void OnDrawGizmosSelected()
    {
        OnDrawGizmos(); // Показывать только когда объект выбран
    }

    void OnDrawGizmos()
    {
        if (fireDirection == null && Application.isPlaying) return;

        Transform direction = fireDirection != null ? fireDirection : transform;

        Gizmos.color = Color.red;
        Vector3 forward = direction.forward * range;

        // Рисуем несколько "срезов" конуса
        for (int ring = 0; ring < 4; ring++)
        {
            float ringAngle = (coneAngle / 2f) * (ring + 1) / 4f;
            float ringRadius = Mathf.Tan(ringAngle * Mathf.Deg2Rad) * range;

            Vector3 ringCenter = transform.position + forward * (ring + 1) / 4f;

            // Рисуем круг для каждого "среза"
            for (int i = 0; i < 16; i++)
            {
                float angle1 = (float)i / 16f * 360f * Mathf.Deg2Rad;
                float angle2 = (float)(i + 1) / 16f * 360f * Mathf.Deg2Rad;

                Vector3 point1 = ringCenter + direction.right * Mathf.Cos(angle1) * ringRadius +
                                direction.up * Mathf.Sin(angle1) * ringRadius;
                Vector3 point2 = ringCenter + direction.right * Mathf.Cos(angle2) * ringRadius +
                                direction.up * Mathf.Sin(angle2) * ringRadius;

                Gizmos.DrawLine(point1, point2);
            }
        }
    }
}