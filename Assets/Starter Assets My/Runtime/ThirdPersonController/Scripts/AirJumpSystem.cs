using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
    public class AirJumpSystem : MonoBehaviour
    {
        [Header("Air Jump Settings")]
        [Tooltip("������������ ���������� �������������� ������� � �������")]
        public int maxAirJumps = 2;

        [Tooltip("������ �������������� �������")]
        public float airJumpHeight = 1.0f;

        [Tooltip("��������� ������ ��� ������� ������������ ������ (1.0 = ���������, 0.8 = ������ �� 20% ������)")]
        [Range(0.1f, 1.5f)]
        public float jumpHeightMultiplier = 0.8f;

        [Tooltip("����� ������������ ����� ��������������� ������ (��� �������������� �����)")]
        public float airJumpCooldown = 0.1f;

        [Tooltip("���� ��������������� ������")]
        public AudioClip airJumpSound;

        [Tooltip("��������� ����� ��������������� ������")]
        [Range(0f, 1f)]
        public float airJumpVolume = 0.7f;

        [Header("Animation")]
        [Tooltip("��� ��������� ��������� ��� ��������������� ������ (�������� ������ ���� �� �����)")]
        public string airJumpAnimationParameter = "";

        [Tooltip("��� ��������� ��������� ��� ������ ��������������� ������ (�������� ������ ���� �� �����)")]
        public string airJumpCountParameter = "";

        [Tooltip("�������� �������� ��� ������")]
        public float spinSpeed = 720f;

        [Tooltip("������ ��������������� ��� ������")]
        public bool enableScaleEffect = true;

        [Tooltip("������������ ������� ��� ������")]
        public float maxScale = 1.2f;

        [Header("Particle Effects")]
        [Tooltip("������ ������ ��� �������������� ������")]
        public ParticleSystem airJumpEffect;

        [Tooltip("������� ��� ������ ������� (���� �� �������, ������������ ����� ���������)")]
        public Transform effectSpawnPoint;

        [Header("Visual Effects")]
        [Tooltip("������� ��������� ��� �������������� ������")]
        public bool enableSpinEffect = true;

        [Tooltip("����� ������� ��������")]
        public float spinDuration = 0.3f;

        [Header("Debug")]
        [Tooltip("���������� ���������� � �������������� ������� � �������")]
        public bool debugMode = false;

        // ��������� ����������
        private CharacterController characterController;
        private Animator animator;
        private ThirdPersonController thirdPersonController;
        private StarterAssetsInputs input;
        private SlopeSlidingAddon slopeSlidingAddon;

        // ��������� �������
        private int currentAirJumps;
        private bool wasGroundedLastFrame;
        private float lastAirJumpTime;
        private int animIDairJump;
        private int animIDairJumpCount;

        // ���������� ��� ���������� ��������
        private Vector3 originalScale;
        private bool isPlayingSpinEffect;
        private bool isPlayingScaleEffect;

        // ������� ��� ���������� � ������� ���������
        public System.Action<int> OnAirJumpPerformed; // ���������� ��� ���������� ��������������� ������
        public System.Action OnAirJumpsReset; // ���������� ��� ������ �������

        void Start()
        {
            // �������� ����������� ����������
            characterController = GetComponent<CharacterController>();
            animator = GetComponent<Animator>();
            thirdPersonController = GetComponent<ThirdPersonController>();
            input = GetComponent<StarterAssetsInputs>();
            slopeSlidingAddon = GetComponent<SlopeSlidingAddon>();

            // �������� ID �������� ������ ���� ��������� ������
            if (animator != null)
            {
                if (!string.IsNullOrEmpty(airJumpAnimationParameter))
                {
                    animIDairJump = Animator.StringToHash(airJumpAnimationParameter);
                }

                if (!string.IsNullOrEmpty(airJumpCountParameter))
                {
                    animIDairJumpCount = Animator.StringToHash(airJumpCountParameter);
                }
            }

            // �������������� ���������
            currentAirJumps = 0;
            wasGroundedLastFrame = thirdPersonController.Grounded;
            originalScale = transform.localScale;

            if (debugMode)
            {
                Debug.Log($"AirJumpSystem ���������������. �������� �������: {maxAirJumps}");
            }
        }

        void Update()
        {
            HandleGroundedStateChange();
            HandleAirJumpInput();
        }

        void HandleGroundedStateChange()
        {
            bool isGroundedNow = thirdPersonController.Grounded;

            // ���� �������� �����������, ���������� ������ �������������� �������
            if (isGroundedNow && !wasGroundedLastFrame)
            {
                ResetAirJumps();
            }

            wasGroundedLastFrame = isGroundedNow;
        }

        void HandleAirJumpInput()
        {
            // ���������, ����� �� �� ��������� �������������� ������
            if (!CanPerformAirJump()) return;

            // ��������� ���� ������
            if (input.jump)
            {
                PerformAirJump();
                input.jump = false; // ���������� ���� ����� �� ������ �������� �������
            }
        }

        bool CanPerformAirJump()
        {
            // �� ����� ������� ���� �� �����
            if (thirdPersonController.Grounded) return false;

            // �� ����� ������� ���� ��� �������
            if (currentAirJumps >= maxAirJumps) return false;

            // ��������� �������
            if (Time.time - lastAirJumpTime < airJumpCooldown) return false;

            return true;
        }

        void PerformAirJump()
        {
            // ����������� ������� �������
            currentAirJumps++;

            // ��������� ���� ������ � ������ ���������
            float jumpPower = airJumpHeight;
            for (int i = 1; i < currentAirJumps; i++)
            {
                jumpPower *= jumpHeightMultiplier;
            }

            // ��������� ������ ����� ��������� �����
            float newVelocity = Mathf.Sqrt(jumpPower * -2f * thirdPersonController.Gravity);
            thirdPersonController.SetVerticalVelocity(newVelocity);

            // *** �����: �������� ��������� ���������� ***
            if (slopeSlidingAddon != null)
            {
                slopeSlidingAddon.DisableSlidingForAirJump();
            }

            // ��������� �������� ������ ���� ��������� ������
            if (animator != null)
            {
                if (!string.IsNullOrEmpty(airJumpAnimationParameter))
                {
                    animator.SetTrigger(animIDairJump);
                }

                if (!string.IsNullOrEmpty(airJumpCountParameter))
                {
                    animator.SetInteger(animIDairJumpCount, currentAirJumps);
                }
            }

            // ������������� ����
            if (airJumpSound != null)
            {
                AudioSource.PlayClipAtPoint(airJumpSound, transform.position, airJumpVolume);
            }

            // ��������� ���������� �������
            if (enableSpinEffect && !isPlayingSpinEffect)
            {
                StartCoroutine(SpinEffect());
            }

            if (enableScaleEffect && !isPlayingScaleEffect)
            {
                StartCoroutine(ScaleEffect());
            }

            // ��������� ������ ������
            if (airJumpEffect != null)
            {
                Vector3 effectPosition = effectSpawnPoint != null ? effectSpawnPoint.position :
                    transform.position + characterController.center;

                if (airJumpEffect.isPlaying) airJumpEffect.Stop();
                airJumpEffect.transform.position = effectPosition;
                airJumpEffect.Play();
            }

            // ���������� ����� ������
            lastAirJumpTime = Time.time;

            // �������� �������
            OnAirJumpPerformed?.Invoke(currentAirJumps);

            if (debugMode)
            {
                Debug.Log($"�������� �������������� ������ #{currentAirJumps}/{maxAirJumps}. ����: {jumpPower:F2}");
            }
        }

        void ResetAirJumps()
        {
            if (currentAirJumps > 0)
            {
                currentAirJumps = 0;

                // ���������� �������� ������ ���� �������� �����
                if (animator != null && !string.IsNullOrEmpty(airJumpCountParameter))
                {
                    animator.SetInteger(animIDairJumpCount, 0);
                }

                // �������� �������
                OnAirJumpsReset?.Invoke();

                if (debugMode)
                {
                    Debug.Log("������ �������������� ������� �������������");
                }
            }
        }

        // ��������� ������ ��� ���������� � ������� ���������

        /// <summary>
        /// ���������� ���������� ���������� �������������� �������
        /// </summary>
        public int GetRemainingAirJumps()
        {
            return maxAirJumps - currentAirJumps;
        }

        /// <summary>
        /// ���������� ���������� �������������� �������������� �������
        /// </summary>
        public int GetUsedAirJumps()
        {
            return currentAirJumps;
        }

        /// <summary>
        /// ���������, ����� �� �������� ��������� �������������� ������
        /// </summary>
        public bool HasAirJumpsLeft()
        {
            return GetRemainingAirJumps() > 0 && !thirdPersonController.Grounded;
        }

        /// <summary>
        /// ������������� ���������� ��� ������ �������������� �������
        /// </summary>
        public void ForceResetAirJumps()
        {
            ResetAirJumps();
        }

        /// <summary>
        /// ��������� �������������� ������ ������� (������� ��� �����-����)
        /// </summary>
        public void AddAirJumpCharges(int amount)
        {
            maxAirJumps += amount;
            if (debugMode)
            {
                Debug.Log($"��������� ������� �������: {amount}. ����� ��������: {maxAirJumps}");
            }
        }

        /// <summary>
        /// ��������� ��������������� ���� ����� ������
        /// </summary>
        public void RestoreOneAirJump()
        {
            if (currentAirJumps > 0)
            {
                currentAirJumps--;
                if (animator != null && !string.IsNullOrEmpty(airJumpCountParameter))
                {
                    animator.SetInteger(animIDairJumpCount, currentAirJumps);
                }

                if (debugMode)
                {
                    Debug.Log($"������������ ���� ����� ������. ���������� ������: {GetRemainingAirJumps()}");
                }
            }
        }

        // �������� ��� ���������� ��������

        private System.Collections.IEnumerator SpinEffect()
        {
            isPlayingSpinEffect = true;
            float elapsedTime = 0f;

            while (elapsedTime < spinDuration)
            {
                float rotationThisFrame = spinSpeed * Time.deltaTime;
                transform.Rotate(0, rotationThisFrame, 0);

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            isPlayingSpinEffect = false;
        }

        private System.Collections.IEnumerator ScaleEffect()
        {
            isPlayingScaleEffect = true;
            float elapsedTime = 0f;
            float halfDuration = spinDuration * 0.5f;

            // ����������� �������
            while (elapsedTime < halfDuration)
            {
                float progress = elapsedTime / halfDuration;
                float currentScale = Mathf.Lerp(1f, maxScale, progress);
                transform.localScale = originalScale * currentScale;

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // ��������� ������� �������
            elapsedTime = 0f;
            while (elapsedTime < halfDuration)
            {
                float progress = elapsedTime / halfDuration;
                float currentScale = Mathf.Lerp(maxScale, 1f, progress);
                transform.localScale = originalScale * currentScale;

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // ����������� ������� � ��������� ��������
            transform.localScale = originalScale;
            isPlayingScaleEffect = false;
        }
    }
}