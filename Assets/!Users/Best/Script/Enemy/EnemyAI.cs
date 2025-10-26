using System.Collections.Generic;
using UnityEngine;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

[RequireComponent(typeof(AwarenessSystem))]
[RequireComponent(typeof(EnemyController))] 
public class EnemyAI : MonoBehaviour
{
    // === DEPENDENCIES ===
    [SerializeField] private EnemyController motor; 
    [SerializeField] TextMeshProUGUI FeedbackDisplay;
    
    [Header("Vision Setup")]
    private Transform playerTarget;
    [Tooltip("Layers that block the enemy's line of sight, e.g., Walls, Obstacles. Must be set for vision to work.")]
    public LayerMask visionBlockerLayers = 1; 

    // --- Detection Range Configuration (For Gizmos and Checks) ---
    [Header("Detection Ranges")]
    [SerializeField] AnimationCurve _VisionSensitivity; 
    [SerializeField] float _VisionConeAngle = 60f;
    [SerializeField] float _VisionConeRange = 30f;
    [SerializeField] Color _VisionConeColour = new Color(1f, 0f, 0f, 0.25f);

    [SerializeField] float _HearingRange = 20f;
    [SerializeField] Color _HearingRangeColour = new Color(1f, 1f, 0f, 0.25f);

    [SerializeField] float _ProximityDetectionRange = 3f;
    [SerializeField] Color _ProximityRangeColour = new Color(1f, 1f, 1f, 0.25f);
    
    // --- Accessors for AwarenessSystem/Gizmos ---
    public AnimationCurve VisionSensitivity => _VisionSensitivity; 
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
    }
    
    void Start()
    {
        GameObject playerGO = GameObject.FindWithTag("Player"); 
        if (playerGO != null)
        {
            playerTarget = playerGO.transform;
        }
        
        if (motor != null && playerTarget != null)
        {
             motor.target = playerTarget;
        }

        if (motor != null && motor.currentState == EnemyState.Stalk)
        {
            motor.StartStalk();
        }
    }

    void Update()
    {
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
                
                if (currentAwareness > 0.05f) 
                {
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
        if (targetComponent == null) return; 

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
            if (Physics.Raycast(eyePosition, targetDirection, out hit, distanceToTarget, visionBlockerLayers))
            {
                if (hit.distance < distanceToTarget - 0.1f)
                {
                    return; // Blocked by wall/obstacle
                }
            } 
            
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
        if (motor.currentState == EnemyState.Chase || motor.currentState == EnemyState.Investigate || motor.currentState == EnemyState.Flee || motor.currentState == EnemyState.Retreat)
            return;

        if (motor.target == null || motor.target.gameObject != targetGO)
            motor.target = targetGO.transform;

        motor.StartInvestigate(lastSensedPosition); 
    }

    public void OnFullyDetected(GameObject targetGO)
    {
        if (motor.currentState == EnemyState.Flee || motor.currentState == EnemyState.Retreat)
            return;
            
        motor.StartChase();
    }
    
    public void OnPlayerShoots(Vector3 location)
    {
        motor.currentPatrolCenter = location;
        motor.StartFlee(); 
    }
    
    /// <summary>
    /// Call this when the player successfully hits the enemy with a flashlight or similar light source.
    /// The hit location is ignored by the Retreat state, which always backs away from the player's current position.
    /// </summary>
    public void OnFlashlightHit(Vector3 hitLocation)
    {
        // Don't interrupt Flee, but interrupt everything else
        if (motor.currentState == EnemyState.Flee)
            return;

        // Note: The motor.currentPatrolCenter is not used by RetreatState, 
        // as Retreat moves opposite the player, regardless of the hit location.
        motor.StartRetreat();
    }


    public void OnLostDetect(GameObject targetGO, Vector3 lastSensedPosition)
    {
        motor.StartInvestigate(lastSensedPosition); 
    }

    public void OnLostSuspicion(GameObject targetGO, Vector3 lastInvestigatedCenter)
    {
        motor.StartPatrol(lastInvestigatedCenter); 
    }

    public void OnFullyLost()
    {
        motor.StartStalk(); 
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
