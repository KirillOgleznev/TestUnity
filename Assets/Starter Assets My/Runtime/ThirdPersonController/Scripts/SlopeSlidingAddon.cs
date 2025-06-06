using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class SlopeSlidingAddon : MonoBehaviour
{
    [Header("Slope Sliding Settings")]
    [Tooltip("���� ������, � �������� ���������� ���������� (� ��������)")]
    public float slidingAngleThreshold = 45f;

    [Tooltip("�������� ����������")]
    public float slideSpeed = 8f;

    [Tooltip("������ ��� ���������� (0 = ��� ������, 1 = ������������ ������)")]
    [Range(0f, 1f)]
    public float slideFriction = 0.3f;

    [Tooltip("������������ �������� ����������")]
    public float maxSlideSpeed = 15f;

    [Tooltip("���� ��������, �� ������� ����� ���������")]
    public LayerMask slidableLayers = -1;

    [Header("Stability Settings")]
    [Tooltip("����������� ����� ���������� �� ������ ����� ������� ����������")]
    public float slideStartDelay = 0.1f;

    [Tooltip("�����, � ������� �������� �������� ��������� �� ����� ����� ������ ��������")]
    public float groundedGraceTime = 0.15f;

    [Tooltip("����������� ���������� �� ����� ��� ��������")]
    public float groundCheckDistance = 0.1f;

    [Header("Air Jump Integration")]
    [Tooltip("����� ���������� ���������� ����� ��������������� ������")]
    public float airJumpSlideDisableTime = 0.5f;

    [Header("Debug")]
    [Tooltip("���������� ���������� � ���������� � �������")]
    public bool debugMode = false;

    // ��������� ����������
    private CharacterController characterController;
    private Vector3 hitNormal;
    private bool isOnSlope;
    private bool isSliding;
    private float currentSlideSpeed;
    private Vector3 slideDirection;

    // ���������� ��� ������������
    private float timeOnSlope;
    private float lastGroundedTime;
    private Vector3 lastValidNormal;
    private bool wasGroundedRecently;

    // *** �����: ���������� ��� ���������� ���������� ***
    private bool slidingTemporarilyDisabled;
    private float slidingDisabledUntil;

    void Start()
    {
        characterController = GetComponent<CharacterController>();

        if (debugMode)
        {
            Debug.Log("SlopeSlidingAddon ���������������");
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

    // *** �����: ���������� ��������� ���������� ���������� ***
    void UpdateSlidingDisableState()
    {
        if (slidingTemporarilyDisabled && Time.time >= slidingDisabledUntil)
        {
            slidingTemporarilyDisabled = false;

            if (debugMode)
            {
                Debug.Log("���������� ����� �������� ����� ��������������� ������");
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
        // *** �����: ���������, �� ��������� �� ���������� �������� ***
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
            string disabledStatus = slidingTemporarilyDisabled ? " [���������]" : "";
            Debug.Log($"�����: {isOnSlope}, ����������: {isSliding}, ����: {slopeAngle:F1}�, ����� �� ������: {timeOnSlope:F2}s{disabledStatus}");
        }
    }

    void StartSliding()
    {
        currentSlideSpeed = 0f;

        if (debugMode)
        {
            Debug.Log("������ ����������");
        }
    }

    void StopSliding()
    {
        currentSlideSpeed = 0f;
        slideDirection = Vector3.zero;
        timeOnSlope = 0f;

        if (debugMode)
        {
            Debug.Log("����� ����������");
        }
    }

    void ApplySliding()
    {
        // *** �����: �� ��������� ���������� ���� ��� �������� ��������� ***
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

    // *** ����� ��������� ������ ***

    /// <summary>
    /// �������� ��������� ���������� �� ��������� �����
    /// </summary>
    public void DisableSlidingTemporarily(float duration)
    {
        slidingTemporarilyDisabled = true;
        slidingDisabledUntil = Time.time + duration;

        // ���� ������ �������� - �������������
        if (isSliding)
        {
            StopSliding();
        }

        if (debugMode)
        {
            Debug.Log($"���������� �������� ��������� �� {duration} ������");
        }
    }

    /// <summary>
    /// ��������� ���������� ����� ��������������� ������
    /// </summary>
    public void DisableSlidingForAirJump()
    {
        DisableSlidingTemporarily(airJumpSlideDisableTime);
    }

    /// <summary>
    /// ���������� �������� ���������� �������
    /// </summary>
    public void EnableSlidingImmediately()
    {
        slidingTemporarilyDisabled = false;
        slidingDisabledUntil = 0f;

        if (debugMode)
        {
            Debug.Log("���������� ������������� ��������");
        }
    }

    /// <summary>
    /// ���������, ��������� �� ���������� ��������
    /// </summary>
    public bool IsSlidingTemporarilyDisabled()
    {
        return slidingTemporarilyDisabled;
    }

    // ������������ ��������� ������
    public bool IsSliding() => isSliding && !slidingTemporarilyDisabled;
    public bool IsOnSlope() => isOnSlope;
    public float GetSlopeAngle()
    {
        if (hitNormal == Vector3.zero) return 0f;
        return Vector3.Angle(Vector3.up, hitNormal);
    }

    void OnDrawGizmosSelected()
    {
        // ��������� ��� ���������� ����������������
        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        // ���� ��� ��� null, �� ������ gizmos
        if (characterController == null) return;

        if (hitNormal != Vector3.zero)
        {
            // ������ ���� ���� ���������� ���������
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

        // ������ �������� ����� ������ ���� characterController ��������
        Gizmos.color = wasGroundedRecently ? Color.green : Color.red;
        Vector3 rayStart = transform.position + Vector3.up * 0.1f;
        float rayDistance = characterController.height * 0.5f + groundCheckDistance;
        Gizmos.DrawRay(rayStart, Vector3.down * rayDistance);
    }
}