using UnityEngine;
using System.Collections.Generic;

public interface ISpell
{
    string SpellName { get; }
    List<ElementType> RequiredElements { get; }
    float CastTime { get; }
    float Cooldown { get; }
    bool CanCast(Transform caster);
    void Cast(Transform caster, Transform spawnPoint);
    void Cast(Transform caster, Transform spawnPoint, Vector3 targetDirection, Vector3 targetPoint);
    void StopCasting();
}