using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;
using System.Collections.Generic;
using System.Collections;

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
    public Animator animator; 
    [Tooltip("The AudioSource component on the enemy. REQUIRED for all sound effects.")]
    public AudioSource audioSource; 

    [Header("State Audio Clips")]
    [Tooltip("The screech or scream sound when the enemy starts Fleeing.")]
    public AudioClip fleeScreechClip; 
    [Tooltip("The heavy hit or roar sound when the enemy starts Retreating.")]
    public AudioClip retreatScreechClip; 
    [Tooltip("The music loop that plays only during the Chase state.")]
    public AudioClip chaseMusicClip; 
    [Tooltip("How long the chase music takes to fade out when leaving the Chase state.")]
    public float chaseMusicFadeDuration = 1.5f; 
    [Tooltip("The distance to the player at which the chase music should begin.")]
    public float chaseMusicStartDistance = 20f; 

    [Header("Ambient Audio Layer")]
    [Tooltip("The persistent, quiet ambient track to play when not in Chase or after Chase fades out.")]
    public AudioClip ambientTrackClip;
    [Tooltip("The volume for the ambient track. Chase music will use originalAudioSourceVolume.")]
    [Range(0f, 1f)]
    public float ambientTrackVolume = 0.3f; 

    [Header("Proximity Enforcement (Teleport)")]
    [Tooltip("Maximum distance from the player before the enemy is automatically teleported closer.")]
    public float maxPlayerDistance = 100f; 
    [Tooltip("The shortest distance from the player to teleport the enemy to.")]
    public float teleportMinRadius = 30f;
    [Tooltip("The furthest distance from the player to teleport the enemy to.")]
    public float teleportMaxRadius = 50f; 
    [Tooltip("How often (in seconds) the system checks the player distance.")]
    public float teleportCheckInterval = 5.0f; 
    
    private float proximityCheckTimer = 0f;

    [Header("Audio Configuration")]
    [Tooltip("The sound clip to use for footsteps.")]
    public AudioClip footstepClip; 
    [Tooltip("How frequently footsteps should play (in seconds).")]
    public float footstepRate = 0.5f; 
    private float footstepTimer = 0f; 
    private float originalAudioSourceVolume = 1.0f; // To store the initial volume

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
    public float fleeSpeed = 7f; 
    public float fleeDistance = 50f;
    public float fleeDuration = 8f; 
    
    [Header("Retreat Configuration")]
    [Tooltip("How long the enemy is stunned before backing away.")]
    public float retreatStunDuration = 1.5f;
    [Tooltip("Speed when walking backward.")]
    public float retreatSpeed = 0.5f;
    [Tooltip("Target distance to retreat to.")]
    public float retreatDistance = 10f; 
    
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
    
    public Transform target; 
    
    private Coroutine fadeOutChaseRoutine;
    private Coroutine fadeInAmbientRoutine;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        
        if (currentState == EnemyState.Stalk)
        {
            agent.speed = stalkSpeed;
        }
        
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        // Start Ambient Music Layer and Store Volume
        if (audioSource != null)
        {
            originalAudioSourceVolume = audioSource.volume;
            
            if (ambientTrackClip != null)
            {
                audioSource.clip = ambientTrackClip;
                audioSource.loop = true;
                // Start ambient track immediately at its target volume
                audioSource.volume = ambientTrackVolume; 
                audioSource.Play();
            }
        }

        agent.updateRotation = false;
    }
    
    private bool IsVulnerableOrFleeing()
    {
        return currentState == EnemyState.Flee || currentState == EnemyState.Retreat;
    }

    public void StartChase()
    {
        if (IsVulnerableOrFleeing()) return; 
        if (target != null) ChangeState(EnemyState.Chase);
    }
    
    public void StartInvestigate(Vector3 location)
    {
        if (IsVulnerableOrFleeing()) return; 

        if (currentState != EnemyState.Investigate || Vector3.Distance(currentPatrolCenter, location) > 1f)
        {
            currentPatrolCenter = location;
            GenerateInvestigationPath(location);
            ChangeState(EnemyState.Investigate);
        }
    }

    public void StartSearch(Vector3 location)
    {
        if (IsVulnerableOrFleeing()) return; 

        if (currentState != EnemyState.Search || Vector3.Distance(currentPatrolCenter, location) > 1f)
        {
            currentPatrolCenter = location;
            ChangeState(EnemyState.Search);
        }
    }
    
    public void StartPatrol(Vector3 center)
    {
        if (IsVulnerableOrFleeing()) return; 
        currentPatrolCenter = center;
        ChangeState(EnemyState.Patrol);
    }

    public void StartStalk()
    {
        if (IsVulnerableOrFleeing()) return; 
        ChangeState(EnemyState.Stalk);
    }
    
    public void StartFlee()
    {
        ChangeState(EnemyState.Flee);
    }
    
    public void StartRetreat()
    {
        if (currentState != EnemyState.Flee) ChangeState(EnemyState.Retreat);
    }

    private void Update()
    {
        // Must check if target is null for states that rely on it
        if (target == null && (currentState == EnemyState.Chase || currentState == EnemyState.Retreat))
        {
            ChangeState(EnemyState.Stalk); 
        }
        
        // --- NEW: Handle Proximity Check Timer ---
        if (target != null)
        {
            proximityCheckTimer -= Time.deltaTime;
            if (proximityCheckTimer <= 0f)
            {
                CheckAndEnforceProximity();
                proximityCheckTimer = teleportCheckInterval; // Reset timer
            }
        }
        // -----------------------------------------
        
        // Handle enemy rotation for non-retreat states
        bool shouldRotateBasedOnVelocity = 
            currentState != EnemyState.Retreat && agent.velocity.sqrMagnitude > 0.01f;

        if (shouldRotateBasedOnVelocity)
        {
            Quaternion lookRotation = Quaternion.LookRotation(agent.velocity.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 10f);
        }
        
        // ANIMATION: Set Speed parameter for Walking/Running (All states EXCEPT Retreat)
        if (animator != null && currentState != EnemyState.Retreat)
        {
            float targetSpeed = agent.velocity.magnitude;
            animator.SetFloat("Speed", targetSpeed);
        }
        
        // AUDIO: Handle Footsteps
        bool isMoving = agent.velocity.sqrMagnitude > 0.01f;
        
        if (isMoving && currentState != EnemyState.Retreat && footstepClip != null)
        {
            footstepTimer -= Time.deltaTime;
            
            if (footstepTimer <= 0f)
            {
                PlayOneShotSound(footstepClip);
                footstepTimer = footstepRate; 
            }
        }
        else 
        {
            footstepTimer = 0f; 
        }

        // Handle chase music only if we are in the Chase state
        if (currentState == EnemyState.Chase)
        {
            HandleChaseMusic();
        }

        switch (currentState)
        {
            case EnemyState.Stalk: StalkState(); break;
            case EnemyState.Chase: ChaseState(); break;
            case EnemyState.Investigate: InvestigateState(); break;
            case EnemyState.Patrol: PatrolState(); break;
            case EnemyState.Search: SearchState(); break;
            case EnemyState.Flee: FleeState(); break;
            case EnemyState.Retreat: RetreatState(); break;
        }
    }
    
    // --- NEW: Proximity Enforcement Methods ---

    /// <summary>
    /// Checks the distance to the player and teleports the enemy if too far away.
    /// </summary>
    private void CheckAndEnforceProximity()
    {
        if (target == null) return;
        
        float distance = Vector3.Distance(transform.position, target.position);
        
        // Check if the enemy is outside the maximum allowed distance
        if (distance > maxPlayerDistance)
        {
            Debug.Log($"Enemy too far ({distance}m)! Teleporting to enforce proximity.");
            
            // 1. Calculate a random direction (horizontal plane) and distance relative to the player
            Vector3 randomDirection = Random.insideUnitCircle.normalized;
            Vector3 offset = new Vector3(randomDirection.x, 0, randomDirection.y);
            float randomRadius = Random.Range(teleportMinRadius, teleportMaxRadius);
            
            Vector3 targetPosition = target.position + offset * randomRadius;

            // 2. Find a valid NavMesh location near the calculated target position
            NavMeshHit hit;
            if (NavMesh.SamplePosition(targetPosition, out hit, teleportMaxRadius, NavMesh.AllAreas))
            {
                // 3. Teleport the enemy and reset state
                TeleportToLocation(hit.position);
            }
            else
            {
                Debug.LogWarning("Could not find a safe NavMesh location for teleportation!");
            }
        }
    }
    
    /// <summary>
    /// Instantly moves the enemy to a new location, resets navigation, and sets state to Stalk.
    /// </summary>
    /// <param name="newPosition">The world position to teleport to.</param>
    private void TeleportToLocation(Vector3 newPosition)
    {
        // Stop all active navigation and music fades
        agent.isStopped = true;
        if (fadeOutChaseRoutine != null) 
        {
            StopCoroutine(fadeOutChaseRoutine);
            fadeOutChaseRoutine = null;
        }
        if (fadeInAmbientRoutine != null) 
        {
            StopCoroutine(fadeInAmbientRoutine);
            fadeInAmbientRoutine = null;
        }

        // Apply new position using Warp, which handles NavMesh connection
        if (agent.Warp(newPosition))
        {
            // Set rotation to face the player/forward immediately after warp
            if (target != null)
            {
                Vector3 lookDirection = (target.position - newPosition).normalized;
                Quaternion targetRotation = Quaternion.LookRotation(new Vector3(lookDirection.x, 0, lookDirection.z));
                transform.rotation = targetRotation;
            }

            // Reset the navigation and state
            agent.ResetPath();
            agent.isStopped = false; 
            
            // Transition back to a passive state (Stalk is a good default for "lost")
            ChangeState(EnemyState.Stalk);
            
            // Ensure the ambient music is playing after teleport
            if (audioSource != null && ambientTrackClip != null)
            {
                audioSource.clip = ambientTrackClip;
                audioSource.loop = true;
                audioSource.volume = ambientTrackVolume;
                if (!audioSource.isPlaying) audioSource.Play();
            }

            Debug.Log("Enemy successfully teleported and state reset to Stalk.");
        }
        else
        {
            Debug.LogError("NavMeshAgent.Warp failed during TeleportToLocation.");
        }
    }
    
    // --- Music Handling Functions ---

    /// <summary>
    /// Checks proximity to the player and starts/fades the chase music accordingly.
    /// This is called only when the enemy is in the Chase state.
    /// </summary>
    private void HandleChaseMusic()
    {
        if (target == null || audioSource == null || chaseMusicClip == null) return;
        
        float distance = Vector3.Distance(transform.position, target.position);
        
        // Check if the current clip is the Chase music OR if we are currently fading it out
        bool isPlayingChaseMusic = audioSource.clip == chaseMusicClip || fadeOutChaseRoutine != null;
        
        if (distance <= chaseMusicStartDistance)
        {
            // --- 1. CLOSE PROXIMITY: START/MAINTAIN CHASE MUSIC ---
            
            // Stop any running fade-in of ambient (Chase music takes priority)
            if (fadeInAmbientRoutine != null) 
            {
                StopCoroutine(fadeInAmbientRoutine);
                fadeInAmbientRoutine = null;
            }

            // If we aren't already playing chase music at full volume, set it up
            if (audioSource.clip != chaseMusicClip || !audioSource.isPlaying)
            {
                // Stop any running chase fade-out
                if (fadeOutChaseRoutine != null) 
                {
                    StopCoroutine(fadeOutChaseRoutine);
                    fadeOutChaseRoutine = null;
                }
                
                // Set up and start Chase Music at full volume
                audioSource.clip = chaseMusicClip;
                audioSource.loop = true;
                audioSource.volume = originalAudioSourceVolume; 
                audioSource.Play();
            }
            else if (audioSource.clip == chaseMusicClip)
            {
                 // Ensure volume is max if it was fading
                 audioSource.volume = originalAudioSourceVolume; 
            }
            
        }
        else 
        {
            // --- 2. FAR PROXIMITY: FADE OUT CHASE, FADE IN AMBIENT ---
            
            // If Chase music is currently playing or fading out, start the fade sequence
            if (isPlayingChaseMusic && fadeOutChaseRoutine == null)
            {
                // Start the fade out of chase music, which will trigger the ambient fade-in
                fadeOutChaseRoutine = StartCoroutine(FadeOutChaseMusic(chaseMusicFadeDuration));
            }
        }
    }
    
    // --- Coroutines for smooth fades ---

    /// <summary>
    /// Smoothly fades out the chase music.
    /// </summary>
    private IEnumerator FadeOutChaseMusic(float fadeDuration)
    {
        // Check if we are actually playing the chase music before proceeding
        if (audioSource == null || audioSource.clip != chaseMusicClip || !audioSource.isPlaying)
        {
            fadeOutChaseRoutine = null;
            yield break;
        }

        float startVolume = audioSource.volume;
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            // Interpolate the volume from startVolume down to 0
            audioSource.volume = Mathf.Lerp(startVolume, 0f, timer / fadeDuration);
            yield return null;
        }

        // Ensure volume is exactly 0 and stop the music completely
        audioSource.volume = 0f;
        audioSource.Stop();
        audioSource.loop = false;
        
        // Start fading the ambient music back in right after the chase music stops
        if (ambientTrackClip != null)
        {
            fadeInAmbientRoutine = StartCoroutine(FadeInAmbientMusic(fadeDuration));
        }
        else
        {
            // If there's no ambient track, restore the volume immediately
            audioSource.volume = originalAudioSourceVolume; 
        }

        fadeOutChaseRoutine = null;
    }
    
    /// <summary>
    /// Smoothly fades in the ambient music layer.
    /// </summary>
    private IEnumerator FadeInAmbientMusic(float fadeDuration)
    {
        if (audioSource == null || ambientTrackClip == null)
        {
            fadeInAmbientRoutine = null;
            yield break;
        }
        
        // Switch clip to ambient and start playing quietly if not already
        if (audioSource.clip != ambientTrackClip)
        {
            audioSource.clip = ambientTrackClip;
            audioSource.loop = true;
            audioSource.volume = 0f; // Start at 0
            audioSource.Play();
        }
        
        float startVolume = audioSource.volume;
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            // Interpolate the volume from current volume up to the target ambient volume
            audioSource.volume = Mathf.Lerp(startVolume, ambientTrackVolume, timer / fadeDuration);
            yield return null;
        }

        audioSource.volume = ambientTrackVolume;
        fadeInAmbientRoutine = null;
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
        
        if (fleeTimer <= 0f)
        {
            if (currentPatrolCenter != Vector3.zero) ChangeState(EnemyState.Investigate); 
            else ChangeState(EnemyState.Stalk);
            return;
        }

        float timeToNextMove = 1f; 
        if (fleeTimer > timeToNextMove && !agent.pathPending && agent.remainingDistance < 1f)
        {
            SetFleeDestination();
        }
        
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
        Vector3 fleeDirection = (transform.position - centerOfThreat).normalized; 
        Vector3 targetPoint = transform.position + fleeDirection * fleeDistance; 

        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPoint, out hit, fleeDistance, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
        // No else block needed as a fallback is assumed in the larger AI logic
    }

    private void RetreatState()
    {
        if (retreatTimer >= fleeDuration * 2) 
        {
            Debug.Log("Retreat timed out! Forcing transition to Stalk.");
            ChangeState(EnemyState.Stalk);
            return;
        }
        
        retreatTimer += Time.deltaTime;

        // Phase 1: Stand Still / Stun
        if (retreatTimer < retreatStunDuration)
        {
            agent.isStopped = true;
            
            if (animator != null) animator.CrossFade("face close", 0f);
            
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
        
        if (animator != null) animator.CrossFade("face close walk backward", 0);
        
        if (target != null)
        {
            Vector3 lookDirection = (target.position - transform.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(new Vector3(lookDirection.x, 0, lookDirection.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
        }

        if (!retreatDestinationSet)
        {
            if (target == null) { ChangeState(EnemyState.Stalk); return; }

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
                ChangeState(EnemyState.Stalk);
                return;
            }
        }
        
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
            // Placeholder for SetDestinationToRandomPoint logic
            
            investigationTimer += Time.deltaTime;
            if (investigationTimer >= investigationDuration)
            {
                ChangeState(EnemyState.Stalk);
            }
        }
    }

    private void SearchState()
    {
        if (agent.remainingDistance > 0.5f) return;
        if (investigationTimer <= 0f) agent.isStopped = true; 
        investigationTimer += Time.deltaTime;

        if (investigationTimer >= investigationDuration)
        {
            agent.isStopped = false; 
            ChangeState(EnemyState.Stalk);
        }
    }

    private void PlayOneShotSound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    // --- State Transition Helper ---

    private void ChangeState(EnemyState newState)
    {
        if (currentState == newState) return;

        // --- Exit Logic ---
        
        // 1. Handle Chase Music Fade-Out
        if (currentState == EnemyState.Chase)
        {
            // If the chase music is playing (or fading out), start the fade-out process
            if (audioSource != null && (audioSource.clip == chaseMusicClip || fadeOutChaseRoutine != null) && fadeOutChaseRoutine == null)
            {
                fadeOutChaseRoutine = StartCoroutine(FadeOutChaseMusic(chaseMusicFadeDuration));
            }
        }
        
        // 2. Clear state-specific timers/flags
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
            
            if (animator != null)
            {
                animator.SetFloat("Speed", 0f);
                animator.CrossFade("Blend Tree", 0.1f); 
            }
        }

        // --- Enter Logic ---
        if (newState == EnemyState.Chase)
        {
            // Stop any fade-out/fade-in immediately if we re-enter Chase
            if (fadeOutChaseRoutine != null) 
            {
                StopCoroutine(fadeOutChaseRoutine);
                fadeOutChaseRoutine = null;
            }
            if (fadeInAmbientRoutine != null) 
            {
                StopCoroutine(fadeInAmbientRoutine);
                fadeInAmbientRoutine = null;
            }
            
            // Reset volume. Music will be started by HandleChaseMusic based on distance.
            if (audioSource != null) audioSource.volume = originalAudioSourceVolume; 
            
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
            
            PlayOneShotSound(fleeScreechClip); 
        }
        else if (newState == EnemyState.Retreat)
        {
            agent.speed = retreatSpeed; 
            retreatTimer = 0f;
            initialRetreatPosition = transform.position;
            agent.isStopped = true; 
            agent.updateRotation = false; 
            retreatDestinationSet = false; 
            
            PlayOneShotSound(retreatScreechClip); 
            
            if (animator != null) animator.SetFloat("Speed", 0f);
        }
        
        currentState = newState;
        Debug.Log(gameObject.name + " changed state to: " + newState);
    }
}
