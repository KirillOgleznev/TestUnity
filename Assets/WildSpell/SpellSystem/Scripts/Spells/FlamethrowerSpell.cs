using UnityEngine;
using System.Collections.Generic;

public class FlamethrowerSpell : ContinuousSpell
{
    [Header("Flamethrower Settings")]
    [SerializeField] private GameObject flamePrefab;
    [SerializeField] private float damagePerSecond = 20f;
    [SerializeField] private float range = 8f;

    void Reset()
    {
        spellName = "Flamethrower";
        requiredElements = new List<ElementType> { ElementType.Fire, ElementType.Fire, ElementType.Fire };
        castTime = 0.2f;
        cooldown = 1f;
        maxDuration = 5f;
    }

    protected override void StartEffect(Transform spawnPoint)
    {
        if (flamePrefab == null) return;

        Vector3 direction = projectileShooter != null ?
            projectileShooter.GetShootDirection() :
            spawnPoint.forward;

        currentEffect = Instantiate(flamePrefab, spawnPoint.position, Quaternion.LookRotation(direction));

        FlameDamage flameDamage = currentEffect.GetComponent<FlameDamage>();
        if (flameDamage == null)
        {
            flameDamage = currentEffect.AddComponent<FlameDamage>();
        }
        flameDamage.damagePerSecond = damagePerSecond;
        flameDamage.range = range;

        currentEffect.transform.SetParent(spawnPoint);
    }

    protected override void StopEffect()
    {
        if (currentEffect != null)
        {
            Destroy(currentEffect);
            currentEffect = null;
        }
    }

    protected override void UpdateEffect()
    {
        if (currentEffect != null && projectileShooter != null)
        {
            CrosshairController crosshair = projectileShooter.GetComponent<CrosshairController>();
            if (crosshair != null)
            {
                Vector3 direction = crosshair.GetDirectionBehindEnemies(currentEffect.transform.position);
                currentEffect.transform.rotation = Quaternion.LookRotation(direction);
            }
        }
    }

}