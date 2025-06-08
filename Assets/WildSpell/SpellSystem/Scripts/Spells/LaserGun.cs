using UnityEngine;

public class LaserGun : MonoBehaviour
{
    [Header("Лазерный пистолет")]
    [SerializeField] private GameObject laserBulletPrefab;
    [SerializeField] private float bulletSpeed = 50f;
    [SerializeField] private ProjectileShooter projectileShooter;

    void Start()
    {
        if (projectileShooter == null)
            projectileShooter = GetComponent<ProjectileShooter>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // ЛКМ
        {
            Shoot();
        }
    }

    void Shoot()
    {
        if (laserBulletPrefab != null && projectileShooter != null)
        {
            projectileShooter.Shoot(laserBulletPrefab, bulletSpeed);
        }
    }
}