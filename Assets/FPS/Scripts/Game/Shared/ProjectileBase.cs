using UnityEngine;
using UnityEngine.Events;

namespace Unity.FPS.Game
{
    public interface IWeaponController
    {
        // Основные свойства
        GameObject Owner { get; set; }
        GameObject SourcePrefab { get; set; }
        Vector3 MuzzleWorldVelocity { get; }
        float CurrentCharge { get; }
        bool IsCharging { get; }
        bool IsWeaponActive { get; }

        // Дополнительные свойства для ИИ
        Transform WeaponMuzzle { get; }
        GameObject WeaponRoot { get; }
        Transform transform { get; }

        // Методы для ИИ
        void ShowWeapon(bool show);
        bool HandleShootInputs(bool inputDown, bool inputHeld, bool inputUp);
        //bool CanShoot();
        int GetCurrentAmmo();
    }

    public abstract class ProjectileBase : MonoBehaviour
    {
        public GameObject Owner { get; private set; }
        public Vector3 InitialPosition { get; private set; }
        public Vector3 InitialDirection { get; private set; }
        public Vector3 InheritedMuzzleVelocity { get; private set; }
        public float InitialCharge { get; private set; }

        public UnityAction OnShoot;

        public void Shoot(IWeaponController controller)
        {
            Owner = controller.Owner;
            InitialPosition = transform.position;
            InitialDirection = transform.forward;
            InheritedMuzzleVelocity = controller.MuzzleWorldVelocity;
            InitialCharge = controller.CurrentCharge;

            OnShoot?.Invoke();
        }
    }
}