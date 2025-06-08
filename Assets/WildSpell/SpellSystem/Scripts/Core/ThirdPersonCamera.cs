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
    public float collisionRadius = 0.2f; // Радиус сферы для проверки коллизий
    public float minDistance = 0.8f; // Минимальное расстояние до персонажа
    public float collisionBuffer = 0.2f; // Буфер от стены
    public float raycastStartOffset = 0.5f; // Отступ от персонажа для начала проверки

    [Header("Distance Smoothing")]
    public float distanceSmoothSpeed = 8f; // Скорость сглаживания приближения (чем больше - тем быстрее)
    public float distanceReturnSpeed = 3f; // Скорость возврата к исходному расстоянию (чем меньше - тем плавнее)

    [Header("Crosshair Settings")]
    public float crosshairDistance = 100f; // Расстояние прицела от камеры

    [Header("Character Visibility")]
    public float fadeCompleteDistance = 2.5f; // Расстояние полного исчезновения
    public float maxViewAngle = 45f; // Максимальный угол обзора персонажа (0 = смотрим прямо на персонажа)
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
        currentSmoothDistance = distance; // Инициализируем сглаженное расстояние

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

        // Инверсия Y
        if (invertY)
            mouseY = -mouseY;

        rotationX += mouseX;
        rotationY -= mouseY;

        // Ограничь поворот
        rotationY = Mathf.Clamp(rotationY, minY, maxY);

        // Позиция цели с высотой
        Vector3 targetPosition = target.position + offset + Vector3.up * height;

        // Поворот камеры
        Quaternion rotation = Quaternion.Euler(rotationY, rotationX, 0);

        // Направление камеры
        Vector3 direction = rotation * Vector3.back;
        Vector3 shoulderPos = rotation * shoulderOffset;
        Vector3 startPos = targetPosition + shoulderPos;

        // Проверка коллизий - определяем ЖЕЛАЕМОЕ расстояние
        float desiredDistance = distance;

        // ПЕРВАЯ ПРОВЕРКА: есть ли стена сразу за персонажем?
        RaycastHit directHit;
        if (Physics.Raycast(startPos, direction, out directHit, distance + 0.5f, collisionLayers))
        {
            // Если стена близко к персонажу, используем её расстояние
            desiredDistance = Mathf.Max(directHit.distance - collisionBuffer, minDistance);

            if (showDebug)
                Debug.Log($"Прямая коллизия с {directHit.collider.name}, желаемое расстояние: {desiredDistance}");
        }
        else
        {
            // ВТОРАЯ ПРОВЕРКА: обычная проверка с отступом (если нет стены сразу за персонажем)
            Vector3 raycastStart = startPos + direction * raycastStartOffset;
            float raycastDistance = distance - raycastStartOffset;

            RaycastHit hit;
            if (raycastDistance > 0 && Physics.SphereCast(raycastStart, collisionRadius, direction, out hit, raycastDistance, collisionLayers))
            {
                // Вычисляем финальное расстояние с учетом отступа
                float hitDistance = raycastStartOffset + hit.distance - collisionBuffer;
                desiredDistance = Mathf.Max(hitDistance, minDistance);

                if (showDebug)
                    Debug.Log($"SphereCast коллизия с {hit.collider.name}, желаемое расстояние: {desiredDistance}");
            }
            else if (showDebug)
            {
                Debug.Log($"Коллизий нет, желаемое расстояние: {desiredDistance}");
            }
        }

        // СГЛАЖИВАНИЕ расстояния
        float smoothSpeed;
        if (desiredDistance < currentSmoothDistance)
        {
            // Приближаемся к стене - быстрее
            smoothSpeed = distanceSmoothSpeed;
        }
        else
        {
            // Отдаляемся от стены - медленнее, плавнее
            smoothSpeed = distanceReturnSpeed;
        }

        currentSmoothDistance = Mathf.Lerp(currentSmoothDistance, desiredDistance, smoothSpeed * Time.deltaTime);

        // Позиция камеры с использованием сглаженного расстояния
        Vector3 finalPosition = startPos + direction * currentSmoothDistance;
        transform.position = finalPosition;
        transform.rotation = rotation; // Камера смотрит по направлению поворота (как в RoR2)

        // Вычисляем позицию прицела (остается без изменений)
        crosshairWorldPosition = transform.position + transform.forward * crosshairDistance;

        // Управление видимостью персонажа
        if (fadeCharacter && characterRenderers != null && characterRenderers.Length > 0)
        {
            UpdateCharacterVisibility();
        }
    }

    private void UpdateCharacterVisibility()
    {
        // Текущее расстояние камеры
        float cameraDistance = currentSmoothDistance;

        // Проверяем угол обзора персонажа
        Vector3 directionToCharacter = (target.position - transform.position).normalized;
        Vector3 cameraForward = transform.forward;
        float angleToCharacter = Vector3.Angle(cameraForward, directionToCharacter);

        // ИСПРАВЛЕНО: Персонаж должен быть СКРЫТ если:
        // 1. Камера слишком близко ИЛИ 2. Угол обзора слишком большой
        bool shouldBeHidden = cameraDistance < fadeCompleteDistance || angleToCharacter > maxViewAngle;
        bool shouldBeVisible = !shouldBeHidden;

        if (showDebug)
        {
            Debug.Log($"Расстояние: {cameraDistance:F2} (<{fadeCompleteDistance}?), Угол: {angleToCharacter:F1}° (>{maxViewAngle}?), Скрыт: {shouldBeHidden}");
        }

        if (shouldBeVisible != characterVisible)
        {
            characterVisible = shouldBeVisible;

            int processedCount = 0;
            foreach (Renderer renderer in characterRenderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = characterVisible;
                    processedCount++;
                }
            }

            Debug.Log($"*** Персонаж {(characterVisible ? "ПОКАЗАН" : "СКРЫТ")} *** расстояние: {cameraDistance:F2}, угол: {angleToCharacter:F1}°, рендереров: {processedCount}");
        }
    }

    // Метод для получения направления стрельбы (для оружия)
    public Vector3 GetShootDirection()
    {
        return (crosshairWorldPosition - transform.position).normalized;
    }

    // Метод для получения точки прицеливания на определенном расстоянии
    public Vector3 GetAimPoint(float distance)
    {
        return transform.position + GetShootDirection() * distance;
    }

    // Метод для принудительного тестирования скрытия персонажа (для отладки)
    void Update()
    {
        // Нажмите клавишу H для принудительного скрытия/показа персонажа (для тестирования)
        if (Input.GetKeyDown(KeyCode.H) && characterRenderers != null)
        {
            characterVisible = !characterVisible;
            int hiddenCount = 0;
            foreach (Renderer renderer in characterRenderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = characterVisible;
                    hiddenCount++;
                    Debug.Log($"Рендерер {renderer.name}: enabled = {renderer.enabled}");
                }
            }
            Debug.Log($"[ТЕСТ H] Персонаж принудительно {(characterVisible ? "ПОКАЗАН" : "СКРЫТ")}, обработано: {hiddenCount} рендереров");
        }

        // Тест с клавишей G - показать информацию о рендерерах
        if (Input.GetKeyDown(KeyCode.G) && characterRenderers != null)
        {
            Debug.Log($"=== ТЕСТ РЕНДЕРЕРОВ ===");
            Debug.Log($"Найдено рендереров: {characterRenderers.Length}");
            for (int i = 0; i < characterRenderers.Length; i++)
            {
                if (characterRenderers[i] != null)
                {
                    Renderer r = characterRenderers[i];
                    Debug.Log($"  {i}: {r.name} ({r.GetType().Name}) - enabled: {r.enabled}, active: {r.gameObject.activeInHierarchy}");
                }
                else
                {
                    Debug.Log($"  {i}: NULL рендерер!");
                }
            }
        }
    }
}