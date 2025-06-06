using UnityEngine;

public class ProjectileDamage : MonoBehaviour
{
    [Header("Damage Settings")]
    public float damage = 25f;
    public LayerMask targetLayers = -1;
    public bool destroyOnHit = true;
    public GameObject hitEffectPrefab;

    void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & targetLayers) != 0)
        {
            // Наносим урон
            Debug.Log($"Projectile hit {other.name} for {damage} damage");

            // Здесь можно добавить компонент здоровья и нанести урон
            // Health health = other.GetComponent<Health>();
            // if (health != null) health.TakeDamage(damage);

            // Создаем эффект попадания
            if (hitEffectPrefab != null)
            {
                GameObject hitEffect = Instantiate(hitEffectPrefab, transform.position, transform.rotation);
                Destroy(hitEffect, 2f);
            }

            // Уничтожаем снаряд
            if (destroyOnHit)
            {
                Destroy(gameObject);
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & targetLayers) != 0)
        {
            Debug.Log($"Projectile hit {collision.gameObject.name} for {damage} damage");

            // Создаем эффект попадания
            if (hitEffectPrefab != null)
            {
                GameObject hitEffect = Instantiate(hitEffectPrefab, collision.contacts[0].point, Quaternion.LookRotation(collision.contacts[0].normal));
                Destroy(hitEffect, 2f);
            }

            // Уничтожаем снаряд
            if (destroyOnHit)
            {
                Destroy(gameObject);
            }
        }
    }
}