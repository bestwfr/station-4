using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

// 1. Define the possible states for the enemy
public enum EnemyState
{
    Wander,
    Chase,
    Investigate
}

// NOTE: This script is now purely responsible for movement execution and state management, 
// its actions are commanded by the EnemyAI/AwarenessSystem.
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : MonoBehaviour
{
    [Header("Dependencies")]
    private NavMeshAgent agent;

    [Header("Movement Configuration")]
    public float chaseSpeed = 5f;
    public float wanderSpeed = 2f;
    public float investigateSpeed = 3f;
    public float wanderRadius = 20f;
    public float investigationDuration = 5f;

    [Header("Current State")]
    public EnemyState currentState = EnemyState.Wander;
    private Vector3 lastTargetLocation;
    private float investigationTimer = 0f;
    
    public Transform target;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        
        // Ensure initial state is set
        if (currentState == EnemyState.Wander)
        {
            SetNewWanderDestination();
        }
    }

    // --- Public Commands from EnemyAI ---

    /// <summary>
    /// Starts the Chase state, moving immediately toward the target's current position.
    /// </summary>
    public void StartChase()
    {
        ChangeState(EnemyState.Chase);
    }
    
    /// <summary>
    /// Starts the Investigate state, moving to a specific noise or last-known location.
    /// </summary>
    public void StartInvestigate(Vector3 location)
    {
        // Only start a new investigation if the location is significantly different or we are not already investigating it.
        if (currentState != EnemyState.Investigate || Vector3.Distance(lastTargetLocation, location) > 1f)
        {
            lastTargetLocation = location;
            ChangeState(EnemyState.Investigate);
        }
    }

    /// <summary>
    /// Starts the Wander state and immediately picks a random destination.
    /// </summary>
    public void StartWander()
    {
        ChangeState(EnemyState.Wander);
    }


    // --- Core Execution Loop (Only runs movement logic) ---
    private void Update()
    {
        switch (currentState)
        {
            case EnemyState.Wander:
                WanderState();
                break;

            case EnemyState.Chase:
                ChaseState();
                break;
            
            case EnemyState.Investigate:
                InvestigateState();
                break;
        }
    }

    // --- State Logic Functions ---

    private void WanderState()
    {
        // If the agent has arrived at its current destination, pick a new one
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            SetNewWanderDestination();
        }
    }
    
    private void SetNewWanderDestination()
    {
        Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
        randomDirection += transform.position;
        NavMeshHit hit;
        
        // Find the nearest valid point on the NavMesh
        if (NavMesh.SamplePosition(randomDirection, out hit, wanderRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    private void ChaseState()
    {
        agent.SetDestination(target.position);
    }

    private void InvestigateState()
    {
        // Move towards the target location
        if (!agent.pathPending && agent.remainingDistance > 0.5f)
        {
             agent.SetDestination(lastTargetLocation);
        }
        
        // Once the enemy reaches the spot, start a timer
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            investigationTimer += Time.deltaTime;
            
            if (investigationTimer >= investigationDuration)
            {
                // Finished investigating, go back to wandering
                StartWander();
            }
        }
    }
    
    // --- State Transition Helper ---

    private void ChangeState(EnemyState newState)
    {
        if (currentState == newState) return;

        // Exit Logic for current state
        if (currentState == EnemyState.Investigate)
        {
            investigationTimer = 0f;
        }

        // Enter Logic for new state
        if (newState == EnemyState.Chase)
        {
            agent.speed = chaseSpeed;
        }
        else if (newState == EnemyState.Wander)
        {
            agent.speed = wanderSpeed;
            SetNewWanderDestination(); // Immediately pick a new destination
        }
        else if (newState == EnemyState.Investigate)
        {
            agent.speed = investigateSpeed;
            agent.SetDestination(lastTargetLocation);
        }
        
        currentState = newState;
        Debug.Log(gameObject.name + " changed state to: " + newState);
    }

    // --- Editor Visualization -
}