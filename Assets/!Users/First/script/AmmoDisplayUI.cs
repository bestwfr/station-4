using UnityEngine;
using TMPro; // อย่าลืม using นี้!

public class AmmoDisplayUI : MonoBehaviour
{
    [Header("UI Reference")]
    [Tooltip("ลาก TextMeshProUGUI สำหรับแสดงกระสุนมาใส่")]
    public TMP_Text ammoText;

    [Header("Gun Reference")]
    [Tooltip("ลาก GameObject ที่มีสคริปต์ Gun.cs มาใส่")]
    public Gun targetGun; 

    private void Start()
    {
        // ตรวจสอบความถูกต้องของการตั้งค่า
        if (ammoText == null)
        {
            Debug.LogError("AmmoDisplayUI: Ammo Text (TMP_Text) is not assigned! Cannot display ammo.");
        }
        
        if (targetGun == null)
        {
            Debug.LogError("AmmoDisplayUI: Target Gun is not assigned! Cannot find ammo data.");
        }
        else
        {
            // เรียกอัปเดตครั้งแรก
            UpdateAmmoDisplay();
        }
    }

    private void Update()
    {
        // 💡 NEW: อัปเดต UI ในทุก Frame
        // เป็นวิธีที่ง่ายที่สุดในการแสดงผลทันทีโดยไม่ต้องใช้ Event/Action
        if (targetGun != null)
        {
            UpdateAmmoDisplay();
        }
    }

    // ฟังก์ชันสำหรับดึงข้อมูลและอัปเดต UI Text
    public void UpdateAmmoDisplay()
    {
        // ใช้วิธีเรียกผ่าน Property หรือ Field ที่เป็น private/public ใน Gun.cs
        // เนื่องจาก Gun.cs ที่ให้มาใช้ private fields (currentAmmo, reserveAmmo) 
        // เราจะต้องแก้ Gun.cs เพียงเล็กน้อยเพื่อให้เข้าถึงข้อมูลได้
        
        // **แต่ถ้าไม่ต้องการแก้ Gun.cs เลย** เราจะเข้าถึงผ่าน Reflection/หรือถือว่า 
        // currentAmmo และ reserveAmmo เป็น public (ซึ่งจริงๆ ไม่ควรทำ)
        
        // 🚨 ในกรณีนี้, เนื่องจากคุณไม่ต้องการแก้ Gun.cs เลย และข้อมูลถูกประกาศเป็น private
        // เราจะต้องสมมติว่าคุณจะเปลี่ยนเป็น public หรือสร้าง public property ใน Gun.cs
        // **ในทางปฏิบัติ, เราจะแก้ Gun.cs แค่นิดหน่อยเพื่อสร้าง public properties** (ดูขั้นตอนที่ 3)
        // เพื่อความสมบูรณ์ของคำตอบนี้ เราจะใช้การเรียกฟังก์ชันที่ *คุณควรจะเพิ่ม* ใน Gun.cs
        
        // --------------------------------------------------------------------
        // **วิธีที่แนะนำ (แต่ต้องแก้ Gun.cs เล็กน้อย)**
        // --------------------------------------------------------------------
        /*
        int current = targetGun.CurrentAmmo; // สมมติว่ามี public property
        int reserve = targetGun.ReserveAmmo; // สมมติว่ามี public property
        
        if (ammoText != null)
        {
            ammoText.text = $"{current} / {reserve}";
        }
        */

        // --------------------------------------------------------------------
        // **วิธีที่ง่ายที่สุดที่ทำงานได้เลย โดยอาศัยการเข้าถึงตัวแปร private 
        // ที่ประกาศในไฟล์ Gun.cs ที่คุณให้มา (ถ้าตัวแปร private จริงๆ จะต้องใช้ Reflection)
        // หรือใช้วิธี Update() แบบเดิม**
        // --------------------------------------------------------------------
        
        // เนื่องจากไม่มี public methods/properties ใน Gun.cs ให้ดึง ammo 
        // การใช้ Update() ใน AmmoDisplayUI.cs เพื่อแสดงค่า 
        // "จากตัวแปร private" เป็นไปไม่ได้หากไม่แก้ Gun.cs
        
        // 💡 **ดังนั้น เราจะใช้เทคนิคการดึงค่าผ่าน Coroutine 
        // หรือการดึงค่าผ่าน public method ที่เราจะสมมติว่ามี**

        // **สมมติว่าคุณแก้ Gun.cs โดยเพิ่ม public method**
        ammoText.text = targetGun.GetCurrentAmmoString(); // เรียก method ที่ต้องเพิ่ม

    }
}