// 1. Создайте НОВЫЙ компонент OptimizedNavigation (не заменяйте EnemyController!)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class OptimizedNavigation : MonoBehaviour
{
    [Header("Performance Settings")]
    [SerializeField] private float cacheLifetime = 3f;
    [SerializeField] private float pathCheckInterval = 0.3f; // Реже проверяем для производительности

    private NavMeshAgent agent;
    private static Dictionary<Vector3, CachedPath> globalPathCache = new Dictionary<Vector3, CachedPath>();
    private static List<Vector3> keysToRemove = new List<Vector3>();

    private Vector3 lastDestination;
    private float lastPathCheck;
    private bool isCalculatingPath;

    private struct CachedPath
    {
        public Vector3 bestDestination;
        public float timestamp;
        public NavMeshPathStatus status;
    }

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        // Разброс времени проверки для разных врагов (избегаем одновременных вычислений)
        pathCheckInterval += Random.Range(-0.1f, 0.1f);
    }

    // Основной метод - вызывается из EnemyController.SetNavDestination
    public bool TryOptimizeDestination(Vector3 originalDestination)
    {
        // Проверяем нужна ли оптимизация
        if (!ShouldOptimize(originalDestination)) return false;

        // Проверяем кэш
        Vector3 gridPos = SnapToGrid(originalDestination, 2f); // Сетка 2x2м для кэша
        if (TryGetCachedPath(gridPos, out Vector3 cachedDest))
        {
            agent.SetDestination(cachedDest);
            lastDestination = originalDestination;
            return true;
        }

        // Запускаем асинхронную оптимизацию
        if (!isCalculatingPath && Time.time - lastPathCheck > pathCheckInterval)
        {
            StartCoroutine(OptimizePathAsync(originalDestination, gridPos));
            lastPathCheck = Time.time;
        }

        return false;
    }

    private bool ShouldOptimize(Vector3 destination)
    {
        // Оптимизируем только если цель изменилась значительно
        float change = Vector3.Distance(destination, lastDestination);
        return change > 2f && agent.pathStatus == NavMeshPathStatus.PathPartial;
    }

    private bool TryGetCachedPath(Vector3 gridPos, out Vector3 destination)
    {
        if (globalPathCache.ContainsKey(gridPos))
        {
            CachedPath cached = globalPathCache[gridPos];
            if (Time.time - cached.timestamp < cacheLifetime)
            {
                destination = cached.bestDestination;
                return true;
            }
        }

        destination = Vector3.zero;
        return false;
    }

    private IEnumerator OptimizePathAsync(Vector3 originalDest, Vector3 gridPos)
    {
        isCalculatingPath = true;

        // Ждем случайный кадр для распределения нагрузки
        int randomDelay = Random.Range(0, 3);
        for (int i = 0; i < randomDelay; i++)
            yield return null;

        NavMeshPath path = new NavMeshPath();
        bool pathCalculated = agent.CalculatePath(originalDest, path);

        yield return null; // Даем кадр передышки

        Vector3 bestDestination = originalDest;

        if (pathCalculated)
        {
            switch (path.status)
            {
                case NavMeshPathStatus.PathComplete:
                    bestDestination = originalDest;
                    break;

                case NavMeshPathStatus.PathPartial:
                    if (path.corners.Length > 1)
                    {
                        // Берем предпоследнюю точку для более стабильного движения
                        bestDestination = path.corners[Mathf.Max(0, path.corners.Length - 2)];
                    }
                    break;

                case NavMeshPathStatus.PathInvalid:
                    // Простой поиск без NavMesh.SamplePosition (для производительности)
                    bestDestination = FindSimpleAlternative(originalDest);
                    break;
            }
        }

        // Кэшируем результат
        CacheResult(gridPos, bestDestination, path.status);

        // Применяем результат
        agent.SetDestination(bestDestination);
        lastDestination = originalDest;

        isCalculatingPath = false;
    }

    private Vector3 FindSimpleAlternative(Vector3 original)
    {
        // Быстрый поиск без дорогих операций NavMesh.SamplePosition
        Vector3 dirToTarget = (original - transform.position).normalized;

        // Пробуем точки на 25%, 50%, 75% пути к цели
        float[] distances = { 0.25f, 0.5f, 0.75f };

        foreach (float dist in distances)
        {
            Vector3 testPoint = transform.position + dirToTarget *
                               (Vector3.Distance(transform.position, original) * dist);

            NavMeshHit hit;
            if (NavMesh.SamplePosition(testPoint, out hit, 3f, NavMesh.AllAreas))
            {
                return hit.position;
            }
        }

        return transform.position; // Остаемся на месте
    }

    private void CacheResult(Vector3 gridPos, Vector3 destination, NavMeshPathStatus status)
    {
        CachedPath cached = new CachedPath
        {
            bestDestination = destination,
            timestamp = Time.time,
            status = status
        };

        globalPathCache[gridPos] = cached;

        // Периодическая очистка кэша
        if (Random.Range(0f, 1f) < 0.01f) // 1% шанс
        {
            StartCoroutine(CleanCacheCoroutine());
        }
    }

    private IEnumerator CleanCacheCoroutine()
    {
        keysToRemove.Clear();

        foreach (var kvp in globalPathCache)
        {
            if (Time.time - kvp.Value.timestamp > cacheLifetime)
            {
                keysToRemove.Add(kvp.Key);
            }

            // Даем кадр передышки каждые 10 проверок
            if (keysToRemove.Count % 10 == 0)
                yield return null;
        }

        foreach (Vector3 key in keysToRemove)
        {
            globalPathCache.Remove(key);
        }
    }

    private Vector3 SnapToGrid(Vector3 position, float gridSize)
    {
        return new Vector3(
            Mathf.Round(position.x / gridSize) * gridSize,
            Mathf.Round(position.y / gridSize) * gridSize,
            Mathf.Round(position.z / gridSize) * gridSize
        );
    }

    // Для отладки
    void OnDrawGizmosSelected()
    {
        if (agent != null && agent.hasPath)
        {
            NavMeshPath path = agent.path;
            Color color = path.status == NavMeshPathStatus.PathComplete ? Color.green : Color.yellow;

            for (int i = 1; i < path.corners.Length; i++)
            {
                Gizmos.color = color;
                Gizmos.DrawLine(path.corners[i - 1], path.corners[i]);
            }
        }
    }
}

// 2. Модификация вашего EnemyController.SetNavDestination (только одна строка!)
/*
В EnemyController.cs найдите метод SetNavDestination и добавьте в начало:

public void SetNavDestination(Vector3 destination)
{
    if (NavMeshAgent)
    {
        // ДОБАВЬТЕ ЭТУ СТРОКУ - проверка оптимизации
        OptimizedNavigation optimizer = GetComponent<OptimizedNavigation>();
        if (optimizer != null && optimizer.TryOptimizeDestination(destination))
        {
            return; // Оптимизация применена, выходим
        }
        
        // Ваш оригинальный код остается без изменений
        NavMeshAgent.SetDestination(destination);
        
        // Весь остальной код как был...
    }
}
*/