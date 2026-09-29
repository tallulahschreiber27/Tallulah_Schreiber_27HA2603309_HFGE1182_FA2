using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AnimalAI : MonoBehaviour
{
    public enum NPCState { Idle, Patrol, Runaway, Dead }
    [SerializeField] private NPCState currentState = NPCState.Idle;

    [Header("AI PARAMETERS")]
    [SerializeField] private NavMeshAgent navMeshAgent;
    [SerializeField] private float patrolRange = 10f;

    [Header("DETECTION PARAMETERS")]
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float runawayRange = 20f;
    [SerializeField] private Transform playerTransform;

    [Header("AREA RESTRICTION")]
    [SerializeField] private Transform areaCenter;
    [SerializeField] private float areaRadius = 30f;

    // --- ANIMATION REFERENCE ---
    private Animator anim;

    private Vector3 target;
    private bool isIdleCoroutineRunning = false;
    private float scaleMultiplier = 1f;
    private Coroutine idleCoroutine;

    void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        
        // Strict parent object Animator collection
        anim = GetComponent<Animator>(); 

        if (anim == null)
        {
            Debug.LogWarning($"[AI WARNING] No Animator component found on the parent object ({gameObject.name})!");
        }

        scaleMultiplier = Mathf.Max(transform.localScale.x, transform.localScale.y, transform.localScale.z);
        if (scaleMultiplier <= 0) scaleMultiplier = 1f; 

        if (areaCenter == null)
        {
            Debug.LogError($"[AI ERROR] Please assign an 'Area Center' GameObject to {gameObject.name} in the inspector!");
        }

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }

        NavMeshHit closestHit;
        if (NavMesh.SamplePosition(transform.position, out closestHit, 5f * scaleMultiplier, NavMesh.AllAreas))
        {
            transform.position = closestHit.position;
        }

        ChangeState(NPCState.Patrol);
    }

    void Update()
    {
        if (currentState == NPCState.Dead) return;
        if (areaCenter == null || navMeshAgent == null) return;

        // Dynamic directional look tracking to drive "Idle Left" vs "Idle Right" loops
        HandleDirectionalTracking();

        // Check if out of bounds
        if (Vector3.Distance(transform.position, areaCenter.position) > (areaRadius * scaleMultiplier))
        {
            navMeshAgent.isStopped = false;
            navMeshAgent.SetDestination(areaCenter.position);
            ChangeState(NPCState.Patrol);
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // State Machine evaluation
        if (distanceToPlayer <= (detectionRange * scaleMultiplier))
        {
            ChangeState(NPCState.Runaway);
        }
        else if (currentState == NPCState.Runaway && distanceToPlayer > ((detectionRange + 5f) * scaleMultiplier))
        {
            ChangeState(NPCState.Idle);
        }

        ExecuteCurrentState();
    }

    private void ChangeState(NPCState newState)
    {
        // Matches the Enemy AI state protection logic to halt multiple triggers
        if (currentState == newState && newState != NPCState.Patrol) return;

        if (currentState == NPCState.Idle && newState != NPCState.Idle)
        {
            if (idleCoroutine != null)
            {
                StopCoroutine(idleCoroutine);
            }
            isIdleCoroutineRunning = false;
        }

        currentState = newState;
        UpdateAnimatorState();

        switch (currentState)
        {
            case NPCState.Idle:
                if (navMeshAgent.enabled) navMeshAgent.isStopped = true;
                break;

            case NPCState.Patrol:
                if (navMeshAgent.enabled) navMeshAgent.isStopped = false;
                PickNewPatrolPoint();
                break;

            case NPCState.Runaway:
                if (navMeshAgent.enabled) navMeshAgent.isStopped = false;
                break;

            case NPCState.Dead:
                if (navMeshAgent != null) navMeshAgent.enabled = false;
                break;
        }
    }

    private void ExecuteCurrentState()
    {
        switch (currentState)
        {
            case NPCState.Idle:
                if (!isIdleCoroutineRunning)
                {
                    idleCoroutine = StartCoroutine(IdleFor(2f));
                }
                break;

            case NPCState.Patrol:
                PatrolRoutine();
                break;

            case NPCState.Runaway:
                if (playerTransform == null || !navMeshAgent.enabled) return;

                Vector3 directionToTarget = transform.position - playerTransform.position;
                Vector3 targetPosition = transform.position + directionToTarget.normalized * (runawayRange * scaleMultiplier);

                navMeshAgent.SetDestination(targetPosition);
                break;
        }
    }

    private void UpdateAnimatorState()
    {
        if (anim != null)
        {
            // Pushes integer state parameters to Animator (0=Idle, 1=Patrol, 2=Runaway, 3=Dead)
            anim.SetInteger("AnimState", (int)currentState);
        }
    }

    private void HandleDirectionalTracking()
    {
        if (anim == null || navMeshAgent == null) return;

        // Tracks movement velocity along the X-axis to keep your directional states informed
        if (navMeshAgent.velocity.magnitude > 0.1f)
        {
            if (navMeshAgent.velocity.x < -0.1f)
            {
                anim.SetBool("FacingLeft", true);
            }
            else if (navMeshAgent.velocity.x > 0.1f)
            {
                anim.SetBool("FacingLeft", false);
            }
        }
    }

    public void PatrolRoutine()
    {
        if (navMeshAgent.enabled && !navMeshAgent.pathPending)
        {
            float arrivalThreshold = (navMeshAgent.stoppingDistance + 0.1f) * scaleMultiplier;
            if (navMeshAgent.remainingDistance <= arrivalThreshold || !navMeshAgent.hasPath)
            {
                ChangeState(NPCState.Idle);
            }
        }
    }

    public void PickNewPatrolPoint()
    {
        if (!navMeshAgent.enabled) return;

        float calculatedRange = patrolRange * scaleMultiplier;
        Vector3 randomDirection = Random.insideUnitSphere * calculatedRange;
        randomDirection += areaCenter.position;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, calculatedRange * 2f, NavMesh.AllAreas))
        {
            target = hit.position;
            navMeshAgent.SetDestination(target);
        }
    }

    public IEnumerator IdleFor(float time)
    {
        isIdleCoroutineRunning = true;
        yield return new WaitForSeconds(time);
        isIdleCoroutineRunning = false;
        
        ChangeState(NPCState.Patrol);
    }

    public void SetDeadState()
    {
        ChangeState(NPCState.Dead);
    }
}
