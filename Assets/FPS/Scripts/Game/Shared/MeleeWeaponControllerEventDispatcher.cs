using UnityEngine;
using UnityEngine.Events;
using Unity.FPS.AI;

public class MeleeWeaponControllerEventDispatcher : MonoBehaviour
{
    void Start()
    {
        // Автоматически находит все оружия в детях
        // НИЧЕГО настраивать не нужно!
    }
    

    // Этот метод ВИДЯТ Animation Events
    public void DealDamage()
    {
        // Автоматически находит MeleeWeaponController и вызывает его DealDamage()
        var weapon = GetComponentInChildren<MeleeWeaponController>();
        weapon?.DealDamage();
    }
}