using UnityEngine;

public class BatteryPickup : MonoBehaviour
{
    [Header("Item Settings")]
    [Tooltip("จำนวนแบตเตอรี่ที่จะเติมให้ (เช่น 25, 50)")]
    public float batteryAmount = 25.0f;

    [Tooltip("ความเร็วในการหมุน (0 = ไม่หมุน)")]
    public float rotationSpeed = 50.0f;

    
    void Update()
    {
        // (โบนัส) ทำให้ไอเทมหมุนๆ จะได้ดูเด่น
        if (rotationSpeed > 0)
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        }
    }

    // --- นี่คือหัวใจสำคัญ! ---
    // ฟังก์ชันนี้จะทำงาน "อัตโนมัติ" เมื่อมี Collider อื่น
    // ที่ "Is Trigger = false" (เช่น Player) วิ่งเข้ามาชน
    private void OnTriggerEnter(Collider other)
    {
        // "other" คือสิ่งที่วิ่งมาชนเรา

        // 1. เช็กก่อนว่า "Player" วิ่งมาชน (ไม่ใช่ผี หรือกระสุน)
        // เราจะเช็กโดยใช้ Tag
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player สัมผัสแบตเตอรี่!");

            // 2. ถ้าใช่ Player... ให้ลองค้นหาสคริปต์ FlashlightController บนตัว Player
            FlashlightController flashlight = other.GetComponentInParent<FlashlightController>();

            // 3. เช็กว่า Player คนนั้นมีสคริปต์ไฟฉายมั้ย
            if (flashlight != null)
            {
                // 4. ถ้ามี... สั่งให้มันเติมแบต!
                flashlight.AddBattery(batteryAmount);

                // 5. ทำลายตัวเองทิ้ง (เพราะเก็บไปแล้ว)
                Destroy(gameObject);
            }
            else
            {
                Debug.LogWarning("Player คนนี้ไม่มีสคริปต์ FlashlightController!");
            }
        }
    }
}