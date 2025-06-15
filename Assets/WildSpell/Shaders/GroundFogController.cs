using UnityEngine;

public class GroundFogController : MonoBehaviour
{
    [Header("Настройки тумана")]
    public GameObject fogPrefab; // Префаб с туманом
    public Transform player; // Ссылка на игрока
    public float minDistanceFromPlayer = 50f; // Минимальное расстояние от игрока
    public float maxDistanceFromPlayer = 200f; // Максимальное расстояние
    public float fogHeight = 5f; // Высота тумана над землей
    public int fogCount = 20; // Количество облаков тумана

    [Header("Анимация")]
    public float driftSpeed = 2f; // Скорость дрейфа тумана
    public Vector3 windDirection = new Vector3(1, 0, 0.5f); // Направление ветра

    private GameObject[] fogInstances;
    private Vector3[] fogDirections;
    private float[] fogSpeeds;

    void Start()
    {
        CreateFogInstances();
    }

    void CreateFogInstances()
    {
        fogInstances = new GameObject[fogCount];
        fogDirections = new Vector3[fogCount];
        fogSpeeds = new float[fogCount];

        for (int i = 0; i < fogCount; i++)
        {
            // Создаем экземпляр тумана
            Vector3 randomPos = GetRandomFogPosition();
            fogInstances[i] = Instantiate(fogPrefab, randomPos, Quaternion.identity);

            // Случайное направление движения
            fogDirections[i] = (windDirection + Random.insideUnitSphere * 0.3f).normalized;
            fogSpeeds[i] = driftSpeed + Random.Range(-0.5f, 0.5f);

            // Случайный размер
            float scale = Random.Range(0.8f, 1.5f);
            fogInstances[i].transform.localScale = Vector3.one * scale;
        }
    }

    Vector3 GetRandomFogPosition()
    {
        Vector3 playerPos = player.position;

        // Генерируем позицию в кольце вокруг игрока
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float distance = Random.Range(minDistanceFromPlayer, maxDistanceFromPlayer);

        Vector3 offset = new Vector3(
            Mathf.Cos(angle) * distance,
            0,
            Mathf.Sin(angle) * distance
        );

        Vector3 fogPos = playerPos + offset;

        // Размещаем туман на поверхности земли + небольшая высота
        if (Physics.Raycast(fogPos + Vector3.up * 100f, Vector3.down, out RaycastHit hit, 200f))
        {
            fogPos.y = hit.point.y + fogHeight;
        }
        else
        {
            fogPos.y = playerPos.y + fogHeight;
        }

        return fogPos;
    }

    void Update()
    {
        UpdateFogPositions();
    }

    void UpdateFogPositions()
    {
        Vector3 playerPos = player.position;

        for (int i = 0; i < fogInstances.Length; i++)
        {
            if (fogInstances[i] == null) continue;

            // Двигаем туман
            fogInstances[i].transform.position += fogDirections[i] * fogSpeeds[i] * Time.deltaTime;

            // Проверяем, не слишком ли близко к игроку или далеко
            float distanceToPlayer = Vector3.Distance(fogInstances[i].transform.position, playerPos);

            if (distanceToPlayer < minDistanceFromPlayer || distanceToPlayer > maxDistanceFromPlayer)
            {
                // Перемещаем туман в новую позицию
                fogInstances[i].transform.position = GetRandomFogPosition();

                // Обновляем направление движения
                fogDirections[i] = (windDirection + Random.insideUnitSphere * 0.3f).normalized;
            }
        }
    }
}