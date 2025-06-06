using UnityEngine;

public class CharacterControllerSliding : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 8f;

    [Header("Slope Sliding Settings")]
    public float slideSpeed = 6f;
    public float slideFriction = 0.3f;
    public bool enableSliding = true;

    [Header("Ground Detection")]
    public float groundCheckDistance = 0.1f;
    public LayerMask groundMask = 1;

    // Приватные переменные
    private CharacterController controller;
    private Vector3 moveDirection;
    private Vector3 velocity;
    private bool isGrounded;
    private Vector3 hitNormal;
    private bool isSliding;
    private float slopeAngle;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        // Устанавливаем slopeLimit в большое значение, чтобы наш код работал правильно
        if (controller.slopeLimit < 80f)
        {
            controller.slopeLimit = 80f;
        }
    }

    void Update()
    {
        HandleInput();
        HandleMovement();
        HandleSliding();
    }

    void HandleInput()
    {
        // Получаем ввод от игрока
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // Вычисляем направление движения
        Vector3 direction = transform.right * horizontal + transform.forward * vertical;
        moveDirection = direction.normalized * moveSpeed;

        // Прыжок
        if (Input.GetButtonDown("Jump") && isGrounded && !isSliding)
        {
            velocity.y = jumpForce;
        }
    }

    void HandleMovement()
    {
        // Применяем гравитацию
        if (!isGrounded)
        {
            velocity.y += Physics.gravity.y * Time.deltaTime;
        }
        else if (velocity.y < 0)
        {
            velocity.y = -2f; // Небольшая отрицательная скорость для удержания на земле
        }

        // Объединяем горизонтальное движение и вертикальную скорость
        Vector3 finalMovement = moveDirection + Vector3.up * velocity.y;

        // Применяем движение
        controller.Move(finalMovement * Time.deltaTime);
    }

    void HandleSliding()
    {
        if (!enableSliding) return;

        // Проверяем, находимся ли мы на склоне, требующем скольжения
        if (hitNormal != Vector3.zero)
        {
            slopeAngle = Vector3.Angle(Vector3.up, hitNormal);

            // Определяем, нужно ли скользить (угол больше безопасного предела)
            isSliding = slopeAngle > 45f && slopeAngle <= 90f && isGrounded;

            if (isSliding)
            {
                // Метод 1: Основанный на трении (более реалистичный)
                Vector3 slideDirection = Vector3.zero;
                slideDirection.x = (1f - hitNormal.y) * hitNormal.x * (slideSpeed - slideFriction);
                slideDirection.z = (1f - hitNormal.y) * hitNormal.z * (slideSpeed - slideFriction);

                // Метод 2: Альтернативный (более быстрое скольжение)
                // slideDirection.x = ((1f - hitNormal.y) * hitNormal.x) * slideSpeed;
                // slideDirection.z = ((1f - hitNormal.y) * hitNormal.z) * slideSpeed;

                // Применяем скольжение
                controller.Move(slideDirection * Time.deltaTime);

                // Предотвращаем прыжки во время скольжения
                if (Input.GetButtonDown("Jump"))
                {
                    Debug.Log("Нельзя прыгать во время скольжения!");
                }
            }
        }
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // Сохраняем нормаль поверхности для расчёта скольжения
        hitNormal = hit.normal;

        // Определяем, находимся ли мы на земле
        // Используем наш собственный расчёт вместо controller.isGrounded
        isGrounded = Vector3.Angle(Vector3.up, hitNormal) <= 45f;

        // Дополнительная проверка для предотвращения скольжения по стенам
        if (hit.moveDirection.y < -0.3f)
        {
            return;
        }
    }

    // Дополнительный метод для более точного определения земли через Raycast
    bool IsGroundedRaycast()
    {
        return Physics.Raycast(transform.position, Vector3.down,
                              controller.height / 2 + groundCheckDistance, groundMask);
    }

    // Метод для получения информации о текущем состоянии (для отладки)
    public void GetSlidingInfo()
    {
        Debug.Log($"Is Grounded: {isGrounded}");
        Debug.Log($"Is Sliding: {isSliding}");
        Debug.Log($"Slope Angle: {slopeAngle:F1}°");
        Debug.Log($"Hit Normal: {hitNormal}");
    }

    // Визуализация для отладки
    void OnDrawGizmosSelected()
    {
        if (hitNormal != Vector3.zero)
        {
            Gizmos.color = isSliding ? Color.red : Color.green;
            Gizmos.DrawRay(transform.position, hitNormal * 2f);

            if (isSliding)
            {
                Gizmos.color = Color.yellow;
                Vector3 slideDir = new Vector3(
                    (1f - hitNormal.y) * hitNormal.x,
                    0,
                    (1f - hitNormal.y) * hitNormal.z
                ).normalized;
                Gizmos.DrawRay(transform.position, slideDir * 3f);
            }
        }
    }
}