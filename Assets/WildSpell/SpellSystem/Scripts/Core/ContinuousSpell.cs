using UnityEngine;

public abstract class ContinuousSpell : BaseSpell
{
    [Header("Continuous Spell Settings")]
    [SerializeField] protected float maxDuration = 5f;
    [SerializeField] protected ProjectileShooter projectileShooter;

    protected float currentDuration = 0f;
    protected GameObject currentEffect;

    protected virtual void Start()
    {
        if (projectileShooter == null)
            projectileShooter = FindObjectOfType<ProjectileShooter>();
    }

    public override void Cast(Transform caster, Transform spawnPoint)
    {
        if (!CanCast(caster) || isActive) return;

        SetLastCastTime();
        isActive = true;
        currentDuration = 0f;

        StartEffect(spawnPoint);
    }

    public override void StopCasting()
    {
        base.StopCasting();
        StopEffect();
    }

    protected virtual void Update()
    {
        if (isActive)
        {
            currentDuration += Time.deltaTime;

            // ������������� ������������� ����� ������������ ������������
            if (currentDuration >= maxDuration)
            {
                StopCasting();
            }

            // ��������� ������ ������ ����
            if (currentEffect != null)
            {
                UpdateEffect();
            }
        }
    }

    protected virtual void UpdateEffect()
    {
        // ��������� ����������� � ������� ����
        if (currentEffect != null && projectileShooter != null)
        {
            Vector3 currentDirection = projectileShooter.GetShootDirection();
            currentEffect.transform.rotation = Quaternion.LookRotation(currentDirection);
        }
    }

    // ����������� ������ ��� ��������������� � �����������
    protected abstract void StartEffect(Transform spawnPoint);
    protected abstract void StopEffect();
}