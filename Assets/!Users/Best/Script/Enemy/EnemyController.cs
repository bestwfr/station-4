using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;
using System.Collections.Generic;

// 1. Define the possible states for the enemy
public enum EnemyState
{
    Wander,     // Neutral patrolling, usually large radius
    Chase,
    Investigate, // Aggressive, multi-point check (high urgency)
    Patrol,      // Small-radius patrolling (on edge/post-investigation persistence)
    Search       // New: Cautious move to a single point, then wait/look (low suspicion)
}

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : MonoBehaviour
{
    [Header("Dependencies")]
    private NavMeshAgent agent;

    [Header("Movement Configuration")]
    public float chaseSpeed = 5f;
    public float wanderSpeed = 2f;
    public float patrolSpeed = 1.5f; // Slower, more deliberate patrol
    public float investigateSpeed = 4f; // Increased speed for urgency
    
    public float wanderRadius = 30f;
    public float patrolRadius = 10f; // Smaller radius for suspicious patrolling
    
    public int investigatePointsCount = 4; // Total points to check (center + 3 random)
    public float investigationDuration = 5f; // Duration of search/patrol/persistence phase

    [Header("Current State")]
    public EnemyState currentState = EnemyState.Wander;
    
    private Vector3 currentPatrolCenter; // Center point for Patrol/Investigate/Search
    private Queue<Vector3> investigationPoints = new Queue<Vector3>(); // Points to check
    private float investigationTimer = 0f;
    
    [Tooltip("The actual target Transform (set by EnemyAI when target is fully known)")]
    public Transform target; // Public field for the target's Transform

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        
        if (currentState == EnemyState.Wander)
        {
            SetDestinationToRandomPoint(transform.position, wanderRadius, wanderSpeed);
        }
    }

    // --- Public Commands from EnemyAI ---

    /// <summary>
    /// Starts the Chase state. Assumes the public 'target' field is set.
    /// </summary>
    public void StartChase()
    {
        if (target != null)
        {
            ChangeState(EnemyState.Chase);
        }
    }
    
    /// <summary>
    /// Starts the Investigate state (aggressive, multi-point search).
    /// </summary>
    public void StartInvestigate(Vector3 location)
    {
        // Only start a new investigation if the location is significantly different
        if (currentState != EnemyState.Investigate || Vector3.Distance(currentPatrolCenter, location) > 1f)
        {
            currentPatrolCenter = location;
            GenerateInvestigationPath(location);
            ChangeState(EnemyState.Investigate);
        }
    }

    /// <summary>
    /// Starts the Cautious Search state (move to single point, then wait/look).
    /// </summary>
    public void StartSearch(Vector3 location)
    {
        if (currentState != EnemyState.Search || Vector3.Distance(currentPatrolCenter, location) > 1f)
        {
            currentPatrolCenter = location;
            ChangeState(EnemyState.Search);
        }
    }
    
    /// <summary>
    /// Starts the Patrol state around a specified center point (Persistence).
    /// </summary>
    public void StartPatrol(Vector3 center)
    {
        currentPatrolCenter = center;
        ChangeState(EnemyState.Patrol);
    }

    /// <summary>
    /// Starts the Wander state with a large radius.
    /// </summary>
    public void StartWander()
    {
        ChangeState(EnemyState.Wander);
    }

    // --- Core Execution Loop ---
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

            case EnemyState.Patrol:
                PatrolState();
                break;
            
            case EnemyState.Search:
                SearchState();
                break;
        }
    }
    
    // --- Helper for generating random navmesh points ---
    private bool SetDestinationToRandomPoint(Vector3 center, float radius, float speed)
    {
        Vector3 randomDirection = Random.insideUnitSphere * radius;
        randomDirection += center;
        NavMeshHit hit;
        
        if (NavMesh.SamplePosition(randomDirection, out hit, radius, NavMesh.AllAreas))
        {
            agent.speed = speed;
            agent.SetDestination(hit.position);
            return true;
        }
        return false;
    }

    private void GenerateInvestigationPath(Vector3 center)
    {
        investigationPoints.Clear();
        
        // 1. Check the noise center first
        NavMeshHit centerHit;
        if (NavMesh.SamplePosition(center, out centerHit, 2f, NavMesh.AllAreas))
            investigationPoints.Enqueue(centerHit.position); 
        else
            investigationPoints.Enqueue(center); // Fallback to raw position

        // 2. Generate remaining random points around the center
        for (int i = 0; i < investigatePointsCount - 1; i++)
        {
            Vector3 randomPoint = center + Random.insideUnitSphere * 5f; // 5m radius search
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, 5f, NavMesh.AllAreas))
            {
                investigationPoints.Enqueue(hit.position);
            }
        }
    }
    
    // --- State Logic Functions ---

    private void WanderState()
    {
        // If the agent has arrived at its current destination, pick a new one
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            SetDestinationToRandomPoint(transform.position, wanderRadius, wanderSpeed);
        }
    }
    
    private void PatrolState()
    {
        // Patrol is like Wander, but slower and focused on the last suspicion center
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            // Patrol around the center of suspicion
            if (!SetDestinationToRandomPoint(currentPatrolCenter, patrolRadius, patrolSpeed))
            {
                 // If a valid patrol point can't be found, give up and wander
                 StartWander();
            }
            
            // Increment timer to control how long the patrol lasts
            investigationTimer += Time.deltaTime;
            if (investigationTimer >= investigationDuration)
            {
                StartWander(); // Go back to global wandering
            }
        }
    }

    private void ChaseState()
    {
        // Chase requires a target and is updated every frame by setting the destination.
        if (target != null)
        {
            agent.SetDestination(target.position);
        }
        else
        {
            // If the target somehow disappeared, go to investigate the last spot
            StartInvestigate(transform.position); 
        }
    }

    private void InvestigateState()
    {
        // 1. Check if we arrived at the current point
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            if (investigationPoints.Count > 0)
            {
                // 2. Move to the next point in the queue
                Vector3 nextPoint = investigationPoints.Dequeue();
                agent.SetDestination(nextPoint);
            }
            else
            {
                // 3. All points checked, transition to a tight Patrol (Persistence!)
                StartPatrol(currentPatrolCenter);
            }
        }
    }
    
    private void SearchState()
    {
        // Phase 1: Moving to the suspicion center (only happens once on entry)
        if (agent.remainingDistance > 0.5f)
        {
            // Still moving towards the initial suspicion point
            return;
        }

        // Phase 2: Arrived, now looking around (if timer hasn't started)
        if (investigationTimer <= 0f)
        {
            // Stop movement at the point of interest and start the timer
            agent.isStopped = true; 
        }

        // Phase 3: Timer counting down
        investigationTimer += Time.deltaTime;

        if (investigationTimer >= investigationDuration)
        {
            // Time's up, nothing found.
            agent.isStopped = false; // Important: Resume movement before starting Wander
            StartWander();
        }
    }

    // --- State Transition Helper ---

    private void ChangeState(EnemyState newState)
    {
        if (currentState == newState) return;

        // Exit Logic for current state (Clean up)
        if (currentState == EnemyState.Investigate || currentState == EnemyState.Patrol || currentState == EnemyState.Search)
        {
            investigationTimer = 0f;
            investigationPoints.Clear();
            agent.isStopped = false; // Ensure movement is re-enabled on exit
        }

        // Enter Logic for new state
        if (newState == EnemyState.Chase)
        {
            agent.speed = chaseSpeed;
        }
        else if (newState == EnemyState.Wander)
        {
            SetDestinationToRandomPoint(transform.position, wanderRadius, wanderSpeed);
        }
        else if (newState == EnemyState.Investigate)
        {
            agent.speed = investigateSpeed;
            // Destination is set in StartInvestigate
        }
        else if (newState == EnemyState.Patrol)
        {
            agent.speed = patrolSpeed;
            // Destination is set in PatrolState when it runs
        }
        else if (newState == EnemyState.Search)
        {
            agent.speed = patrolSpeed; // Use slower speed for cautious approach
            // Set destination to the single point
            NavMeshHit hit;
            if (NavMesh.SamplePosition(currentPatrolCenter, out hit, 1f, NavMesh.AllAreas))
                agent.SetDestination(hit.position);
            // Timer logic starts in SearchState()
        }
        
        currentState = newState;
        Debug.Log(gameObject.name + " changed state to: " + newState);
    }
}
