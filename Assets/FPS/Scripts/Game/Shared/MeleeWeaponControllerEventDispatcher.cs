using UnityEngine;
using UnityEngine.Events;
using Unity.FPS.AI;

public class MeleeWeaponControllerEventDispatcher : MonoBehaviour
{
    void Start()
    {
        // ������������� ������� ��� ������ � �����
        // ������ ����������� �� �����!
    }
    

    // ���� ����� ����� Animation Events
    public void DealDamage()
    {
        // ������������� ������� MeleeWeaponController � �������� ��� DealDamage()
        var weapon = GetComponentInChildren<MeleeWeaponController>();
        weapon?.DealDamage();
    }
}