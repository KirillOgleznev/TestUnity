using UnityEngine;
using System.Collections.Generic;

public class FlamethrowerSpell : BaseSpell
{
    [Header("Flamethrower Settings")]
    [SerializeField] private GameObject flamePrefab;
    [SerializeField] private float maxDuration = 5f;
    [SerializeField] private float damagePerSecond = 20f;
    [SerializeField] private float range = 8f;

    private GameObject currentFlameEffect;
    private float currentDuration = 0f;

    void Reset()
    {
        // Устанавливаем дефолтные значения при создании компонента
        spellName = "Flamethrower";
        requiredElements = new List<ElementType> { ElementType.Fire, ElementType.Fire, ElementType.Fire };
        castTime = 0.2f;
        cooldown = 1f;
    }

    public override void Cast(Transform caster, Transform spawnPoint)
    {
        if (!CanCast(caster) || isActive) return;

        SetLastCastTime();
        isActive = true;
        currentDuration = 0f;

        StartFlaming(spawnPoint);
    }

    public override void StopCasting()
    {
        base.StopCasting();
        StopFlaming();
    }

    void Update()
    {
        if (isActive)
        {
            currentDuration += Time.deltaTime;

            // Автоматически останавливаем через максимальную длительность
            if (currentDuration >= maxDuration)
            {
                StopCasting();
            }

            // Поворачиваем эффект в направлении персонажа
            if (currentFlameEffect != null)
            {
                UpdateFlameDirection();
            }
        }
    }

    private void StartFlaming(Transform spawnPoint)
    {
        if (flamePrefab == null) return;

        currentFlameEffect = Instantiate(flamePrefab, spawnPoint.position, spawnPoint.rotation);

        // Добавляем компонент урона
        FlameDamage flameDamage = currentFlameEffect.GetComponent<FlameDamage>();
        if (flameDamage == null)
        {
            flameDamage = currentFlameEffect.AddComponent<FlameDamage>();
        }
        flameDamage.damagePerSecond = damagePerSecond;
        flameDamage.range = range;

        // Остальной код...
        currentFlameEffect.transform.SetParent(spawnPoint);
    }

    private void StopFlaming()
    {
        if (currentFlameEffect != null)
        {
            Destroy(currentFlameEffect);
            currentFlameEffect = null;
        }
    }

    private void UpdateFlameDirection()
    {
        if (currentFlameEffect != null && currentFlameEffect.transform.parent != null)
        {
            currentFlameEffect.transform.rotation = currentFlameEffect.transform.parent.rotation;
        }
    }
}