using Unity.FPS.Game;
using UnityEngine;

namespace Unity.FPS.AI
{
    [RequireComponent(typeof(EnemyController))]
    public class EnemyMobile : MonoBehaviour
    {
        public enum AIState
        {
            Patrol,
            Follow,
            Attack,
        }

        [Tooltip("Аниматор для управления анимациями врага")]
        public Animator Animator;

        [Tooltip("Доля от дистанции атаки, на которой враг останавливается при атаке (0.5 = останавливается на половине дистанции)")]
        [Range(0f, 1f)]
        public float AttackStopDistanceRatio = 0.5f;

        [Tooltip("Массив эффектов искр при получении урона (выбирается случайный)")]
        public ParticleSystem[] RandomHitSparks;

        [Tooltip("Массив визуальных эффектов при обнаружении цели")]
        public ParticleSystem[] OnDetectVfx;

        [Tooltip("Звук воспроизводимый при обнаружении цели")]
        public AudioClip OnDetectSfx;

        [Header("Звук")]
        [Tooltip("Звук передвижения врага (проигрывается в цикле)")]
        public AudioClip MovementSound;

        [Tooltip("Диапазон изменения высоты тона звука движения в зависимости от скорости")]
        public MinMaxFloat PitchDistortionMovementSpeed;

        public AIState AiState { get; private set; }
        EnemyController m_EnemyController;
        AudioSource m_AudioSource;

        const string k_AnimMoveSpeedParameter = "MoveSpeed";
        const string k_AnimAttackParameter = "Attack";
        const string k_AnimAlertedParameter = "Alerted";
        const string k_AnimOnDamagedParameter = "OnDamaged";

        void Start()
        {
            m_EnemyController = GetComponent<EnemyController>();
            DebugUtility.HandleErrorIfNullGetComponent<EnemyController, EnemyMobile>(m_EnemyController, this,
                gameObject);

            m_EnemyController.onAttack += OnAttack;
            m_EnemyController.onDetectedTarget += OnDetectedTarget;
            m_EnemyController.onLostTarget += OnLostTarget;
            m_EnemyController.SetPathDestinationToClosestNode();
            m_EnemyController.onDamaged += OnDamaged;

            // Start patrolling
            AiState = AIState.Patrol;

            // adding a audio source to play the movement sound on it
            m_AudioSource = GetComponent<AudioSource>();
            DebugUtility.HandleErrorIfNullGetComponent<AudioSource, EnemyMobile>(m_AudioSource, this, gameObject);
            m_AudioSource.clip = MovementSound;
            m_AudioSource.Play();
        }

        void Update()
        {
            UpdateAiStateTransitions();
            UpdateCurrentAiState();

            float moveSpeed = m_EnemyController.NavMeshAgent.velocity.magnitude;

            // Update animator speed parameter
                Animator.SetFloat(k_AnimMoveSpeedParameter, moveSpeed);

            // changing the pitch of the movement sound depending on the movement speed
                m_AudioSource.pitch = Mathf.Lerp(PitchDistortionMovementSpeed.Min, PitchDistortionMovementSpeed.Max,
                    moveSpeed / m_EnemyController.NavMeshAgent.speed);
                    }

        void UpdateAiStateTransitions()
        {
            // Handle transitions 
            switch (AiState)
            {
                case AIState.Follow:
                    // Transition to attack when there is a line of sight to the target
                    if (m_EnemyController.IsSeeingTarget && m_EnemyController.IsTargetInAttackRange)
                    {
                        AiState = AIState.Attack;
                        m_EnemyController.SetNavDestination(transform.position);
                    }

                    break;
                case AIState.Attack:
                    // Transition to follow when no longer a target in attack range
                    if (!m_EnemyController.IsTargetInAttackRange)
                    {
                        AiState = AIState.Follow;
            }

                    break;
            }
        }
        private void DebugAgentState()
        {
            Debug.Log($"[AGENT DEBUG] " +
                      $"\nEnabled: {m_EnemyController.NavMeshAgent.enabled}" +
                      $"\nHasPath: {m_EnemyController.NavMeshAgent.hasPath}" +
                      $"\nPathPending: {m_EnemyController.NavMeshAgent.pathPending}" +
                      $"\nPathStatus: {m_EnemyController.NavMeshAgent.pathStatus}" +
                      $"\nRemainingDistance: {m_EnemyController.NavMeshAgent.remainingDistance:F2}" +
                      $"\nVelocity: {m_EnemyController.NavMeshAgent.velocity.magnitude:F2}" +
                      $"\nIsOnNavMesh: {m_EnemyController.NavMeshAgent.isOnNavMesh}" +
                      $"\nIsStopped: {m_EnemyController.NavMeshAgent.isStopped}" +
                      $"\nDestination: {m_EnemyController.NavMeshAgent.destination}" +
                      $"\nPosition: {transform.position}");
        }
        void UpdateCurrentAiState()
        {
            // Handle logic 
            DebugAgentState();
            switch (AiState)
            {
                case AIState.Patrol:
                    m_EnemyController.UpdatePathDestination();
                        m_EnemyController.SetNavDestination(m_EnemyController.GetDestinationOnPath());
                    break;
                case AIState.Follow:
                    m_EnemyController.SetNavDestination(m_EnemyController.KnownDetectedTarget.transform.position);
                    m_EnemyController.OrientTowards(m_EnemyController.KnownDetectedTarget.transform.position);
                    m_EnemyController.OrientWeaponsTowards(m_EnemyController.KnownDetectedTarget.transform.position);
                    break;
                case AIState.Attack:
                    if (Vector3.Distance(m_EnemyController.KnownDetectedTarget.transform.position,
                            m_EnemyController.DetectionModule.DetectionSourcePoint.position)
                        >= (AttackStopDistanceRatio * m_EnemyController.DetectionModule.AttackRange))
                        {
                        m_EnemyController.SetNavDestination(m_EnemyController.KnownDetectedTarget.transform.position);
                        }
                        else
                        {
                            m_EnemyController.SetNavDestination(transform.position);
                        }

                    m_EnemyController.OrientTowards(m_EnemyController.KnownDetectedTarget.transform.position);
                    m_EnemyController.TryAtack(m_EnemyController.KnownDetectedTarget.transform.position);
                    break;
            }
        }

        void OnAttack()
        {
            Animator.SetTrigger(k_AnimAttackParameter);
        }

        void OnDetectedTarget()
        {
            if (AiState == AIState.Patrol)
            {
                AiState = AIState.Follow;
            }

            for (int i = 0; i < OnDetectVfx.Length; i++)
            {
                OnDetectVfx[i].Play();
            }

            if (OnDetectSfx)
            {
                AudioUtility.CreateSFX(OnDetectSfx, transform.position, AudioUtility.AudioGroups.EnemyDetection, 1f);
            }

            Animator.SetBool(k_AnimAlertedParameter, true);
        }

        void OnLostTarget()
        {
            if (AiState == AIState.Follow || AiState == AIState.Attack)
            {
                AiState = AIState.Patrol;
            }

            for (int i = 0; i < OnDetectVfx.Length; i++)
            {
                OnDetectVfx[i].Stop();
            }

            Animator.SetBool(k_AnimAlertedParameter, false);
        }

        void OnDamaged()
        {
            // Исправлена ошибка с проверкой массива
            if (RandomHitSparks != null && RandomHitSparks.Length > 0)
            {
                int n = Random.Range(0, RandomHitSparks.Length);
                if (RandomHitSparks[n] != null)
                {
                    RandomHitSparks[n].Play();
                }
            }

            Animator.SetTrigger(k_AnimOnDamagedParameter);
        }
    }
}