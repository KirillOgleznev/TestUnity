using UnityEngine;
using StarterAssets;

public class SimpleSlopeHandler : MonoBehaviour
{
    [Header("Slope Settings")]
    [Tooltip("Maximum angle you can walk on")]
    public float maxWalkableAngle = 35f;

    [Tooltip("Angle where jumping is blocked")]
    public float jumpBlockAngle = 25f;

    [Tooltip("Angle where you start sliding")]
    public float slideAngle = 45f;

    [Tooltip("How fast you slide down")]
    public float slideSpeed = 5f;

    [Tooltip("Block jumping on slopes")]
    public bool blockJumping = true;

    [Header("Ground Detection")]
    public LayerMask groundLayers = 1;
    public float groundCheckDistance = 1.5f;

    [Header("Debug")]
    public bool showDebug = true;

    // Components
    private CharacterController _controller;
    private StarterAssetsInputs _input;

    // Slope data
    private float _currentAngle = 0f;
    private Vector3 _currentNormal = Vector3.up;
    private bool _isSliding = false;

    void Start()
    {
        _controller = GetComponent<CharacterController>();
        _input = GetComponent<StarterAssetsInputs>();

        if (_controller == null)
            Debug.LogError("SimpleSlopeHandler: CharacterController not found!");
        if (_input == null)
            Debug.LogError("SimpleSlopeHandler: StarterAssetsInputs not found!");
    }

    void Update()
    {
        CheckGround();
        HandleJumpBlocking();
        HandleSliding();
    }

    private void CheckGround()
    {
        RaycastHit hit;
        Vector3 rayStart = transform.position + Vector3.up * 0.1f;

        if (Physics.Raycast(rayStart, Vector3.down, out hit, groundCheckDistance, groundLayers))
        {
            _currentNormal = hit.normal;
            _currentAngle = Vector3.Angle(Vector3.up, _currentNormal);

            if (showDebug)
            {
                Color rayColor = _currentAngle > slideAngle ? Color.red :
                                _currentAngle > jumpBlockAngle ? Color.yellow : Color.green;
                Debug.DrawRay(rayStart, Vector3.down * hit.distance, rayColor);
            }
        }
        else
        {
            _currentNormal = Vector3.up;
            _currentAngle = 0f;
        }
    }

    private void HandleJumpBlocking()
    {
        if (!blockJumping || _input == null) return;

        // Блокируем прыжок уже на небольших наклонах
        if (_currentAngle > jumpBlockAngle && _input.jump)
        {
            _input.jump = false;
        }
    }

    private void HandleSliding()
    {
        if (_controller == null) return;

        // Простое условие: соскальзываем если угол больше slideAngle и мы на земле
        _isSliding = _currentAngle > slideAngle && _controller.isGrounded;

        if (_isSliding)
        {
            // Простое направление соскальзывания - вниз по склону
            Vector3 slideDirection = Vector3.ProjectOnPlane(Vector3.down, _currentNormal);
            slideDirection = slideDirection.normalized;

            // Применяем движение
            Vector3 slideMovement = slideDirection * slideSpeed * Time.deltaTime;
            _controller.Move(slideMovement);

            if (showDebug)
            {
                Debug.DrawRay(transform.position, slideDirection * 2f, Color.magenta);
            }
        }
    }

    // Публичные методы для других скриптов
    public bool IsSliding() => _isSliding;
    public float GetSlopeAngle() => _currentAngle;
    public bool CanWalk() => _currentAngle <= maxWalkableAngle;
    public bool CanJump() => _currentAngle <= jumpBlockAngle;

    void OnDrawGizmos()
    {
        if (!showDebug) return;

        // Цветная сфера показывает состояние
        if (_currentAngle > slideAngle)
            Gizmos.color = Color.red;      // Соскальзывание
        else if (_currentAngle > jumpBlockAngle)
            Gizmos.color = Color.yellow;   // Нельзя прыгать, но можно ходить
        else
            Gizmos.color = Color.green;    // Можно всё

        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }

    void OnGUI()
    {
        if (!showDebug) return;

        GUILayout.BeginArea(new Rect(10, 10, 280, 140));
        GUILayout.BeginVertical("box");

        GUILayout.Label("=== SIMPLE SLOPE ===");
        GUILayout.Label($"Angle: {_currentAngle:F1}°");
        GUILayout.Label($"Jump Block: {jumpBlockAngle:F1}° | Slide: {slideAngle:F1}°");
        GUILayout.Label($"Can Walk: {(_currentAngle <= maxWalkableAngle ? "YES" : "NO")}");
        GUILayout.Label($"Can Jump: {(_currentAngle <= jumpBlockAngle ? "YES" : "NO")}");
        GUILayout.Label($"Is Sliding: {(_isSliding ? "YES" : "NO")}");

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}