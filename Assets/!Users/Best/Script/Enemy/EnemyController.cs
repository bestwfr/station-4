using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;
using System.Collections.Generic;

// 1. Define the possible states for the enemy
public enum EnemyState
{
    Stalk,       // Default state: Follows player from a calculated distance.
    Chase,
    Investigate, // Aggressive, multi-point check (high urgency)
    Patrol,      // Small-radius patrolling (on edge/post-investigation persistence)
    Search,      // Cautious move to a single point, then wait/look (low suspicion)
    Flee,        // Runs away from the player/threat (high speed)
    Retreat      // Slowly backs away while facing the player (slow speed, light vulnerability)
}

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : MonoBehaviour
{
    [Header("Dependencies")]
    private NavMeshAgent agent;
    public Animator animator; // <--- This reference is now used for animations!

    [Header("Movement Configuration")]
    public float chaseSpeed = 5f;
    public float investigateSpeed = 4f; 
    public float patrolSpeed = 1.5f; 
    
    [Header("Stalk Configuration")]
    [Tooltip("The speed used when stalking.")]
    public float stalkSpeed = 1.0f;
    public float stalkDistance = 25f;
    public float stalkPathRecalculateDelay = 3.0f;
    
    [Header("Flee Configuration")]
    // This is set high (7f) to make the Flee state look scared and panicked.
    public float fleeSpeed = 7f; 
    public float fleeDistance = 50f;
    public float fleeDuration = 8f; // Also used as Retreat Timeout
    
    [Header("Retreat Configuration")]
    [Tooltip("How long the enemy is stunned before backing away.")]
    public float retreatStunDuration = 1.5f;
    [Tooltip("Speed when walking backward.")]
    public float retreatSpeed = 0.5f;
    [Tooltip("Target distance to retreat to.")]
    public float retreatDistance = 10f; // Adjusted from 15f to 10f
    
    public float patrolRadius = 10f; 
    public int investigatePointsCount = 4; 
    public float investigationDuration = 5f;

    [Header("Current State")]
    public EnemyState currentState = EnemyState.Stalk;
    
    public Vector3 currentPatrolCenter; 
    private Queue<Vector3> investigationPoints = new Queue<Vector3>(); 
    private float investigationTimer = 0f;
    private float stalkTimer = 0f; 
    private float fleeTimer = 0f;
    private float retreatTimer = 0f;
    private Vector3 initialRetreatPosition; 
    private bool retreatDestinationSet = false; 
    
    [Tooltip("The actual target Transform (set by EnemyAI when target is fully known)")]
    public Transform target; 

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        
        if (currentState == EnemyState.Stalk)
        {
            agent.speed = stalkSpeed;
        }
        
        agent.updateRotation = false;
    }
    
    // --- State Locking Helper ---

    /// <summary>
    /// Checks if the current state is Flee or Retreat, which block external state changes.
    /// </summary>
    private bool IsVulnerableOrFleeing()
    {
        return currentState == EnemyState.Flee || currentState == EnemyState.Retreat;
    }


    // --- Public Commands from EnemyAI ---
    // (StartChase, StartInvestigate, StartSearch, StartPatrol, StartStalk, StartFlee, StartRetreat remain unchanged)
    // ... [Omitted for brevity]

    public void StartChase()
    {
        if (IsVulnerableOrFleeing()) return; // IGNORE command if Fleeing/Retreating
        
        if (target != null)
        {
            ChangeState(EnemyState.Chase);
        }
    }
    
    public void StartInvestigate(Vector3 location)
    {
        if (IsVulnerableOrFleeing()) return; // IGNORE command if Fleeing/Retreating

        if (currentState != EnemyState.Investigate || Vector3.Distance(currentPatrolCenter, location) > 1f)
        {
            currentPatrolCenter = location;
            GenerateInvestigationPath(location);
            ChangeState(EnemyState.Investigate);
        }
    }

    public void StartSearch(Vector3 location)
    {
        if (IsVulnerableOrFleeing()) return; // IGNORE command if Fleeing/Retreating

        if (currentState != EnemyState.Search || Vector3.Distance(currentPatrolCenter, location) > 1f)
        {
            currentPatrolCenter = location;
            ChangeState(EnemyState.Search);
        }
    }
    
    public void StartPatrol(Vector3 center)
    {
        if (IsVulnerableOrFleeing()) return; // IGNORE command if Fleeing/Retreating
        
        currentPatrolCenter = center;
        ChangeState(EnemyState.Patrol);
    }

    public void StartStalk()
    {
        if (IsVulnerableOrFleeing()) return; // IGNORE command if Fleeing/Retreating

        ChangeState(EnemyState.Stalk);
    }
    
    public void StartFlee()
    {
        // Flee is the only state that can interrupt anything else.
        ChangeState(EnemyState.Flee);
    }
    
    public void StartRetreat()
    {
        // Allow Retreat to interrupt any non-Flee state.
        if (currentState != EnemyState.Flee)
        {
             ChangeState(EnemyState.Retreat);
        }
    }


    // --- Core Execution Loop ---
    private void Update()
    {
        // Must check if target is null for states that rely on it
        if (target == null && (currentState == EnemyState.Chase || currentState == EnemyState.Stalk || currentState == EnemyState.Retreat))
        {
            // Safety transition: The enemy can always transition to Stalk internally 
            // if its primary target vanishes (which is likely if it was in Retreat or Chase).
            ChangeState(EnemyState.Stalk); 
        }
        
        // Handle enemy rotation for non-retreat states
        bool shouldRotateBasedOnVelocity = 
            currentState != EnemyState.Retreat && agent.velocity.sqrMagnitude > 0.01f;

        if (shouldRotateBasedOnVelocity)
        {
            Quaternion lookRotation = Quaternion.LookRotation(agent.velocity.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 10f);
        }
        
        // --- ANIMATION: Set Speed parameter for Walking/Running (All states EXCEPT Retreat) ---
        if (animator != null && currentState != EnemyState.Retreat)
        {
            // Calculate speed relative to the agent's current maximum allowed speed (for normalization)
            // Running (Chase/Flee) will have Speed closer to 1.0, Walking will be lower.
            float targetSpeed = agent.velocity.magnitude;
            animator.SetFloat("Speed", targetSpeed);
        }
        // ------------------------------------------------------------------------------------

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
            case EnemyState.Flee:
                FleeState();
                break;
            case EnemyState.Retreat: 
                RetreatState();
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
    
    // --- State Logic Functions ---

    private void StalkState()
    {
        stalkTimer += Time.deltaTime;
        if (stalkTimer >= stalkPathRecalculateDelay || (agent.remainingDistance < 1f && !agent.pathPending))
        {
            SetNewStalkDestination();
            stalkTimer = 0f; 
        }
    }
    
    private void SetNewStalkDestination()
    {
        if (target == null) return; 

        Vector3 playerPos = target.position;
        Vector3 enemyPos = transform.position;
        float distance = Vector3.Distance(playerPos, enemyPos);
        
        Vector3 desiredDirection;
        if (distance < stalkDistance * 0.9f)
        {
            desiredDirection = (enemyPos - playerPos).normalized;
        }
        else if (distance > stalkDistance * 1.5f)
        {
            desiredDirection = (playerPos - enemyPos).normalized;
        }
        else 
        {
            float angle = Random.Range(0f, 360f);
            desiredDirection = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle));
        }

        float finalStalkDistance = stalkDistance + Random.Range(-5f, 5f);
        Vector3 targetPoint = playerPos + desiredDirection.normalized * finalStalkDistance;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPoint, out hit, stalkDistance * 2f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    private void ChaseState()
    {
        
        
        if (target != null)
        {
            agent.SetDestination(target.position);
        }
    }
    
    private void FleeState()
    {
        fleeTimer -= Time.deltaTime;
        
        // 1. EXIT CONDITION: Flee timer has expired.
        if (fleeTimer <= 0f)
        {
            if (currentPatrolCenter != Vector3.zero) 
            {
                ChangeState(EnemyState.Investigate); 
            }
            else
            {
                ChangeState(EnemyState.Stalk);
            }
            return;
        }

        // 2. RECALCULATE DESTINATION: If we are close to the target destination OR path is invalid,
        // and we have enough time left to reach a new point, set a new destination.
        float timeToNextMove = 1f; // Minimum time needed to justify setting a new point
        if (fleeTimer > timeToNextMove && !agent.pathPending && agent.remainingDistance < 1f)
        {
            SetFleeDestination();
        }
        
        // Secondary check: If the target (player) is too close, immediately reset destination 
        // to ensure we keep running away.
        if (target != null)
        {
            if (Vector3.Distance(transform.position, target.position) < fleeDistance / 3f)
            {
                SetFleeDestination();
            }
        }
    }
    
    private void SetFleeDestination()
    {
        Vector3 centerOfThreat = target != null ? target.position : currentPatrolCenter;
        // The direction *away* from the threat
        Vector3 fleeDirection = (transform.position - centerOfThreat).normalized; 
        Vector3 targetPoint = transform.position + fleeDirection * fleeDistance; 

        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPoint, out hit, fleeDistance, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
        else
        {
            // If the calculated point is off-navmesh, try a random direction 
            // just to keep the enemy moving until the timer expires.
            SetDestinationToRandomPoint(transform.position, fleeDistance, fleeSpeed);
        }
    }

    private void RetreatState()
    {
        // --- TIMEOUT EXIT LOGIC ---
        if (retreatTimer >= fleeDuration * 2) 
        {
            Debug.Log("Retreat timed out! Forcing transition to Stalk.");
            ChangeState(EnemyState.Stalk);
            return;
        }
        // --------------------------
        
        retreatTimer += Time.deltaTime;

        // Phase 1: Stand Still / Stun
        if (retreatTimer < retreatStunDuration)
        {
            agent.isStopped = true;
            
            // --- ANIMATION: STUN/IDLE ---
            if (animator != null)
            {
                // Play the face close animation during the stun
                animator.CrossFade("face close", 0f);
            }
            
            if (target != null)
            {
                Vector3 lookDirection = (target.position - transform.position).normalized;
                Quaternion targetRotation = Quaternion.LookRotation(new Vector3(lookDirection.x, 0, lookDirection.z));
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
            }
            return;
        }

        // Phase 2: Straight Back-Walk Retreat
        agent.isStopped = false;
        
        // --- ANIMATION: BACKWARD WALK ---
        if (animator != null)
        {
            // Play the backward walk animation after the stun is over
            animator.CrossFade("face close walk backward", 0);
        }
        
        // Keep facing the player 
        if (target != null)
        {
            Vector3 lookDirection = (target.position - transform.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(new Vector3(lookDirection.x, 0, lookDirection.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
        }

        // 1. Set the destination once after stun phase
        if (!retreatDestinationSet)
        {
            if (target == null)
            {
                ChangeState(EnemyState.Stalk);
                return;
            }

            Vector3 targetDirection = (target.position - initialRetreatPosition).normalized;
            Vector3 retreatDirection = -targetDirection;
            Vector3 targetPoint = initialRetreatPosition + retreatDirection * retreatDistance;
            
            NavMeshHit hit;
            if (NavMesh.SamplePosition(targetPoint, out hit, retreatDistance, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                retreatDestinationSet = true;
            } 
            else
            {
                // If destination fails, transition out
                ChangeState(EnemyState.Stalk);
                return;
            }
        }
        
        // 2. Check for successful retreat (either destination reached OR required distance covered)
        if ((agent.remainingDistance < 0.5f && retreatDestinationSet) || 
            Vector3.Distance(transform.position, initialRetreatPosition) >= retreatDistance)
        {
            ChangeState(EnemyState.Stalk);
        }
    }

    private void GenerateInvestigationPath(Vector3 center)
    {
        investigationPoints.Clear();
        NavMeshHit centerHit;
        if (NavMesh.SamplePosition(center, out centerHit, 2f, NavMesh.AllAreas))
            investigationPoints.Enqueue(centerHit.position); 
        else
            investigationPoints.Enqueue(center); 

        for (int i = 0; i < investigatePointsCount - 1; i++)
        {
            Vector3 randomPoint = center + Random.insideUnitSphere * 5f; 
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, 5f, NavMesh.AllAreas))
            {
                investigationPoints.Enqueue(hit.position);
            }
        }
    }
    
    private void InvestigateState()
    {
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            if (investigationPoints.Count > 0)
            {
                Vector3 nextPoint = investigationPoints.Dequeue();
                agent.SetDestination(nextPoint);
            }
            else
            {
                ChangeState(EnemyState.Patrol);
            }
        }
    }
    
    private void PatrolState()
    {
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            if (!SetDestinationToRandomPoint(currentPatrolCenter, patrolRadius, patrolSpeed))
            {
                 ChangeState(EnemyState.Stalk);
            }
            
            investigationTimer += Time.deltaTime;
            if (investigationTimer >= investigationDuration)
            {
                ChangeState(EnemyState.Stalk);
            }
        }
    }

    private void SearchState()
    {
        if (agent.remainingDistance > 0.5f)
        {
            return;
        }

        if (investigationTimer <= 0f)
        {
            agent.isStopped = true; 
        }

        investigationTimer += Time.deltaTime;

        if (investigationTimer >= investigationDuration)
        {
            agent.isStopped = false; 
            ChangeState(EnemyState.Stalk);
        }
    }


    // --- State Transition Helper ---

    private void ChangeState(EnemyState newState)
    {
        if (currentState == newState) return;

        // --- Exit Logic ---
        // Ensure all timers/flags are reset when exiting a temporary state
        if (currentState == EnemyState.Investigate || currentState == EnemyState.Patrol || currentState == EnemyState.Search)
        {
            investigationTimer = 0f;
            investigationPoints.Clear();
            agent.isStopped = false; 
        }
        else if (currentState == EnemyState.Flee)
        {
            fleeTimer = 0f;
            agent.isStopped = false;
        }
        else if (currentState == EnemyState.Retreat)
        {
            retreatTimer = 0f;
            agent.isStopped = false;
            retreatDestinationSet = false;
            
            // --- ANIMATION EXIT: Reset Speed to 0 when leaving Retreat to avoid glitches ---
            if (animator != null)
            {
                animator.SetFloat("Speed", 0f);
                animator.CrossFade("Blend Tree", 0.1f); 
            }
        }

        // --- Enter Logic ---
        if (newState == EnemyState.Chase)
        {
            agent.speed = chaseSpeed;
            agent.updateRotation = false; 
        }
        else if (newState == EnemyState.Stalk)
        {
            agent.speed = stalkSpeed;
            stalkTimer = stalkPathRecalculateDelay; 
            agent.updateRotation = false; 
        }
        else if (newState == EnemyState.Investigate)
        {
            agent.speed = investigateSpeed;
            agent.updateRotation = false; 
        }
        else if (newState == EnemyState.Patrol)
        {
            agent.speed = patrolSpeed;
            agent.updateRotation = false; 
        }
        else if (newState == EnemyState.Search)
        {
            agent.speed = patrolSpeed; 
            NavMeshHit hit;
            if (NavMesh.SamplePosition(currentPatrolCenter, out hit, 1f, NavMesh.AllAreas))
                agent.SetDestination(hit.position);
            agent.updateRotation = false; 
        }
        else if (newState == EnemyState.Flee)
        {
            agent.speed = fleeSpeed; 
            fleeTimer = fleeDuration;
            SetFleeDestination(); 
            agent.updateRotation = false; 
        }
        else if (newState == EnemyState.Retreat)
        {
            agent.speed = retreatSpeed; 
            retreatTimer = 0f;
            initialRetreatPosition = transform.position;
            agent.isStopped = true; // Start in the stun phase
            agent.updateRotation = false; // We handle rotation manually
            retreatDestinationSet = false; // Initialize flag on enter
            
            // --- ANIMATION ENTER: Stop the Speed float from controlling this state ---
            if (animator != null)
            {
                animator.SetFloat("Speed", 0f);
            }
        }
        
        currentState = newState;
        Debug.Log(gameObject.name + " changed state to: " + newState);
    }
}
