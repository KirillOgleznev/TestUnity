// ============================================
// ПРОСТОЕ X-RAY РЕШЕНИЕ НА КАМЕРЕ
// (основано на реальных решениях с форумов Unity)
// ============================================
using UnityEngine;
using System.Collections.Generic;

public class CameraXRay : MonoBehaviour
{
    [Header("X-Ray Settings")]
    public Transform player; // Персонаж
    public LayerMask obstructionLayers = -1; // Слои препятствий
    public float transparentAlpha = 0.3f; // Прозрачность препятствий
    public float fadeSpeed = 5f; // Скорость изменения прозрачности
    [Space]
    public float xrayRadius = 10f; // РАДИУС действия X-Ray эффекта
    public bool useGradientByDistance = false; // Градиентная прозрачность по расстоянию
    [Space]
    public bool instantRestore = true; // МГНОВЕННЫЙ возврат прозрачности
    public bool enableXRay = true; // Включить X-Ray эффект

    [Header("Debug")]
    public bool showDebug = false;

    // Структура для отслеживания прозрачных объектов
    [System.Serializable]
    public class TransparentObject
    {
        public GameObject obj;
        public Renderer renderer;
        public Material originalMaterial;
        public Material transparentMaterial;
        public Color originalColor;
        public float currentAlpha;
        public bool isTransparent;
        public float gradientAlpha; // Альфа с учетом расстояния

        public TransparentObject(GameObject gameObject, Renderer rend)
        {
            obj = gameObject;
            renderer = rend;
            originalMaterial = rend.material;
            originalColor = originalMaterial.color;
            currentAlpha = originalColor.a;
            isTransparent = false;
            gradientAlpha = 0.3f; // Значение по умолчанию

            // ПРОСТОЙ ПОДХОД: Создаем копию материала для этого объекта
            transparentMaterial = new Material(originalMaterial);
            renderer.material = transparentMaterial; // Сразу применяем копию

            SetupTransparentMaterial(transparentMaterial);
        }

        private void SetupTransparentMaterial(Material mat)
        {
            string shaderName = mat.shader.name.ToLower();

            if (shaderName.Contains("universal render pipeline") || shaderName.Contains("urp"))
            {
                // URP Lit шейдер
                if (mat.HasProperty("_Surface"))
                {
                    mat.SetFloat("_Surface", 1); // 0 = Opaque, 1 = Transparent
                    mat.SetFloat("_Blend", 0); // 0 = Alpha, 1 = Premultiply, 2 = Additive, 3 = Multiply
                    mat.SetFloat("_AlphaClip", 0); // Disable alpha clipping
                    mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetFloat("_ZWrite", 0);
                    mat.renderQueue = 3000;

                    // Включаем правильные keywords для URP
                    mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    mat.EnableKeyword("_ALPHABLEND_ON");
                    mat.DisableKeyword("_ALPHATEST_ON");

                    Debug.Log($"X-Ray: Setup URP transparency for {mat.name}");
                }
            }
            else if (mat.HasProperty("_Mode"))
            {
                // Standard шейдер
                mat.SetInt("_Mode", 3); // Transparent mode
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;

                Debug.Log($"X-Ray: Setup Standard transparency for {mat.name}");
            }
            else
            {
                // Для других шейдеров - просто ставим очередь прозрачности
                mat.renderQueue = 3000;
                Debug.LogWarning($"X-Ray: Unknown shader {mat.shader.name}, only set render queue");
            }
        }

        // ОБНОВЛЕННЫЙ МЕТОД с поддержкой мгновенного восстановления
        public void UpdateTransparency(float targetAlpha, float speed, bool instantRestore = false, bool debug = false)
        {
            if (transparentMaterial == null || renderer == null) return;

            // МГНОВЕННОЕ восстановление если нужно
            if (instantRestore && targetAlpha >= originalColor.a)
            {
                currentAlpha = targetAlpha;
                if (debug)
                    Debug.Log($"X-Ray: INSTANT restore for {obj.name} to alpha: {currentAlpha:F3}");
            }
            else
            {
                // Обычное плавное изменение
                currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, speed * Time.deltaTime);
            }

            // Обновляем альфа для URP и Standard шейдеров
            Color color = transparentMaterial.color;
            color.a = currentAlpha;
            transparentMaterial.color = color;

            // Дополнительно для URP - обновляем _BaseColor если есть
            if (transparentMaterial.HasProperty("_BaseColor"))
            {
                transparentMaterial.SetColor("_BaseColor", color);
            }

            if (debug)
                Debug.Log($"X-Ray: {obj.name} alpha: {currentAlpha:F3} (target: {targetAlpha:F3}), Color: {color}");
        }

        public void RestoreOriginal()
        {
            if (renderer != null && originalMaterial != null)
            {
                renderer.material = originalMaterial;
                currentAlpha = originalColor.a;
                isTransparent = false;
            }
        }

        public void Cleanup()
        {
            if (transparentMaterial != null)
            {
                Object.DestroyImmediate(transparentMaterial);
            }
        }
    }

    private List<TransparentObject> currentObstructions = new List<TransparentObject>();
    private Dictionary<GameObject, TransparentObject> trackedObjects = new Dictionary<GameObject, TransparentObject>();

    void Start()
    {
        if (player == null)
        {
            GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null)
            {
                player = playerGO.transform;
            }
            else
            {
                Debug.LogWarning("CameraXRay: Player not found! Assign player manually or add 'Player' tag.");
                enableXRay = false;
            }
        }
    }

    void LateUpdate()
    {
        if (!enableXRay || player == null) return;

        // Очищаем список текущих препятствий
        currentObstructions.Clear();

        // Выполняем raycast от камеры к игроку
        PerformXRayRaycast();

        // Обновляем прозрачность всех отслеживаемых объектов
        UpdateAllTransparencies();
    }

    void Update()
    {
        // ОТЛАДКА: Клавиша X для принудительного тестирования эффекта
        if (Input.GetKeyDown(KeyCode.X) && enableXRay)
        {
            Debug.Log("=== X-RAY DEBUG TEST ===");
            Debug.Log($"Player position: {(player != null ? player.position.ToString() : "NULL")}");
            Debug.Log($"Camera position: {transform.position}");
            Debug.Log($"Tracked objects: {trackedObjects.Count}");
            Debug.Log($"Current obstructions: {currentObstructions.Count}");
            Debug.Log($"X-Ray radius: {xrayRadius}");
            Debug.Log($"Gradient by distance: {useGradientByDistance}");
            Debug.Log($"Instant restore: {instantRestore}");

            // Показываем все отслеживаемые объекты
            foreach (var kvp in trackedObjects)
            {
                var obj = kvp.Value;
                Debug.Log($"  - {kvp.Key.name}: Alpha={obj.currentAlpha:F3}, Transparent={obj.isTransparent}, Material={obj.transparentMaterial?.name}");

                // Показываем свойства материала
                if (obj.transparentMaterial != null)
                {
                    var mat = obj.transparentMaterial;
                    Debug.Log($"    Shader: {mat.shader.name}");
                    Debug.Log($"    RenderQueue: {mat.renderQueue}");
                    Debug.Log($"    Color: {mat.color}");
                    if (mat.HasProperty("_BaseColor"))
                        Debug.Log($"    _BaseColor: {mat.GetColor("_BaseColor")}");
                    if (mat.HasProperty("_Surface"))
                        Debug.Log($"    _Surface: {mat.GetFloat("_Surface")}");
                }
            }
        }

        // ПРИНУДИТЕЛЬНОЕ ТЕСТИРОВАНИЕ: Клавиша Z для ручной установки прозрачности
        if (Input.GetKeyDown(KeyCode.Z) && trackedObjects.Count > 0)
        {
            Debug.Log("=== FORCE TRANSPARENCY TEST ===");
            foreach (var kvp in trackedObjects)
            {
                var obj = kvp.Value;
                if (obj.transparentMaterial != null && obj.renderer != null)
                {
                    // ПРЯМОЕ изменение свойств материала для URP
                    var mat = obj.transparentMaterial;

                    // Принудительно устанавливаем настройки URP
                    if (mat.HasProperty("_Surface"))
                    {
                        mat.SetFloat("_Surface", 1); // Transparent
                        Debug.Log($"Force set _Surface=1 for {kvp.Key.name}");
                    }

                    // Устанавливаем очень заметную прозрачность
                    Color color = mat.color;
                    color.a = 0.1f; // Почти полностью прозрачный
                    mat.color = color;

                    if (mat.HasProperty("_BaseColor"))
                    {
                        mat.SetColor("_BaseColor", color);
                        Debug.Log($"Force set _BaseColor alpha=0.1 for {kvp.Key.name}");
                    }

                    Debug.Log($"Forced transparency for {kvp.Key.name}, color: {color}");
                }
            }
        }
    }

    private void PerformXRayRaycast()
    {
        Vector3 cameraPosition = transform.position;
        Vector3 playerPosition = player.position;
        Vector3 direction = (playerPosition - cameraPosition).normalized;
        float distance = Vector3.Distance(cameraPosition, playerPosition);

        // RaycastAll для обнаружения всех препятствий между камерой и игроком
        RaycastHit[] hits = Physics.RaycastAll(cameraPosition, direction, distance, obstructionLayers);

        if (showDebug)
        {
            Debug.DrawRay(cameraPosition, direction * distance, Color.red);
            Debug.Log($"X-Ray: Found {hits.Length} obstructions between camera and player");
        }

        // Обрабатываем все найденные препятствия
        foreach (RaycastHit hit in hits)
        {
            GameObject hitObject = hit.collider.gameObject;

            // Проверяем, что это не сам игрок
            if (hitObject == player.gameObject || hitObject.transform.IsChildOf(player))
                continue;

            // НОВАЯ ПРОВЕРКА: Ограничиваем радиусом от игрока
            float distanceToPlayer = Vector3.Distance(hit.point, playerPosition);
            if (distanceToPlayer > xrayRadius)
            {
                if (showDebug)
                    Debug.Log($"X-Ray: Object {hitObject.name} is outside radius ({distanceToPlayer:F2} > {xrayRadius})");
                continue;
            }

            Renderer renderer = hitObject.GetComponent<Renderer>();
            if (renderer == null)
            {
                if (showDebug)
                    Debug.LogWarning($"X-Ray: Object {hitObject.name} has no Renderer!");
                continue;
            }

            if (showDebug)
                Debug.Log($"X-Ray: Processing obstruction - {hitObject.name}, Distance: {distanceToPlayer:F2}, Material: {renderer.material.shader.name}");

            // Добавляем или получаем объект для отслеживания
            TransparentObject transparentObj = GetOrCreateTransparentObject(hitObject, renderer);
            if (transparentObj != null)
            {
                currentObstructions.Add(transparentObj);
                transparentObj.isTransparent = true;

                // ГРАДИЕНТНАЯ ПРОЗРАЧНОСТЬ по расстоянию (опционально)
                if (useGradientByDistance)
                {
                    float normalizedDistance = distanceToPlayer / xrayRadius;
                    float gradientAlpha = Mathf.Lerp(transparentAlpha, 1f, normalizedDistance);
                    transparentObj.gradientAlpha = gradientAlpha;
                }
                else
                {
                    transparentObj.gradientAlpha = transparentAlpha;
                }
            }
        }
    }

    private TransparentObject GetOrCreateTransparentObject(GameObject obj, Renderer renderer)
    {
        if (trackedObjects.ContainsKey(obj))
        {
            return trackedObjects[obj];
        }

        // Создаем новый прозрачный объект
        TransparentObject transparentObj = new TransparentObject(obj, renderer);
        trackedObjects[obj] = transparentObj;

        if (showDebug)
            Debug.Log($"X-Ray: Added new obstruction - {obj.name}");

        return transparentObj;
    }

    private void UpdateAllTransparencies()
    {
        List<GameObject> objectsToRemove = new List<GameObject>();

        foreach (var kvp in trackedObjects)
        {
            GameObject obj = kvp.Key;
            TransparentObject transparentObj = kvp.Value;

            if (obj == null || transparentObj.renderer == null)
            {
                objectsToRemove.Add(obj);
                continue;
            }

            // Определяем, должен ли объект быть прозрачным
            bool shouldBeTransparent = currentObstructions.Contains(transparentObj);
            float targetAlpha = shouldBeTransparent ? transparentObj.gradientAlpha : transparentObj.originalColor.a;

            if (showDebug && shouldBeTransparent)
                Debug.Log($"X-Ray: Making {obj.name} transparent (target alpha: {targetAlpha:F3}, gradient: {transparentObj.gradientAlpha:F3})");

            // КЛЮЧЕВОЕ ИЗМЕНЕНИЕ: передаем instantRestore параметр
            bool useInstantRestore = instantRestore && !shouldBeTransparent;
            transparentObj.UpdateTransparency(targetAlpha, fadeSpeed, useInstantRestore, showDebug);

            // Если объект полностью восстановлен и не должен быть прозрачным
            if (!shouldBeTransparent && Mathf.Approximately(transparentObj.currentAlpha, transparentObj.originalColor.a))
            {
                transparentObj.RestoreOriginal();
                objectsToRemove.Add(obj);

                if (showDebug)
                    Debug.Log($"X-Ray: Fully restored {obj.name}");
            }
        }

        // Удаляем объекты которые больше не нужно отслеживать
        foreach (GameObject obj in objectsToRemove)
        {
            if (trackedObjects.ContainsKey(obj))
            {
                trackedObjects[obj].Cleanup();
                trackedObjects.Remove(obj);
            }
        }
    }

    // Принудительная очистка всех эффектов
    public void ClearAllXRayEffects()
    {
        foreach (var kvp in trackedObjects)
        {
            if (kvp.Value != null)
            {
                kvp.Value.RestoreOriginal();
                kvp.Value.Cleanup();
            }
        }
        trackedObjects.Clear();
        currentObstructions.Clear();
    }

    void OnDestroy()
    {
        ClearAllXRayEffects();
    }

    void OnDisable()
    {
        ClearAllXRayEffects();
    }

    // Отладочная визуализация
    void OnDrawGizmos()
    {
        if (!showDebug || player == null) return;

        // Показываем луч от камеры к игроку
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, player.position);

        // НОВОЕ: Показываем радиус действия X-Ray эффекта
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(player.position, xrayRadius);

        // Показываем текст с радиусом
#if UNITY_EDITOR
        UnityEditor.Handles.Label(player.position + Vector3.up * (xrayRadius + 1f), $"X-Ray Radius: {xrayRadius:F1}m");
#endif

        // Показываем все препятствия
        Gizmos.color = Color.red;
        foreach (var transparentObj in currentObstructions)
        {
            if (transparentObj.obj != null)
            {
                Gizmos.DrawWireCube(transparentObj.obj.transform.position, Vector3.one * 0.5f);

                // Показываем линию от игрока к препятствию
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(player.position, transparentObj.obj.transform.position);
                Gizmos.color = Color.red;
            }
        }
    }
}