using UnityEngine;

public class FlamethrowerController : MonoBehaviour
{
    [Header("Flamethrower Settings")]
    [SerializeField] private GameObject flamePrefab; // Префаб огня из HOVL ассета
    [SerializeField] private Transform spawnPoint; // Точка где появляется огонь

    private GameObject currentFlameEffect; // Текущий активный эффект огня
    private bool isFlaming = false;
    private Camera playerCamera;

    private void Start()
    {
        // Если не назначена точка спауна, используем позицию объекта
        if (spawnPoint == null)
            spawnPoint = transform;

        // Получаем камеру игрока
        playerCamera = Camera.main;
        if (playerCamera == null)
            playerCamera = FindObjectOfType<Camera>();
    }

    private void Update()
    {
        UpdateFlameDirection();
        HandleInput();
    }

    private void UpdateFlameDirection()
    {
        // Поворачиваем spawn point в направлении модели игрока
        if (spawnPoint != null)
        {
            spawnPoint.rotation = transform.rotation;
        }
    }

    private void HandleInput()
    {
        // Проверяем зажатие ЛКМ
        bool wantsToFlame = Input.GetMouseButton(0);

        if (wantsToFlame && !isFlaming)
        {
            StartFlaming();
        }
        else if (!wantsToFlame && isFlaming)
        {
            StopFlaming();
        }
    }

    private void StartFlaming()
    {
        if (flamePrefab == null) return;

        isFlaming = true;

        // Создаем эффект огня в точке спауна
        currentFlameEffect = Instantiate(flamePrefab, spawnPoint.position, spawnPoint.rotation);

        // Убираем движение если есть Rigidbody
        Rigidbody rb = currentFlameEffect.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        // Делаем эффект дочерним объектом spawn point
        currentFlameEffect.transform.SetParent(spawnPoint);
    }

    private void StopFlaming()
    {
        isFlaming = false;

        // Уничтожаем текущий эффект огня
        if (currentFlameEffect != null)
        {
            Destroy(currentFlameEffect);
            currentFlameEffect = null;
        }
    }
}