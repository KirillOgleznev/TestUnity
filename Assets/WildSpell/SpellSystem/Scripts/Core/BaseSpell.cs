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

    public virtual string SpellName => spellName;
    public virtual List<ElementType> RequiredElements => requiredElements;
    public virtual float CastTime => castTime;
    public virtual float Cooldown => cooldown;

    public virtual bool CanCast(Transform caster)
    {
        return Time.time >= lastCastTime + cooldown;
    }

    public abstract void Cast(Transform caster, Transform spawnPoint);

    public virtual void StopCasting()
    {
        isActive = false;
    }

    protected void SetLastCastTime()
    {
        lastCastTime = Time.time;
    }
}