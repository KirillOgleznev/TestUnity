using UnityEngine;
using StarterAssets;

public class FixedSlopeHandler : MonoBehaviour
{
    [Header("Slope Settings")]
    [Tooltip("Angle where jumping starts getting blocked")]
    public float jumpBlockAngle = 20f;

    [Tooltip("Angle where you start sliding")]
    public float slideAngle = 40f;

    [Tooltip("How fast you slide down")]
    public float slideSpeed = 6f;

    [Tooltip("Force to stick to ground while sliding")]
    public float groundStickForce = 10f;

    [Header("Ground Detection")]
    public LayerMask groundLayers = 1;
    public float raycastDistance = 2f;

    [Header("Debug")]
    public bool showDebug = true;

    // Components
    private CharacterController _controller;
    private StarterAssetsInputs _input;

    // Slope data
    private float _currentAngle = 0f;
    private Vector3 _currentNormal = Vector3.up;
    private bool _isSliding = false;
    private bool _jumpBlocked = false;

    // Multiple raycast points for better detection
    private Vector3[] _raycastOffsets = {
        Vector3.zero,           // Center
        Vector3.forward * 0.2f, // Front  
        Vector3.back * 0.2f,    // Back
        Vector3.left * 0.2f,    // Left
        Vector3.right * 0.2f    // Right
    };

    void Start()
    {
        _controller = GetComponent<CharacterController>();
        _input = GetComponent<StarterAssetsInputs>();

        if (_controller == null)
            Debug.LogError("FixedSlopeHandler: CharacterController not found!");
        if (_input == null)
            Debug.LogError("FixedSlopeHandler: StarterAssetsInputs not found!");
    }

    void Update()
    {
        CheckGround();
        BlockJumping();
        HandleSliding();
    }

    void LateUpdate()
    {
        // Дополнительная блокировка прыжков в LateUpdate
        // чтобы перехватить любые изменения input после других скриптов
        if (_jumpBlocked && _input != null && _input.jump)
        {
            _input.jump = false;
        }
    }

    private void CheckGround()
    {
        // ОСНОВНАЯ проверка - только центральный raycast для определения угла ПОД НОГАМИ
        Vector3 centerRayStart = transform.position + Vector3.up * 0.1f;
        RaycastHit centerHit;

        if (Physics.Raycast(centerRayStart, Vector3.down, out centerHit, raycastDistance, groundLayers))
        {
            _currentAngle = Vector3.Angle(Vector3.up, centerHit.normal);
            _currentNormal = centerHit.normal;

            if (showDebug)
            {
                Color rayColor = _currentAngle > slideAngle ? Color.red :
                                _currentAngle > jumpBlockAngle ? Color.yellow : Color.green;
                Debug.DrawRay(centerRayStart, Vector3.down * centerHit.distance, rayColor, 0.1f, false);
            }
        }
        else
        {
            _currentAngle = 0f;
            _currentNormal = Vector3.up;

            if (showDebug)
            {
                Debug.DrawRay(centerRayStart, Vector3.down * raycastDistance, Color.gray);
            }
        }

        // ДОПОЛНИТЕЛЬНЫЕ raycast только для улучшения соскальзывания, НЕ для блокировки прыжков
        if (showDebug)
        {
            for (int i = 1; i < _raycastOffsets.Length; i++) // Пропускаем центральный
            {
                Vector3 rayStart = transform.position + Vector3.up * 0.1f + _raycastOffsets[i];
                RaycastHit hit;

                if (Physics.Raycast(rayStart, Vector3.down, out hit, raycastDistance, groundLayers))
                {
                    float angle = Vector3.Angle(Vector3.up, hit.normal);
                    Color rayColor = angle > slideAngle ? Color.magenta : Color.blue;
                    Debug.DrawRay(rayStart, Vector3.down * hit.distance, rayColor, 0.1f, false);
                }
                else
                {
                    Debug.DrawRay(rayStart, Vector3.down * raycastDistance, Color.gray);
                }
            }
        }

        // Обновляем состояния ТОЛЬКО на основе центрального raycast
        _jumpBlocked = _currentAngle > jumpBlockAngle;
        _isSliding = _currentAngle > slideAngle && _controller != null && _controller.isGrounded;
    }

    private void BlockJumping()
    {
        if (_input == null) return;

        // Агрессивная блокировка прыжков
        if (_jumpBlocked && _input.jump)
        {
            _input.jump = false;

            if (showDebug)
            {
                Debug.DrawRay(transform.position + Vector3.up, Vector3.up, Color.red, 0.1f);
            }
        }
    }

    private void HandleSliding()
    {
        if (!_isSliding || _controller == null) return;

        // Направление соскальзывания
        Vector3 slideDirection = Vector3.ProjectOnPlane(Vector3.down, _currentNormal).normalized;

        // Основное движение соскальзывания
        Vector3 slideMovement = slideDirection * slideSpeed * Time.deltaTime;

        // Усиленное притягивание к земле для крутых склонов
        RaycastHit groundHit;
        Vector3 rayStart = transform.position + Vector3.up * 0.1f;

        if (Physics.Raycast(rayStart, Vector3.down, out groundHit, raycastDistance + 1f, groundLayers))
        {
            float distanceToGround = groundHit.distance - 0.1f; // Убираем offset raystart
            float skinWidth = _controller.skinWidth;

            // Если есть зазор больше skin width - притягиваем к земле
            if (distanceToGround > skinWidth + 0.02f)
            {
                // Увеличиваем силу притягивания для очень крутых склонов
                float angleFactor = Mathf.Clamp01(_currentAngle / 90f); // 0 для 0°, 1 для 90°
                float enhancedStickForce = groundStickForce * (1f + angleFactor * 2f); // До 3x силы для вертикальных

                float pullDownDistance = Mathf.Min(
                    distanceToGround - skinWidth,
                    enhancedStickForce * Time.deltaTime
                );
                slideMovement += Vector3.down * pullDownDistance;
            }

            if (showDebug)
            {
                Debug.DrawRay(groundHit.point, Vector3.up * distanceToGround, Color.cyan);
            }
        }

        // Применяем движение
        _controller.Move(slideMovement);

        if (showDebug)
        {
            Debug.DrawRay(transform.position, slideDirection * 3f, Color.magenta);
            Debug.DrawRay(transform.position + Vector3.up, slideMovement * 30f, Color.red);
        }
    }

    // Публичные методы
    public bool IsSliding() => _isSliding;
    public float GetSlopeAngle() => _currentAngle;
    public bool CanJump() => !_jumpBlocked;
    public bool IsJumpBlocked() => _jumpBlocked;

    void OnDrawGizmos()
    {
        if (!showDebug) return;

        // Цветная сфера показывает состояние
        Gizmos.color = _isSliding ? Color.red : _jumpBlocked ? Color.yellow : Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.4f);

        // Показываем только центральную точку raycast (главную)
        Gizmos.color = Color.white;
        Vector3 centerPoint = transform.position + Vector3.up * 0.1f;
        Gizmos.DrawWireSphere(centerPoint, 0.08f);

        // Дополнительные точки показываем меньшими и полупрозрачными
        Gizmos.color = new Color(1f, 1f, 1f, 0.3f);
        for (int i = 1; i < _raycastOffsets.Length; i++)
        {
            Vector3 point = transform.position + Vector3.up * 0.1f + _raycastOffsets[i];
            Gizmos.DrawWireSphere(point, 0.03f);
        }
    }

    void OnGUI()
    {
        if (!showDebug) return;

        GUILayout.BeginArea(new Rect(10, 10, 320, 180));
        GUILayout.BeginVertical("box");

        GUILayout.Label("=== FIXED SLOPE DEBUG ===");
        GUILayout.Label($"CENTER Slope Angle: {_currentAngle:F1}° (main check)");
        GUILayout.Label($"Jump Block Angle: {jumpBlockAngle:F1}°");
        GUILayout.Label($"Slide Angle: {slideAngle:F1}°");
        GUILayout.Label($"Jump Blocked: {(_jumpBlocked ? "YES" : "NO")}");
        GUILayout.Label($"Is Sliding: {(_isSliding ? "YES" : "NO")}");
        GUILayout.Label($"Is Grounded: {(_controller != null ? _controller.isGrounded : false)}");

        if (_isSliding)
        {
            float angleFactor = Mathf.Clamp01(_currentAngle / 90f);
            float enhancedForce = groundStickForce * (1f + angleFactor * 2f);
            GUILayout.Label($"Enhanced Stick Force: {enhancedForce:F1}");
        }

        // Цветовой индикатор
        GUI.color = _isSliding ? Color.red : _jumpBlocked ? Color.yellow : Color.green;
        string status = _isSliding ? "SLIDING" : _jumpBlocked ? "NO JUMP" : "NORMAL";
        GUILayout.Label($"● {status}");
        GUI.color = Color.white;

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}