using System.Collections.Generic;
using UnityEngine;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

// NOTE: This file assumes the EnemyController script's EnemyState enum has been updated
// to include Stalk, Investigate, Patrol, and Search.

[RequireComponent(typeof(AwarenessSystem))]
[RequireComponent(typeof(EnemyController))] // Ensure EnemyController is still required
public class EnemyAI : MonoBehaviour
{
    // === DEPENDENCIES ===
    [SerializeField] private EnemyController motor; 
    [SerializeField] TextMeshProUGUI FeedbackDisplay;
    
    [Header("Vision Setup")]
    private Transform playerTarget;
    [Tooltip("Layers that block the enemy's line of sight, e.g., Walls, Obstacles. Must be set for vision to work.")]
    public LayerMask visionBlockerLayers = 1; // Default to layer 1 (Default)

    // --- Detection Range Configuration (For Gizmos and Checks) ---
    [Header("Detection Ranges")]
    [SerializeField] AnimationCurve _VisionSensitivity; // Exposed for AwarenessSystem
    [SerializeField] float _VisionConeAngle = 60f;
    [SerializeField] float _VisionConeRange = 30f;
    [SerializeField] Color _VisionConeColour = new Color(1f, 0f, 0f, 0.25f);

    [SerializeField] float _HearingRange = 20f;
    [SerializeField] Color _HearingRangeColour = new Color(1f, 1f, 0f, 0.25f);

    [SerializeField] float _ProximityDetectionRange = 3f;
    [SerializeField] Color _ProximityRangeColour = new Color(1f, 1f, 1f, 0.25f);
    
    // --- Accessors for AwarenessSystem/Gizmos ---
    public AnimationCurve VisionSensitivity => _VisionSensitivity; // New accessor for AwarenessSystem
    public Vector3 EyeLocation => transform.position;
    public Vector3 EyeDirection => transform.forward;
    public float VisionConeAngle => _VisionConeAngle;
    public float VisionConeRange => _VisionConeRange;
    public Color VisionConeColour => _VisionConeColour;
    public float HearingRange => _HearingRange;
    public Color HearingRangeColour => _HearingRangeColour;
    public float ProximityDetectionRange => _ProximityDetectionRange;
    public Color ProximityDetectionColour => _ProximityRangeColour;
    public float CosVisionConeAngle { get; private set; } = 0f;

    AwarenessSystem Awareness;

    void Awake()
    {
        CosVisionConeAngle = Mathf.Cos(VisionConeAngle * Mathf.Deg2Rad);
        Awareness = GetComponent<AwarenessSystem>();
        
        // Initial state set in EnemyController
    }
    
    void Start()
    {
        // Find the player once at the start. IMPORTANT: Ensure your player object is tagged "Player".
        GameObject playerGO = GameObject.FindWithTag("Player"); 
        if (playerGO != null)
        {
            playerTarget = playerGO.transform;
        }
        
        // Ensure the enemy starts stalking/wandering immediately if not already doing so
        if (motor != null && motor.currentState == EnemyState.Stalk)
        {
            motor.StartStalk();
        }
    }

    void Update()
    {
        // Continuous check for passive sight
        if (playerTarget != null)
        {
            CheckForSight();
        }

        // --- AWARENESS DEBUG CODE ---
        if (motor != null && FeedbackDisplay != null)
        {
            string debugText = motor.currentState.ToString();

            if (playerTarget != null && Awareness != null)
            {
                float currentAwareness = Awareness.GetAwarenessForTarget(playerTarget.gameObject); 
                
                if (currentAwareness > 0.05f) // Target is actively tracked
                {
                    // Display the current state AND the awareness level
                    debugText = $"[{motor.currentState.ToString()}] | AWARENESS: {currentAwareness:F2}";
                }
            }
            
            FeedbackDisplay.text = debugText;
        }
        // ---------------------------
    }

    // Performs the actual vision calculation using raycasting
    void CheckForSight()
    {
        DetectableTarget targetComponent = playerTarget.GetComponent<DetectableTarget>();
        // We must have a DetectableTarget component on the player to report sight
        if (targetComponent == null) return; 

        // Offset the eye position slightly upwards (assuming the pivot is at the feet)
        Vector3 eyePosition = EyeLocation + Vector3.up * 1f; 
        Vector3 targetPosition = playerTarget.position;
        
        Vector3 targetDirection = (targetPosition - eyePosition).normalized;
        float distanceToTarget = Vector3.Distance(eyePosition, targetPosition);
        float angleToTarget = Vector3.Angle(EyeDirection, targetDirection);

        // 1. Cone and Range Check
        if (distanceToTarget <= VisionConeRange && angleToTarget <= VisionConeAngle) 
        {
            // 2. Line of Sight (LOS) Check using raycast
            RaycastHit hit;
            
            // Raycast check: Does anything block the view before the target?
            if (Physics.Raycast(eyePosition, targetDirection, out hit, distanceToTarget, visionBlockerLayers))
            {
                // If the ray hits something, check if the hit distance is significantly shorter than the target distance.
                if (hit.distance < distanceToTarget - 0.1f)
                {
                    return; // Blocked by wall/obstacle
                }
            } 
            
            // If we pass all checks, the target is visible.
            ReportCanSee(targetComponent);
        }
    }
    
    // --- State Listeners (Commands EnemyController) ---
    
    public void OnSuspicious(GameObject targetGO, Vector3 lastSensedPosition)
    {
        motor.StartSearch(lastSensedPosition);
    }

    public void OnDetected(GameObject targetGO, Vector3 lastSensedPosition)
    {
        // FIX: Prevent state regression. If the AI is already chasing (Chase) or aggressively investigating,
        // we ignore this lower-priority OnDetected command (which starts Search/Investigate).
        if (motor.currentState == EnemyState.Chase || motor.currentState == EnemyState.Investigate)
            return;

        // Set the target for the motor (Chase state)
        if (motor.target == null || motor.target.gameObject != targetGO)
            motor.target = targetGO.transform;

        // Start investigation, assuming the player is nearby
        motor.StartInvestigate(lastSensedPosition); 
    }

    public void OnFullyDetected(GameObject targetGO)
    {
        // Set the target for the motor (Chase state)
        if (motor.target == null || motor.target.gameObject != targetGO)
            motor.target = targetGO.transform;
            
        motor.StartChase();
    }

    public void OnLostDetect(GameObject targetGO, Vector3 lastSensedPosition)
    {
        // Stop chasing, start investigating the last known position.
        motor.StartInvestigate(lastSensedPosition); 
    }

    public void OnLostSuspicion(GameObject targetGO, Vector3 lastInvestigatedCenter)
    {
        // Transition to the persistent Patrol state around the center of the last suspicion.
        motor.StartPatrol(lastInvestigatedCenter); 
    }

    public void OnFullyLost()
    {
        // Clear the target reference from the motor
        if (motor.target != null)
            motor.target = null;
            
        motor.StartStalk(); // Changed from StartWander() to StartStalk()
    }
    
    // --- Report Handlers (Feeds Awareness System) ---
    
    public void ReportCanSee(DetectableTarget seen)
    {
        Awareness.ReportCanSee(seen);
    }
    
    public void ReportCanHear(GameObject source, Vector3 location, EHeardSoundCategory category, float intensity)
    {
        if (Vector3.Distance(EyeLocation, location) <= HearingRange)
        {
            Debug.Log($"canHear: {source} Do: {category} At: {location} intensity: {intensity}");
            Awareness.ReportCanHear(source, location, category, intensity);
        }
    }

    public void ReportInProximity(DetectableTarget target)
    {
        if (Vector3.Distance(EyeLocation, target.transform.position) <= ProximityDetectionRange)
            Awareness.ReportInProximity(target);
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(EnemyAI))]
public class EnemyAIEditor : Editor
{
    public void OnSceneGUI()
    {
        var ai = target as EnemyAI;
        Vector3 origin = ai.transform.position + Vector3.up * 0.1f;

        // draw the detectopm range
        Handles.color = ai.ProximityDetectionColour;
        Handles.DrawSolidDisc(origin, Vector3.up, ai.ProximityDetectionRange);

        // draw the hearing range
        Handles.color = ai.HearingRangeColour;
        Handles.DrawSolidDisc(origin, Vector3.up, ai.HearingRange);

        // work out the start point of the vision cone
        Vector3 forward = ai.transform.forward;
        Vector3 startPoint = Quaternion.Euler(0, -ai.VisionConeAngle, 0) * forward;

        // draw the vision cone
        Handles.color = ai.VisionConeColour;
        Handles.DrawSolidArc(origin, Vector3.up, startPoint, ai.VisionConeAngle * 2f, ai.VisionConeRange);        
    }
}
#endif // UNITY_EDITOR
