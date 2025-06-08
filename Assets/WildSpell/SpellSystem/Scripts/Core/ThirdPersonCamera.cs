using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("References")]
    public Transform target; // Персонаж

    [Header("Camera Settings")]
    public float distance = 5f;
    public float sensitivity = 2f;
    public float minY = -80f;
    public float maxY = 80f;
    public bool invertY = false; // Инверсия Y оси

    [Header("Camera Position")]
    public Vector3 offset = Vector3.zero; // Смещение от персонажа
    public float height = 1.5f; // Высота над персонажем
    public Vector3 shoulderOffset = Vector3.zero; // Смещение через плечо

    [Header("Collision Settings")]
    public LayerMask collisionLayers = -1; // Слои для проверки коллизий (НЕ включайте слой персонажа!)
    public float collisionBuffer = 0.2f; // Буфер от стены
    public float minDistance = 0.8f; // Минимальное расстояние до персонажа

    [Header("Distance Smoothing")]
    public float distanceSmoothSpeed = 8f; // Скорость сглаживания приближения
    public float distanceReturnSpeed = 3f; // Скорость возврата к исходному расстоянию

    [Header("Crosshair Settings")]
    public float crosshairDistance = 100f; // Расстояние прицела от камеры

    [Header("Character Visibility")]
    public float fadeCompleteDistance = 2.5f; // Расстояние полного исчезновения
    public float maxViewAngle = 45f; // Максимальный угол обзора персонажа
    public bool fadeCharacter = true; // Включить/выключить исчезновение персонажа

    [Header("Debug")]
    public bool showDebug = false; // Показать отладочную информацию

    private float rotationX = 0f;
    private float rotationY = 0f;
    private Vector3 crosshairWorldPosition;
    private float currentSmoothDistance; // Текущее сглаженное расстояние
    private Renderer[] characterRenderers; // Рендереры персонажа
    private bool characterVisible = true;

    // Публичные свойства для доступа из других скриптов
    public Vector3 CrosshairWorldPosition => crosshairWorldPosition;
    public Vector3 CameraForward => transform.forward;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        currentSmoothDistance = distance;

        // Начальные углы
        Vector3 angles = transform.eulerAngles;
        rotationX = angles.y;
        rotationY = angles.x;

        // Находим все рендереры персонажа
        if (target != null && fadeCharacter)
        {
            characterRenderers = target.GetComponentsInChildren<Renderer>();
            if (characterRenderers.Length > 0)
            {
                Debug.Log($"Найдено {characterRenderers.Length} рендереров для скрытия персонажа");
            }
            else
            {
                Debug.LogWarning("Рендереры персонажа не найдены!");
            }
        }

        // ВАЖНО: Убедитесь что слой персонажа НЕ включен в collisionLayers!
        if (target != null && target.gameObject.layer != 0)
        {
            int targetLayerMask = 1 << target.gameObject.layer;
            if ((collisionLayers.value & targetLayerMask) != 0)
            {
                Debug.LogWarning("Персонаж находится в слое коллизий камеры! Исключите слой персонажа из Collision Layers.");
            }
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Ввод мыши
        float mouseX = Input.GetAxis("Mouse X") * sensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity;

        if (invertY) mouseY = -mouseY;

        rotationX += mouseX;
        rotationY -= mouseY;
        rotationY = Mathf.Clamp(rotationY, minY, maxY);

        // Позиция цели с высотой
        Vector3 targetPosition = target.position + offset + Vector3.up * height;

        // Поворот камеры (НЕ МЕНЯЕТСЯ - важно для прицеливания)
        Quaternion rotation = Quaternion.Euler(rotationY, rotationX, 0);

        // Направление от персонажа к камере
        Vector3 direction = rotation * Vector3.back;
        Vector3 shoulderPos = rotation * shoulderOffset;
        Vector3 startPos = targetPosition + shoulderPos;

        // ПРОСТОЕ И НАДЁЖНОЕ РЕШЕНИЕ: Linecast от персонажа к желаемой позиции камеры
        Vector3 desiredCameraPosition = startPos + direction * distance;
        Vector3 finalCameraPosition = GetOccludedCameraPosition(startPos, desiredCameraPosition);

        // Вычисляем желаемое расстояние на основе финальной позиции
        float desiredDistance = Vector3.Distance(startPos, finalCameraPosition);
        desiredDistance = Mathf.Max(desiredDistance, minDistance);

        // Сглаживание расстояния
        float smoothSpeed = desiredDistance < currentSmoothDistance ? distanceSmoothSpeed : distanceReturnSpeed;
        currentSmoothDistance = Mathf.Lerp(currentSmoothDistance, desiredDistance, smoothSpeed * Time.deltaTime);

        // Устанавливаем финальную позицию камеры с сглаживанием
        Vector3 smoothedPosition = startPos + direction * currentSmoothDistance;
        transform.position = smoothedPosition;
        transform.rotation = rotation;

        // Вычисляем позицию прицела
        crosshairWorldPosition = transform.position + transform.forward * crosshairDistance;

        // Управление видимостью персонажа
        if (fadeCharacter && characterRenderers != null && characterRenderers.Length > 0)
        {
            UpdateCharacterVisibility();
        }
    }

    // КЛЮЧЕВОЙ МЕТОД: Проверка препятствий между персонажем и камерой (из реальных проектов)
    private Vector3 GetOccludedCameraPosition(Vector3 startPosition, Vector3 desiredPosition)
    {
        RaycastHit hit;
        Vector3 direction = (desiredPosition - startPosition).normalized;
        float distance = Vector3.Distance(startPosition, desiredPosition);

        // Linecast от персонажа к желаемой позиции камеры
        if (Physics.Linecast(startPosition, desiredPosition, out hit, collisionLayers))
        {
            // Если есть препятствие - ставим камеру перед ним
            Vector3 safePosition = hit.point - direction * collisionBuffer;

            if (showDebug)
                Debug.Log($"Linecast hit: {hit.collider.name}, distance: {hit.distance:F2}");

            return safePosition;
        }

        // Если препятствий нет - возвращаем желаемую позицию
        return desiredPosition;
    }

    private void UpdateCharacterVisibility()
    {
        float cameraDistance = currentSmoothDistance;
        Vector3 directionToCharacter = (target.position - transform.position).normalized;
        Vector3 cameraForward = transform.forward;
        float angleToCharacter = Vector3.Angle(cameraForward, directionToCharacter);

        bool shouldBeHidden = cameraDistance < fadeCompleteDistance || angleToCharacter > maxViewAngle;
        bool shouldBeVisible = !shouldBeHidden;

        if (shouldBeVisible != characterVisible)
        {
            characterVisible = shouldBeVisible;

            foreach (Renderer renderer in characterRenderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = characterVisible;
                }
            }

            if (showDebug)
                Debug.Log($"Персонаж {(characterVisible ? "ПОКАЗАН" : "СКРЫТ")} - расстояние: {cameraDistance:F2}, угол: {angleToCharacter:F1}°");
        }
    }

    // Методы для оружия
    public Vector3 GetShootDirection()
    {
        return (crosshairWorldPosition - transform.position).normalized;
    }

    public Vector3 GetAimPoint(float distance)
    {
        return transform.position + GetShootDirection() * distance;
    }

    void Update()
    {
        // Отладочные клавиши
        if (Input.GetKeyDown(KeyCode.C) && target != null)
        {
            Vector3 targetPos = target.position + offset + Vector3.up * height;
            Quaternion rotation = Quaternion.Euler(rotationY, rotationX, 0);
            Vector3 direction = rotation * Vector3.back;
            Vector3 shoulderPos = rotation * shoulderOffset;
            Vector3 startPos = targetPos + shoulderPos;
            Vector3 desiredPos = startPos + direction * distance;

            RaycastHit hit;
            bool lineBlocked = Physics.Linecast(startPos, desiredPos, out hit, collisionLayers);

            Debug.Log($"=== LINECAST ТЕСТ ===");
            Debug.Log($"Линия от персонажа к камере заблокирована: {lineBlocked}");
            if (lineBlocked)
            {
                Debug.Log($"  Препятствие: {hit.collider.name} на расстоянии {hit.distance:F2}");
                Debug.Log($"  Позиция препятствия: {hit.point}");
            }
            Debug.Log($"Текущее сглаженное расстояние: {currentSmoothDistance:F2}");
            Debug.Log($"Желаемое расстояние: {distance}");
        }

        if (Input.GetKeyDown(KeyCode.H) && characterRenderers != null)
        {
            characterVisible = !characterVisible;
            foreach (Renderer renderer in characterRenderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = characterVisible;
                }
            }
            Debug.Log($"Персонаж принудительно {(characterVisible ? "ПОКАЗАН" : "СКРЫТ")}");
        }
    }

    void OnDrawGizmos()
    {
        if (!showDebug || target == null) return;

        Vector3 targetPos = target.position + offset + Vector3.up * height;
        Quaternion rotation = Quaternion.Euler(rotationY, rotationX, 0);
        Vector3 shoulderPos = rotation * shoulderOffset;
        Vector3 startPos = targetPos + shoulderPos;
        Vector3 direction = rotation * Vector3.back;

        // Показываем желаемую позицию камеры
        Vector3 desiredPos = startPos + direction * distance;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(desiredPos, 0.2f);

        // Показываем Linecast от персонажа к желаемой позиции
        RaycastHit hit;
        bool lineBlocked = Physics.Linecast(startPos, desiredPos, out hit, collisionLayers);

        Gizmos.color = lineBlocked ? Color.red : Color.green;

        if (lineBlocked)
        {
            // Линия до препятствия
            Gizmos.DrawLine(startPos, hit.point);
            // Точка препятствия
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(hit.point, 0.15f);
            // Безопасная позиция камеры
            Vector3 safePos = hit.point - direction * collisionBuffer;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(safePos, 0.1f);
        }
        else
        {
            // Полная линия если препятствий нет
            Gizmos.DrawLine(startPos, desiredPos);
        }

        // Показываем текущую позицию камеры
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, 0.1f);

        // Линия прицеливания
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, transform.forward * 5f);
    }
}