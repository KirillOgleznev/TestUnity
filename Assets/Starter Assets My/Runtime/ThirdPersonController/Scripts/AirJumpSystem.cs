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
        [Tooltip("Максимальное количество дополнительных прыжков в воздухе")]
        public int maxAirJumps = 2;

        [Tooltip("Высота дополнительных прыжков")]
        public float airJumpHeight = 1.0f;

        [Tooltip("Множитель высоты для каждого последующего прыжка (1.0 = одинаково, 0.8 = каждый на 20% слабее)")]
        [Range(0.1f, 1.5f)]
        public float jumpHeightMultiplier = 0.8f;

        [Tooltip("Время неуязвимости после дополнительного прыжка (для предотвращения спама)")]
        public float airJumpCooldown = 0.1f;

        [Tooltip("Звук дополнительного прыжка")]
        public AudioClip airJumpSound;

        [Tooltip("Громкость звука дополнительного прыжка")]
        [Range(0f, 1f)]
        public float airJumpVolume = 0.7f;

        [Header("Animation")]
        [Tooltip("Имя параметра аниматора для дополнительного прыжка (оставьте пустым если не нужно)")]
        public string airJumpAnimationParameter = "";

        [Tooltip("Имя параметра аниматора для номера дополнительного прыжка (оставьте пустым если не нужно)")]
        public string airJumpCountParameter = "";

        [Tooltip("Скорость поворота при прыжке")]
        public float spinSpeed = 720f;

        [Tooltip("Эффект масштабирования при прыжке")]
        public bool enableScaleEffect = true;

        [Tooltip("Максимальный масштаб при прыжке")]
        public float maxScale = 1.2f;

        [Header("Particle Effects")]
        [Tooltip("Эффект частиц при дополнительном прыжке")]
        public ParticleSystem airJumpEffect;

        [Tooltip("Позиция для спавна эффекта (если не указана, используется центр персонажа)")]
        public Transform effectSpawnPoint;

        [Header("Visual Effects")]
        [Tooltip("Поворот персонажа при дополнительном прыжке")]
        public bool enableSpinEffect = true;

        [Tooltip("Время эффекта поворота")]
        public float spinDuration = 0.3f;

        [Header("Debug")]
        [Tooltip("Показывать информацию о дополнительных прыжках в консоли")]
        public bool debugMode = false;

        // Приватные переменные
        private CharacterController characterController;
        private Animator animator;
        private ThirdPersonController thirdPersonController;
        private StarterAssetsInputs input;
        private SlopeSlidingAddon slopeSlidingAddon;

        // Состояние системы
        private int currentAirJumps;
        private bool wasGroundedLastFrame;
        private float lastAirJumpTime;
        private int animIDairJump;
        private int animIDairJumpCount;

        // Переменные для визуальных эффектов
        private Vector3 originalScale;
        private bool isPlayingSpinEffect;
        private bool isPlayingScaleEffect;

        // События для интеграции с другими системами
        public System.Action<int> OnAirJumpPerformed; // Вызывается при выполнении дополнительного прыжка
        public System.Action OnAirJumpsReset; // Вызывается при сбросе зарядов

        void Start()
        {
            // Получаем необходимые компоненты
            characterController = GetComponent<CharacterController>();
            animator = GetComponent<Animator>();
            thirdPersonController = GetComponent<ThirdPersonController>();
            input = GetComponent<StarterAssetsInputs>();
            slopeSlidingAddon = GetComponent<SlopeSlidingAddon>();

            // Кэшируем ID анимаций только если параметры заданы
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

            // Инициализируем состояние
            currentAirJumps = 0;
            wasGroundedLastFrame = thirdPersonController.Grounded;
            originalScale = transform.localScale;

            if (debugMode)
            {
                Debug.Log($"AirJumpSystem инициализирован. Максимум прыжков: {maxAirJumps}");
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

            // Если персонаж приземлился, сбрасываем заряды дополнительных прыжков
            if (isGroundedNow && !wasGroundedLastFrame)
            {
                ResetAirJumps();
            }

            wasGroundedLastFrame = isGroundedNow;
        }

        void HandleAirJumpInput()
        {
            // Проверяем, можем ли мы выполнить дополнительный прыжок
            if (!CanPerformAirJump()) return;

            // Проверяем ввод прыжка
            if (input.jump)
            {
                PerformAirJump();
                input.jump = false; // Сбрасываем ввод чтобы не мешать основной системе
            }
        }

        bool CanPerformAirJump()
        {
            // Не можем прыгать если на земле
            if (thirdPersonController.Grounded) return false;

            // Не можем прыгать если нет зарядов
            if (currentAirJumps >= maxAirJumps) return false;

            // Проверяем кулдаун
            if (Time.time - lastAirJumpTime < airJumpCooldown) return false;

            return true;
        }

        void PerformAirJump()
        {
            // Увеличиваем счетчик прыжков
            currentAirJumps++;

            // Вычисляем силу прыжка с учетом множителя
            float jumpPower = airJumpHeight;
            for (int i = 1; i < currentAirJumps; i++)
            {
                jumpPower *= jumpHeightMultiplier;
            }

            // Применяем прыжок через публичный метод
            float newVelocity = Mathf.Sqrt(jumpPower * -2f * thirdPersonController.Gravity);
            thirdPersonController.SetVerticalVelocity(newVelocity);

            // *** НОВОЕ: Временно отключаем скольжение ***
            if (slopeSlidingAddon != null)
            {
                slopeSlidingAddon.DisableSlidingForAirJump();
            }

            // Обновляем анимации только если параметры заданы
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

            // Воспроизводим звук
            if (airJumpSound != null)
            {
                AudioSource.PlayClipAtPoint(airJumpSound, transform.position, airJumpVolume);
            }

            // Запускаем визуальные эффекты
            if (enableSpinEffect && !isPlayingSpinEffect)
            {
                StartCoroutine(SpinEffect());
            }

            if (enableScaleEffect && !isPlayingScaleEffect)
            {
                StartCoroutine(ScaleEffect());
            }

            // Запускаем эффект частиц
            if (airJumpEffect != null)
            {
                Vector3 effectPosition = effectSpawnPoint != null ? effectSpawnPoint.position :
                    transform.position + characterController.center;

                if (airJumpEffect.isPlaying) airJumpEffect.Stop();
                airJumpEffect.transform.position = effectPosition;
                airJumpEffect.Play();
            }

            // Запоминаем время прыжка
            lastAirJumpTime = Time.time;

            // Вызываем событие
            OnAirJumpPerformed?.Invoke(currentAirJumps);

            if (debugMode)
            {
                Debug.Log($"Выполнен дополнительный прыжок #{currentAirJumps}/{maxAirJumps}. Сила: {jumpPower:F2}");
            }
        }

        void ResetAirJumps()
        {
            if (currentAirJumps > 0)
            {
                currentAirJumps = 0;

                // Сбрасываем анимации только если параметр задан
                if (animator != null && !string.IsNullOrEmpty(airJumpCountParameter))
                {
                    animator.SetInteger(animIDairJumpCount, 0);
                }

                // Вызываем событие
                OnAirJumpsReset?.Invoke();

                if (debugMode)
                {
                    Debug.Log("Заряды дополнительных прыжков восстановлены");
                }
            }
        }

        // Публичные методы для интеграции с другими системами

        /// <summary>
        /// Возвращает количество оставшихся дополнительных прыжков
        /// </summary>
        public int GetRemainingAirJumps()
        {
            return maxAirJumps - currentAirJumps;
        }

        /// <summary>
        /// Возвращает количество использованных дополнительных прыжков
        /// </summary>
        public int GetUsedAirJumps()
        {
            return currentAirJumps;
        }

        /// <summary>
        /// Проверяет, может ли персонаж выполнить дополнительный прыжок
        /// </summary>
        public bool HasAirJumpsLeft()
        {
            return GetRemainingAirJumps() > 0 && !thirdPersonController.Grounded;
        }

        /// <summary>
        /// Принудительно сбрасывает все заряды дополнительных прыжков
        /// </summary>
        public void ForceResetAirJumps()
        {
            ResetAirJumps();
        }

        /// <summary>
        /// Добавляет дополнительные заряды прыжков (полезно для пауэр-апов)
        /// </summary>
        public void AddAirJumpCharges(int amount)
        {
            maxAirJumps += amount;
            if (debugMode)
            {
                Debug.Log($"Добавлено зарядов прыжков: {amount}. Новый максимум: {maxAirJumps}");
            }
        }

        /// <summary>
        /// Мгновенно восстанавливает один заряд прыжка
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
                    Debug.Log($"Восстановлен один заряд прыжка. Оставшиеся прыжки: {GetRemainingAirJumps()}");
                }
            }
        }

        // Корутины для визуальных эффектов

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

            // Увеличиваем масштаб
            while (elapsedTime < halfDuration)
            {
                float progress = elapsedTime / halfDuration;
                float currentScale = Mathf.Lerp(1f, maxScale, progress);
                transform.localScale = originalScale * currentScale;

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // Уменьшаем масштаб обратно
            elapsedTime = 0f;
            while (elapsedTime < halfDuration)
            {
                float progress = elapsedTime / halfDuration;
                float currentScale = Mathf.Lerp(maxScale, 1f, progress);
                transform.localScale = originalScale * currentScale;

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // Гарантируем возврат к исходному масштабу
            transform.localScale = originalScale;
            isPlayingScaleEffect = false;
        }
    }
}