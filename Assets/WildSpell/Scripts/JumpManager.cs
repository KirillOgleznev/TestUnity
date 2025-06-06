using UnityEngine;
using StarterAssets;

[RequireComponent(typeof(ThirdPersonController), typeof(CharacterController))]
public class JumpManager : MonoBehaviour
{
    [Header("Double Jump Settings")]
    public float doubleJumpHeight = 1.2f;
    public int maxJumps = 2;
    [Range(0.5f, 1.5f)]
    public float doubleJumpPowerMultiplier = 0.8f;

    [Header("Animation Settings")]
    public bool useDoubleJumpAnimation = true;
    public Animator characterAnimator;
    public string doubleJumpAnimParameter = "DoubleJump";
    [Range(0.1f, 1.0f)]
    public float animationDuration = 0.3f;

    [Header("Debug")]
    public bool enableDebug = true;

    private StarterAssetsInputs input;
    private CharacterController characterController;
    private ThirdPersonController controller;
    private int currentJumps = 0;
    private bool wasGrounded = false;
    private bool jumpPressed = false;

    void Start()
    {
        input = GetComponent<StarterAssetsInputs>();
        characterController = GetComponent<CharacterController>();
        controller = GetComponent<ThirdPersonController>();

        if (input == null)
            Debug.LogError("JumpManager: StarterAssetsInputs not found!");
        if (characterController == null)
            Debug.LogError("JumpManager: CharacterController not found!");
        if (controller == null)
            Debug.LogError("JumpManager: ThirdPersonController not found!");

        wasGrounded = characterController != null ? characterController.isGrounded : false;

        if (characterAnimator == null)
        {
            characterAnimator = GetComponent<Animator>();
            if (characterAnimator == null)
                characterAnimator = GetComponentInChildren<Animator>();
        }

        Debug.Log($"JumpManager: Components - Input:{input != null}, Controller:{characterController != null}, 3rdPerson:{controller != null}");
    }

    void Update()
    {
        bool isGrounded = characterController != null ? characterController.isGrounded : false;

        if (input != null && input.jump && !jumpPressed)
        {
            jumpPressed = true;

            if (isGrounded)
            {
                currentJumps = 1;
            }
            else if (currentJumps < maxJumps)
            {
                PerformDoubleJump();
            }
        }

        if (isGrounded && !wasGrounded)
        {
            currentJumps = 0;
        }

        if (input != null && !input.jump)
        {
            jumpPressed = false;
        }

        wasGrounded = isGrounded;
    }

    private void PerformDoubleJump()
    {
        currentJumps++;

        if (!TrySetVerticalVelocity())
        {
            StartCoroutine(ApplyJumpForce());
        }

        if (useDoubleJumpAnimation && characterAnimator != null)
        {
            TriggerDoubleJumpAnimation();
        }

        if (input != null)
            input.jump = false;
    }

    private bool TrySetVerticalVelocity()
    {
        if (controller == null) return false;

        try
        {
            var field = typeof(ThirdPersonController).GetField("_verticalVelocity",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (field != null)
            {
                float jumpVelocity = Mathf.Sqrt(controller.JumpHeight * -2f * controller.Gravity);
                jumpVelocity *= doubleJumpPowerMultiplier;
                field.SetValue(controller, jumpVelocity);
                return true;
            }
        }
        catch
        {
        }

        return false;
    }

    private System.Collections.IEnumerator ApplyJumpForce()
    {
        float strongForce = 100f;

        for (int i = 0; i < 10; i++)
        {
            if (characterController != null)
            {
                Vector3 jumpVelocity = Vector3.up * strongForce;
                characterController.Move(jumpVelocity * Time.deltaTime);
                strongForce *= 0.9f;
            }
            yield return null;
        }
    }

    private void TriggerDoubleJumpAnimation()
    {
        if (characterAnimator == null || characterAnimator.runtimeAnimatorController == null)
            return;

        if (HasAnimatorParameter(characterAnimator, doubleJumpAnimParameter))
        {
            characterAnimator.SetBool(doubleJumpAnimParameter, true);
            StartCoroutine(ResetDoubleJumpAnimation());
        }
    }

    private bool HasAnimatorParameter(Animator animator, string paramName)
    {
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName && param.type == AnimatorControllerParameterType.Bool)
                return true;
        }
        return false;
    }

    private System.Collections.IEnumerator ResetDoubleJumpAnimation()
    {
        yield return new WaitForSeconds(animationDuration);
        if (characterAnimator != null)
            characterAnimator.SetBool(doubleJumpAnimParameter, false);
    }

    public int GetCurrentJumps() => currentJumps;
    public int GetRemainingJumps() => maxJumps - currentJumps;
    public void ResetJumps() => currentJumps = 0;

    [ContextMenu("Test Double Jump")]
    public void TestDoubleJump()
    {
        currentJumps = 1;
        PerformDoubleJump();
    }

    [ContextMenu("Test - Set High Velocity")]
    public void TestSetHighVelocity()
    {
        try
        {
            var field = typeof(ThirdPersonController).GetField("_verticalVelocity",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (field != null)
            {
                float oldVelocity = (float)field.GetValue(controller);
                field.SetValue(controller, 50f);
                float newVelocity = (float)field.GetValue(controller);

                Debug.Log($"TEST: Set velocity {oldVelocity} → {newVelocity}");
                Debug.Log($"TEST: If character jumps HIGH - reflection works!");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"TEST: Error setting velocity: {e.Message}");
        }
    }

    [ContextMenu("Test - Manual Move")]
    public void TestManualMove()
    {
        Debug.Log("TEST: Trying CharacterController.Move() directly...");
        Vector3 upward = Vector3.up * 5f;
        if (characterController != null)
        {
            characterController.Move(upward);
            Debug.Log($"TEST: Moved by {upward}");
        }
    }
}