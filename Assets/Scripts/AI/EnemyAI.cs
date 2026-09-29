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
    [SerializeField] private float attackRange = 2.5f; // Increased to let the AI stop slightly before bumping
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

    void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();

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
            if (currentState != EnemyState.Patrol && currentState != EnemyState.Idle)
            {
                ChangeState(EnemyState.Patrol);
            }
            if (!isKnockbackActive) ExecuteCurrentState();
            return;
        }

        if (isKnockbackActive) return;

        // Check if out of bounds
        if (areaCenter != null && Vector3.Distance(transform.position, areaCenter.position) > areaRadius)
        {
            navMeshAgent.isStopped = false;
            navMeshAgent.SetDestination(areaCenter.position);
            ChangeState(EnemyState.Chase);
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // Core State Machine decisions based on distance
        if (distanceToPlayer <= attackRange)
        {
            ChangeState(EnemyState.Attack);
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
    }

    private void ChangeState(EnemyState newState)
    {
        if (currentState == newState && newState != EnemyState.Patrol && newState != EnemyState.Attack) return;

        if (currentState == EnemyState.Idle && newState != EnemyState.Idle)
        {
            if (idleCoroutine != null)
            {
                StopCoroutine(idleCoroutine);
            }
            isIdleTiming = false;
        }

        currentState = newState;
        UpdateAnimatorState();

        switch (currentState)
        {
            case EnemyState.Idle:
                if (navMeshAgent.enabled) navMeshAgent.isStopped = true;
                break;

            case EnemyState.Patrol:
                if (navMeshAgent.enabled) navMeshAgent.isStopped = false;
                PickNewPatrolPoint();
                break;

            case EnemyState.Chase:
                if (navMeshAgent.enabled) navMeshAgent.isStopped = false;
                break;

            case EnemyState.Attack:
                if (navMeshAgent.enabled) navMeshAgent.isStopped = true;
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
                    // Ensure the horizontal angle is completely flat
                    Vector3 lookPos = new Vector3(playerTransform.position.x, transform.position.y, playerTransform.position.z);
                    transform.LookAt(lookPos);

                    // If we are close enough, trigger the combat loop
                    if (Time.time >= nextAttackTime)
                    {
                        StartCoroutine(KnockbackCombatSequence());
                        nextAttackTime = Time.time + attackRate;
                    }
                }
                break;
        }
    }

    private void UpdateAnimatorState()
    {
        if (anim != null)
        {
            anim.SetInteger("AnimState", (int)currentState);
        }
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

    // FIX: Combined Sequence that forces animation, damage, and physics knockback simultaneously!
    public IEnumerator KnockbackCombatSequence()
    {
        isKnockbackActive = true;

        // 1. Play the attack animation instantly when the hit lands
        if (anim != null)
        {
            anim.ResetTrigger("AttackTrigger");
            anim.SetTrigger("AttackTrigger");
        }

        // 2. Deal the damage right now
        ApplyDamageToPlayer();

        // 3. Initiate the physics flight backward
        navMeshAgent.enabled = false;
        rb.isKinematic = false;
        rb.useGravity = true;

        Vector3 sourcePos = playerTransform != null ? playerTransform.position : target;
        Vector3 forceDirection = (transform.position - sourcePos).normalized;
        forceDirection.y = 0.4f; // Clean upward arc

        rb.linearVelocity = Vector3.zero;
        rb.AddForce(forceDirection * knockbackForce, ForceMode.Impulse);

        yield return new WaitForSeconds(0.5f); // Duration of the physical flying recoil

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
                Debug.Log($"[EnemyAI] Impact! Dealing {damage} damage to Player.");
                playerHealth.DamageHandler("Enemy", damage);
            }
        }
    }

    public void TriggerDeath()
    {
        isKnockbackActive = false;
        StopAllCoroutines();
        if (navMeshAgent != null) navMeshAgent.enabled = false;
        if (rb != null) rb.isKinematic = true;
        Debug.Log($"[EnemyAI] {gameObject.name} has died.");
    }

    public void Die()
    {
        ChangeState(EnemyState.Dead);
    }

    private void OnCollisionEnter(Collision col)
    {
        // Physical fallback touch if needed
        if (col.gameObject.CompareTag("Player") && currentState != EnemyState.Dead && !isKnockbackActive)
        {
            if (Time.time >= nextAttackTime)
            {
                StartCoroutine(KnockbackCombatSequence());
                nextAttackTime = Time.time + attackRate;
            }
        }
    }
}
