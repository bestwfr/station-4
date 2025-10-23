using UnityEngine;

// สคริปต์นี้จัดการสุขภาพของเป้าหมาย
public class Target : MonoBehaviour
{
    // กำหนดค่า Max Health ใน Inspector
    public float health = 50f; 

    // ฟังก์ชันนี้จะถูกเรียกโดยสคริปต์ Gun.cs เมื่อเป้าหมายถูกยิง
    public void TakeDamage(float amount)
    {
        // ลดสุขภาพ
        health -= amount;
        Debug.Log(gameObject.name + " Health remaining: " + health);

        // ตรวจสอบว่าสุขภาพหมดแล้วหรือยัง
        if (health <= 0f)
        {
            Die();
        }
    }

    // ฟังก์ชันสำหรับจัดการเมื่อเป้าหมาย "ตาย"
    void Die()
    {
        Debug.Log(gameObject.name + " has died!");
        
        // เราจะลบ GameObject นี้ทิ้งเมื่อตาย
        Destroy(gameObject); 
        
        // (คุณสามารถใส่เอฟเฟกต์ระเบิด หรือทำแอนิเมชันตายตรงนี้ได้)
    }
}