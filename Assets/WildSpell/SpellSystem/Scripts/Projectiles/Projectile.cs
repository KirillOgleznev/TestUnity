using System.Collections.Generic;
using UnityEngine;
using Unity.FPS.Game;

public class Projectile : MonoBehaviour
{
    [Header("General")]
    public float Radius = 0.01f;
    public Transform Root;
    public Transform Tip;
    public float MaxLifeTime = 5f;
    public GameObject ImpactVfx;
    public float ImpactVfxLifetime = 5f;
    public float ImpactVfxSpawnOffset = 0.1f;
    public AudioClip ImpactSfxClip;
    public LayerMask HittableLayers = -1;

    [Header("Movement")]
    public float Speed = 20f;
    public float GravityDownAcceleration = 0f;

    [Header("Damage")]
    public float Damage = 40f;

    [Header("Debug")]
    public Color RadiusColor = Color.cyan * 0.2f;

    private Vector3 m_LastRootPosition;
    private Vector3 m_Velocity;
    private float m_ShootTime;
    private List<Collider> m_IgnoredColliders;
    private GameObject m_Owner;

    const QueryTriggerInteraction k_TriggerInteraction = QueryTriggerInteraction.Collide;

    public void Initialize(Vector3 direction, float speed, GameObject owner = null)
    {
        if (Root == null) Root = transform;
        if (Tip == null) Tip = transform;

        m_Owner = owner;
        m_ShootTime = Time.time;
        m_LastRootPosition = Root.position;
        m_Velocity = direction.normalized * speed;
        transform.forward = direction.normalized;

        m_IgnoredColliders = new List<Collider>();

        // Ignore colliders of owner
        if (owner != null)
        {
            Collider[] ownerColliders = owner.GetComponentsInChildren<Collider>();
            m_IgnoredColliders.AddRange(ownerColliders);
        }

        Destroy(gameObject, MaxLifeTime);
    }

    void Update()
    {
        // Move
        transform.position += m_Velocity * Time.deltaTime;

        // Orient towards velocity
        transform.forward = m_Velocity.normalized;

        // Gravity
        if (GravityDownAcceleration > 0)
        {
            m_Velocity += Vector3.down * GravityDownAcceleration * Time.deltaTime;
        }

        // Hit detection
        RaycastHit closestHit = new RaycastHit();
        closestHit.distance = Mathf.Infinity;
        bool foundHit = false;

        // Sphere cast
        Vector3 displacementSinceLastFrame = Tip.position - m_LastRootPosition;
        if (displacementSinceLastFrame.magnitude > 0)
        {
            RaycastHit[] hits = Physics.SphereCastAll(m_LastRootPosition, Radius,
                displacementSinceLastFrame.normalized, displacementSinceLastFrame.magnitude, HittableLayers,
                k_TriggerInteraction);

            foreach (var hit in hits)
            {
                if (IsHitValid(hit) && hit.distance < closestHit.distance)
                {
                    foundHit = true;
                    closestHit = hit;
                }
            }

            if (foundHit)
            {
                if (closestHit.distance <= 0f)
                {
                    closestHit.point = Root.position;
                    closestHit.normal = -transform.forward;
                }

                OnHit(closestHit.point, closestHit.normal, closestHit.collider);
            }
        }

        m_LastRootPosition = Root.position;
    }

    bool IsHitValid(RaycastHit hit)
    {
        // ignore hits with triggers that don't have a Damageable component
        if (hit.collider.isTrigger && hit.collider.GetComponent<Damageable>() == null)
        {
            return false;
        }

        // ignore hits with specific ignored colliders (self colliders, by default)
        if (m_IgnoredColliders != null && m_IgnoredColliders.Contains(hit.collider))
        {
            return false;
        }

        return true;
    }

    void OnHit(Vector3 point, Vector3 normal, Collider collider)
    {
        // point damage
        Damageable damageable = collider.GetComponent<Damageable>();
        if (damageable)
        {
            damageable.InflictDamage(Damage, false, m_Owner);
        }

        // impact vfx
        if (ImpactVfx)
        {
            GameObject impactVfxInstance = Instantiate(ImpactVfx, point + (normal * ImpactVfxSpawnOffset),
                Quaternion.LookRotation(normal));
            if (ImpactVfxLifetime > 0)
            {
                Destroy(impactVfxInstance.gameObject, ImpactVfxLifetime);
            }
        }

        // impact sfx
        if (ImpactSfxClip)
        {
            AudioSource.PlayClipAtPoint(ImpactSfxClip, point);
        }

        // Self Destruct
        Destroy(this.gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = RadiusColor;
        Gizmos.DrawSphere(transform.position, Radius);
    }
}