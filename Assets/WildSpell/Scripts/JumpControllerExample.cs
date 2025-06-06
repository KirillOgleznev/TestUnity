using UnityEngine;

public class JumpControllerExample : MonoBehaviour
{
    private JumpManager jumpManager;

    void Start()
    {
        jumpManager = GetComponent<JumpManager>();
        if (jumpManager == null)
        {
            Debug.LogError("JumpManager not found!");
        }
        else
        {
            Debug.Log("Double Jump ready! Press R to reset jumps, I for info");
        }
    }

    void Update()
    {
        if (jumpManager == null) return;

        // Сброс прыжков
        if (Input.GetKeyDown(KeyCode.R))
        {
            jumpManager.ResetJumps();
            Debug.Log("Jumps reset");
        }

        // Показать информацию
        if (Input.GetKeyDown(KeyCode.I))
        {
            Debug.Log($"Current jumps: {jumpManager.GetCurrentJumps()}, " +
                     $"Remaining: {jumpManager.GetRemainingJumps()}");
        }
    }
}