using UnityEngine;

public class LaserGun : MonoBehaviour
{
    [Header("�������� ��������")]
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
        if (Input.GetMouseButtonDown(0)) // ���
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