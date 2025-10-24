using UnityEngine;
// Removed: using System.Collections;

public class AmmoPickup : MonoBehaviour
{
    // กำหนดจำนวนกระสุนสำรองที่จะได้รับจากกล่องนี้
    [Header("Pickup Settings")]
    public int ammoAmount = 18; 
    
    // 💡 NEW: ตัวแปรสถานะ เพื่อให้เก็บได้แค่ครั้งเดียวต่อการปรากฏตัว
    private bool collected = false;

    // ฟังก์ชันนี้ถูกเรียกโดยสคริปต์ Gun เมื่อผู้เล่นกด E (Raycast Hit)
    public void Collect(Gun gunScript)
    {
        // ตรวจสอบว่ายังไม่ได้เก็บ
        if (collected)
        {
            return;
        }
        
        // 1. เรียกฟังก์ชันในสคริปต์ Gun เพื่อเพิ่มกระสุนสำรอง
        gunScript.AddReserveAmmo(ammoAmount);
        
        // 2. ปิด Collider เพื่อให้กล่องอยู่แต่เก็บซ้ำไม่ได้
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }
        
        // 3. ตั้งสถานะว่าเก็บแล้ว
        collected = true;

        // *** สามารถเพิ่มเสียงหรือเอฟเฟกต์เก็บกระสุนตรงนี้ได้ในอนาคต ***
    }
}