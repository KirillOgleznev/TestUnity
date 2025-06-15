using System;
using UnityEngine;
using UnityEngine.Events;
using Unity.FPS.Game; // �������� ��� ���������� IWeaponController

namespace Unity.FPS.Game
{
    [RequireComponent(typeof(AudioSource))]
    public class EnemyWeaponController : MonoBehaviour, Unity.FPS.Game.IWeaponController
    {
        [Header("Basic Info")]
        [Tooltip("�������� ������")]
        public string WeaponName;

        [Header("Internal References")]
        [Tooltip("�������� ������ ������")]
        public GameObject WeaponRoot;

        [Tooltip("���� ������, ������ �������������� �������")]
        public Transform WeaponMuzzle;

        [Header("Shoot Parameters")]
        [Tooltip("��� ������ ������ �� ��, ��� ��� ��������")]
        public WeaponShootType ShootType;

        [Tooltip("������ �������")]
        public ProjectileBase ProjectilePrefab;

        [Tooltip("����������� ������������ ����� ����� ����������")]
        public float DelayBetweenShots = 0.5f;

        [Tooltip("���� ������ �������� ���� (0 = ��� ��������)")]
        public float BulletSpreadAngle = 0f;

        [Tooltip("���������� ���� �� �������")]
        public int BulletsPerShot = 1;

        [Header("Ammo Parameters")]
        [Tooltip("������������ ���������� �����������")]
        public int MaxAmmo = 30;

        [Tooltip("�������������� �����������")]
        public bool AutomaticReload = true;

        [Tooltip("�������� ����������� � �������")]
        public float AmmoReloadRate = 10f;

        [Tooltip("�������� ����� ������� �����������")]
        public float AmmoReloadDelay = 2f;

        [Header("Charging (��� ����������� ������)")]
        [Tooltip("����������� ��� ������ ������")]
        public bool AutomaticReleaseOnCharged;

        [Tooltip("����� ������� �� ���������")]
        public float MaxChargeDuration = 2f;

        [Tooltip("���������� ��� ������ �������")]
        public float AmmoUsedOnStartCharge = 1f;

        [Tooltip("������ ����������� �� ����� �������")]
        public float AmmoUsageRateWhileCharging = 1f;

        [Header("Visual & Audio")]
        [Tooltip("������ ������� ����")]
        public GameObject MuzzleFlashPrefab;

        [Tooltip("���� ��������")]
        public AudioClip ShootSfx;

        // �������
        public UnityAction OnShoot;
        public event Action OnShootProcessed;

        // ��������� ����������
        private float m_CurrentAmmo;
        private float m_LastTimeShot = Mathf.NegativeInfinity;
        private AudioSource m_ShootAudioSource;
        private Vector3 m_LastMuzzlePosition;

        // ��������� ��������
        public GameObject Owner { get; set; }
        public GameObject SourcePrefab { get; set; } // ��� ����������
        public bool IsCharging { get; private set; }
        public float CurrentAmmoRatio { get; private set; }
        public bool IsWeaponActive { get; private set; }
        public bool IsCooling { get; private set; }
        public float CurrentCharge { get; private set; }
        public Vector3 MuzzleWorldVelocity { get; private set; }
        public float LastChargeTriggerTimestamp { get; private set; }

        // ������ �������
        public int GetCurrentAmmo() => Mathf.FloorToInt(m_CurrentAmmo);
        public float GetAmmoNeededToShoot() => ShootType != WeaponShootType.Charge ? 1f : Mathf.Max(1f, AmmoUsedOnStartCharge);

        void Awake()
        {
            m_CurrentAmmo = MaxAmmo;
            m_LastMuzzlePosition = WeaponMuzzle.position;
            m_ShootAudioSource = GetComponent<AudioSource>();

            // ������������� ��� ����������
            Owner = gameObject;
            SourcePrefab = null; // ������ ���������� ������ ������
        }

        void Update()
        {
            UpdateAmmo();
            UpdateCharge();
            UpdateMuzzleVelocity();
        }

        void UpdateAmmo()
        {
            // �������������� �����������
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

            // ���������� ����������� �����������
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

        // ��������� ����� (��� �� ��� ���������������� ����������)
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

                    // ��������� ���������� ��� �������������� ������� ��� ������ ������
                    if (inputUp || (AutomaticReleaseOnCharged && CurrentCharge >= 1f))
                    {
                        return TryReleaseCharge();
                    }
                    return false;

                default:
                    return false;
            }
        }

        // ������� �������� ��� ��
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

            // ������� �������
            for (int i = 0; i < bulletsToShoot; i++)
            {
                Vector3 shotDirection = GetShotDirectionWithinSpread(WeaponMuzzle);
                ProjectileBase newProjectile = Instantiate(ProjectilePrefab, WeaponMuzzle.position,
                    Quaternion.LookRotation(shotDirection));
                newProjectile.Shoot(this);
            }

            // ������� ����
            if (MuzzleFlashPrefab != null)
            {
                GameObject muzzleFlash = Instantiate(MuzzleFlashPrefab, WeaponMuzzle.position, WeaponMuzzle.rotation);
                Destroy(muzzleFlash, 2f);
            }

            // ���� ��������
            if (ShootSfx != null)
            {
                m_ShootAudioSource.PlayOneShot(ShootSfx);
            }

            m_LastTimeShot = Time.time;

            // �������
            OnShoot?.Invoke();
            OnShootProcessed?.Invoke();
        }

        public Vector3 GetShotDirectionWithinSpread(Transform shootTransform)
        {
            float spreadAngleRatio = BulletSpreadAngle / 180f;
            return Vector3.Slerp(shootTransform.forward, UnityEngine.Random.insideUnitSphere, spreadAngleRatio);
        }

        // ������� ������ ��� ��
        public bool CanShoot() => m_CurrentAmmo >= 1f && m_LastTimeShot + DelayBetweenShots < Time.time;
        public bool HasAmmo() => m_CurrentAmmo > 0;
        public bool IsFullyCharged() => CurrentCharge >= 1f;

        #region IWeaponController Implementation
        // ���������� ���������� ��� �����
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

        // ������ ���������� (��� ����������, ������ ����� ����������)
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