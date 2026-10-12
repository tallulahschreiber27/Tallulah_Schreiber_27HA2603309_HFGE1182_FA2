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
    [SerializeField] private float patrolRange = 4f;

    [Header("DETECTION PARAMETERS")]
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float runawayRange = 20f;
    [SerializeField] private Transform playerTransform;

    [Header("AREA RESTRICTION")]
    [SerializeField] private Transform areaCenter;
    [SerializeField] private float areaRadius = 30f;

    private Vector3 target;
    private Animator anim;
    private HealthHandler healthScript;

    
    private float lastRotationY;
    private NPCState previousState;

    // Start is called before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        healthScript = GetComponent<HealthHandler>(); 

        if (navMeshAgent == null) Debug.LogError($"<b>[{gameObject.name}]</b> NavMeshAgent component is missing!");
        if (anim == null) Debug.LogWarning($"<b>[{gameObject.name}]</b> Animator component is missing!");
        if (playerTransform == null) Debug.LogError($"<b>[{gameObject.name}]</b> Player Transform is not assigned in the Inspector!");
        if (areaCenter == null) Debug.LogError($"<b>[{gameObject.name}]</b> Area Center Transform is not assigned in the Inspector!");
        if (healthScript == null) Debug.LogWarning($"<b>[{gameObject.name}]</b> Existing HealthHandler script was not found on this object!");

        lastRotationY = transform.eulerAngles.y;
        previousState = currentState;

        PickNewPatrolPoint();
    }

    #region BostonCity Campus
    /* This code is property of Boston City Campus, and should not be used, or copied for any other subject.
     If you would copy and or use this in an assessment outside the assessment that this was supplied for
     action will be taken against you for plagiarism.
    */
    #endregion

    // Update is called once per frame
    void Update()
    {
        if (currentState == NPCState.Dead)
        {
            return;
        }

        // Log state changes dynamically
        if (currentState != previousState)
        {
            Debug.Log($"<b>[{gameObject.name}]</b> State changed from <color=orange>{previousState}</color> to <color=cyan>{currentState}</color>");
            previousState = currentState;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // Check if out of bounds
        if (Vector3.Distance(transform.position, areaCenter.position) > areaRadius)
        {
            navMeshAgent.SetDestination(areaCenter.position);
            UpdateAnimatorParameters();
            return;
        }

        // Check player detection
        if (distanceToPlayer <= detectionRange && currentState != NPCState.Runaway)
        {
            currentState = NPCState.Runaway;
        }

        StateHandler();
        UpdateAnimatorParameters();
    }

    public void StateHandler()
    {
        switch (currentState)
        {
            case NPCState.Idle:
                navMeshAgent.isStopped = true;
                navMeshAgent.velocity = Vector3.zero; 
                StartCoroutine(IdleFor(3f));
                break;
            case NPCState.Patrol:
                navMeshAgent.isStopped = false;
                PatrolRoutine();
                break;
            case NPCState.Runaway:
                navMeshAgent.isStopped = false;

                Vector3 directionToTarget = transform.position - playerTransform.position;
                Vector3 targetPosition = transform.position + directionToTarget.normalized * runawayRange;

                NavMeshHit runawayHit;
                if (NavMesh.SamplePosition(targetPosition, out runawayHit, 5f, NavMesh.AllAreas))
                {
                    navMeshAgent.SetDestination(runawayHit.position);
                }
                else
                {
                    PickNewPatrolPoint();
                    currentState = NPCState.Patrol;
                }
                break;
            case NPCState.Dead:
                break;
        }
    }

    // PATROL
    public void PatrolRoutine()
    {
        bool arrived = !navMeshAgent.pathPending && navMeshAgent.remainingDistance < 0.1f;
        bool physicallyStuck = !navMeshAgent.pathPending && navMeshAgent.velocity.sqrMagnitude < 0.01f && navMeshAgent.remainingDistance > 0.5f;

        if (arrived || physicallyStuck)
        {
            currentState = NPCState.Idle;
        }
    }

    public void PickNewPatrolPoint()
    {
        Vector3 randomDirection = Random.insideUnitSphere * patrolRange;
        randomDirection += areaCenter.position;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, patrolRange, NavMesh.AllAreas))
        {
            target = hit.position;
            navMeshAgent.SetDestination(target);
        }
    }

    public IEnumerator IdleFor(float time)
    {
        float elapsed = 0f;
        while (elapsed < time)
        {
            if (currentState == NPCState.Runaway || currentState == NPCState.Dead)
            {
                yield break;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (currentState == NPCState.Idle)
        {
            PickNewPatrolPoint();
            currentState = NPCState.Patrol;
        }
    }

    private void UpdateAnimatorParameters()
    {
        if (anim == null) return;

        float currentSpeed = navMeshAgent.velocity.magnitude;
        anim.SetFloat("Speed", currentSpeed);

        float currentRotationY = transform.eulerAngles.y;
        float deltaRotationY = Mathf.DeltaAngle(lastRotationY, currentRotationY);
        lastRotationY = currentRotationY;

        float turnValue = Mathf.Clamp(deltaRotationY / (Time.deltaTime * 100f), -1f, 1f);
        anim.SetFloat("Turn", turnValue);
    }

    /// <summary>
    /// This function shuts down the AI and triggers the animator clip immediately.
    /// </summary>
    public void Die()
    {
        if (currentState == NPCState.Dead) return;

        Debug.Log($"<b>[{gameObject.name}]</b> AI received lethal call. Playing death animation.");
        currentState = NPCState.Dead;

        if (navMeshAgent != null)
        {
            navMeshAgent.isStopped = true;
            navMeshAgent.velocity = Vector3.zero;
            navMeshAgent.enabled = false; // Prevents the AI engine from sliding the corpse
        }

        if (anim != null)
        {
            anim.SetTrigger("Die");
        }
    }
}
