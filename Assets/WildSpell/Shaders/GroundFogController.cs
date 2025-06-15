using UnityEngine;

public class GroundFogController : MonoBehaviour
{
    [Header("��������� ������")]
    public GameObject fogPrefab; // ������ � �������
    public Transform player; // ������ �� ������
    public float minDistanceFromPlayer = 50f; // ����������� ���������� �� ������
    public float maxDistanceFromPlayer = 200f; // ������������ ����������
    public float fogHeight = 5f; // ������ ������ ��� ������
    public int fogCount = 20; // ���������� ������� ������

    [Header("��������")]
    public float driftSpeed = 2f; // �������� ������ ������
    public Vector3 windDirection = new Vector3(1, 0, 0.5f); // ����������� �����

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
            // ������� ��������� ������
            Vector3 randomPos = GetRandomFogPosition();
            fogInstances[i] = Instantiate(fogPrefab, randomPos, Quaternion.identity);

            // ��������� ����������� ��������
            fogDirections[i] = (windDirection + Random.insideUnitSphere * 0.3f).normalized;
            fogSpeeds[i] = driftSpeed + Random.Range(-0.5f, 0.5f);

            // ��������� ������
            float scale = Random.Range(0.8f, 1.5f);
            fogInstances[i].transform.localScale = Vector3.one * scale;
        }
    }

    Vector3 GetRandomFogPosition()
    {
        Vector3 playerPos = player.position;

        // ���������� ������� � ������ ������ ������
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float distance = Random.Range(minDistanceFromPlayer, maxDistanceFromPlayer);

        Vector3 offset = new Vector3(
            Mathf.Cos(angle) * distance,
            0,
            Mathf.Sin(angle) * distance
        );

        Vector3 fogPos = playerPos + offset;

        // ��������� ����� �� ����������� ����� + ��������� ������
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

            // ������� �����
            fogInstances[i].transform.position += fogDirections[i] * fogSpeeds[i] * Time.deltaTime;

            // ���������, �� ������� �� ������ � ������ ��� ������
            float distanceToPlayer = Vector3.Distance(fogInstances[i].transform.position, playerPos);

            if (distanceToPlayer < minDistanceFromPlayer || distanceToPlayer > maxDistanceFromPlayer)
            {
                // ���������� ����� � ����� �������
                fogInstances[i].transform.position = GetRandomFogPosition();

                // ��������� ����������� ��������
                fogDirections[i] = (windDirection + Random.insideUnitSphere * 0.3f).normalized;
            }
        }
    }
}