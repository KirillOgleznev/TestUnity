using UnityEngine;

public class ProjectileShooter : MonoBehaviour
{
    [Header("Настройки стрельбы")]
    [SerializeField] private CrosshairController crosshairController;
    [SerializeField] private Transform shootPoint;

    void Start()
    {
        if (crosshairController == null)
            crosshairController = FindObjectOfType<CrosshairController>();

        if (shootPoint == null)
            shootPoint = transform;
    }

    public void Shoot(GameObject projectilePrefab, float speed)
    {
        Vector3 direction = crosshairController.GetDirectionFromPoint(shootPoint.position);
        GameObject projectile = Instantiate(projectilePrefab, shootPoint.position, Quaternion.LookRotation(direction));

        Projectile advancedProjectile = projectile.GetComponent<Projectile>();
        if (advancedProjectile != null)
        {
            // Передавай корневой объект игрока, а не компонент
            GameObject playerRoot = transform.root.gameObject;
            advancedProjectile.Initialize(direction, speed, playerRoot);
        }
    }

    public Vector3 GetShootDirection()
    {
        return crosshairController.GetDirectionFromPoint(shootPoint.position);
    }

    public Vector3 GetTargetPoint()
    {
        return crosshairController.GetTargetPoint();
    }
}