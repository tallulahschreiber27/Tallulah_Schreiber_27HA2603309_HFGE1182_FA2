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
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float attackRange = 1f;
    [SerializeField] private float patrolRange = 4f;
    [SerializeField] private float knockbackForce = 5f;
    private Rigidbody rb;

    [Header("ATTACK PARAMETERS")] 
    [SerializeField] private Transform playerTransform;
    [SerializeField] private float damage;

    [Header("AREA RESTRICTION")] 
    [SerializeField] private Transform areaCenter;
    [SerializeField] private float areaRadius = 12f;
    
    private Vector3 target;
    
    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        navMeshAgent =  GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        PickNewPatrolPoint();
    }

    // Update is called once per frame
    void Update()
    {
        if (currentState == EnemyState.Dead)
        {
            return;
        }
        
        if (navMeshAgent.enabled == false)
        {
            return;
        }
        
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (Vector3.Distance(transform.position, areaCenter.position) > areaRadius)
        {
            navMeshAgent.SetDestination(areaCenter.position);
            
            return;
        }

        if (distanceToPlayer <= attackRange)
        {
            currentState = EnemyState.Attack;
        }
        else if (distanceToPlayer <= detectionRange)
        {
            currentState = EnemyState.Chase;
        }
        
        StateHandler();
    }

    public void StateHandler()
    {
      
        switch (currentState)
        {
            case EnemyState.Idle:
                navMeshAgent.isStopped = false;
                StartCoroutine(IdleFor(1f));
                break;
            case EnemyState.Patrol:
                PatrolRoutine();
                break;
            case EnemyState.Chase:
                navMeshAgent.isStopped = false;
                navMeshAgent.SetDestination(playerTransform.position);
                break;
            case EnemyState.Attack:
                navMeshAgent.isStopped = true;
                transform.LookAt(playerTransform.position);
                AttackRoutine();
                break;
            case EnemyState.Dead:
                break;
        }
    }
    
    //PATROL

    public void PatrolRoutine()
    {

        if (navMeshAgent.enabled == false)
        {
            return;
        }
        
        navMeshAgent.isStopped = false;

        if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance < .5f)
        {
            PickNewPatrolPoint();
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
        yield return new WaitForSeconds(time);
        currentState = EnemyState.Patrol;
    }
    
    //ATTACK

    public void AttackRoutine()
    {
        if (navMeshAgent.enabled == false)
        {
            return;
        }
        
        navMeshAgent.SetDestination(playerTransform.position);
    }

    public IEnumerator Knockback()
    {
        navMeshAgent.enabled = false;
        rb.isKinematic = false;
        rb.useGravity = true;
        
        Vector3 forceDirection = (transform.position - target).normalized;
        forceDirection.y = 0.5f; 

        
        rb.linearVelocity = Vector3.zero;
        rb.AddForce(forceDirection * knockbackForce, ForceMode.Impulse);
        yield return new WaitForSeconds(1f);
        
        navMeshAgent.enabled = true;
        rb.isKinematic = true;
        rb.useGravity = false;
        navMeshAgent.isStopped = false;
    }

    private void OnCollisionEnter(Collision col)
    {
        if (col.gameObject.CompareTag("Player"))
        {
            //navMeshAgent.isStopped = true;
            StartCoroutine(Knockback());
            col.gameObject.GetComponent<HealthHandler>().DamageHandler("Enemy", damage);
        }
    }
}
