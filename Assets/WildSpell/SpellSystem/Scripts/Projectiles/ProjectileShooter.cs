using UnityEngine;
using System.Reflection;

public class ProjectileShooter : MonoBehaviour
{
    [Header("Настройки стрельбы")]
    [SerializeField] private CrosshairController crosshair;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;

    void Start()
    {
        if (crosshair == null)
            crosshair = FindObjectOfType<CrosshairController>();
        if (firePoint == null)
            firePoint = transform;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
            FireProjectile();
    }

    void FireProjectile()
    {
        if (projectilePrefab == null || crosshair == null) return;

        Vector3 direction = crosshair.GetDirectionFromPoint(firePoint.position);

        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);

        // Запускаем снаряд в нужном направлении
        var projStandard = projectile.GetComponent<Unity.FPS.Gameplay.ProjectileStandard>();
        if (projStandard != null)
        {
            projStandard.LaunchInDirection(direction);
        }

        Debug.Log($"Запустили снаряд! Направление: {direction}");
    }
}