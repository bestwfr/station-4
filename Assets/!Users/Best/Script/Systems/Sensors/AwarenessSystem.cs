using System.Collections;
using System.Collections.Generic;
using UnityEngine;

class TrackedTarget
{
    public DetectableTarget Detectable;
    public Vector3 RawPosition;

    public float LastSensedTime = -1f;
    public float Awareness; // 0     = not aware (will be culled); 
                            // 0-1   = rough idea (no set location); 
                            // 1-2   = likely target (location)
                            // 2     = fully detected

    public bool UpdateAwareness(DetectableTarget target, Vector3 position, float awareness, float minAwareness)
    {
        var oldAwareness = Awareness;

        if (target != null)
            Detectable      = target;
        RawPosition     = position; // Crucial: Update the last known position
        LastSensedTime  = Time.time;
        Awareness       = Mathf.Clamp(Mathf.Max(Awareness, minAwareness) + awareness, 0f, 2f);
        
        if (oldAwareness < 2f && Awareness >= 2f)
            return true;
        if (oldAwareness < 1f && Awareness >= 1f)
            return true;
        if (oldAwareness <= 0f && Awareness >= 0f)
            return true;

        return false;
    }

    public bool DecayAwareness(float decayTime, float amount)
    {
        // detected too recently - no change
        if ((Time.time - LastSensedTime) < decayTime)
            return false;

        var oldAwareness = Awareness;

        Awareness -= amount;

        if (oldAwareness >= 2f && Awareness < 2f)
            return true;
        if (oldAwareness >= 1f && Awareness < 1f)
            return true;
        return Awareness <= 0f;
    }
}

[RequireComponent(typeof(EnemyAI))]
public class AwarenessSystem : MonoBehaviour
{
    [SerializeField] AnimationCurve VisionSensitivity;
    [SerializeField] float VisionMinimumAwareness = 1f;
    [SerializeField] float VisionAwarenessBuildRate = 10f;

    [SerializeField] float HearingMinimumAwareness = 0f;
    [SerializeField] float HearingAwarenessBuildRate = 0.5f;

    [SerializeField] float ProximityMinimumAwareness = 0f;
    [SerializeField] float ProximityAwarenessBuildRate = 1f;

    [SerializeField] float AwarenessDecayDelay = 0.1f;
    [SerializeField] float AwarenessDecayRate = 0.1f;

    Dictionary<GameObject, TrackedTarget> Targets = new Dictionary<GameObject, TrackedTarget>();
    EnemyAI LinkedAI;

    // Start is called before the first frame update
    void Start()
    {
        LinkedAI = GetComponent<EnemyAI>();
    }

    // Update is called once per frame
    void Update()
    {
        List<GameObject> toCleanup = new List<GameObject>();
        foreach(var targetGO in Targets.Keys)
        {
            var targetData = Targets[targetGO]; // Get data for passing to AI

            if (targetData.DecayAwareness(AwarenessDecayDelay, AwarenessDecayRate * Time.deltaTime))
            {
                if (targetData.Awareness <= 0f)
                {
                    LinkedAI.OnFullyLost();
                    toCleanup.Add(targetGO);
                }
                else
                {
                    // PASS LAST KNOWN POSITION when losing detection
                    if (targetData.Awareness >= 1f)
                        LinkedAI.OnLostDetect(targetGO, targetData.RawPosition);
                    else
                        LinkedAI.OnLostSuspicion(targetGO, targetData.RawPosition);
                }
            }
        }

        // cleanup targets that are no longer detected
        foreach(var target in toCleanup)
            Targets.Remove(target);
    }

    void UpdateAwareness(GameObject targetGO, DetectableTarget target, Vector3 position, float awareness, float minAwareness)
    {
        // not in targets
        if (!Targets.ContainsKey(targetGO))
            Targets[targetGO] = new TrackedTarget();

        // update target awareness
        if (Targets[targetGO].UpdateAwareness(target, position, awareness, minAwareness))
        {
            var targetData = Targets[targetGO]; // Get data for passing to AI

            // PASS LAST KNOWN POSITION when gaining detection
            if (targetData.Awareness >= 2f)
                LinkedAI.OnFullyDetected(targetGO);
            else if (targetData.Awareness >= 1f)
                LinkedAI.OnDetected(targetGO, targetData.RawPosition);
            else if (targetData.Awareness >= 0f)
                LinkedAI.OnSuspicious(targetGO, targetData.RawPosition);
        }
    }

    public void ReportCanSee(DetectableTarget seen)
    {
        // determine where the target is in the field of view
        var vectorToTarget = (seen.transform.position - LinkedAI.EyeLocation).normalized;
        var dotProduct = Vector3.Dot(vectorToTarget, LinkedAI.EyeDirection);

        // determine the awareness contribution
        var awareness = LinkedAI.VisionSensitivity.Evaluate(dotProduct) * VisionAwarenessBuildRate * Time.deltaTime; // Access curve via AI
        
        UpdateAwareness(seen.gameObject, seen, seen.transform.position, awareness, VisionMinimumAwareness);
    }

    public void ReportCanHear(GameObject source, Vector3 location, EHeardSoundCategory category, float intensity)
    {
        var awareness = intensity * HearingAwarenessBuildRate * Time.deltaTime;

        // NOTE: The location here IS the last sensed position (noise source)
        UpdateAwareness(source, null, location, awareness, HearingMinimumAwareness);
    }

    public void ReportInProximity(DetectableTarget target)
    {
        var awareness = ProximityAwarenessBuildRate * Time.deltaTime;

        UpdateAwareness(target.gameObject, target, target.transform.position, awareness, ProximityMinimumAwareness);
    }   
    
    public float GetAwarenessForTarget(GameObject targetGO)
    {
        if (Targets.ContainsKey(targetGO))
        {
            return Targets[targetGO].Awareness;
        }
        return 0f;
    }
}
