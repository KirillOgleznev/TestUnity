using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("References")]
    public Transform target; // ��������

    [Header("Camera Settings")]
    public float distance = 5f;
    public float sensitivity = 2f;
    public float minY = -80f;
    public float maxY = 80f;
    public bool invertY = false; // �������� Y ���

    [Header("Camera Position")]
    public Vector3 offset = Vector3.zero; // �������� �� ���������
    public float height = 1.5f; // ������ ��� ����������
    public Vector3 shoulderOffset = Vector3.zero; // �������� ����� �����

    [Header("Collision Settings")]
    public LayerMask collisionLayers = -1; // ���� ��� �������� �������� (�� ��������� ���� ���������!)
    public float collisionBuffer = 0.2f; // ����� �� �����
    public float minDistance = 0.8f; // ����������� ���������� �� ���������

    [Header("Distance Smoothing")]
    public float distanceSmoothSpeed = 8f; // �������� ����������� �����������
    public float distanceReturnSpeed = 3f; // �������� �������� � ��������� ����������

    [Header("Crosshair Settings")]
    public float crosshairDistance = 100f; // ���������� ������� �� ������

    [Header("Character Visibility")]
    public float fadeCompleteDistance = 2.5f; // ���������� ������� ������������
    public float maxViewAngle = 45f; // ������������ ���� ������ ���������
    public bool fadeCharacter = true; // ��������/��������� ������������ ���������

    [Header("Debug")]
    public bool showDebug = false; // �������� ���������� ����������

    private float rotationX = 0f;
    private float rotationY = 0f;
    private Vector3 crosshairWorldPosition;
    private float currentSmoothDistance; // ������� ���������� ����������
    private Renderer[] characterRenderers; // ��������� ���������
    private bool characterVisible = true;

    // ��������� �������� ��� ������� �� ������ ��������
    public Vector3 CrosshairWorldPosition => crosshairWorldPosition;
    public Vector3 CameraForward => transform.forward;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        currentSmoothDistance = distance;

        // ��������� ����
        Vector3 angles = transform.eulerAngles;
        rotationX = angles.y;
        rotationY = angles.x;

        // ������� ��� ��������� ���������
        if (target != null && fadeCharacter)
        {
            characterRenderers = target.GetComponentsInChildren<Renderer>();
            if (characterRenderers.Length > 0)
            {
                Debug.Log($"������� {characterRenderers.Length} ���������� ��� ������� ���������");
            }
            else
            {
                Debug.LogWarning("��������� ��������� �� �������!");
            }
        }

        // �����: ��������� ��� ���� ��������� �� ������� � collisionLayers!
        if (target != null && target.gameObject.layer != 0)
        {
            int targetLayerMask = 1 << target.gameObject.layer;
            if ((collisionLayers.value & targetLayerMask) != 0)
            {
                Debug.LogWarning("�������� ��������� � ���� �������� ������! ��������� ���� ��������� �� Collision Layers.");
            }
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // ���� ����
        float mouseX = Input.GetAxis("Mouse X") * sensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity;

        if (invertY) mouseY = -mouseY;

        rotationX += mouseX;
        rotationY -= mouseY;
        rotationY = Mathf.Clamp(rotationY, minY, maxY);

        // ������� ���� � �������
        Vector3 targetPosition = target.position + offset + Vector3.up * height;

        // ������� ������ (�� �������� - ����� ��� ������������)
        Quaternion rotation = Quaternion.Euler(rotationY, rotationX, 0);

        // ����������� �� ��������� � ������
        Vector3 direction = rotation * Vector3.back;
        Vector3 shoulderPos = rotation * shoulderOffset;
        Vector3 startPos = targetPosition + shoulderPos;

        // ������� � ��Ĩ���� �������: Linecast �� ��������� � �������� ������� ������
        Vector3 desiredCameraPosition = startPos + direction * distance;
        Vector3 finalCameraPosition = GetOccludedCameraPosition(startPos, desiredCameraPosition);

        // ��������� �������� ���������� �� ������ ��������� �������
        float desiredDistance = Vector3.Distance(startPos, finalCameraPosition);
        desiredDistance = Mathf.Max(desiredDistance, minDistance);

        // ����������� ����������
        float smoothSpeed = desiredDistance < currentSmoothDistance ? distanceSmoothSpeed : distanceReturnSpeed;
        currentSmoothDistance = Mathf.Lerp(currentSmoothDistance, desiredDistance, smoothSpeed * Time.deltaTime);

        // ������������� ��������� ������� ������ � ������������
        Vector3 smoothedPosition = startPos + direction * currentSmoothDistance;
        transform.position = smoothedPosition;
        transform.rotation = rotation;

        // ��������� ������� �������
        crosshairWorldPosition = transform.position + transform.forward * crosshairDistance;

        // ���������� ���������� ���������
        if (fadeCharacter && characterRenderers != null && characterRenderers.Length > 0)
        {
            UpdateCharacterVisibility();
        }
    }

    // �������� �����: �������� ����������� ����� ���������� � ������� (�� �������� ��������)
    private Vector3 GetOccludedCameraPosition(Vector3 startPosition, Vector3 desiredPosition)
    {
        RaycastHit hit;
        Vector3 direction = (desiredPosition - startPosition).normalized;
        float distance = Vector3.Distance(startPosition, desiredPosition);

        // Linecast �� ��������� � �������� ������� ������
        if (Physics.Linecast(startPosition, desiredPosition, out hit, collisionLayers))
        {
            // ���� ���� ����������� - ������ ������ ����� ���
            Vector3 safePosition = hit.point - direction * collisionBuffer;

            if (showDebug)
                Debug.Log($"Linecast hit: {hit.collider.name}, distance: {hit.distance:F2}");

            return safePosition;
        }

        // ���� ����������� ��� - ���������� �������� �������
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
                Debug.Log($"�������� {(characterVisible ? "�������" : "�����")} - ����������: {cameraDistance:F2}, ����: {angleToCharacter:F1}�");
        }
    }

    // ������ ��� ������
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
        // ���������� �������
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

            Debug.Log($"=== LINECAST ���� ===");
            Debug.Log($"����� �� ��������� � ������ �������������: {lineBlocked}");
            if (lineBlocked)
            {
                Debug.Log($"  �����������: {hit.collider.name} �� ���������� {hit.distance:F2}");
                Debug.Log($"  ������� �����������: {hit.point}");
            }
            Debug.Log($"������� ���������� ����������: {currentSmoothDistance:F2}");
            Debug.Log($"�������� ����������: {distance}");
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
            Debug.Log($"�������� ������������� {(characterVisible ? "�������" : "�����")}");
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

        // ���������� �������� ������� ������
        Vector3 desiredPos = startPos + direction * distance;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(desiredPos, 0.2f);

        // ���������� Linecast �� ��������� � �������� �������
        RaycastHit hit;
        bool lineBlocked = Physics.Linecast(startPos, desiredPos, out hit, collisionLayers);

        Gizmos.color = lineBlocked ? Color.red : Color.green;

        if (lineBlocked)
        {
            // ����� �� �����������
            Gizmos.DrawLine(startPos, hit.point);
            // ����� �����������
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(hit.point, 0.15f);
            // ���������� ������� ������
            Vector3 safePos = hit.point - direction * collisionBuffer;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(safePos, 0.1f);
        }
        else
        {
            // ������ ����� ���� ����������� ���
            Gizmos.DrawLine(startPos, desiredPos);
        }

        // ���������� ������� ������� ������
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, 0.1f);

        // ����� ������������
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, transform.forward * 5f);
    }
}