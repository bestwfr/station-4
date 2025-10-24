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

    [Header("Battery System")]
    public float maxBattery = 100.0f;
    public float currentBattery;
    public float normalDrainRate = 0.5f;
    public float highBeamDrainRate = 2.0f;

    [Header("Audio Settings")]
    public AudioClip soundFlashlightOn;
    public AudioClip soundFlashlightOff;
    public AudioClip soundBatteryDead;
    
    private AudioSource audioSource;

    void Start()
    {
        if (flashlight != null)
        {
            flashlight.enabled = false;
            isFlashlightOn = false;
            flashlight.intensity = normalIntensity;
            flashlight.spotAngle = normalSpotAngle;
        }
        currentBattery = maxBattery;
        
        // (ใช้ GetComponent เพราะสคริปต์นี้อยู่บน Player ที่มี AudioSource)
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogError("FlashlightController: หา AudioSource ไม่เจอ! อย่าลืมแปะไว้ที่ Player นะ");
        }
    }

    void Update()
    {
        // (ลบ Input.GetButtonDown("Fire1") ออกไปแล้ว... ถูกต้อง)

        if (isFlashlightOn)
        {
            if (currentBattery <= 0)
            {
                currentBattery = 0;
                isFlashlightOn = false;
                flashlight.enabled = false;
                PlaySound(soundFlashlightOff);
                Debug.Log("แบตไฟฉายหมด!! (Battery Dead!)");
                return;
            }
            
            // (ยังเช็กปุ่ม "Fire2" (คลิกขวา) สำหรับ High Beam ได้)
            if (Input.GetButton("Fire2")) 
            {
                flashlight.intensity = highBeamIntensity;
                flashlight.spotAngle = highBeamSpotAngle;
                currentBattery -= highBeamDrainRate * Time.deltaTime;
            }
            else
            {
                flashlight.intensity = normalIntensity;
                flashlight.spotAngle = normalSpotAngle;
                currentBattery -= normalDrainRate * Time.deltaTime;
            }
        }
    }

    // --- ฟังก์ชันสำหรับให้ EquipmentManager เรียก (คืนค่า bool) ---
    public bool ToggleFlashlight()
    {
        if (flashlight == null) return false;

        isFlashlightOn = !isFlashlightOn;

        if (isFlashlightOn) // "กำลังจะเปิด"
        {
            if (currentBattery > 0)
            {
                flashlight.enabled = true;
                flashlight.intensity = normalIntensity;
                flashlight.spotAngle = normalSpotAngle;
                PlaySound(soundFlashlightOn);
                return true; // <-- "ส่งคำตอบกลับ" ว่าเพิ่งเปิด
            }
            else
            {
                isFlashlightOn = false; 
                flashlight.enabled = false;
                PlaySound(soundBatteryDead);
                return false; // <-- "ส่งคำตอบกลับ" ว่าเปิดไม่ติด
            }
        }
        else // "กำลังจะปิด"
        {
            flashlight.enabled = false;
            PlaySound(soundFlashlightOff);
            return false; // <-- "ส่งคำตอบกลับ" ว่าเพิ่งปิด
        }
    }

    // --- ฟังก์ชันสำหรับ "บังคับปิด" (สั่งจากข้างนอก) ---
    public void ForceFlashlightOff()
    {
        if (isFlashlightOn) // <<<--- เช็กก่อนว่ามันเปิดอยู่มั้ย
        {
            isFlashlightOn = false;
            flashlight.enabled = false;
            PlaySound(soundFlashlightOff);
        }
    }

    // --- ฟังก์ชันเล่นเสียง ---
    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    // --- ฟังก์ชันเติมแบต ---
    public void AddBattery(float amount)
    {
        currentBattery += amount;
        if (currentBattery > maxBattery)
        {
            currentBattery = maxBattery;
        }
        Debug.Log($"เติมแบตไฟฉายแล้ว! ปัจจุบัน: {currentBattery}");
    }
}