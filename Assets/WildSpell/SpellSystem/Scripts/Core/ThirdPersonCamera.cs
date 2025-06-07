using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("References")]
    public Transform target; // Персонаж

    [Header("Camera Settings")]
    public float distance = 5f;
    public float sensitivity = 2f;
    public float minY = -80f;
    public float maxY = 80f;
    public bool invertY = false; // Инверсия Y оси

    [Header("Camera Position")]
    public Vector3 offset = Vector3.zero; // Смещение от персонажа
    public float height = 1.5f; // Высота над персонажем
    public Vector3 shoulderOffset = Vector3.zero; // Смещение через плечо

    private float rotationX = 0f;
    private float rotationY = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        // Начальные углы
        Vector3 angles = transform.eulerAngles;
        rotationX = angles.y;
        rotationY = angles.x;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Ввод мыши
        float mouseX = Input.GetAxis("Mouse X") * sensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity;

        // Инверсия Y
        if (invertY)
            mouseY = -mouseY;

        rotationX += mouseX;
        rotationY -= mouseY;

        // Ограничь поворот
        rotationY = Mathf.Clamp(rotationY, minY, maxY);

        // Позиция цели с высотой
        Vector3 targetPosition = target.position + offset + Vector3.up * height;

        // Поворот камеры
        Quaternion rotation = Quaternion.Euler(rotationY, rotationX, 0);

        // Позиция камеры БЕЗ сглаживания
        Vector3 baseOffset = rotation * Vector3.back * distance;
        Vector3 shoulder = rotation * shoulderOffset;

        transform.position = targetPosition + baseOffset + shoulder;
        transform.LookAt(targetPosition);
    }
}