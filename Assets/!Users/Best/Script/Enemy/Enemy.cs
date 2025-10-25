using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

// 1. Define the possible states for the enemy
public enum EnemyState
{
    Wander,     // Replaces Idle: Enemy moves randomly within an area
    Chase,
    Investigate // Enemy moves to a location where the player was last seen or a noise was heard
}

// Ensure the enemy GameObject has a NavMeshAgent component
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("Drag the player's Transform component here.")]
    public Transform playerTarget;
    private NavMeshAgent agent;

    [Header("Vision & Detection")]
    [Tooltip("The angular field of view (in degrees) for detection.")]
    public float fieldOfViewAngle = 100f;
    [Tooltip("Maximum distance the enemy can see the player.")]
    public float viewDistance = 15f;
    [Tooltip("The height offset for the Raycast origin (enemy's 'eyes').")]
    public float eyeHeight = 1.5f;

    [Header("Movement Configuration")]
    public float chaseSpeed = 5f;
    public float wanderSpeed = 2f;
    public float wanderRadius = 20f;
    public float investigationDuration = 5f;

    [Header("Current State")]
    public EnemyState currentState = EnemyState.Wander;
    private Vector3 lastKnownPlayerPosition;
    private float investigationTimer = 0f;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (playerTarget == null)
        {
            Debug.LogError("Player Target is not assigned on " + gameObject.name + "! AI will be disabled.");
            enabled = false;
        }
        
        // Start in Wander state
        agent.speed = wanderSpeed;
        if (currentState == EnemyState.Wander)
        {
            SetNewWanderDestination();
        }
    }

    // --- Main State Machine Update ---
    private void Update()
    {
        // High-priority check: Can the enemy see the player?
        if (CanSeePlayer())
        {
            // If the enemy sees the player, immediately transition to Chase
            if (currentState != EnemyState.Chase)
            {
                ChangeState(EnemyState.Chase);
            }
        }
        
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
        // ACTION: Move towards the player's current position
        agent.SetDestination(playerTarget.position);

        // TRANSITION: If LOS is broken (enemy cannot see player anymore)
        if (!CanSeePlayer())
        {
            // Set the last position and start investigating
            lastKnownPlayerPosition = playerTarget.position;
            ChangeState(EnemyState.Investigate);
        }
    }

    private void InvestigateState()
    {
        // If we haven't reached the last known position yet, keep moving there
        if (!agent.pathPending && agent.remainingDistance > 0.5f)
        {
             agent.SetDestination(lastKnownPlayerPosition);
        }
        
        // TRANSITION: Once the enemy reaches the spot, start a timer
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            investigationTimer += Time.deltaTime;
            
            // Look around briefly (optional: add rotation logic here)
            
            if (investigationTimer >= investigationDuration)
            {
                // Finished investigating, go back to wandering
                ChangeState(EnemyState.Wander);
            }
        }
    }
    
    // --- Detection Helper ---
    
    private bool CanSeePlayer()
    {
        Vector3 eyePosition = transform.position + Vector3.up * eyeHeight;
        Vector3 directionToPlayer = (playerTarget.position - eyePosition).normalized;
        float distanceToPlayer = Vector3.Distance(eyePosition, playerTarget.position);

        // 1. Distance Check (Within view distance)
        if (distanceToPlayer > viewDistance)
        {
            return false;
        }

        // 2. FOV Check (Within viewing angle)
        if (Vector3.Angle(transform.forward, directionToPlayer) > fieldOfViewAngle / 2f)
        {
            return false;
        }
        
        // 3. Line of Sight (LOS) Check (No obstacles blocking view, like trees or rocks)
        RaycastHit hit;
        
        // We use viewDistance as the max raycast distance
        if (Physics.Raycast(eyePosition, directionToPlayer, out hit, viewDistance))
        {
            // Check if the raycast hit the player's collider
            // NOTE: Ensure your player GameObject has a unique tag (e.g., "Player")
            if (hit.transform == playerTarget)
            {
                return true;
            }
        }
        
        return false;
    }

    // --- State Transition Helper ---

    private void ChangeState(EnemyState newState)
    {
        if (currentState == newState) return;

        // Exit Logic for current state
        if (currentState == EnemyState.Investigate)
        {
            // Reset the timer when leaving investigate state
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
            agent.speed = wanderSpeed; // Move slowly while investigating
        }
        
        currentState = newState;
        Debug.Log(gameObject.name + " changed state to: " + newState);
    }

    // --- Editor Visualization ---

    // Draw gizmos in the Scene view to visualize the AI's detection parameters
    private void OnDrawGizmosSelected()
    {
        // Visualize view distance
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewDistance);

        // Visualize FOV Cone (approximated)
        Vector3 forward = transform.forward;
        Vector3 origin = transform.position + Vector3.up * eyeHeight;
        
        // Draw the direction lines for the FOV cone
        Quaternion leftRayRotation = Quaternion.AngleAxis(-fieldOfViewAngle * 0.5f, Vector3.up);
        Quaternion rightRayRotation = Quaternion.AngleAxis(fieldOfViewAngle * 0.5f, Vector3.up);
        
        Vector3 leftRayDirection = leftRayRotation * forward;
        Vector3 rightRayDirection = rightRayRotation * forward;

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(origin, leftRayDirection * viewDistance);
        Gizmos.DrawRay(origin, rightRayDirection * viewDistance);
        
        if (currentState == EnemyState.Investigate)
        {
             // Visualize last known position
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(lastKnownPlayerPosition, 0.5f);
        }
    }
}
