using System;
using System.Collections;
using System.Collections.Generic;
using Unity.FPS.Game;
using Unity.FPS.AI;  // Добавили для EnemyController
using UnityEngine;
using UnityEngine.Events;

namespace Unity.FPS.AI
{
    [System.Serializable]
    public class MeleeAttackData
    {
        [Header("Attack Properties")]
        public string attackName = "Basic Attack";
        public float damage = 30f;
        public float range = 2f;
        public float attackAngle = 45f;
        public float cooldown = 1f;

        [Header("Timing")]
        public float windupTime = 0.3f;      // Время подготовки к атаке
        public float activeTime = 0.2f;      // Время активной фазы атаки
        public float recoveryTime = 0.5f;    // Время восстановления

        [Header("Effects")]
        public GameObject hitEffectPrefab;
        public AudioClip attackSound;
        public AudioClip hitSound;
        public float knockbackForce = 5f;
        public float stunDuration = 0.5f;
    }

    public enum AttackState
    {
        Ready,
        Windup,     // Подготовка к атаке (телеграфирование)
        Active,     // Активная фаза урона
        Recovery    // Восстановление после атаки
    }

    [RequireComponent(typeof(AudioSource))]
    public class MeleeWeaponController : MonoBehaviour  // Убрали "Improved" для совместимости
    {
        [Header("Weapon Information")]
        [Tooltip("Название оружия")]
        public string WeaponName = "Claws";

        [Tooltip("Иконка оружия")]
        public Sprite WeaponIcon;

        [Header("References")]
        [Tooltip("Корневой объект оружия")]
        public GameObject WeaponRoot;

        [Tooltip("Точка атаки")]
        public Transform AttackPoint;

        [Header("Attack Settings")]
        [Tooltip("Настройки атак")]
        public MeleeAttackData[] attacks = new MeleeAttackData[1];

        [Tooltip("Слои, которые могут получить урон")]
        public LayerMask damageableLayers = -1;

        [Tooltip("Слои препятствий для проверки линии обзора")]
        public LayerMask obstacleLayers = 1;

        [Header("Visual Feedback")]
        [Tooltip("Материал для подсветки при подготовке атаки")]
        public Material windupMaterial;

        [Tooltip("Показывать области атаки в редакторе")]
        public bool showAttackGizmos = true;

        [Header("Audio")]
        [Tooltip("Аниматор для анимаций атак")]
        public Animator weaponAnimator;

        [Header("Animation Integration")]
        [Tooltip("Использовать Animation Events вместо таймеров")]
        public bool useAnimationEvents = false;

        [Tooltip("Аниматор врага для воспроизведения атак")]
        public Animator enemyAnimator;

        [Tooltip("Проверять наличие целей перед атакой")]
        public bool requireTargetsToAttack = true;

        // События
        public UnityAction<string> OnAttackStarted;
        public UnityAction<GameObject> OnTargetHit;
        public UnityAction OnAttackFinished;

        // Приватные переменные
        private AttackState currentState = AttackState.Ready;
        private int currentAttackIndex = 0;
        private float stateTimer = 0f;
        private float lastAttackTime = 0f;  // Добавили для кулдауна
        private AudioSource audioSource;
        private List<GameObject> hitTargetsThisAttack = new List<GameObject>();
        private Renderer[] weaponRenderers;
        private Material[] originalMaterials;

        // Публичные свойства
        public GameObject Owner { get; set; }
        public bool IsWeaponActive { get; private set; }
        public AttackState CurrentState => currentState;
        public bool CanAttack => currentState == AttackState.Ready && (Time.time - lastAttackTime) >= GetCurrentAttackCooldown();

        // Метод для получения кулдауна текущей атаки
        private float GetCurrentAttackCooldown()
        {
            if (attacks == null || attacks.Length == 0) return 1f;
            var attack = attacks[Mathf.Clamp(currentAttackIndex, 0, attacks.Length - 1)];
            return attack.cooldown;
        }

        void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (AttackPoint == null)
                AttackPoint = transform;

            // Сохраняем оригинальные материалы для эффектов
            weaponRenderers = GetComponentsInChildren<Renderer>();
            if (weaponRenderers.Length > 0)
            {
                originalMaterials = new Material[weaponRenderers.Length];
                for (int i = 0; i < weaponRenderers.Length; i++)
                {
                    originalMaterials[i] = weaponRenderers[i].material;
                }
            }

            // Проверяем корректность настроек
            ValidateAttackData();
        }

        void Update()
        {
            UpdateAttackState();
        }

        void ValidateAttackData()
        {
            if (attacks == null || attacks.Length == 0)
            {
                attacks = new MeleeAttackData[1];
                attacks[0] = new MeleeAttackData();
                Debug.LogWarning($"[MeleeWeapon] {name}: Создана атака по умолчанию");
            }
        }

        void UpdateAttackState()
        {
            if (currentState == AttackState.Ready) return;

            stateTimer += Time.deltaTime;
            var currentAttack = attacks[currentAttackIndex];

            switch (currentState)
            {
                case AttackState.Windup:
                    // Эффект подготовки к атаке
                    UpdateWindupEffects();

                    if (stateTimer >= currentAttack.windupTime)
                    {
                        StartActivePhase();
                    }
                    break;

                case AttackState.Active:
                    // Проверяем урон каждый кадр в активной фазе
                    CheckForHits();

                    if (stateTimer >= currentAttack.windupTime + currentAttack.activeTime)
                    {
                        StartRecoveryPhase();
                    }
                    break;

                case AttackState.Recovery:
                    if (stateTimer >= currentAttack.windupTime + currentAttack.activeTime + currentAttack.recoveryTime)
                    {
                        EndAttack();
                    }
                    break;
            }
        }

        public void ShowWeapon(bool show)
        {
            if (WeaponRoot != null)
                WeaponRoot.SetActive(show);
            IsWeaponActive = show;
        }

        public bool HandleShootInputs(bool inputDown, bool inputHeld, bool inputUp)
        {
            // Для ближнего боя используем inputDown или inputHeld
            if ((inputDown || inputHeld) && CanAttack)
            {
                return TryStartAttack();
            }
            return false;
        }

        // Дополнительный метод для совместимости со старым API
        public bool TryMeleeAttack()
        {
            return TryStartAttack();
        }

        public bool TryStartAttack(int attackIndex = 0)
        {
            // Проверяем можем ли атаковать (состояние + кулдаун)
            if (!CanAttack || attackIndex >= attacks.Length)
            {
                Debug.Log($"[MeleeWeapon] Атака заблокирована. CanAttack: {CanAttack}, State: {currentState}, Cooldown remaining: {GetCurrentAttackCooldown() - (Time.time - lastAttackTime):F1}");
                return false;
            }

            currentAttackIndex = attackIndex;
            var attack = attacks[currentAttackIndex];

            // Проверяем есть ли цели в радиусе (если требуется)
            if (requireTargetsToAttack && !HasValidTargetsInRange())
            {
                Debug.Log($"[MeleeWeapon] Нет целей в радиусе атаки");
                return false;
            }

            // Запоминаем время атаки для кулдауна
            lastAttackTime = Time.time;

            // Если используем Animation Events, запускаем анимацию
            if (useAnimationEvents && enemyAnimator != null)
            {
                enemyAnimator.SetTrigger($"Attack{attackIndex}");
                Debug.Log($"[MeleeWeapon] Запущена анимация: Attack{attackIndex}");

                // Очищаем список целей
                hitTargetsThisAttack.Clear();
                return true;
            }
            else
            {
                // Используем старый метод с таймерами
                StartWindupPhase();
                return true;
            }
        }

        /// <summary>
        /// Принудительный запуск атаки без проверки целей (для агрессивного ИИ)
        /// </summary>
        public bool ForceStartAttack(int attackIndex = 0)
        {
            // Даже принудительная атака должна учитывать кулдаун и состояние
            if (!CanAttack || attackIndex >= attacks.Length)
            {
                return false;
            }

            currentAttackIndex = attackIndex;
            hitTargetsThisAttack.Clear();
            lastAttackTime = Time.time; // Запоминаем время атаки

            if (useAnimationEvents && enemyAnimator != null)
            {
                enemyAnimator.SetTrigger($"Attack{attackIndex}");
                Debug.Log($"[MeleeWeapon] Принудительно запущена атака: Attack{attackIndex}");
                return true;
            }
            else
            {
                StartWindupPhase();
                return true;
            }
        }

        bool HasValidTargetsInRange()
        {
            var attack = attacks[currentAttackIndex];

            // Быстрая проверка радиуса
            Collider[] nearbyTargets = Physics.OverlapSphere(AttackPoint.position, attack.range, damageableLayers);

            foreach (var target in nearbyTargets)
            {
                if (IsValidTarget(target.gameObject, attack))
                    return true;
            }

            return false;
        }

        void StartWindupPhase()
        {
            currentState = AttackState.Windup;
            stateTimer = 0f;
            hitTargetsThisAttack.Clear();

            var attack = attacks[currentAttackIndex];

            // Анимация
            if (weaponAnimator != null)
                weaponAnimator.SetTrigger($"Attack{currentAttackIndex}");

            // Звук начала атаки
            if (attack.attackSound != null)
                audioSource.PlayOneShot(attack.attackSound);

            // Визуальный эффект подготовки
            StartWindupVisualEffect();

            OnAttackStarted?.Invoke(attack.attackName);

            Debug.Log($"[MeleeWeapon] Начата атака: {attack.attackName}");
        }

        void StartActivePhase()
        {
            currentState = AttackState.Active;
            StopWindupVisualEffect();
            Debug.Log($"[MeleeWeapon] Активная фаза атаки");
        }

        void StartRecoveryPhase()
        {
            currentState = AttackState.Recovery;
            Debug.Log($"[MeleeWeapon] Фаза восстановления");
        }

        void EndAttack()
        {
            currentState = AttackState.Ready;
            stateTimer = 0f;
            OnAttackFinished?.Invoke();

            // Показываем информацию о кулдауне
            var attack = attacks[currentAttackIndex];
            Debug.Log($"[MeleeWeapon] Атака завершена. Кулдаун: {attack.cooldown} сек. Следующая атака через: {attack.cooldown - (Time.time - lastAttackTime):F1} сек");
        }

        /// <summary>
        /// Получить оставшееся время кулдауна
        /// </summary>
        public float GetRemainingCooldown()
        {
            if (currentState != AttackState.Ready) return 999f; // Если атакуем, кулдаун "бесконечный"

            float timeSinceLastAttack = Time.time - lastAttackTime;
            float cooldown = GetCurrentAttackCooldown();
            return Mathf.Max(0f, cooldown - timeSinceLastAttack);
        }

        /// <summary>
        /// Проверка готовности к атаке с подробной информацией
        /// </summary>
        public bool CanAttackDetailed(out string reason)
        {
            if (currentState != AttackState.Ready)
            {
                reason = $"Состояние: {currentState}";
                return false;
            }

            float remainingCooldown = GetRemainingCooldown();
            if (remainingCooldown > 0f)
            {
                reason = $"Кулдаун: {remainingCooldown:F1} сек";
                return false;
            }

            reason = "Готов к атаке";
            return true;
        }

        void CheckForHits()
        {
            var attack = attacks[currentAttackIndex];

            // 1. Находим потенциальные цели в радиусе (быстро)
            Collider[] potentialTargets = Physics.OverlapSphere(AttackPoint.position, attack.range, damageableLayers);

            foreach (var target in potentialTargets)
            {
                // Пропускаем уже поврежденные цели в этой атаке
                if (hitTargetsThisAttack.Contains(target.gameObject))
                    continue;

                // 2. Проверяем угол атаки
                if (!IsInAttackAngle(target.transform.position, attack))
                    continue;

                // 3. Проверяем линию обзора (нет препятствий)
                if (!HasLineOfSight(target.transform.position))
                    continue;

                // 4. Проверяем что цель валидна
                if (!IsValidTarget(target.gameObject, attack))
                    continue;

                // Наносим урон
                ApplyDamage(target.gameObject, attack);
            }
        }

        bool IsInAttackAngle(Vector3 targetPosition, MeleeAttackData attack)
        {
            Vector3 directionToTarget = (targetPosition - AttackPoint.position).normalized;
            float angleToTarget = Vector3.Angle(AttackPoint.forward, directionToTarget);
            return angleToTarget <= attack.attackAngle / 2f;
        }

        bool HasLineOfSight(Vector3 targetPosition)
        {
            Vector3 direction = targetPosition - AttackPoint.position;
            float distance = direction.magnitude;

            // Рейкаст для проверки препятствий
            if (Physics.Raycast(AttackPoint.position, direction.normalized, out RaycastHit hit, distance, obstacleLayers))
            {
                Debug.DrawRay(AttackPoint.position, direction, Color.red, 0.1f);
                return false; // Есть препятствие
            }

            Debug.DrawRay(AttackPoint.position, direction, Color.green, 0.1f);
            return true; // Линия обзора чистая
        }

        bool IsValidTarget(GameObject target, MeleeAttackData attack)
        {
            // Пропускаем себя
            if (target.transform.root == transform.root)
                return false;

            // Проверяем есть ли компонент Health
            Health targetHealth = target.GetComponent<Health>();
            if (targetHealth == null)
                targetHealth = target.GetComponentInParent<Health>();

            return targetHealth != null;
        }

        void ApplyDamage(GameObject target, MeleeAttackData attack)
        {
            // Добавляем в список поврежденных
            hitTargetsThisAttack.Add(target);

            // Получаем компонент здоровья
            Health targetHealth = target.GetComponent<Health>();
            if (targetHealth == null)
                targetHealth = target.GetComponentInParent<Health>();

            if (targetHealth != null)
            {
                // Наносим урон
                targetHealth.TakeDamage(attack.damage, Owner);

                // Эффект попадания
                SpawnHitEffect(target.transform.position, attack);

                // Звук попадания
                if (attack.hitSound != null)
                    AudioUtility.CreateSFX(attack.hitSound, target.transform.position, AudioUtility.AudioGroups.Impact, 0f);

                // Отбрасывание
                ApplyKnockback(target, attack);

                // Оглушение (если есть компонент)

                OnTargetHit?.Invoke(target);

                Debug.Log($"[MeleeWeapon] Попадание по {target.name}, урон: {attack.damage}");
            }
        }

        void SpawnHitEffect(Vector3 position, MeleeAttackData attack)
        {
            if (attack.hitEffectPrefab != null)
            {
                Vector3 effectDirection = (position - AttackPoint.position).normalized;
                GameObject effect = Instantiate(attack.hitEffectPrefab, position, Quaternion.LookRotation(effectDirection));
                Destroy(effect, 3f);
            }
        }

        void ApplyKnockback(GameObject target, MeleeAttackData attack)
        {
            if (attack.knockbackForce <= 0) return;

            Rigidbody targetRb = target.GetComponent<Rigidbody>();
            if (targetRb != null)
            {
                Vector3 knockbackDirection = (target.transform.position - AttackPoint.position).normalized;
                targetRb.AddForce(knockbackDirection * attack.knockbackForce, ForceMode.Impulse);
            }
        }

        void StartWindupVisualEffect()
        {
            if (windupMaterial != null && weaponRenderers != null)
            {
                foreach (var renderer in weaponRenderers)
                {
                    renderer.material = windupMaterial;
                }
            }
        }

        void StopWindupVisualEffect()
        {
            if (originalMaterials != null && weaponRenderers != null)
            {
                for (int i = 0; i < weaponRenderers.Length && i < originalMaterials.Length; i++)
                {
                    weaponRenderers[i].material = originalMaterials[i];
                }
            }
        }

        void UpdateWindupEffects()
        {
            // Можно добавить пульсацию материала или другие эффекты
            var attack = attacks[currentAttackIndex];
            float windupProgress = stateTimer / attack.windupTime;

            // Пример: изменение интенсивности свечения
            if (windupMaterial != null)
            {
                float intensity = Mathf.PingPong(Time.time * 3f, 1f) * windupProgress;
                windupMaterial.SetFloat("_EmissionIntensity", intensity);
            }
        }

        void OnDrawGizmosSelected()
        {
            if (!showAttackGizmos || AttackPoint == null || attacks == null) return;

            var attack = attacks[currentAttackIndex];
            if (attack == null) return;

            // Радиус атаки (цвет зависит от состояния)
            if (currentState == AttackState.Active)
                Gizmos.color = Color.red;
            else if (CanAttack)
                Gizmos.color = Color.green;
            else
                Gizmos.color = Color.yellow;

            Gizmos.DrawWireSphere(AttackPoint.position, attack.range);

            // Конус атаки
            Gizmos.color = currentState == AttackState.Active ? Color.red : Color.cyan;
            Vector3 forward = AttackPoint.forward * attack.range;
            Vector3 right = Quaternion.Euler(0, attack.attackAngle / 2f, 0) * forward;
            Vector3 left = Quaternion.Euler(0, -attack.attackAngle / 2f, 0) * forward;

            Gizmos.DrawRay(AttackPoint.position, forward);
            Gizmos.DrawRay(AttackPoint.position, right);
            Gizmos.DrawRay(AttackPoint.position, left);

            // Дуга атаки
            for (int i = 0; i <= 10; i++)
            {
                float angle = Mathf.Lerp(-attack.attackAngle / 2f, attack.attackAngle / 2f, i / 10f);
                Vector3 direction = Quaternion.Euler(0, angle, 0) * forward;
                Gizmos.DrawRay(AttackPoint.position, direction);
            }

            // Индикатор кулдауна
            if (!CanAttack && currentState == AttackState.Ready)
            {
                float remainingCooldown = GetRemainingCooldown();
                float cooldownProgress = 1f - (remainingCooldown / GetCurrentAttackCooldown());

                // Рисуем дугу прогресса кулдауна
                Gizmos.color = Color.red;
                Vector3 upOffset = Vector3.up * 0.5f;
                Vector3 center = AttackPoint.position + upOffset;

                // Показываем прогресс кулдауна как дугу
                for (int i = 0; i < Mathf.FloorToInt(cooldownProgress * 20); i++)
                {
                    float angle = (i / 20f) * 360f;
                    Vector3 point = center + new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad), 0, Mathf.Cos(angle * Mathf.Deg2Rad)) * 0.3f;
                    Gizmos.DrawWireCube(point, Vector3.one * 0.05f);
                }
            }
        }

        /// <summary>
        /// Вспомогательный метод для отрисовки сферы в Scene view
        /// </summary>
        public static void Debug_DrawWireSphere(Vector3 center, float radius, Color color, float duration)
        {
            float angle = 0f;
            Vector3 lastPoint = Vector3.zero;
            Vector3 thisPoint = Vector3.zero;

            for (int i = 0; i < 61; i++)
            {
                thisPoint.x = Mathf.Sin(Mathf.Deg2Rad * angle) * radius;
                thisPoint.z = Mathf.Cos(Mathf.Deg2Rad * angle) * radius;
                if (i > 0)
                {
                    Debug.DrawLine(center + lastPoint, center + thisPoint, color, duration);
                }
                lastPoint = thisPoint;
                angle += 6f;
            }
        }

        /// <summary>
        /// Простое событие нанесения урона (для Animation Events)
        /// </summary>
        public void DealDamage()
        {
            Debug.Log("[MeleeWeapon] 💥 Animation Event: УРОН!");

            // Если не используем Animation Events, игнорируем
            if (!useAnimationEvents)
            {
                Debug.LogWarning("[MeleeWeapon] DealDamage вызван, но useAnimationEvents = false");
                return;
            }

            var attack = attacks[currentAttackIndex];

            // Находим цели и наносим урон
            Collider[] potentialTargets = Physics.OverlapSphere(AttackPoint.position, attack.range, damageableLayers);

            Debug.Log($"[MeleeWeapon] Найдено потенциальных целей: {potentialTargets.Length}");

            foreach (var target in potentialTargets)
            {
                // Пропускаем уже поврежденные цели в этой атаке
                if (hitTargetsThisAttack.Contains(target.gameObject))
                {
                    Debug.Log($"[MeleeWeapon] Пропускаем уже поврежденную цель: {target.name}");
                    continue;
                }

                // Проверяем угол атаки
                if (!IsInAttackAngle(target.transform.position, attack))
                {
                    Debug.Log($"[MeleeWeapon] Цель {target.name} вне угла атаки");
                    continue;
                }

                // Проверяем линию обзора
                if (!HasLineOfSight(target.transform.position))
                {
                    Debug.Log($"[MeleeWeapon] Нет линии обзора к {target.name}");
                    continue;
                }

                // Проверяем валидность цели
                if (!IsValidTarget(target.gameObject, attack))
                {
                    Debug.Log($"[MeleeWeapon] Цель {target.name} не валидна");
                    continue;
                }

                // 🎯 НАНОСИМ УРОН!
                ApplyDamage(target.gameObject, attack);
            }
        }

        /// <summary>
        /// Простой запуск атаки для Animation Events
        /// </summary>
        public bool SimpleAttack(int attackIndex = 0)
        {
            if (!CanAttack || attackIndex >= attacks.Length)
            {
                Debug.Log($"[MeleeWeapon] SimpleAttack заблокирована. CanAttack: {CanAttack}");
                return false;
            }

            currentAttackIndex = attackIndex;
            lastAttackTime = Time.time;
            hitTargetsThisAttack.Clear();

            // Просто запускаем анимацию, урон нанесется через Animation Event
            if (enemyAnimator != null)
            {
                enemyAnimator.SetTrigger($"Attack{attackIndex}");
                Debug.Log($"[MeleeWeapon] 🚀 Простая атака запущена: Attack{attackIndex}");
                return true;
            }

            Debug.LogWarning("[MeleeWeapon] Enemy Animator не назначен!");
            return false;
        }

        /// <summary>
        /// Метод для быстрого тестирования атак в редакторе
        /// </summary>
        [ContextMenu("Test Attack")]
        public void TestAttack()
        {
            if (Application.isPlaying)
            {
                if (useAnimationEvents)
                {
                    SimpleAttack();
                }
                else
                {
                    ForceStartAttack();
                }
            }
            else
            {
                Debug.Log("[MeleeWeapon] Тестирование атак доступно только в Play Mode");
            }
        }

        /// <summary>
        /// Контекстное меню для диагностики настроек
        /// </summary>
        [ContextMenu("Debug Settings")]
        public void DebugSettings()
        {
            Debug.Log("=== НАСТРОЙКИ MELEE WEAPON ===");
            Debug.Log($"Use Animation Events: {useAnimationEvents}");
            Debug.Log($"Enemy Animator: {(enemyAnimator != null ? "✅ Назначен" : "❌ НЕ НАЗНАЧЕН")}");
            Debug.Log($"Attack Point: {(AttackPoint != null ? "✅ Назначен" : "❌ НЕ НАЗНАЧЕН")}");
            Debug.Log($"Attacks Count: {attacks.Length}");
            Debug.Log($"Can Attack: {CanAttack}");
            Debug.Log($"Current State: {currentState}");
            Debug.Log($"Cooldown Remaining: {GetRemainingCooldown():F1} сек");
            Debug.Log("===============================");
        }
    }
}