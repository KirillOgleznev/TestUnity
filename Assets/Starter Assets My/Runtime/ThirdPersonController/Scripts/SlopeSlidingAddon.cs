using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class SlopeSlidingAddon : MonoBehaviour
{
    [Header("Slope Sliding Settings")]
    [Tooltip("Угол склона, с которого начинается скольжение (в градусах)")]
    public float slidingAngleThreshold = 45f;

    [Tooltip("Скорость скольжения")]
    public float slideSpeed = 8f;

    [Tooltip("Трение при скольжении (0 = нет трения, 1 = максимальное трение)")]
    [Range(0f, 1f)]
    public float slideFriction = 0.3f;

    [Tooltip("Максимальная скорость скольжения")]
    public float maxSlideSpeed = 15f;

    [Tooltip("Слои объектов, по которым можно скользить")]
    public LayerMask slidableLayers = -1;

    [Header("Stability Settings")]
    [Tooltip("Минимальное время нахождения на склоне перед началом скольжения")]
    public float slideStartDelay = 0.1f;

    [Tooltip("Время, в течение которого персонаж считается на земле после потери контакта")]
    public float groundedGraceTime = 0.15f;

    [Tooltip("Минимальное расстояние до земли для проверки")]
    public float groundCheckDistance = 0.1f;

    [Header("Air Jump Integration")]
    [Tooltip("Время отключения скольжения после дополнительного прыжка")]
    public float airJumpSlideDisableTime = 0.5f;

    [Header("Debug")]
    [Tooltip("Показывать информацию о скольжении в консоли")]
    public bool debugMode = false;

    // Приватные переменные
    private CharacterController characterController;
    private Vector3 hitNormal;
    private bool isOnSlope;
    private bool isSliding;
    private float currentSlideSpeed;
    private Vector3 slideDirection;

    // Переменные для стабильности
    private float timeOnSlope;
    private float lastGroundedTime;
    private Vector3 lastValidNormal;
    private bool wasGroundedRecently;

    // *** НОВОЕ: Переменные для временного отключения ***
    private bool slidingTemporarilyDisabled;
    private float slidingDisabledUntil;

    void Start()
    {
        characterController = GetComponent<CharacterController>();

        if (debugMode)
        {
            Debug.Log("SlopeSlidingAddon инициализирован");
        }
    }

    void Update()
    {
        UpdateGroundedState();
        UpdateSlidingDisableState();
        CheckSliding();
        ApplySliding();
    }

    void UpdateGroundedState()
    {
        if (characterController.isGrounded)
        {
            lastGroundedTime = Time.time;
            wasGroundedRecently = true;
        }
        else
        {
            if (IsGroundedByRaycast())
            {
                lastGroundedTime = Time.time;
                wasGroundedRecently = true;
            }
            else
            {
                wasGroundedRecently = Time.time - lastGroundedTime < groundedGraceTime;
            }
        }
    }

    // *** НОВОЕ: Обновление состояния временного отключения ***
    void UpdateSlidingDisableState()
    {
        if (slidingTemporarilyDisabled && Time.time >= slidingDisabledUntil)
        {
            slidingTemporarilyDisabled = false;

            if (debugMode)
            {
                Debug.Log("Скольжение снова включено после дополнительного прыжка");
            }
        }
    }

    bool IsGroundedByRaycast()
    {
        Vector3 rayStart = transform.position + Vector3.up * 0.1f;
        float rayDistance = characterController.height * 0.5f + groundCheckDistance;

        return Physics.Raycast(rayStart, Vector3.down, rayDistance, slidableLayers);
    }

    void CheckSliding()
    {
        // *** НОВОЕ: Проверяем, не отключено ли скольжение временно ***
        if (slidingTemporarilyDisabled)
        {
            if (isSliding)
            {
                StopSliding();
            }
            timeOnSlope = 0f;
            return;
        }

        if (hitNormal == Vector3.zero)
        {
            timeOnSlope = 0f;
            return;
        }

        float slopeAngle = Vector3.Angle(Vector3.up, hitNormal);
        bool currentlyOnSlope = slopeAngle > slidingAngleThreshold && slopeAngle < 85f;

        if (currentlyOnSlope && wasGroundedRecently)
        {
            timeOnSlope += Time.deltaTime;
            isOnSlope = true;
            lastValidNormal = hitNormal;
        }
        else
        {
            timeOnSlope = 0f;
            isOnSlope = false;
        }

        bool wasSliding = isSliding;
        isSliding = isOnSlope && wasGroundedRecently && timeOnSlope >= slideStartDelay;

        if (isSliding && !wasSliding)
        {
            StartSliding();
        }
        else if (!isSliding && wasSliding)
        {
            StopSliding();
        }

        if (debugMode && (isSliding || isOnSlope))
        {
            string disabledStatus = slidingTemporarilyDisabled ? " [ОТКЛЮЧЕНО]" : "";
            Debug.Log($"Склон: {isOnSlope}, Скольжение: {isSliding}, Угол: {slopeAngle:F1}°, Время на склоне: {timeOnSlope:F2}s{disabledStatus}");
        }
    }

    void StartSliding()
    {
        currentSlideSpeed = 0f;

        if (debugMode)
        {
            Debug.Log("Начало скольжения");
        }
    }

    void StopSliding()
    {
        currentSlideSpeed = 0f;
        slideDirection = Vector3.zero;
        timeOnSlope = 0f;

        if (debugMode)
        {
            Debug.Log("Конец скольжения");
        }
    }

    void ApplySliding()
    {
        // *** НОВОЕ: Не применяем скольжение если оно временно отключено ***
        if (!isSliding || slidingTemporarilyDisabled) return;

        Vector3 normalToUse = lastValidNormal != Vector3.zero ? lastValidNormal : hitNormal;
        slideDirection = Vector3.ProjectOnPlane(Vector3.down, normalToUse).normalized;
        currentSlideSpeed = Mathf.Min(currentSlideSpeed + slideSpeed * Time.deltaTime, maxSlideSpeed);

        float frictionMultiplier = 1f - slideFriction;
        Vector3 slideMovement = slideDirection * currentSlideSpeed * frictionMultiplier * Time.deltaTime;

        characterController.Move(slideMovement);
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (((1 << hit.gameObject.layer) & slidableLayers) == 0)
        {
            return;
        }

        float dotProduct = Vector3.Dot(hit.normal, Vector3.up);
        if (dotProduct > 0.1f)
        {
            hitNormal = hit.normal;
        }
    }

    // *** НОВЫЕ ПУБЛИЧНЫЕ МЕТОДЫ ***

    /// <summary>
    /// Временно отключает скольжение на указанное время
    /// </summary>
    public void DisableSlidingTemporarily(float duration)
    {
        slidingTemporarilyDisabled = true;
        slidingDisabledUntil = Time.time + duration;

        // Если сейчас скользим - останавливаем
        if (isSliding)
        {
            StopSliding();
        }

        if (debugMode)
        {
            Debug.Log($"Скольжение временно отключено на {duration} секунд");
        }
    }

    /// <summary>
    /// Отключает скольжение после дополнительного прыжка
    /// </summary>
    public void DisableSlidingForAirJump()
    {
        DisableSlidingTemporarily(airJumpSlideDisableTime);
    }

    /// <summary>
    /// Немедленно включает скольжение обратно
    /// </summary>
    public void EnableSlidingImmediately()
    {
        slidingTemporarilyDisabled = false;
        slidingDisabledUntil = 0f;

        if (debugMode)
        {
            Debug.Log("Скольжение принудительно включено");
        }
    }

    /// <summary>
    /// Проверяет, отключено ли скольжение временно
    /// </summary>
    public bool IsSlidingTemporarilyDisabled()
    {
        return slidingTemporarilyDisabled;
    }

    // Существующие публичные методы
    public bool IsSliding() => isSliding && !slidingTemporarilyDisabled;
    public bool IsOnSlope() => isOnSlope;
    public float GetSlopeAngle()
    {
        if (hitNormal == Vector3.zero) return 0f;
        return Vector3.Angle(Vector3.up, hitNormal);
    }

    void OnDrawGizmosSelected()
    {
        // Проверяем что компоненты инициализированы
        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        // Если все еще null, не рисуем gizmos
        if (characterController == null) return;

        if (hitNormal != Vector3.zero)
        {
            // Меняем цвет если скольжение отключено
            Color normalColor = slidingTemporarilyDisabled ? Color.blue :
                              (isSliding ? Color.red : (isOnSlope ? Color.yellow : Color.green));

            Gizmos.color = normalColor;
            Gizmos.DrawRay(transform.position, hitNormal * 2f);

            if (isSliding && slideDirection != Vector3.zero && !slidingTemporarilyDisabled)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(transform.position, slideDirection * 3f);
            }
        }

        // Рисуем проверку земли только если characterController доступен
        Gizmos.color = wasGroundedRecently ? Color.green : Color.red;
        Vector3 rayStart = transform.position + Vector3.up * 0.1f;
        float rayDistance = characterController.height * 0.5f + groundCheckDistance;
        Gizmos.DrawRay(rayStart, Vector3.down * rayDistance);
    }
}