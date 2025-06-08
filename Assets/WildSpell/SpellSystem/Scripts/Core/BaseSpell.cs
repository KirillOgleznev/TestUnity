using UnityEngine;
using System.Collections.Generic;

public abstract class BaseSpell : MonoBehaviour, ISpell
{
    [Header("Base Spell Settings")]
    public string spellName;
    public List<ElementType> requiredElements;
    public float castTime = 1f;
    public float cooldown = 0f;

    protected bool isActive = false;
    protected float lastCastTime = 0f;

    // ������� ������ ������������
    protected Vector3 targetDirection;
    protected Vector3 targetPoint;
    protected Transform casterTransform;
    protected Transform spawnPointTransform;

    public virtual string SpellName => spellName;
    public virtual List<ElementType> RequiredElements => requiredElements;
    public virtual float CastTime => castTime;
    public virtual float Cooldown => cooldown;

    public virtual bool CanCast(Transform caster)
    {
        return Time.time >= lastCastTime + cooldown;
    }

    // ������ �����
    public abstract void Cast(Transform caster, Transform spawnPoint);

    // ����� ����� � �������������
    public virtual void Cast(Transform caster, Transform spawnPoint, Vector3 targetDirection, Vector3 targetPoint)
    {
        // ��������� ������ ������������ ��� ������������� � �����������
        this.targetDirection = targetDirection;
        this.targetPoint = targetPoint;
        this.casterTransform = caster;
        this.spawnPointTransform = spawnPoint;

        // �������� �������� ����� �����
        Cast(caster, spawnPoint);
    }

    public virtual void StopCasting()
    {
        isActive = false;
    }

    protected void SetLastCastTime()
    {
        lastCastTime = Time.time;
    }

    // ������� �������� ������ ��� ������ � �������������
    protected Quaternion GetTargetRotation()
    {
        return Quaternion.LookRotation(targetDirection);
    }

    protected Vector3 GetDirectionToTarget()
    {
        return targetDirection.normalized;
    }

    protected float GetDistanceToTarget()
    {
        if (spawnPointTransform != null)
            return Vector3.Distance(spawnPointTransform.position, targetPoint);
        return 0f;
    }
}