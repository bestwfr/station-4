using UnityEngine;

public class FlashlightController : MonoBehaviour
{
    public Light flashlight; 
    private bool isFlashlightOn = false;

    [Header("Normal Mode Settings")]
    public float normalIntensity = 2.0f;
    public float normalSpotAngle = 45.0f;

    [Header("High Beam Mode Settings")]
    public float highBeamIntensity = 8.0f;
    public float highBeamSpotAngle = 15.0f;

    // --- ของใหม่ที่เพิ่มเข้ามา ---
    [Header("Battery System")]
    public float maxBattery = 100.0f; // แบตเตอรี่เต็ม (หน่วยเป็น วินาที หรือแล้วแต่เราจะเทียบ)
    public float currentBattery; // แบตเตอรี่ปัจจุบัน
    
    [Tooltip("แบตจะลดเท่าไหร่ 'ต่อวินาที' ในโหมดปกติ")]
    public float normalDrainRate = 0.5f; // ใช้แบตน้อย (ปรับได้)

    [Tooltip("แบตจะลดเท่าไหร่ 'ต่อวินาที' ในโหมดส่องจ้า")]
    public float highBeamDrainRate = 2.0f; // ใช้แบตเยอะ (ปรับได้)
    
    // (เดี๋ยวเราจะทำ UI มาแสดงค่า currentBattery ทีหลัง)
    // --- จบของใหม่ ---


    void Start()
    {
        // สั่งให้ไฟฉายปิดตอนเริ่มเกม
        if (flashlight != null)
        {
            flashlight.enabled = false;
            isFlashlightOn = false;
            
            flashlight.intensity = normalIntensity;
            flashlight.spotAngle = normalSpotAngle;
        }

        // --- ของใหม่ ---
        // ตั้งค่าแบตเตอรี่ให้เต็มตอนเริ่มเกม
        currentBattery = maxBattery;
        // --- จบของใหม่ ---
    }

    void Update()
    {
        // --- 1. เช็กการกดปุ่มเปิด/ปิด (LMB) ---
        if (Input.GetButtonDown("Fire1"))
        {
            ToggleFlashlight();
        }


        // --- 2. อัปเดตการทำงานของไฟฉาย (ถ้ามันเปิดอยู่) ---
        if (isFlashlightOn)
        {
            // --- 2a. เช็กว่าแบตหมดหรือยัง ---
            if (currentBattery <= 0)
            {
                // ถ้าแบตหมด = บังคับปิดไฟทันที
                currentBattery = 0; // ไม่ให้ติดลบ
                isFlashlightOn = false;
                flashlight.enabled = false;
                Debug.Log("แบตหมด!! (Battery Dead!)"); // แจ้งเตือนใน Console
                return; // จบการทำงานในเฟรมนี้ ไม่ต้องทำต่อ
            }

            // --- 2b. ถ้าแบตยังไม่หมด: ทำงานตามโหมด ---
            
            // เช็กว่ากำลัง "กดคลิกขวาค้าง" (Fire2) มั้ย?
            if (Input.GetButton("Fire2"))
            {
                // โหมด High Beam
                flashlight.intensity = highBeamIntensity;
                flashlight.spotAngle = highBeamSpotAngle;
                
                // ลดแบตแบบ High Beam
                // (คูณ Time.deltaTime เพื่อให้มันลด "ต่อวินาที" ไม่ใช่ "ต่อเฟรม")
                currentBattery -= highBeamDrainRate * Time.deltaTime;
            }
            else
            {
                // โหมดปกติ
                flashlight.intensity = normalIntensity;
                flashlight.spotAngle = normalSpotAngle;

                // ลดแบตแบบปกติ
                currentBattery -= normalDrainRate * Time.deltaTime;
            }

            // --- (Optional) ไว้เช็กค่าแบตใน Console ---
            // Debug.Log("Battery: " + currentBattery);
        }
    }

    // ฟังก์ชันสำหรับเปิด/ปิดไฟฉาย
    void ToggleFlashlight()
    {
        if (flashlight == null) return;

        // สลับค่า true/false
        isFlashlightOn = !isFlashlightOn;

        // --- ลอจิกใหม่: เช็กแบตก่อนเปิด ---
        if (isFlashlightOn) // ถ้าสถานะคือ "กำลังจะเปิด"
        {
            if (currentBattery > 0)
            {
                // แบตยังเหลือ -> เปิดได้
                flashlight.enabled = true;
                // ตั้งค่าเริ่มต้นเป็นโหมดปกติ
                flashlight.intensity = normalIntensity;
                flashlight.spotAngle = normalSpotAngle;
            }
            else
            {
                // แบตหมด -> เปิดไม่ได้
                isFlashlightOn = false; // สลับกลับไปเป็น false
                flashlight.enabled = false;
                Debug.Log("คลิกเปิด... แต่แบตหมด!");
            }
        }
        else
        {
            // ถ้าสถานะคือ "กำลังจะปิด" -> ปิดได้เลย
            flashlight.enabled = false;
        }
    }

    // --- (โบนัส) ฟังก์ชันสำหรับให้ไอเทมอื่นมา "เติมแบต" ---
    public void AddBattery(float amount)
    {
        currentBattery += amount;
        if (currentBattery > maxBattery)
        {
            currentBattery = maxBattery; // ไม่ให้เกินค่า Max
        }
        Debug.Log("เติมแบตแล้ว! ปัจจุบัน: " + currentBattery);
    }
}