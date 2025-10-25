using UnityEngine;

// ต้อง implements IInteractable
public class AmmoPickup : MonoBehaviour, IInteractable
{
    public int ammoAmount = 30;

    // ใช้เมธอด GetInteractionText() เพื่อบอก UI ว่าต้องแสดงอะไร
    public string GetInteractionText()
    {
        return "Pickup Ammo";
    }

    // ใช้เมธอด Interact() เพื่อบอกว่าเมื่อถูกโต้ตอบแล้วต้องทำอะไร
    public void Interact(Gun interactor)
    {
        if (interactor != null)
        {
            // เรียกเมธอด AddReserveAmmo ของสคริปต์ Gun
            interactor.AddReserveAmmo(ammoAmount);
            
            // เก็บเสร็จแล้วก็ทำลายตัวเอง
            Destroy(gameObject);
        }
    }
}