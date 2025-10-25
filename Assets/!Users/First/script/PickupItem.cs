using UnityEngine;

public class PickupItem : MonoBehaviour, IInteractable
{
    public string itemDisplayName = "Key";

    public void Interact(GameObject interactor)
    {
        Debug.Log("Picked up: " + itemDisplayName + " by " + interactor.name);
        // เพิ่มไอเท็มใน Inventory ของ Player ที่ interactor.GetComponent<PlayerInventory>()

        Destroy(gameObject);
    }

    public string GetInteractionText()
    {
        return "Pickup " + itemDisplayName;
    }
}
