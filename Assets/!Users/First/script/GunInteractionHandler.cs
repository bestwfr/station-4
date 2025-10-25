using UnityEngine;

public class InteractionHandler : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactRange = 5f;
    public InteractionUI interactionUI;
    public Camera playerCamera;

    private IInteractable currentInteractable;

    void Update()
    {
        DetectInteraction();

        if (Input.GetKeyDown(KeyCode.E) && currentInteractable != null)
    {
        Debug.Log("Press E on: " + currentInteractable.GetType().Name);
        currentInteractable.Interact(gameObject);
    currentInteractable = null;
    interactionUI?.HidePrompt();
}


    }

    private void DetectInteraction()
    {
        if (playerCamera == null) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactRange))
        {
            // Debug ตรวจสอบว่า Raycast เจออะไร
            Debug.Log("Ray hit: " + hit.collider.name);

            if (hit.collider.TryGetComponent(out IInteractable interactable))
            {
                string promptText = interactable.GetInteractionText();

                if (currentInteractable != interactable)
                    currentInteractable = interactable;

                if (interactionUI != null)
                {
                    if (string.IsNullOrEmpty(promptText))
                        interactionUI.HidePrompt();
                    else
                        interactionUI.ShowPrompt(promptText);
                }
                return;
            }
        }

        // ถ้าไม่มีวัตถุที่ interact ได้
        if (currentInteractable != null)
        {
            currentInteractable = null;
            interactionUI?.HidePrompt();
        }
    }
}
