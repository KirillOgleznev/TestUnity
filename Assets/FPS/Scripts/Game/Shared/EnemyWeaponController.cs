using System;
using UnityEngine;
using UnityEngine.Events;
using Unity.FPS.Game; // Добавили для интерфейса IWeaponController

namespace Unity.FPS.Game
{
    [RequireComponent(typeof(AudioSource))]
    public class EnemyWeaponController : MonoBehaviour, Unity.FPS.Game.IWeaponController
    {
        [Header("Basic Info")]
        [Tooltip("Название оружия")]
        public string WeaponName;

        [Header("Internal References")]
        [Tooltip("Корневой объект оружия")]
        public GameObject WeaponRoot;

        [Tooltip("Дуло оружия, откуда выстреливаются снаряды")]
        public Transform WeaponMuzzle;

        [Header("Shoot Parameters")]
        [Tooltip("Тип оружия влияет на то, как оно стреляет")]
        public WeaponShootType ShootType;

        [Tooltip("Префаб снаряда")]
        public ProjectileBase ProjectilePrefab;

        [Tooltip("Минимальная длительность между двумя выстрелами")]
        public float DelayBetweenShots = 0.5f;

        [Tooltip("Угол конуса разброса пуль (0 = без разброса)")]
        public float BulletSpreadAngle = 0f;

        [Tooltip("Количество пуль за выстрел")]
        public int BulletsPerShot = 1;

        [Header("Ammo Parameters")]
        [Tooltip("Максимальное количество боеприпасов")]
        public int MaxAmmo = 30;

        [Tooltip("Автоматическая перезарядка")]
        public bool AutomaticReload = true;

        [Tooltip("Скорость перезарядки в секунду")]
        public float AmmoReloadRate = 10f;

        [Tooltip("Задержка перед началом перезарядки")]
        public float AmmoReloadDelay = 2f;

        [Header("Charging (для заряжаемого оружия)")]
        [Tooltip("Автовыстрел при полном заряде")]
        public bool AutomaticReleaseOnCharged;

        [Tooltip("Время зарядки до максимума")]
        public float MaxChargeDuration = 2f;

        [Tooltip("Боеприпасы для начала зарядки")]
        public float AmmoUsedOnStartCharge = 1f;

        [Tooltip("Расход боеприпасов во время зарядки")]
        public float AmmoUsageRateWhileCharging = 1f;

        [Header("Visual & Audio")]
        [Tooltip("Префаб вспышки дула")]
        public GameObject MuzzleFlashPrefab;

        [Tooltip("Звук выстрела")]
        public AudioClip ShootSfx;

        // События
        public UnityAction OnShoot;
        public event Action OnShootProcessed;

        // Приватные переменные
        private float m_CurrentAmmo;
        private float m_LastTimeShot = Mathf.NegativeInfinity;
        private AudioSource m_ShootAudioSource;
        private Vector3 m_LastMuzzlePosition;

        // Публичные свойства
        public GameObject Owner { get; set; }
        public GameObject SourcePrefab { get; set; } // Для интерфейса
        public bool IsCharging { get; private set; }
        public float CurrentAmmoRatio { get; private set; }
        public bool IsWeaponActive { get; private set; }
        public bool IsCooling { get; private set; }
        public float CurrentCharge { get; private set; }
        public Vector3 MuzzleWorldVelocity { get; private set; }
        public float LastChargeTriggerTimestamp { get; private set; }

        // Методы доступа
        public int GetCurrentAmmo() => Mathf.FloorToInt(m_CurrentAmmo);
        public float GetAmmoNeededToShoot() => ShootType != WeaponShootType.Charge ? 1f : Mathf.Max(1f, AmmoUsedOnStartCharge);

        void Awake()
        {
            m_CurrentAmmo = MaxAmmo;
            m_LastMuzzlePosition = WeaponMuzzle.position;
            m_ShootAudioSource = GetComponent<AudioSource>();

            // Инициализация для интерфейса
            Owner = gameObject;
            SourcePrefab = null; // Можете установить нужный префаб
        }

        void Update()
        {
            UpdateAmmo();
            UpdateCharge();
            UpdateMuzzleVelocity();
        }

        void UpdateAmmo()
        {
            // Автоматическая перезарядка
            if (AutomaticReload &&
                m_LastTimeShot + AmmoReloadDelay < Time.time &&
                m_CurrentAmmo < MaxAmmo &&
                !IsCharging)
            {
                m_CurrentAmmo += AmmoReloadRate * Time.deltaTime;
                m_CurrentAmmo = Mathf.Clamp(m_CurrentAmmo, 0, MaxAmmo);
                IsCooling = true;
            }
            else
            {
                IsCooling = false;
            }

            // Обновление соотношения боеприпасов
            CurrentAmmoRatio = MaxAmmo == Mathf.Infinity ? 1f : m_CurrentAmmo / MaxAmmo;
        }

        void UpdateCharge()
        {
            if (IsCharging && CurrentCharge < 1f)
            {
                float chargeLeft = 1f - CurrentCharge;
                float chargeAdded = MaxChargeDuration <= 0f ? chargeLeft : (1f / MaxChargeDuration) * Time.deltaTime;
                chargeAdded = Mathf.Clamp(chargeAdded, 0f, chargeLeft);

                float ammoRequired = chargeAdded * AmmoUsageRateWhileCharging;
                if (ammoRequired <= m_CurrentAmmo)
                {
                    UseAmmo(ammoRequired);
                    CurrentCharge = Mathf.Clamp01(CurrentCharge + chargeAdded);
                }
            }
        }

        void UpdateMuzzleVelocity()
        {
            if (Time.deltaTime > 0)
            {
                MuzzleWorldVelocity = (WeaponMuzzle.position - m_LastMuzzlePosition) / Time.deltaTime;
                m_LastMuzzlePosition = WeaponMuzzle.position;
            }
        }

        public void ShowWeapon(bool show)
        {
            if (WeaponRoot != null)
                WeaponRoot.SetActive(show);
            IsWeaponActive = show;
        }

        public void UseAmmo(float amount)
        {
            m_CurrentAmmo = Mathf.Clamp(m_CurrentAmmo - amount, 0f, MaxAmmo);
            m_LastTimeShot = Time.time;
        }

        // Обработка ввода (для ИИ или унифицированного управления)
        public bool HandleShootInputs(bool inputDown, bool inputHeld, bool inputUp)
        {
            switch (ShootType)
            {
                case WeaponShootType.Manual:
                    if (inputDown)
                    {
                        return TryShoot();
                    }
                    return false;

                case WeaponShootType.Automatic:
                    if (inputHeld)
                    {
                        return TryShoot();
                    }
                    return false;

                case WeaponShootType.Charge:
                    if (inputHeld)
                    {
                        TryBeginCharge();
                    }

                    // Проверяем отпускание или автоматический выстрел при полном заряде
                    if (inputUp || (AutomaticReleaseOnCharged && CurrentCharge >= 1f))
                    {
                        return TryReleaseCharge();
                    }
                    return false;

                default:
                    return false;
            }
        }

        // Простая стрельба для ИИ
        public bool TryShoot()
        {
            if (m_CurrentAmmo >= 1f && m_LastTimeShot + DelayBetweenShots < Time.time)
            {
                HandleShoot();
                m_CurrentAmmo -= 1f;
                return true;
            }
            return false;
        }

        public bool TryBeginCharge()
        {
            if (!IsCharging &&
                m_CurrentAmmo >= AmmoUsedOnStartCharge &&
                m_LastTimeShot + DelayBetweenShots < Time.time)
            {
                UseAmmo(AmmoUsedOnStartCharge);
                LastChargeTriggerTimestamp = Time.time;
                IsCharging = true;
                return true;
            }
            return false;
        }

        public bool TryReleaseCharge()
        {
            if (IsCharging)
            {
                HandleShoot();
                CurrentCharge = 0f;
                IsCharging = false;
                return true;
            }
            return false;
        }

        void HandleShoot()
        {
            int bulletsToShoot = ShootType == WeaponShootType.Charge
                ? Mathf.CeilToInt(CurrentCharge * BulletsPerShot)
                : BulletsPerShot;

            // Создаем снаряды
            for (int i = 0; i < bulletsToShoot; i++)
            {
                Vector3 shotDirection = GetShotDirectionWithinSpread(WeaponMuzzle);
                ProjectileBase newProjectile = Instantiate(ProjectilePrefab, WeaponMuzzle.position,
                    Quaternion.LookRotation(shotDirection));
                newProjectile.Shoot(this);
            }

            // Вспышка дула
            if (MuzzleFlashPrefab != null)
            {
                GameObject muzzleFlash = Instantiate(MuzzleFlashPrefab, WeaponMuzzle.position, WeaponMuzzle.rotation);
                Destroy(muzzleFlash, 2f);
            }

            // Звук выстрела
            if (ShootSfx != null)
            {
                m_ShootAudioSource.PlayOneShot(ShootSfx);
            }

            m_LastTimeShot = Time.time;

            // События
            OnShoot?.Invoke();
            OnShootProcessed?.Invoke();
        }

        public Vector3 GetShotDirectionWithinSpread(Transform shootTransform)
        {
            float spreadAngleRatio = BulletSpreadAngle / 180f;
            return Vector3.Slerp(shootTransform.forward, UnityEngine.Random.insideUnitSphere, spreadAngleRatio);
        }

        // Простые методы для ИИ
        public bool CanShoot() => m_CurrentAmmo >= 1f && m_LastTimeShot + DelayBetweenShots < Time.time;
        public bool HasAmmo() => m_CurrentAmmo > 0;
        public bool IsFullyCharged() => CurrentCharge >= 1f;

        #region IWeaponController Implementation
        // Реализация интерфейса для полей
        Transform IWeaponController.WeaponMuzzle
        {
            get { return WeaponMuzzle; }
        }

        GameObject IWeaponController.WeaponRoot
        {
            get { return WeaponRoot; }
        }

        Transform IWeaponController.transform
        {
            get { return transform; }
        }

        // Методы интерфейса (уже существуют, просто явная реализация)
        void IWeaponController.ShowWeapon(bool show)
        {
            ShowWeapon(show);
        }

        bool IWeaponController.HandleShootInputs(bool inputDown, bool inputHeld, bool inputUp)
        {
            return HandleShootInputs(inputDown, inputHeld, inputUp);
        }

        int IWeaponController.GetCurrentAmmo()
        {
            return GetCurrentAmmo();
        }
        #endregion
    }
}