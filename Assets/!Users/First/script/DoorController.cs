using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class DoorController : MonoBehaviour, IInteractable
{
    [Header("Audio Settings")]
    public AudioSource audioSource; // Assign the AudioSource component here
    public AudioClip openSound;      // Drag the sound file for opening
    public AudioClip closeSound;     // Drag the sound file for closing
    
    [Header("Collider Settings")]
    // Assign the Collider component of the actual door mesh child here in the Inspector
    public Collider doorMeshCollider;
    
    [Header("Door Settings")]
    public Vector3 openRotation = new Vector3(0, 90f, 0);
    public float rotationSpeed = 3f;

    [Header("Lock Settings")]
    public bool isLocked = true; // ตั้ง true → ประตูล็อคตั้งแต่เริ่ม

    [Header("Permanent Seal Settings")]
    public bool sealOnClose = false;
    public float timeToWaitBeforeSeal = 0.5f;
    
    [Header("Hitbox Bypass Settings")]
    public float timeToWaitBeforeColliderEnable = 0.5f;

    private bool isOpen = false;
    private bool isPermanentlySealed = false;
    private bool isRotating = false;
    private Quaternion initialRotation;
    private Quaternion targetRotation;

    void Start()
    {
        initialRotation = transform.localRotation;
        targetRotation = initialRotation;

        // --- NEW ADDITION ---
        // Optional: If 'doorMeshCollider' is not assigned, try to find a Collider in a child.
        if (doorMeshCollider == null)
        {
            doorMeshCollider = GetComponentInChildren<Collider>();
            if (doorMeshCollider != null)
                Debug.LogWarning("Door Mesh Collider found on child, but was not assigned in Inspector. Auto-assigning it.");
        }
        // --- END NEW ADDITION ---
        
        // Try to automatically get the AudioSource if not assigned
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                Debug.LogError("DoorController on " + gameObject.name + " is missing an AudioSource component.");
            }
        }
    }

    void Update()
    {
        if (isPermanentlySealed) return;

        if (transform.localRotation != targetRotation)
        {
            isRotating = true;
            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
        else
        {
            isRotating = false;
        }
    }

    // ----------------------------------------------------
    // Public Methods
    // ----------------------------------------------------
    public void UnlockDoor()
    {
        isLocked = false;
        isPermanentlySealed = false;
        Debug.Log(gameObject.name + " is now UNLOCKED!");
    }

    public void LockDoor()
    {
        isLocked = true;
        Debug.Log(gameObject.name + " is now LOCKED!");
    }

    public void ToggleDoor()
    {
        isOpen = !isOpen;

        if (isOpen)
        {
            targetRotation = initialRotation * Quaternion.Euler(openRotation);
            
            if (doorMeshCollider != null) doorMeshCollider.enabled = true;
            
            PlaySound(openSound);
            HearingManager.Instance.OnSoundEmitted(gameObject, transform.position,EHeardSoundCategory.Idk, 16f);
        }
        else
        {
            targetRotation = initialRotation;
            if (sealOnClose) StartCoroutine(SealAfterClose());
            
            // --- NEW ADDITION: Start Coroutine to disable/enable Collider ---
            if (doorMeshCollider != null) 
                StartCoroutine(TemporarilyDisableCollider(timeToWaitBeforeColliderEnable));
            
            PlaySound(closeSound);
            
            HearingManager.Instance.OnSoundEmitted(gameObject, transform.position,EHeardSoundCategory.Idk, 16f);
        }

        Debug.Log("Door Toggled: " + (isOpen ? "Open" : "Closed"));
    }

    // ----------------------------------------------------
    // Public property สำหรับเช็คจากภายนอก
    // ----------------------------------------------------
    public bool IsOpen
    {
        get { return isOpen; }
    }

    IEnumerator SealAfterClose()
    {
        yield return new WaitForSeconds(timeToWaitBeforeSeal);
        isPermanentlySealed = true;
        Debug.Log(gameObject.name + " has been permanently sealed.");
    }

    // ----------------------------------------------------
    // IInteractable
    // ----------------------------------------------------
    public void Interact(GameObject interactor)
    {
        if (isPermanentlySealed || isRotating) return;

        if (isLocked)
        {
            Debug.Log(gameObject.name + " is LOCKED! Cannot open.");
            return;
        }

        if (interactor != null && interactor.TryGetComponent(out InteractionHandler handler))
            handler.interactionUI?.HidePrompt();

        ToggleDoor();
    }
    
    IEnumerator TemporarilyDisableCollider(float waitTime)
    {
        if (doorMeshCollider == null) yield break;

        // 1. **Close door mesh hitbox for a brief moment**
        doorMeshCollider.enabled = false;
        
        Debug.Log(gameObject.name + " Collider/Hitbox DISABLED briefly.");

        // Wait for the specified duration (e.g., until the door is mostly closed)
        yield return new WaitForSeconds(waitTime);

        // 2. Re-enable the Collider
        // Only re-enable if the door is fully closed and not sealed
        if (!isOpen && !isPermanentlySealed)
        {
            doorMeshCollider.enabled = true;
            Debug.Log(gameObject.name + " Collider/Hitbox ENABLED.");
        }
        else if (isPermanentlySealed)
        {
            // If sealed, we leave the collider disabled to prevent interaction
            Debug.Log(gameObject.name + " is SEALED. Collider remains DISABLED.");
        }
    }
    
    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    public string GetInteractionText()
    {
        if (isPermanentlySealed) return "";
        if (isLocked) return "Door is Locked";
        return isOpen ? "Close Door" : "Open Door";
    }
}
