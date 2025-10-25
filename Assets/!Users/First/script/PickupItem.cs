using UnityEngine;

public class PickupItem : MonoBehaviour, IInteractable
{
    [Tooltip("ชื่อของไอเท็มนี้สำหรับแสดงบน UI")]
    public string itemDisplayName = "Key";

    // 💡 แก้ไข: เพิ่มพารามิเตอร์ (Gun interactor) เพื่อให้ตรงกับ IInteractable
    public void Interact(Gun interactor) 
    {
        Debug.Log("Picked up: " + itemDisplayName + " by " + interactor.gameObject.name);
        
        // 🚨 หมายเหตุ: ถ้านี่คือ Item ธรรมดาที่ไม่ใช่กระสุน
        // คุณอาจจะต้องเรียกเมธอดบนตัว Player แทนที่จะเป็น Gun
        // เช่น interactor.GetComponentInParent<PlayerInventory>().AddItem(itemDisplayName);
        
        // ท้าลายวัตถุนี้เพื่อแสดงว่าถูกเก็บไปแล้ว
        Destroy(gameObject);
    }

    public string GetInteractionText()
    {
        return "Pickup " + itemDisplayName;
    }
}