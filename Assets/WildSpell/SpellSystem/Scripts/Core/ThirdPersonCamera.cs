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

    [Header("Crosshair Settings")]
    public float crosshairDistance = 100f; // Расстояние прицела от камеры

    [Header("Debug")]
    public bool showDebug = false; // Показать отладочную информацию

    private float rotationX = 0f;
    private float rotationY = 0f;
    private Vector3 crosshairWorldPosition;

    // Публичные свойства для доступа из других скриптов
    public Vector3 CrosshairWorldPosition => crosshairWorldPosition;
    public Vector3 CameraForward => transform.forward;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        // Начальные углы
        Vector3 angles = transform.eulerAngles;
        rotationX = angles.y;
        rotationY = angles.x;

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

        // Проверка коллизий - МГНОВЕННАЯ без сглаживания
        float currentDistance = distance;

        // ПЕРВАЯ ПРОВЕРКА: есть ли стена сразу за персонажем?
        RaycastHit directHit;
        if (Physics.Raycast(startPos, direction, out directHit, distance + 0.5f, collisionLayers))
        {
            // Если стена близко к персонажу, используем её расстояние
            currentDistance = Mathf.Max(directHit.distance - collisionBuffer, minDistance);

            if (showDebug)
                Debug.Log($"Прямая коллизия с {directHit.collider.name}, расстояние: {currentDistance}");
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
                currentDistance = Mathf.Max(hitDistance, minDistance);

                if (showDebug)
                    Debug.Log($"SphereCast коллизия с {hit.collider.name}, расстояние: {currentDistance}");
            }
            else if (showDebug)
            {
                Debug.Log($"Коллизий нет, расстояние: {currentDistance}");
            }
        }

        // Позиция камеры - БЕЗ сглаживания, мгновенная
        Vector3 finalPosition = startPos + direction * currentDistance;
        transform.position = finalPosition;
        transform.rotation = rotation; // Камера смотрит по направлению поворота (как в RoR2)

        // Вычисляем позицию прицела
        crosshairWorldPosition = transform.position + transform.forward * crosshairDistance;
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

    // Дебаг визуализация
    void OnDrawGizmosSelected()
    {
        if (target == null) return;

        Vector3 targetPosition = target.position + offset + Vector3.up * height;
        Quaternion rotation = Quaternion.Euler(rotationY, rotationX, 0);
        Vector3 direction = rotation * Vector3.back;
        Vector3 shoulderPos = rotation * shoulderOffset;
        Vector3 startPos = targetPosition + shoulderPos;

        // ПЕРВАЯ ПРОВЕРКА: прямой raycast от персонажа (красная линия)
        Gizmos.color = Color.red;
        RaycastHit directHit;
        if (Physics.Raycast(startPos, direction, out directHit, distance + 0.5f, collisionLayers))
        {
            Gizmos.DrawLine(startPos, directHit.point);
            Gizmos.DrawWireSphere(directHit.point, 0.2f);
        }
        else
        {
            Gizmos.DrawLine(startPos, startPos + direction * distance);
        }

        // ВТОРАЯ ПРОВЕРКА: SphereCast с отступом (синяя линия)
        Vector3 raycastStart = startPos + direction * raycastStartOffset;
        Gizmos.color = Color.green;
        Gizmos.DrawLine(startPos, raycastStart);
        Gizmos.DrawWireSphere(raycastStart, 0.1f);

        Gizmos.color = Color.blue;
        float raycastDistance = distance - raycastStartOffset;
        if (raycastDistance > 0)
        {
            Gizmos.DrawLine(raycastStart, raycastStart + direction * raycastDistance);
            Gizmos.DrawWireSphere(raycastStart + direction * raycastDistance, collisionRadius);
        }

        // Текущая позиция камеры (желтая сфера)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.15f);

        // Направление прицела (фиолетовая линия)
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(transform.position, crosshairWorldPosition);
        Gizmos.DrawWireSphere(crosshairWorldPosition, 0.3f);
    }

    void OnGUI()
    {
        // Простой прицел в центре экрана
        float crosshairSize = 20f;
        float centerX = Screen.width * 0.5f;
        float centerY = Screen.height * 0.5f;

        GUI.color = Color.white;
        GUI.DrawTexture(new Rect(centerX - 1, centerY - crosshairSize * 0.5f, 2, crosshairSize), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(centerX - crosshairSize * 0.5f, centerY - 1, crosshairSize, 2), Texture2D.whiteTexture);
    }
}