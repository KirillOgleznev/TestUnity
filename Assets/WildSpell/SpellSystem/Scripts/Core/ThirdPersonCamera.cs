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

    [Header("Crosshair Settings")]
    public float crosshairDistance = 100f; // ���������� ������� �� ������

    [Header("Debug")]
    public bool showDebug = false; // �������� ���������� ����������

    private float rotationX = 0f;
    private float rotationY = 0f;
    private Vector3 crosshairWorldPosition;

    // ��������� �������� ��� ������� �� ������ ��������
    public Vector3 CrosshairWorldPosition => crosshairWorldPosition;
    public Vector3 CameraForward => transform.forward;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        // ��������� ����
        Vector3 angles = transform.eulerAngles;
        rotationX = angles.y;
        rotationY = angles.x;

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

        // �������� �������� - ���������� ��� �����������
        float currentDistance = distance;

        // ������ ��������: ���� �� ����� ����� �� ����������?
        RaycastHit directHit;
        if (Physics.Raycast(startPos, direction, out directHit, distance + 0.5f, collisionLayers))
        {
            // ���� ����� ������ � ���������, ���������� � ����������
            currentDistance = Mathf.Max(directHit.distance - collisionBuffer, minDistance);

            if (showDebug)
                Debug.Log($"������ �������� � {directHit.collider.name}, ����������: {currentDistance}");
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
                currentDistance = Mathf.Max(hitDistance, minDistance);

                if (showDebug)
                    Debug.Log($"SphereCast �������� � {hit.collider.name}, ����������: {currentDistance}");
            }
            else if (showDebug)
            {
                Debug.Log($"�������� ���, ����������: {currentDistance}");
            }
        }

        // ������� ������ - ��� �����������, ����������
        Vector3 finalPosition = startPos + direction * currentDistance;
        transform.position = finalPosition;
        transform.rotation = rotation; // ������ ������� �� ����������� �������� (��� � RoR2)

        // ��������� ������� �������
        crosshairWorldPosition = transform.position + transform.forward * crosshairDistance;
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

    // ����� ������������
    void OnDrawGizmosSelected()
    {
        if (target == null) return;

        Vector3 targetPosition = target.position + offset + Vector3.up * height;
        Quaternion rotation = Quaternion.Euler(rotationY, rotationX, 0);
        Vector3 direction = rotation * Vector3.back;
        Vector3 shoulderPos = rotation * shoulderOffset;
        Vector3 startPos = targetPosition + shoulderPos;

        // ������ ��������: ������ raycast �� ��������� (������� �����)
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

        // ������ ��������: SphereCast � �������� (����� �����)
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

        // ������� ������� ������ (������ �����)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.15f);

        // ����������� ������� (���������� �����)
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(transform.position, crosshairWorldPosition);
        Gizmos.DrawWireSphere(crosshairWorldPosition, 0.3f);
    }

    void OnGUI()
    {
        // ������� ������ � ������ ������
        float crosshairSize = 20f;
        float centerX = Screen.width * 0.5f;
        float centerY = Screen.height * 0.5f;

        GUI.color = Color.white;
        GUI.DrawTexture(new Rect(centerX - 1, centerY - crosshairSize * 0.5f, 2, crosshairSize), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(centerX - crosshairSize * 0.5f, centerY - 1, crosshairSize, 2), Texture2D.whiteTexture);
    }
}