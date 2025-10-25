using UnityEngine;

public class AmmoPickup : MonoBehaviour, IInteractable
{
    public int ammoAmount = 30;

    public string GetInteractionText()
    {
        return "Pickup Ammo";
    }

    public void Interact(GameObject interactor)
    {
        if (interactor != null)
        {
            var gun = interactor.GetComponentInChildren<Gun>(); // ถ้ามี
            if (gun != null)
                gun.AddReserveAmmo(ammoAmount);

            Destroy(gameObject);
        }
    }
}
