using UnityEngine;
using System.Collections;

public class DoorController : MonoBehaviour, IInteractable
{
    [Header("Door Settings")]
    public Vector3 openRotation = new Vector3(0, 90f, 0);
    public float rotationSpeed = 3f;

    [Header("Lock Settings")]
    public bool isLocked = true; // ตั้ง true → ประตูล็อคตั้งแต่เริ่ม

    [Header("Permanent Seal Settings")]
    public bool sealOnClose = false;
    public float timeToWaitBeforeSeal = 0.5f;

    private bool isOpen = false;
    private bool isPermanentlySealed = false;
    private bool isRotating = false;
    private Quaternion initialRotation;
    private Quaternion targetRotation;

    void Start()
    {
        initialRotation = transform.localRotation;
        targetRotation = initialRotation;
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
            targetRotation = initialRotation * Quaternion.Euler(openRotation);
        else
        {
            targetRotation = initialRotation;
            if (sealOnClose) StartCoroutine(SealAfterClose());
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

    public string GetInteractionText()
    {
        if (isPermanentlySealed) return "";
        if (isLocked) return "Door is Locked";
        return isOpen ? "Close Door" : "Open Door";
    }
}
