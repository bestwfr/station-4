using UnityEngine;

public class AmmoPickup : MonoBehaviour
{
    // กำหนดจำนวนกระสุนสำรองที่จะได้รับจากกล่องนี้
    [Header("Pickup Settings")]
    public int ammoAmount = 18; // อาจจะเป็น 3 แม็ก (6x3) หรือตามที่คุณต้องการ

    // ฟังก์ชันนี้ถูกเรียกโดยสคริปต์ Gun เมื่อผู้เล่นกด E
    public void Collect(Gun gunScript)
    {
        // 1. เรียกฟังก์ชันในสคริปต์ Gun เพื่อเพิ่มกระสุนสำรอง
        gunScript.AddReserveAmmo(ammoAmount);

        // 2. ทำลายวัตถุ (กล่องกระสุน) ทิ้ง
        Destroy(gameObject);

        // *** สามารถเพิ่มเสียงหรือเอฟเฟกต์เก็บกระสุนตรงนี้ได้ในอนาคต ***
    }
}