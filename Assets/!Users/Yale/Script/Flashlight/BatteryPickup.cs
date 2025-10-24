using UnityEngine;

public class BatteryPickup : MonoBehaviour
{
    public enum DeviceType 
    { 
        Flashlight, 
        Radio       
    }

    [Header("Item Settings")]
    [Tooltip("ถ่านนี้สำหรับอุปกรณ์ไหน?")]
    public DeviceType forDevice;

    [Tooltip("จำนวนแบตเตอรี่ที่จะเติมให้")]
    public float batteryAmount = 25.0f;

    [Tooltip("ความเร็วในการหมุน")]
    public float rotationSpeed = 50.0f;

    
    void Update()
    {
        if (rotationSpeed > 0)
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 1. เช็กว่า Player วิ่งมาชน
        if (other.CompareTag("Player"))
        {
            switch (forDevice)
            {
                // ---- 1. ถ้าเป็น "ถ่านไฟฉาย" ----
                case DeviceType.Flashlight:
                    
                    // --- แก้ตรงนี้! ---
                    // ใช้ GetComponentInParent เพื่อหา "พ่อ" (Player) ที่มีสคริปต์ไฟฉาย
                    FlashlightController flashlight = other.GetComponentInParent<FlashlightController>();
                    
                    if (flashlight != null)
                    {
                        // --- เพิ่มเช็กตรงนี้! ---
                        // เช็กก่อนว่าแบตเต็มรึยัง
                        if (flashlight.currentBattery < flashlight.maxBattery)
                        {
                            flashlight.AddBattery(batteryAmount);
                            Debug.Log("เติมแบตไฟฉายแล้ว!");
                            Destroy(gameObject); // ค่อยทำลายทิ้ง
                        }
                        else
                        {
                            Debug.Log("แบตไฟฉายเต็มแล้ว! เก็บไม่ได้!");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Player คนนี้ไม่มีสคริปต์ FlashlightController!");
                    }
                    break;

                // ---- 2. ถ้าเป็น "ถ่านวิทยุ" ----
                case DeviceType.Radio:
                    // (อันนี้ใช้ GetComponent ถูกแล้ว เพราะสคริปต์อยู่บน Character)
                    RadioTracker radio = other.GetComponent<RadioTracker>();
                    if (radio != null)
                    {
                        // --- เพิ่มเช็กตรงนี้! ---
                        // เช็กก่อนว่าแบตเต็มรึยัง
                        if (radio.currentBattery < radio.maxBattery)
                        {
                            radio.AddBattery(batteryAmount);
                            Debug.Log("เติมแบตวิทยุแล้ว!");
                            Destroy(gameObject); // ค่อยทำลายทิ้ง
                        }
                        else
                        {
                            Debug.Log("แบตวิทยุเต็มแล้ว! เก็บไม่ได้!");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Player คนนี้ไม่มีสคริปต์ RadioTracker!");
                    }
                    break;
            }
        }
    }
}