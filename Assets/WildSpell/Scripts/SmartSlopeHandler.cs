using UnityEngine;
using StarterAssets;

public class SmartSlopeHandler : MonoBehaviour
{
    [Header("Slope Settings")]
    [Tooltip("Angle where jumping gets blocked")]
    public float jumpBlockAngle = 20f;

    [Tooltip("Angle where sliding starts")]
    public float slideAngle = 40f;

    [Tooltip("Sliding speed")]
    public float slideSpeed = 6f;

    [Tooltip("Force to stick to ground")]
    public float groundStickForce = 15f;

    [Header("Detection Settings")]
    public LayerMask groundLayers = 1;
    public float raycastDistance = 2f;
    public float edgeDetectionRadius = 0.3f;

    [Header("Debug")]
    public bool showDebug = true;

    // Components
    private CharacterController _controller;
    private StarterAssetsInputs _input;

    // State
    private float _worstSlopeAngle = 0f;  // Самый крутой угол из всех проверок
    private Vector3 _averageNormal = Vector3.up;
    private bool _isSliding = false;
    private bool _jumpBlocked = false;
    private bool _wasOnSlope = false;
    private float _slideInertiaTimer = 0f;

    // Расширенные точки проверки (включая ребра)
    private Vector3[] _checkPoints;

    void Start()
    {
        _controller = GetComponent<CharacterController>();
        _input = GetComponent<StarterAssetsInputs>();

        // Создаем сетку точек проверки вокруг персонажа
        _checkPoints = new Vector3[]
        {
            Vector3.zero,                                    // Центр
            Vector3.forward * edgeDetectionRadius,           // Перед
            Vector3.back * edgeDetectionRadius,              // Зад  
            Vector3.left * edgeDetectionRadius,              // Лево
            Vector3.right * edgeDetectionRadius,             // Право
            new Vector3(edgeDetectionRadius, 0, edgeDetectionRadius),      // Перед-право
            new Vector3(-edgeDetectionRadius, 0, edgeDetectionRadius),     // Перед-лево
            new Vector3(edgeDetectionRadius, 0, -edgeDetectionRadius),     // Зад-право
            new Vector3(-edgeDetectionRadius, 0, -edgeDetectionRadius)     // Зад-лево
        };

        if (_controller == null)
            Debug.LogError("SmartSlopeHandler: CharacterController not found!");
        if (_input == null)
            Debug.LogError("SmartSlopeHandler: StarterAssetsInputs not found!");
    }

    void Update()
    {
        AnalyzeGround();
        HandleJumpBlocking();
        HandleSliding();

        // Таймер инерции соскальзывания
        if (_slideInertiaTimer > 0)
            _slideInertiaTimer -= Time.deltaTime;
    }

    void LateUpdate()
    {
        // Финальная блокировка прыжков
        if (_jumpBlocked && _input != null && _input.jump)
        {
            _input.jump = false;
        }
    }

    private void AnalyzeGround()
    {
        _worstSlopeAngle = 0f;
        Vector3 normalSum = Vector3.zero;
        int validHits = 0;

        // Проверяем все точки
        foreach (Vector3 offset in _checkPoints)
        {
            Vector3 rayStart = transform.position + Vector3.up * 0.2f + offset;
            RaycastHit hit;

            if (Physics.Raycast(rayStart, Vector3.down, out hit, raycastDistance, groundLayers))
            {
                float angle = Vector3.Angle(Vector3.up, hit.normal);

                // Запоминаем самый крутой угол
                if (angle > _worstSlopeAngle)
                {
                    _worstSlopeAngle = angle;
                }

                normalSum += hit.normal;
                validHits++;

                if (showDebug)
                {
                    Color rayColor = angle > slideAngle ? Color.red :
                                    angle > jumpBlockAngle ? Color.yellow : Color.green;
                    Debug.DrawRay(rayStart, Vector3.down * hit.distance, rayColor, 0.1f);
                }
            }
            else if (showDebug)
            {
                Debug.DrawRay(rayStart, Vector3.down * raycastDistance, Color.gray, 0.1f);
            }
        }

        // Вычисляем среднюю нормаль
        if (validHits > 0)
        {
            _averageNormal = (normalSum / validHits).normalized;
        }
        else
        {
            _averageNormal = Vector3.up;
        }

        // Обновляем состояния
        bool wasOnSlope = _wasOnSlope;
        _wasOnSlope = _worstSlopeAngle > slideAngle;

        // Блокируем прыжки если ЛЮБАЯ точка показывает крутой склон
        _jumpBlocked = _worstSlopeAngle > jumpBlockAngle;

        // Соскальзывание: активируем если на крутом склоне ИЛИ есть инерция
        bool isGrounded = _controller != null && _controller.isGrounded;
        _isSliding = (_worstSlopeAngle > slideAngle && isGrounded) ||
                     (_slideInertiaTimer > 0 && !isGrounded);

        // Запускаем инерцию если покидаем склон
        if (wasOnSlope && !isGrounded && _slideInertiaTimer <= 0)
        {
            _slideInertiaTimer = 1f; // 1 секунда инерции
        }
    }

    private void HandleJumpBlocking()
    {
        if (_input == null) return;

        if (_jumpBlocked && _input.jump)
        {
            _input.jump = false;

            if (showDebug)
            {
                Debug.DrawRay(transform.position + Vector3.up * 0.5f, Vector3.up * 0.5f, Color.red, 0.2f);
            }
        }
    }

    private void HandleSliding()
    {
        if (!_isSliding || _controller == null) return;

        Vector3 slideMovement = Vector3.zero;

        if (_controller.isGrounded)
        {
            // Обычное соскальзывание по склону
            Vector3 slideDirection = Vector3.ProjectOnPlane(Vector3.down, _averageNormal).normalized;
            slideMovement = slideDirection * slideSpeed * Time.deltaTime;

            // Усиленное прижимание к земле
            RaycastHit groundHit;
            Vector3 rayStart = transform.position + Vector3.up * 0.1f;

            if (Physics.Raycast(rayStart, Vector3.down, out groundHit, raycastDistance + 1f, groundLayers))
            {
                float distanceToGround = groundHit.distance - 0.1f;
                float skinWidth = _controller.skinWidth;

                if (distanceToGround > skinWidth + 0.02f)
                {
                    float stickForce = groundStickForce * (1f + _worstSlopeAngle / 45f);
                    float pullDown = Mathf.Min(distanceToGround - skinWidth, stickForce * Time.deltaTime);
                    slideMovement += Vector3.down * pullDown;
                }
            }

            if (showDebug)
            {
                Debug.DrawRay(transform.position, slideDirection * 3f, Color.magenta, 0.1f);
            }
        }
        else if (_slideInertiaTimer > 0)
        {
            // Инерционное падение после покидания склона
            Vector3 downwardForce = Vector3.down * slideSpeed * 1.5f * Time.deltaTime;
            slideMovement = downwardForce;

            if (showDebug)
            {
                Debug.DrawRay(transform.position, downwardForce * 5f, Color.cyan, 0.1f);
            }
        }

        if (slideMovement.magnitude > 0.001f)
        {
            _controller.Move(slideMovement);
        }
    }

    // Публичные методы
    public bool IsSliding() => _isSliding;
    public float GetWorstSlopeAngle() => _worstSlopeAngle;
    public bool CanJump() => !_jumpBlocked;
    public bool IsJumpBlocked() => _jumpBlocked;

    void OnDrawGizmos()
    {
        if (!showDebug || _checkPoints == null) return;

        // Цветная сфера показывает состояние
        Gizmos.color = _isSliding ? Color.red : _jumpBlocked ? Color.yellow : Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.4f);

        // Показываем все точки проверки
        foreach (Vector3 offset in _checkPoints)
        {
            Vector3 point = transform.position + Vector3.up * 0.2f + offset;

            if (offset == Vector3.zero)
            {
                // Центральная точка - больше
                Gizmos.color = Color.white;
                Gizmos.DrawWireSphere(point, 0.06f);
            }
            else
            {
                // Боковые точки - меньше
                Gizmos.color = new Color(1f, 1f, 1f, 0.5f);
                Gizmos.DrawWireSphere(point, 0.03f);
            }
        }

        // Показываем радиус детекции ребер
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, edgeDetectionRadius);
    }

    void OnGUI()
    {
        if (!showDebug) return;

        GUILayout.BeginArea(new Rect(10, 10, 350, 200));
        GUILayout.BeginVertical("box");

        GUILayout.Label("=== SMART SLOPE DEBUG ===");
        GUILayout.Label($"Worst Slope Angle: {_worstSlopeAngle:F1}°");
        GUILayout.Label($"Jump Block Angle: {jumpBlockAngle:F1}°");
        GUILayout.Label($"Slide Angle: {slideAngle:F1}°");
        GUILayout.Label($"Jump Blocked: {(_jumpBlocked ? "YES" : "NO")}");
        GUILayout.Label($"Is Sliding: {(_isSliding ? "YES" : "NO")}");
        GUILayout.Label($"Is Grounded: {(_controller != null ? _controller.isGrounded : false)}");
        GUILayout.Label($"Was On Slope: {(_wasOnSlope ? "YES" : "NO")}");
        GUILayout.Label($"Slide Inertia: {_slideInertiaTimer:F1}s");

        // Цветовой индикатор
        GUI.color = _isSliding ? Color.red : _jumpBlocked ? Color.yellow : Color.green;
        string status = _isSliding ? "SLIDING" : _jumpBlocked ? "NO JUMP" : "NORMAL";
        GUILayout.Label($"● {status}");
        GUI.color = Color.white;

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}