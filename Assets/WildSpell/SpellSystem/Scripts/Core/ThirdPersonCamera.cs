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
    public float collisionRadius = 0.2f; // ������ ����� ��� �������� ��������
    public float minDistance = 0.8f; // ����������� ���������� �� ���������
    public float collisionBuffer = 0.2f; // ����� �� �����
    public float raycastStartOffset = 0.5f; // ������ �� ��������� ��� ������ ��������

    [Header("Distance Smoothing")]
    public float distanceSmoothSpeed = 8f; // �������� ����������� ����������� (��� ������ - ��� �������)
    public float distanceReturnSpeed = 3f; // �������� �������� � ��������� ���������� (��� ������ - ��� �������)

    [Header("Crosshair Settings")]
    public float crosshairDistance = 100f; // ���������� ������� �� ������

    [Header("Character Visibility")]
    public float fadeCompleteDistance = 2.5f; // ���������� ������� ������������
    public float maxViewAngle = 45f; // ������������ ���� ������ ��������� (0 = ������� ����� �� ���������)
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
        currentSmoothDistance = distance; // �������������� ���������� ����������

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

        // �������� Y
        if (invertY)
            mouseY = -mouseY;

        rotationX += mouseX;
        rotationY -= mouseY;

        // �������� �������
        rotationY = Mathf.Clamp(rotationY, minY, maxY);

        // ������� ���� � �������
        Vector3 targetPosition = target.position + offset + Vector3.up * height;

        // ������� ������
        Quaternion rotation = Quaternion.Euler(rotationY, rotationX, 0);

        // ����������� ������
        Vector3 direction = rotation * Vector3.back;
        Vector3 shoulderPos = rotation * shoulderOffset;
        Vector3 startPos = targetPosition + shoulderPos;

        // �������� �������� - ���������� �������� ����������
        float desiredDistance = distance;

        // ������ ��������: ���� �� ����� ����� �� ����������?
        RaycastHit directHit;
        if (Physics.Raycast(startPos, direction, out directHit, distance + 0.5f, collisionLayers))
        {
            // ���� ����� ������ � ���������, ���������� � ����������
            desiredDistance = Mathf.Max(directHit.distance - collisionBuffer, minDistance);

            if (showDebug)
                Debug.Log($"������ �������� � {directHit.collider.name}, �������� ����������: {desiredDistance}");
        }
        else
        {
            // ������ ��������: ������� �������� � �������� (���� ��� ����� ����� �� ����������)
            Vector3 raycastStart = startPos + direction * raycastStartOffset;
            float raycastDistance = distance - raycastStartOffset;

            RaycastHit hit;
            if (raycastDistance > 0 && Physics.SphereCast(raycastStart, collisionRadius, direction, out hit, raycastDistance, collisionLayers))
            {
                // ��������� ��������� ���������� � ������ �������
                float hitDistance = raycastStartOffset + hit.distance - collisionBuffer;
                desiredDistance = Mathf.Max(hitDistance, minDistance);

                if (showDebug)
                    Debug.Log($"SphereCast �������� � {hit.collider.name}, �������� ����������: {desiredDistance}");
            }
            else if (showDebug)
            {
                Debug.Log($"�������� ���, �������� ����������: {desiredDistance}");
            }
        }

        // ����������� ����������
        float smoothSpeed;
        if (desiredDistance < currentSmoothDistance)
        {
            // ������������ � ����� - �������
            smoothSpeed = distanceSmoothSpeed;
        }
        else
        {
            // ���������� �� ����� - ���������, �������
            smoothSpeed = distanceReturnSpeed;
        }

        currentSmoothDistance = Mathf.Lerp(currentSmoothDistance, desiredDistance, smoothSpeed * Time.deltaTime);

        // ������� ������ � �������������� ����������� ����������
        Vector3 finalPosition = startPos + direction * currentSmoothDistance;
        transform.position = finalPosition;
        transform.rotation = rotation; // ������ ������� �� ����������� �������� (��� � RoR2)

        // ��������� ������� ������� (�������� ��� ���������)
        crosshairWorldPosition = transform.position + transform.forward * crosshairDistance;

        // ���������� ���������� ���������
        if (fadeCharacter && characterRenderers != null && characterRenderers.Length > 0)
        {
            UpdateCharacterVisibility();
        }
    }

    private void UpdateCharacterVisibility()
    {
        // ������� ���������� ������
        float cameraDistance = currentSmoothDistance;

        // ��������� ���� ������ ���������
        Vector3 directionToCharacter = (target.position - transform.position).normalized;
        Vector3 cameraForward = transform.forward;
        float angleToCharacter = Vector3.Angle(cameraForward, directionToCharacter);

        // ����������: �������� ������ ���� ����� ����:
        // 1. ������ ������� ������ ��� 2. ���� ������ ������� �������
        bool shouldBeHidden = cameraDistance < fadeCompleteDistance || angleToCharacter > maxViewAngle;
        bool shouldBeVisible = !shouldBeHidden;

        if (showDebug)
        {
            Debug.Log($"����������: {cameraDistance:F2} (<{fadeCompleteDistance}?), ����: {angleToCharacter:F1}� (>{maxViewAngle}?), �����: {shouldBeHidden}");
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

            Debug.Log($"*** �������� {(characterVisible ? "�������" : "�����")} *** ����������: {cameraDistance:F2}, ����: {angleToCharacter:F1}�, ����������: {processedCount}");
        }
    }

    // ����� ��� ��������� ����������� �������� (��� ������)
    public Vector3 GetShootDirection()
    {
        return (crosshairWorldPosition - transform.position).normalized;
    }

    // ����� ��� ��������� ����� ������������ �� ������������ ����������
    public Vector3 GetAimPoint(float distance)
    {
        return transform.position + GetShootDirection() * distance;
    }

    // ����� ��� ��������������� ������������ ������� ��������� (��� �������)
    void Update()
    {
        // ������� ������� H ��� ��������������� �������/������ ��������� (��� ������������)
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
                    Debug.Log($"�������� {renderer.name}: enabled = {renderer.enabled}");
                }
            }
            Debug.Log($"[���� H] �������� ������������� {(characterVisible ? "�������" : "�����")}, ����������: {hiddenCount} ����������");
        }

        // ���� � �������� G - �������� ���������� � ����������
        if (Input.GetKeyDown(KeyCode.G) && characterRenderers != null)
        {
            Debug.Log($"=== ���� ���������� ===");
            Debug.Log($"������� ����������: {characterRenderers.Length}");
            for (int i = 0; i < characterRenderers.Length; i++)
            {
                if (characterRenderers[i] != null)
                {
                    Renderer r = characterRenderers[i];
                    Debug.Log($"  {i}: {r.name} ({r.GetType().Name}) - enabled: {r.enabled}, active: {r.gameObject.activeInHierarchy}");
                }
                else
                {
                    Debug.Log($"  {i}: NULL ��������!");
                }
            }
        }
    }
}