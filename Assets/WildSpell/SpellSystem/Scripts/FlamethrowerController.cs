using UnityEngine;

public class FlamethrowerController : MonoBehaviour
{
    [Header("Flamethrower Settings")]
    [SerializeField] private GameObject flamePrefab; // ������ ���� �� HOVL ������
    [SerializeField] private Transform spawnPoint; // ����� ��� ���������� �����

    private GameObject currentFlameEffect; // ������� �������� ������ ����
    private bool isFlaming = false;
    private Camera playerCamera;

    private void Start()
    {
        // ���� �� ��������� ����� ������, ���������� ������� �������
        if (spawnPoint == null)
            spawnPoint = transform;

        // �������� ������ ������
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
        // ������������ spawn point � ����������� ������ ������
        if (spawnPoint != null)
        {
            spawnPoint.rotation = transform.rotation;
        }
    }

    private void HandleInput()
    {
        // ��������� ������� ���
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

        // ������� ������ ���� � ����� ������
        currentFlameEffect = Instantiate(flamePrefab, spawnPoint.position, spawnPoint.rotation);

        // ������� �������� ���� ���� Rigidbody
        Rigidbody rb = currentFlameEffect.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        // ������ ������ �������� �������� spawn point
        currentFlameEffect.transform.SetParent(spawnPoint);
    }

    private void StopFlaming()
    {
        isFlaming = false;

        // ���������� ������� ������ ����
        if (currentFlameEffect != null)
        {
            Destroy(currentFlameEffect);
            currentFlameEffect = null;
        }
    }
}