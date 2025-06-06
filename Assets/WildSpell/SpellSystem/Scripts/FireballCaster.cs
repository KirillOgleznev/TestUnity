using UnityEngine;

public class FireballCaster : MonoBehaviour
{
    [Header("Fireball Settings")]
    [SerializeField] private GameObject fireballPrefab; // ������ �������� �� HOVL ������
    [SerializeField] private Transform castPoint; // ����� ������ ������ (������ ����/�����)
    [SerializeField] private float fireballSpeed = 20f;
    [SerializeField] private float fireballLifetime = 5f;

    [Header("Casting Settings")]
    [SerializeField] private float castCooldown = 0.5f; // �������� ����� �������
    [SerializeField] private LayerMask targetLayerMask = -1; // ��� ����� ���� �����
    [SerializeField] private float maxCastRange = 100f;
    [SerializeField] private bool useSimpleDirection = false; // ������� ����������� �� ������

    [Header("Audio (Optional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip castSound;

    private Camera playerCamera;
    private float lastCastTime;

    private void Start()
    {
        // �������� ������ ������
        playerCamera = Camera.main;
        if (playerCamera == null)
            playerCamera = FindObjectOfType<Camera>();

        // ���� �� ��������� ����� �����, ���������� ������� �������
        if (castPoint == null)
            castPoint = transform;

        // ��������� ������� �������
        if (fireballPrefab == null)
        {
            Debug.LogError("Fireball Prefab �� ��������! �������� ������ �� HOVL ������.");
        }
    }

    private void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        // ��������� ������� ���
        if (Input.GetMouseButtonDown(0))
        {
            TryCastFireball();
        }
    }

    private void TryCastFireball()
    {
        // ��������� �������
        if (Time.time < lastCastTime + castCooldown)
            return;

        // ��������� ������� �������
        if (fireballPrefab == null)
            return;

        // ���������� ����������� �����
        Vector3 targetDirection = GetCastDirection();

        // ������� �������
        CastFireball(targetDirection);

        // ��������� ����� ���������� �����
        lastCastTime = Time.time;
    }

    private Vector3 GetCastDirection()
    {
        if (useSimpleDirection)
        {
            // ������� ������: ����� ����� �� ����������� ������
            return playerCamera.transform.forward;
        }

        // ������ ������: ������� � ������� ����
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        Vector3 targetPoint;

        if (Physics.Raycast(ray, out hit, maxCastRange, targetLayerMask))
        {
            // ���� ������ � ���������, ����� � ����� ���������
            targetPoint = hit.point;
        }
        else
        {
            // ���� �� ������, ����� �� ����������� ��� �� ������������ ���������
            targetPoint = ray.origin + ray.direction * maxCastRange;
        }

        // �����������: ���������� ������ �����������
        Vector3 direction = (targetPoint - castPoint.position).normalized;

        // �������������� �������� - ���� ����������� ������� ����, ������������
        if (direction.y < -0.5f)
        {
            direction.y = Mathf.Max(direction.y, -0.3f);
            direction = direction.normalized;
        }

        return direction;
    }

    private void CastFireball(Vector3 direction)
    {
        // ������� ������� � ����� �����
        GameObject fireball = Instantiate(fireballPrefab, castPoint.position, Quaternion.LookRotation(direction));

        // ��������� �������� ��������
        Rigidbody rb = fireball.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = direction * fireballSpeed;
        }
        else
        {
            // ���� ��� Rigidbody, ��������� ������� ��������
            FireballMover mover = fireball.AddComponent<FireballMover>();
            mover.Initialize(direction, fireballSpeed);
        }

        // ���������� ������� ����� �������� �����
        Destroy(fireball, fireballLifetime);

        // ������������� ���� �����
        PlayCastSound();

        Debug.Log($"������� ������� � �����������: {direction}");
    }

    private void PlayCastSound()
    {
        if (audioSource != null && castSound != null)
        {
            audioSource.PlayOneShot(castSound);
        }
    }

    // ������������ � Scene view
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

// �������������� ��������� ��� �������� �������� ��� Rigidbody
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