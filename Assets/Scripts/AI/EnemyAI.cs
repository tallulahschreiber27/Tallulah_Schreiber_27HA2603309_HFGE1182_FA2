using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class EnemyAI : MonoBehaviour
{
    public enum EnemyState { Idle, Patrol, Chase, Attack, Dead }
    [SerializeField] private EnemyState currentState = EnemyState.Idle;

    [Header("AI PARAMETERS")]
    [SerializeField] private NavMeshAgent navMeshAgent;
    [SerializeField] private float detectionRange = 12f;
    [SerializeField] private float attackRange = 2.5f;
    [SerializeField] private float patrolRange = 4f;
    [SerializeField] private float knockbackForce = 6f;
    private Rigidbody rb;

    [Header("ATTACK PARAMETERS")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float attackRate = 1.2f;
    private float nextAttackTime = 0f;

    [Header("AREA RESTRICTION")]
    [SerializeField] private Transform areaCenter;
    [SerializeField] private float areaRadius = 15f;

    private Vector3 target;
    private Animator anim;

    private bool isIdleTiming = false;
    private Coroutine idleCoroutine;
    private bool isKnockbackActive = false;

    #region BostonCity Campus
    /* This code is property of Boston City Campus, and should not be used, or copied for any other subject.
     If you would copy and or use this in an assessment outside the assessment that this was supplied for
     action will be taken against you for plagiarism.
    */
    #endregion
    void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();

        if (navMeshAgent == null) Debug.LogError($"<b>[{gameObject.name}]</b> NavMeshAgent is missing!");
        if (anim == null) Debug.LogWarning($"<b>[{gameObject.name}]</b> Animator component is missing!");
        if (rb == null) Debug.LogError($"<b>[{gameObject.name}]</b> Rigidbody component is missing!");

        if (navMeshAgent != null)
        {
            navMeshAgent.isStopped = false;
        }

        ChangeState(EnemyState.Idle);
    }

    void Update()
    {
        if (currentState == EnemyState.Dead) return;
        if (navMeshAgent == null) return;

        if (playerTransform == null)
        {
            Debug.LogError($"<b>[{gameObject.name}]</b> <color=red>ATTACK CANCELLED:</color> 'Player Transform' slot is completely empty in the Inspector!");
            if (currentState != EnemyState.Patrol && currentState != EnemyState.Idle)
            {
                ChangeState(EnemyState.Patrol);
            }
            if (!isKnockbackActive) ExecuteCurrentState();
            UpdateAnimatorParameters();
            return;
        }

        if (isKnockbackActive) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        float currentAttackRange = (currentState == EnemyState.Attack) ? (attackRange + 0.5f) : attackRange;

        Debug.Log($"<b>[{gameObject.name}]</b> Distance to Player: <color=yellow>{distanceToPlayer:F2}m</color> | Current Strike Threshold: <color=cyan>{currentAttackRange:F2}m</color> (State: {currentState})");

        if (distanceToPlayer <= currentAttackRange)
        {
            Debug.Log($"<b>[{gameObject.name}]</b> <color=green>SUCCESS:</color> Player is inside range! Forcing state to Attack.");
            ChangeState(EnemyState.Attack);
        }
        else if (areaCenter != null && Vector3.Distance(transform.position, areaCenter.position) > areaRadius)
        {
            ChangeState(EnemyState.Chase);
            navMeshAgent.SetDestination(areaCenter.position);
        }
        else if (distanceToPlayer <= detectionRange)
        {
            ChangeState(EnemyState.Chase);
        }
        else
        {
            if (currentState == EnemyState.Chase || currentState == EnemyState.Attack)
            {
                ChangeState(EnemyState.Patrol);
            }
        }

        ExecuteCurrentState();
        UpdateAnimatorParameters();
    }



    private void ChangeState(EnemyState newState)
    {
        if (currentState == newState && newState != EnemyState.Patrol && newState != EnemyState.Attack && newState != EnemyState.Chase) return;

        if (currentState == EnemyState.Idle && newState != EnemyState.Idle)
        {
            if (idleCoroutine != null)
            {
                StopCoroutine(idleCoroutine);
            }
            isIdleTiming = false;
        }

        currentState = newState;

        switch (currentState)
        {
            case EnemyState.Idle:
                if (navMeshAgent.enabled)
                {
                    navMeshAgent.isStopped = true;
                    navMeshAgent.velocity = Vector3.zero; 
                }
                break;

            case EnemyState.Patrol:
                if (navMeshAgent.enabled)
                {
                    navMeshAgent.isStopped = false;
                    navMeshAgent.speed = 2f; 
                }
                PickNewPatrolPoint();
                break;

            case EnemyState.Chase:
                if (navMeshAgent.enabled)
                {
                    navMeshAgent.isStopped = false;
                    navMeshAgent.speed = 5f; 
                }
                break;

            case EnemyState.Attack:
                if (navMeshAgent.enabled)
                {
                    navMeshAgent.isStopped = true;
                    navMeshAgent.velocity = Vector3.zero;
                }
                break;

            case EnemyState.Dead:
                TriggerDeath();
                break;
        }
    }

    public void ExecuteCurrentState()
    {
        switch (currentState)
        {
            case EnemyState.Idle:
                if (!isIdleTiming)
                {
                    idleCoroutine = StartCoroutine(IdleFor(2f));
                }
                break;

            case EnemyState.Patrol:
                PatrolRoutine();
                break;

            case EnemyState.Chase:
                if (playerTransform != null && navMeshAgent.enabled)
                {
                    navMeshAgent.SetDestination(playerTransform.position);
                }
                break;

            case EnemyState.Attack:
                if (playerTransform != null)
                {
                    Vector3 lookPos = new Vector3(playerTransform.position.x, transform.position.y, playerTransform.position.z);
                    transform.LookAt(lookPos);

                    if (Time.time >= nextAttackTime)
                    {
                        StartCoroutine(KnockbackCombatSequence());
                        nextAttackTime = Time.time + attackRate;
                    }
                }
                break;
        }
    }

    /// <summary>
    /// Continually extracts velocity values from the NavMeshAgent and updates the float parameter driving the Blend Tree.
    /// </summary>
    private void UpdateAnimatorParameters()
    {
        if (anim == null) return;

        float currentSpeed = (navMeshAgent != null && navMeshAgent.enabled) ? navMeshAgent.velocity.magnitude : 0f;

        anim.SetFloat("Speed", currentSpeed);
    }

    public void PatrolRoutine()
    {
        if (navMeshAgent.enabled && !navMeshAgent.pathPending && (navMeshAgent.remainingDistance < 0.8f || !navMeshAgent.hasPath))
        {
            ChangeState(EnemyState.Idle);
        }
    }

    public void PickNewPatrolPoint()
    {
        if (!navMeshAgent.enabled) return;

        Vector3 origin = areaCenter != null ? areaCenter.position : transform.position;
        Vector3 randomDirection = Random.insideUnitSphere * patrolRange;
        randomDirection += origin;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, patrolRange, NavMesh.AllAreas))
        {
            target = hit.position;
            navMeshAgent.SetDestination(target);
        }
    }

    public IEnumerator IdleFor(float time)
    {
        isIdleTiming = true;
        yield return new WaitForSeconds(time);
        isIdleTiming = false;
        ChangeState(EnemyState.Patrol);
    }

    public IEnumerator KnockbackCombatSequence()
    {
        isKnockbackActive = true;

        if (anim != null)
        {
            anim.ResetTrigger("AttackTrigger");
            anim.SetTrigger("AttackTrigger");
        }

        ApplyDamageToPlayer();

        navMeshAgent.enabled = false;
        rb.isKinematic = false;
        rb.useGravity = true;

        Vector3 sourcePos = playerTransform != null ? playerTransform.position : target;
        Vector3 forceDirection = (transform.position - sourcePos).normalized;
        forceDirection.y = 0.2f; 

        rb.linearVelocity = Vector3.zero;
        rb.AddForce(forceDirection * knockbackForce, ForceMode.Impulse);

        
        yield return new WaitForSeconds(0.4f);

        
        NavMeshHit groundHit;
        if (NavMesh.SamplePosition(transform.position, out groundHit, 5f, NavMesh.AllAreas))
        {
            
            transform.position = groundHit.position;
        }
        else if (areaCenter != null)
        {
            
            Vector3 emergencyPos = transform.position;
            emergencyPos.y = areaCenter.position.y;
            transform.position = emergencyPos;
        }

        
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;
        rb.useGravity = false;
        navMeshAgent.enabled = true;
        isKnockbackActive = false;

        ChangeState(EnemyState.Patrol);
    }


    private void ApplyDamageToPlayer()
    {
        if (playerTransform != null)
        {
            HealthHandler playerHealth = playerTransform.GetComponent<HealthHandler>();
            if (playerHealth != null)
            {
                playerHealth.DamageHandler("Enemy", damage);
            }
        }
    }

    private void TriggerDeath()
    {
        Debug.Log($"<b>[{gameObject.name}]</b> Bear has died. Disabling AI tracking systems.");
             if (navMeshAgent != null) navMeshAgent.enabled = false;
             if (rb != null) rb.isKinematic = true;
             if (anim != null)
        {
            anim.SetTrigger("DieTrigger");
        }
        Destroy(gameObject, 4f);
    }
}