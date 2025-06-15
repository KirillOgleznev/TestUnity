using UnityEngine;
using UnityEngine.Events;

namespace Unity.FPS.Game
{
    // Интерфейс для всех типов оружия
    public interface IWeapon
    {
        string WeaponName { get; }
        Sprite WeaponIcon { get; }
        GameObject Owner { get; set; }
        bool IsWeaponActive { get; }
        GameObject WeaponRoot { get; }

        UnityAction OnAttack { get; set; }

        void ShowWeapon(bool show);
        bool HandleShootInputs(bool inputDown, bool inputHeld, bool inputUp);
        void OrientTowards(Vector3 targetPosition);
    }

    // Абстрактный базовый класс для оружия
    [RequireComponent(typeof(AudioSource))]
    public abstract class BaseWeapon : MonoBehaviour, IWeapon
    {
        [Header("Weapon Information")]
        [Tooltip("The name that will be displayed in the UI for this weapon")]
        public string WeaponName = "Base Weapon";

        [Tooltip("The image that will be displayed in the UI for this weapon")]
        public Sprite WeaponIcon;

        [Header("Internal References")]
        [Tooltip("The root object for the weapon")]
        public GameObject WeaponRoot;

        [Header("Audio")]
        [Tooltip("Sound played when changing to this weapon")]
        public AudioClip ChangeWeaponSfx;

        public UnityAction OnAttack { get; set; }
        public GameObject Owner { get; set; }
        public bool IsWeaponActive { get; protected set; }

        protected AudioSource m_AudioSource;

        // Имплементация интерфейса
        string IWeapon.WeaponName => WeaponName;
        Sprite IWeapon.WeaponIcon => WeaponIcon;
        GameObject IWeapon.WeaponRoot => WeaponRoot;

        protected virtual void Awake()
        {
            m_AudioSource = GetComponent<AudioSource>();
            if (m_AudioSource == null)
            {
                Debug.LogError($"BaseWeapon on {gameObject.name} requires an AudioSource component!");
            }
        }

        public virtual void ShowWeapon(bool show)
        {
            if (WeaponRoot != null)
                WeaponRoot.SetActive(show);

            if (show && ChangeWeaponSfx && m_AudioSource != null)
            {
                m_AudioSource.PlayOneShot(ChangeWeaponSfx);
            }

            IsWeaponActive = show;
        }

        public abstract bool HandleShootInputs(bool inputDown, bool inputHeld, bool inputUp);
        public abstract void OrientTowards(Vector3 targetPosition);
    }
}

namespace Unity.FPS.AI
{
    using Unity.FPS.Game;

    // БЛИЖНЕЕ ОРУЖИЕ
    public class MeleeWeapon : BaseWeapon
    {
        [Header("Melee Attack Parameters")]
        [Tooltip("Damage dealt by melee attack")]
        public float MeleeDamage = 30f;

        [Tooltip("Range of melee attack")]
        public float MeleeRange = 2f;

        [Tooltip("Minimum duration between two attacks")]
        public float DelayBetweenAttacks = 1f;

        [Tooltip("Layers that can be damaged by melee attack")]
        public LayerMask DamageableLayers = -1;

        [Tooltip("Angle for the cone attack (0 = direct hit only)")]
        public float AttackAngle = 45f;

        [Tooltip("Point from which melee attack originates")]
        public Transform AttackPoint;

        [Header("Melee Audio & Visual")]
        [Tooltip("Optional weapon animator")]
        public Animator WeaponAnimator;

        [Tooltip("Sound played when attacking")]
        public AudioClip AttackSfx;

        [Tooltip("Sound played when hitting target")]
        public AudioClip HitSfx;

        [Tooltip("Effect spawned on successful hit")]
        public GameObject HitEffectPrefab;

        private float m_LastTimeAttacked = Mathf.NegativeInfinity;
        const string k_AnimAttackParameter = "Attack";

        protected override void Awake()
        {
            base.Awake();
            if (AttackPoint == null)
                AttackPoint = transform;
        }

        public override bool HandleShootInputs(bool inputDown, bool inputHeld, bool inputUp)
        {
            // Милишное оружие атакует по нажатию или удержанию
            if (inputDown || inputHeld)
            {
                return TryMeleeAttack();
            }
            return false;
        }

        public override void OrientTowards(Vector3 targetPosition)
        {
            // Поворачиваем милишное оружие к цели
            Vector3 direction = (targetPosition - transform.position).normalized;
            if (direction.sqrMagnitude > 0.01f)
            {
                transform.forward = direction;
            }
        }

        public bool TryMeleeAttack()
        {
            if (Time.time - m_LastTimeAttacked < DelayBetweenAttacks)
                return false;

            bool didHit = PerformMeleeAttack();

            if (didHit)
            {
                m_LastTimeAttacked = Time.time;

                // Звук атаки
                if (AttackSfx && m_AudioSource != null)
                    m_AudioSource.PlayOneShot(AttackSfx);

                // Анимация
                if (WeaponAnimator)
                    WeaponAnimator.SetTrigger(k_AnimAttackParameter);

                OnAttack?.Invoke();
            }

            return didHit;
        }

        private bool PerformMeleeAttack()
        {
            Vector3 attackOrigin = AttackPoint.position;
            Vector3 attackDirection = AttackPoint.forward;

            Collider[] hitColliders = Physics.OverlapSphere(attackOrigin, MeleeRange, DamageableLayers);
            bool hitSomething = false;

            foreach (Collider hitCollider in hitColliders)
            {
                if (hitCollider.transform.root == transform.root)
                    continue;

                // Проверка угла атаки
                Vector3 directionToTarget = (hitCollider.transform.position - attackOrigin).normalized;
                float angleToTarget = Vector3.Angle(attackDirection, directionToTarget);

                if (angleToTarget > AttackAngle / 2f)
                    continue;

                // Нанесение урона
                Health targetHealth = hitCollider.GetComponent<Health>();
                if (targetHealth == null)
                    targetHealth = hitCollider.GetComponentInParent<Health>();

                if (targetHealth != null)
                {
                    targetHealth.TakeDamage(MeleeDamage, Owner);
                    hitSomething = true;

                    // Звук попадания
                    if (HitSfx)
                        AudioUtility.CreateSFX(HitSfx, hitCollider.transform.position,
                            AudioUtility.AudioGroups.Impact, 0f);

                    // Эффект попадания
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
                // Радиус атаки
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(AttackPoint.position, MeleeRange);

                // Конус атаки
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

    // ДАЛЬНЕЕ ОРУЖИЕ (адаптер для WeaponController)
    public class RangedWeapon : BaseWeapon
    {
        [Header("Ranged Weapon")]
        [Tooltip("The underlying WeaponController component")]
        public WeaponController WeaponController;

        protected override void Awake()
        {
            base.Awake();

            // Автоматически находим WeaponController если не назначен
            if (WeaponController == null)
            {
                WeaponController = GetComponent<WeaponController>();
            }

            if (WeaponController == null)
            {
                Debug.LogError($"RangedWeapon on {gameObject.name} requires a WeaponController component!");
                return;
            }

            // Синхронизируем свойства с WeaponController
            if (string.IsNullOrEmpty(WeaponName))
                WeaponName = WeaponController.WeaponName;

            if (WeaponIcon == null)
                WeaponIcon = WeaponController.WeaponIcon;

            if (WeaponRoot == null)
                WeaponRoot = WeaponController.WeaponRoot;
        }

        public override bool HandleShootInputs(bool inputDown, bool inputHeld, bool inputUp)
        {
            if (WeaponController != null)
            {
                bool didShoot = WeaponController.HandleShootInputs(inputDown, inputHeld, inputUp);

                // Передаем событие OnAttack
                if (didShoot)
                {
                    OnAttack?.Invoke();
                }

                return didShoot;
            }
            return false;
        }

        public override void OrientTowards(Vector3 targetPosition)
        {
            if (WeaponController?.WeaponRoot != null)
            {
                // Поворачиваем дальнее оружие к цели
                Vector3 weaponForward = (targetPosition - WeaponController.WeaponRoot.transform.position).normalized;
                WeaponController.transform.forward = weaponForward;
            }
        }

        public override void ShowWeapon(bool show)
        {
            if (WeaponController != null)
            {
                WeaponController.ShowWeapon(show);
            }
            base.ShowWeapon(show);
        }

        // Дополнительные методы для доступа к WeaponController
        public WeaponController GetWeaponController() => WeaponController;

        public float GetCurrentAmmoRatio()
        {
            return WeaponController?.CurrentAmmoRatio ?? 1f;
        }

        public bool IsReloading()
        {
            return WeaponController?.IsReloading ?? false;
        }
    }
}