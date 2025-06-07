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

    private float rotationX = 0f;
    private float rotationY = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        // ��������� ����
        Vector3 angles = transform.eulerAngles;
        rotationX = angles.y;
        rotationY = angles.x;
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

        // ������� ������ ��� �����������
        Vector3 baseOffset = rotation * Vector3.back * distance;
        Vector3 shoulder = rotation * shoulderOffset;

        transform.position = targetPosition + baseOffset + shoulder;
        transform.LookAt(targetPosition);
    }
}