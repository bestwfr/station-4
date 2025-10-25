using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;
using System.Collections.Generic;

// 1. Define the possible states for the enemy
public enum EnemyState
{
    Stalk,       // NEW DEFAULT: Follows player from a calculated distance.
    Chase,
    Investigate, // Aggressive, multi-point search (high urgency)
    Patrol,      // Small-radius patrolling (on edge/post-investigation persistence)
    Search       // Cautious move to a single point, then wait/look (low suspicion)
}

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : MonoBehaviour
{
    [Header("Dependencies")]
    private NavMeshAgent agent;

    [Header("Movement Configuration")]
    public float chaseSpeed = 5f;
    public float investigateSpeed = 4f; // Increased speed for urgency
    public float patrolSpeed = 1.5f; // Slower, more deliberate patrol
    
    [Header("Stalk Configuration")]
    [Tooltip("The speed used when stalking.")]
    public float stalkSpeed = 1.0f;
    [Tooltip("The desired distance to maintain from the player.")]
    public float stalkDistance = 25f;
    [Tooltip("How often (in seconds) the enemy recalculates its stalking position.")]
    public float stalkPathRecalculateDelay = 3.0f;
    
    public float patrolRadius = 10f; 
    public int investigatePointsCount = 4; 
    public float investigationDuration = 5f;

    [Header("Current State")]
    public EnemyState currentState = EnemyState.Stalk;
    
    private Vector3 currentPatrolCenter; 
    private Queue<Vector3> investigationPoints = new Queue<Vector3>(); 
    private float investigationTimer = 0f;
    private float stalkTimer = 0f; // Timer for stalking path recalculation
    
    [Tooltip("The actual target Transform (set by EnemyAI when target is fully known)")]
    public Transform target; // Public field for the target's Transform

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        
        // Initial state setup
        if (currentState == EnemyState.Stalk)
        {
            agent.speed = stalkSpeed;
        }
    }

    // --- Public Commands from EnemyAI ---

    public void StartChase()
    {
        if (target != null)
        {
            ChangeState(EnemyState.Chase);
        }
    }
    
    public void StartInvestigate(Vector3 location)
    {
        if (currentState != EnemyState.Investigate || Vector3.Distance(currentPatrolCenter, location) > 1f)
        {
            currentPatrolCenter = location;
            GenerateInvestigationPath(location);
            ChangeState(EnemyState.Investigate);
        }
    }

    public void StartSearch(Vector3 location)
    {
        if (currentState != EnemyState.Search || Vector3.Distance(currentPatrolCenter, location) > 1f)
        {
            currentPatrolCenter = location;
            ChangeState(EnemyState.Search);
        }
    }
    
    public void StartPatrol(Vector3 center)
    {
        currentPatrolCenter = center;
        ChangeState(EnemyState.Patrol);
    }

    /// <summary>
    /// Starts the Stalk state (the new default neutral behavior).
    /// </summary>
    public void StartStalk()
    {
        ChangeState(EnemyState.Stalk);
    }

    // --- Core Execution Loop ---
    private void Update()
    {
        switch (currentState)
        {
            case EnemyState.Stalk:
                StalkState();
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
    
    // --- Stalking Logic ---
    private void StalkState()
    {
        // Action: Slowly track the player from a distance
        stalkTimer += Time.deltaTime;

        // Recalculate the stalking destination periodically or if we've arrived
        if (stalkTimer >= stalkPathRecalculateDelay || (agent.remainingDistance < 1f && !agent.pathPending))
        {
            SetNewStalkDestination();
            stalkTimer = 0f; // Reset timer after setting new path
        }
    }
    
    private void SetNewStalkDestination()
    {
        if (target == null) return; // Cannot stalk without a target

        Vector3 playerPos = target.position;
        Vector3 enemyPos = transform.position;
        float distance = Vector3.Distance(playerPos, enemyPos);
        
        Vector3 desiredDirection;
        
        // 1. Determine a direction to move based on current distance
        if (distance < stalkDistance * 0.9f)
        {
            // Too close, move away (using the inverse direction)
            desiredDirection = (enemyPos - playerPos).normalized;
        }
        else if (distance > stalkDistance * 1.5f)
        {
            // Too far, move towards (close the gap faster)
            desiredDirection = (playerPos - enemyPos).normalized;
        }
        else 
        {
            // Maintain general stalking distance, slightly circle the player
            float angle = Random.Range(0f, 360f);
            desiredDirection = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle));
        }

        // 2. Calculate the target point offset by the stalk distance
        float finalStalkDistance = stalkDistance + Random.Range(-5f, 5f);
        Vector3 targetPoint = playerPos + desiredDirection.normalized * finalStalkDistance;

        NavMeshHit hit;
        // 3. Sample the NavMesh to find a valid point near the calculated spot
        if (NavMesh.SamplePosition(targetPoint, out hit, stalkDistance * 2f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }
    // -----------------------

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

    private void ChaseState()
    {
        // Chase requires a target and is updated every frame by setting the destination.
        if (target != null)
        {
            agent.SetDestination(target.position);
        }
        else
        {
            // Target lost (shouldn't happen if EnemyAI is working, but safe fails)
            StartStalk(); 
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
    
    private void PatrolState()
    {
        // Patrol is like Wander, but slower and focused on the last suspicion center
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            // Patrol around the center of suspicion
            if (!SetDestinationToRandomPoint(currentPatrolCenter, patrolRadius, patrolSpeed))
            {
                 // If a valid patrol point can't be found, give up and stalk
                 StartStalk();
            }
            
            // Increment timer to control how long the patrol lasts
            investigationTimer += Time.deltaTime;
            if (investigationTimer >= investigationDuration)
            {
                StartStalk(); // Go back to stalking the player
            }
        }
    }

    private void SearchState()
    {
        // Phase 1: Moving to the suspicion center
        if (agent.remainingDistance > 0.5f)
        {
            return;
        }

        // Phase 2: Arrived, now looking around
        if (investigationTimer <= 0f)
        {
            agent.isStopped = true; 
        }

        // Phase 3: Timer counting down
        investigationTimer += Time.deltaTime;

        if (investigationTimer >= investigationDuration)
        {
            // Time's up, nothing found.
            agent.isStopped = false; 
            StartStalk(); // Go back to stalking the player
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
        else if (newState == EnemyState.Stalk)
        {
            agent.speed = stalkSpeed;
            stalkTimer = stalkPathRecalculateDelay; // Force immediate path calculation on entry
        }
        else if (newState == EnemyState.Investigate)
        {
            agent.speed = investigateSpeed;
        }
        else if (newState == EnemyState.Patrol)
        {
            agent.speed = patrolSpeed;
        }
        else if (newState == EnemyState.Search)
        {
            agent.speed = patrolSpeed; // Use slower speed for cautious approach
            // Set destination to the single point
            NavMeshHit hit;
            if (NavMesh.SamplePosition(currentPatrolCenter, out hit, 1f, NavMesh.AllAreas))
                agent.SetDestination(hit.position);
        }
        
        currentState = newState;
        Debug.Log(gameObject.name + " changed state to: " + newState);
    }
}
