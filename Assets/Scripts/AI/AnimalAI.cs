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

    private Vector3 target;
    private bool isIdleCoroutineRunning = false;

    private float scaleMultiplier = 1f;

    void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();

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

        currentState = NPCState.Patrol;
        PickNewPatrolPoint();
    }

    void Update()
    {
        if (currentState == NPCState.Dead || areaCenter == null) return;

        if (Vector3.Distance(transform.position, areaCenter.position) > (areaRadius * scaleMultiplier))
        {
            isIdleCoroutineRunning = false;
            StopAllCoroutines();

            navMeshAgent.isStopped = false;
            navMeshAgent.SetDestination(areaCenter.position);
            currentState = NPCState.Patrol;
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= (detectionRange * scaleMultiplier))
        {
            if (currentState != NPCState.Runaway)
            {
                isIdleCoroutineRunning = false;
                StopAllCoroutines();
                currentState = NPCState.Runaway;
            }
        }
        else if (currentState == NPCState.Runaway && distanceToPlayer > ((detectionRange + 5f) * scaleMultiplier))
        {
            currentState = NPCState.Idle;
        }

        StateHandler();
    }

    public void StateHandler()
    {
        switch (currentState)
        {
            case NPCState.Idle:
                navMeshAgent.isStopped = true;
                if (!isIdleCoroutineRunning)
                {
                    StartCoroutine(IdleFor(2f));
                }
                break;

            case NPCState.Patrol:
                PatrolRoutine();
                break;

            case NPCState.Runaway:
                if (playerTransform == null) return;

                navMeshAgent.isStopped = false;
                Vector3 directionToTarget = transform.position - playerTransform.position;

                Vector3 targetPosition = transform.position + directionToTarget.normalized * (runawayRange * scaleMultiplier);

                navMeshAgent.SetDestination(targetPosition);
                break;

            case NPCState.Dead:
                navMeshAgent.isStopped = true;
                break;
        }
    }

    

    public void PatrolRoutine()
    {
        navMeshAgent.isStopped = false;

        float arrivalThreshold = (navMeshAgent.stoppingDistance + 0.1f) * scaleMultiplier;

        if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance <= arrivalThreshold)
        {
            currentState = NPCState.Idle;
        }
    }

    public void PickNewPatrolPoint()
    {
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

        PickNewPatrolPoint();

        currentState = NPCState.Patrol;
        isIdleCoroutineRunning = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (areaCenter != null)
        {
            float currentGizmoScale = Application.isPlaying ? scaleMultiplier : Mathf.Max(transform.localScale.x, transform.localScale.y, transform.localScale.z);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(areaCenter.position, areaRadius * currentGizmoScale);
        }
    }
}
