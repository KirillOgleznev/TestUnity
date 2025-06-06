using UnityEngine;

public class FireballCaster : MonoBehaviour
{
    [Header("Fireball Settings")]
    [SerializeField] private GameObject fireballPrefab; // Префаб файрбола из HOVL ассета
    [SerializeField] private Transform castPoint; // Точка откуда кастим (обычно рука/посох)
    [SerializeField] private float fireballSpeed = 20f;
    [SerializeField] private float fireballLifetime = 5f;

    [Header("Casting Settings")]
    [SerializeField] private float castCooldown = 0.5f; // Задержка между кастами
    [SerializeField] private LayerMask targetLayerMask = -1; // Что может быть целью
    [SerializeField] private float maxCastRange = 100f;
    [SerializeField] private bool useSimpleDirection = false; // Простое направление по камере

    [Header("Audio (Optional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip castSound;

    private Camera playerCamera;
    private float lastCastTime;

    private void Start()
    {
        // Получаем камеру игрока
        playerCamera = Camera.main;
        if (playerCamera == null)
            playerCamera = FindObjectOfType<Camera>();

        // Если не назначена точка каста, используем позицию объекта
        if (castPoint == null)
            castPoint = transform;

        // Проверяем наличие префаба
        if (fireballPrefab == null)
        {
            Debug.LogError("Fireball Prefab не назначен! Добавьте префаб из HOVL ассета.");
        }
    }

    private void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        // Проверяем нажатие ЛКМ
        if (Input.GetMouseButtonDown(0))
        {
            TryCastFireball();
        }
    }

    private void TryCastFireball()
    {
        // Проверяем кулдаун
        if (Time.time < lastCastTime + castCooldown)
            return;

        // Проверяем наличие префаба
        if (fireballPrefab == null)
            return;

        // Определяем направление каста
        Vector3 targetDirection = GetCastDirection();

        // Создаем файрбол
        CastFireball(targetDirection);

        // Обновляем время последнего каста
        lastCastTime = Time.time;
    }

    private Vector3 GetCastDirection()
    {
        if (useSimpleDirection)
        {
            // ПРОСТОЙ СПОСОБ: летим прямо по направлению камеры
            return playerCamera.transform.forward;
        }

        // ТОЧНЫЙ СПОСОБ: рейкаст к курсору мыши
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        Vector3 targetPoint;

        if (Physics.Raycast(ray, out hit, maxCastRange, targetLayerMask))
        {
            // Если попали в коллайдер, летим к точке попадания
            targetPoint = hit.point;
        }
        else
        {
            // Если не попали, летим по направлению рея на максимальную дистанцию
            targetPoint = ray.origin + ray.direction * maxCastRange;
        }

        // ИСПРАВЛЕНИЕ: Правильный расчёт направления
        Vector3 direction = (targetPoint - castPoint.position).normalized;

        // Дополнительная проверка - если направление смотрит вниз, корректируем
        if (direction.y < -0.5f)
        {
            direction.y = Mathf.Max(direction.y, -0.3f);
            direction = direction.normalized;
        }

        return direction;
    }

    private void CastFireball(Vector3 direction)
    {
        // Создаем файрбол в точке каста
        GameObject fireball = Instantiate(fireballPrefab, castPoint.position, Quaternion.LookRotation(direction));

        // Добавляем движение файрболу
        Rigidbody rb = fireball.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = direction * fireballSpeed;
        }
        else
        {
            // Если нет Rigidbody, добавляем простое движение
            FireballMover mover = fireball.AddComponent<FireballMover>();
            mover.Initialize(direction, fireballSpeed);
        }

        // Уничтожаем файрбол через заданное время
        Destroy(fireball, fireballLifetime);

        // Воспроизводим звук каста
        PlayCastSound();

        Debug.Log($"Файрбол выпущен в направлении: {direction}");
    }

    private void PlayCastSound()
    {
        if (audioSource != null && castSound != null)
        {
            audioSource.PlayOneShot(castSound);
        }
    }

    // Визуализация в Scene view
    private void OnDrawGizmosSelected()
    {
        if (castPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(castPoint.position, 0.1f);

            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(castPoint.position, transform.forward * 2f);
        }
    }
}

// Дополнительный компонент для движения файрбола без Rigidbody
public class FireballMover : MonoBehaviour
{
    private Vector3 direction;
    private float speed;

    public void Initialize(Vector3 moveDirection, float moveSpeed)
    {
        direction = moveDirection;
        speed = moveSpeed;
    }

    private void Update()
    {
        transform.position += direction * speed * Time.deltaTime;
    }
}