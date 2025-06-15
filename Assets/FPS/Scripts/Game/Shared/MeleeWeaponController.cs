using System;
using Unity.FPS.Game;
using UnityEngine;
using UnityEngine.Events;

namespace Unity.FPS.AI
{
    [RequireComponent(typeof(AudioSource))]
    public class MeleeWeaponController : MonoBehaviour
    {
        [Header("Melee Weapon Information")]
        [Tooltip("The name that will be displayed in the UI for this weapon")]
        public string MeleeWeaponName = "Claws";

        [Tooltip("The image that will be displayed in the UI for this weapon")]
        public Sprite MeleeWeaponIcon;

        [Header("Internal References")]
        [Tooltip("The root object for the weapon, this is what will be deactivated when the weapon isn't active")]
        public GameObject MeleeWeaponRoot;

        [Tooltip("Point from which melee attack originates")]
        public Transform AttackPoint;

        [Header("Melee Attack Parameters")]
        [Tooltip("Damage dealt by melee attack")]
        public float MeleeDamage = 30f;

        [Tooltip("Range of melee attack")]
        public float MeleeRange = 2f;

        [Tooltip("Minimum duration between two attacks")]
        public float DelayBetweenAttacks = 1f;

        [Tooltip("Layers that can be damaged by melee attack")]
        public LayerMask DamageableLayers = -1;

        [Tooltip("Angle for the cone in which the attack will hit (0 means direct hit only)")]
        public float AttackAngle = 45f;

        [Header("Audio & Visual")]
        [Tooltip("Optional weapon animator for attack animations")]
        public Animator MeleeAnimator;

        [Tooltip("Sound played when attacking")]
        public AudioClip AttackSfx;

        [Tooltip("Sound played when hitting target")]
        public AudioClip HitSfx;

        [Tooltip("Sound played when changing to this weapon")]
        public AudioClip ChangeWeaponSfx;

        [Tooltip("Effect spawned on successful hit")]
        public GameObject HitEffectPrefab;

        public UnityAction OnAttack;
        public event Action OnAttackProcessed;

        public GameObject Owner { get; set; }
        public bool IsWeaponActive { get; private set; }

        private float m_LastTimeAttacked = Mathf.NegativeInfinity;
        private AudioSource m_AudioSource;

        const string k_AnimAttackParameter = "Attack";

        void Awake()
        {
            m_AudioSource = GetComponent<AudioSource>();
            if (m_AudioSource == null)
            {
                Debug.LogError($"MeleeWeaponController on {gameObject.name} requires an AudioSource component!");
            }

            if (AttackPoint == null)
                AttackPoint = transform;
        }

        public void ShowWeapon(bool show)
        {
            if (MeleeWeaponRoot != null)
                MeleeWeaponRoot.SetActive(show);

            if (show && ChangeWeaponSfx && m_AudioSource != null)
            {
                m_AudioSource.PlayOneShot(ChangeWeaponSfx);
            }

            IsWeaponActive = show;
        }

        // Этот метод имитирует интерфейс WeaponController для совместимости
        public bool HandleShootInputs(bool inputDown, bool inputHeld, bool inputUp)
        {
            // Для милишного оружия атакуем только при нажатии
            if (inputDown || inputHeld)
            {
                return TryMeleeAttack();
            }

            return false;
        }

        public bool TryMeleeAttack()
        {
            // Проверяем кулдаун
            if (Time.time - m_LastTimeAttacked < DelayBetweenAttacks)
                return false;

            bool didHit = PerformMeleeAttack();

            if (didHit)
            {
                m_LastTimeAttacked = Time.time;

                // Играем звук атаки
                if (AttackSfx && m_AudioSource != null)
                    m_AudioSource.PlayOneShot(AttackSfx);

                // Запускаем анимацию
                if (MeleeAnimator)
                    MeleeAnimator.SetTrigger(k_AnimAttackParameter);

                OnAttack?.Invoke();
                OnAttackProcessed?.Invoke();
            }

            return didHit;
        }

        private bool PerformMeleeAttack()
        {
            Vector3 attackOrigin = AttackPoint.position;
            Vector3 attackDirection = AttackPoint.forward;

            // Находим все коллайдеры в радиусе атаки
            Collider[] hitColliders = Physics.OverlapSphere(attackOrigin, MeleeRange, DamageableLayers);

            bool hitSomething = false;

            foreach (Collider hitCollider in hitColliders)
            {
                // Пропускаем самого себя
                if (hitCollider.transform.root == transform.root)
                    continue;

                // Проверяем угол атаки
                Vector3 directionToTarget = (hitCollider.transform.position - attackOrigin).normalized;
                float angleToTarget = Vector3.Angle(attackDirection, directionToTarget);

                if (angleToTarget > AttackAngle / 2f)
                    continue;

                // Проверяем есть ли у цели компонент Health
                Health targetHealth = hitCollider.GetComponent<Health>();
                if (targetHealth == null)
                    targetHealth = hitCollider.GetComponentInParent<Health>();

                if (targetHealth != null)
                {
                    // Наносим урон
                    targetHealth.TakeDamage(MeleeDamage, Owner);
                    hitSomething = true;

                    // Играем звук попадания
                    if (HitSfx)
                        AudioUtility.CreateSFX(HitSfx, hitCollider.transform.position,
                            AudioUtility.AudioGroups.Impact, 0f);

                    // Создаем эффект попадания
                    if (HitEffectPrefab != null)
                    {
                        GameObject hitEffect = Instantiate(HitEffectPrefab,
                            hitCollider.transform.position,
                            Quaternion.LookRotation(-directionToTarget));
                        Destroy(hitEffect, 3f);
                    }

                    Debug.Log($"Melee attack hit {hitCollider.name} for {MeleeDamage} damage");
                }
            }

            return hitSomething;
        }

        void OnDrawGizmosSelected()
        {
            if (AttackPoint != null)
            {
                // Показываем радиус атаки
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(AttackPoint.position, MeleeRange);

                // Показываем конус атаки
                Gizmos.color = Color.yellow;
                Vector3 forward = AttackPoint.forward * MeleeRange;
                Vector3 right = Quaternion.Euler(0, AttackAngle / 2f, 0) * forward;
                Vector3 left = Quaternion.Euler(0, -AttackAngle / 2f, 0) * forward;

                Gizmos.DrawRay(AttackPoint.position, right);
                Gizmos.DrawRay(AttackPoint.position, left);
                Gizmos.DrawRay(AttackPoint.position, forward);
            }
        }
    }
}