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

        // 1. Check if the player link is actually assigned in the inspector
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

        // 2. Measure distance and calculate the dynamic grace threshold range
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        float currentAttackRange = (currentState == EnemyState.Attack) ? (attackRange + 0.5f) : attackRange;

        // 3. Print out the exact real-time distance metrics in the console window
        Debug.Log($"<b>[{gameObject.name}]</b> Distance to Player: <color=yellow>{distanceToPlayer:F2}m</color> | Current Strike Threshold: <color=cyan>{currentAttackRange:F2}m</color> (State: {currentState})");

        // 4. State Decision Matrix
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
        // Allowed sequential updates to Chase, Patrol, and Attack to ensure velocity variables adapt dynamically
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
                    navMeshAgent.velocity = Vector3.zero; // Halts momentum instantly so idle animations kick in
                }
                break;

            case EnemyState.Patrol:
                if (navMeshAgent.enabled)
                {
                    navMeshAgent.isStopped = false;
                    navMeshAgent.speed = 2f; // Force casual walking speed
                }
                PickNewPatrolPoint();
                break;

            case EnemyState.Chase:
                if (navMeshAgent.enabled)
                {
                    navMeshAgent.isStopped = false;
                    navMeshAgent.speed = 5f; // Force frantic chasing sprint speed
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
                    // Ensure the horizontal angle is completely flat to prevent flipping over
                    Vector3 lookPos = new Vector3(playerTransform.position.x, transform.position.y, playerTransform.position.z);
                    transform.LookAt(lookPos);

                    // Combat cadence processing
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

        // If active, pull real velocity, otherwise clamp to zero
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

        // 1. Turn off AI pathfinding, turn on active physics engine
        navMeshAgent.enabled = false;
        rb.isKinematic = false;
        rb.useGravity = true;

        Vector3 sourcePos = playerTransform != null ? playerTransform.position : target;
        Vector3 forceDirection = (transform.position - sourcePos).normalized;
        forceDirection.y = 0.2f; // Lowered from 0.4f to prevent jumping too high into the air

        rb.linearVelocity = Vector3.zero;
        rb.AddForce(forceDirection * knockbackForce, ForceMode.Impulse);

        // 2. Wait a split second for the physical velocity impact impulse to finish
        yield return new WaitForSeconds(0.4f);

        // 3. --- PERMANENT FLOATING FIX: SPREAD NAVMESH RAYCAST GROUND CHECK ---
        // Look directly beneath the bear's current spatial coordinates to locate the blue baked floor
        NavMeshHit groundHit;
        if (NavMesh.SamplePosition(transform.position, out groundHit, 5f, NavMesh.AllAreas))
        {
            // Smoothly snap the bear's absolute Y height back to the exact surface level of the floor
            transform.position = groundHit.position;
        }
        else if (areaCenter != null)
        {
            // Emergency Backup: If he somehow flew off-mesh completely, snap his feet to the home zone height
            Vector3 emergencyPos = transform.position;
            emergencyPos.y = areaCenter.position.y;
            transform.position = emergencyPos;
        }

        // 4. Cleanly restore rigid body state settings back to standard tracking movement modes
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