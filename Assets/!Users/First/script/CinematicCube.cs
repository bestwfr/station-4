using UnityEngine;

public class CinematicCube : MonoBehaviour, IInteractable
{
    [Header("Interaction/Light Settings")]
    public Light[] targetLights;

    [Header("Door Unlock Settings")]
    public DoorController targetDoor;

    [Tooltip("ลาก GameObject ของ Player UI มาใส่ (ไม่บังคับ)")]
    public GameObject playerUI;

    private bool hasBeenActivated = false;

    void Start()
    {
        if (targetLights != null)
            foreach (Light light in targetLights)
                if (light != null) light.enabled = false;

        if (targetDoor == null)
            Debug.LogWarning("CinematicCube: Target Door Controller is not assigned!");
    }

    public string GetInteractionText()
    {
        if (hasBeenActivated) return "";
        return "Activate Light";
    }

    public void Interact(GameObject interactor)
    {
        if (hasBeenActivated) return;

        if (interactor != null && interactor.TryGetComponent(out InteractionHandler handler))
            handler.interactionUI?.HidePrompt();

        ActivateSystem();
    }

    void ActivateSystem()
    {
        hasBeenActivated = true;

        if (targetLights != null)
            foreach (Light light in targetLights)
                if (light != null) light.enabled = true;

        Debug.Log("All assigned lights Activated!");

        if (targetDoor != null)
        {
            targetDoor.UnlockDoor();
            if (!targetDoor.IsOpen)
                targetDoor.ToggleDoor();

            Debug.Log("Target door unlocked and opened: " + targetDoor.gameObject.name);
        }

        if (playerUI != null)
        {
            // ... โค้ด UI ถ้ามี ...
        }
    }
}
