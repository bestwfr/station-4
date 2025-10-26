using UnityEngine;

public class RadioTracker : MonoBehaviour
{
    [Header("เป้าหมาย (ตัวมัน)")]
    public Transform monsterTransform;

    [Header("แหล่งกำเนิดเสียง")]
    public AudioSource staticSource;
    public AudioSource alertSource;
    public AudioSource whisperSource;

    [Header("เสียงเอฟเฟกต์ (SFX)")]
    public AudioSource radioOnSound;
    public AudioSource radioOffSound;

    [Header("ตั้งค่าระยะ")]
    public float maxRadioRange = 25f;
    public float whisperRange = 15f;
    public float proximityAlertRange = 5f;

    [Header("ระบบแบตเตอรี่")]
    public float maxBattery = 100f;
    public float currentBattery = 100f;
    public float batteryDrainRate = 1f;

    [Header("ไฟสถานะ (Indicator)")]
    [Tooltip("ลาก 'MeshRenderer' ของหลอดไฟ (IndicatorLight) มาใส่")]
    public MeshRenderer indicatorLightRenderer;
    [Tooltip("ลาก Material 'Neon_Green_On' มาใส่")]
    public Material materialOn;
    [Tooltip("ลาก Material 'Neon_Red_Off' มาใส่")]
    public Material materialOff;

    private bool isRadioOn = true; // <<<--- เริ่มมาเป็น false (ปิด)
    private bool isAlertPlaying = false;

    void Start()
    {
        // (เช็กของ)
        if (monsterTransform == null) Debug.LogError("Error: RadioTracker - ยังไม่ได้ลาก 'Monster Transform' มาใส่!");
        if (staticSource == null) Debug.LogError("Error: RadioTracker - ยังไม่ได้ลาก 'Static Source' มาใส่!");
        if (alertSource == null) Debug.LogError("Error: RadioTracker - ยังไม่ได้ลาก 'Alert Source' มาใส่!");
        if (whisperSource == null) Debug.LogError("Error: RadioTracker - ยังไม่ได้ลาก 'Whisper Source' มาใส่!");
        if (radioOnSound == null) Debug.LogError("Error: RadioTracker - ยังไม่ได้ลาก 'Radio On Sound' มาใส่!");
        if (radioOffSound == null) Debug.LogError("Error: RadioTracker - ยังไม่ได้ลาก 'Radio Off Sound' มาใส่!");
        if (indicatorLightRenderer == null) Debug.LogError("Error: RadioTracker - ยังไม่ได้ลาก 'Indicator Light Renderer' มาใส่!");
        if (materialOn == null) Debug.LogError("Error: RadioTracker - ยังไม่ได้ลาก 'Material On' มาใส่!");
        if (materialOff == null) Debug.LogError("Error: RadioTracker - ยังไม่ได้ลาก 'Material Off' มาใส่!");

        staticSource.loop = true;
        alertSource.loop = true;
        whisperSource.loop = true;

        if (currentBattery <= 0)
        {
            isRadioOn = false;
        }
        
        UpdateIndicatorLight();
    }

    void Update()
    {
        // (ลบ Input.GetKeyDown(KeyCode.V) ออกไปแล้ว... ถูกต้อง)

        // --- ระบบจัดการพลังงาน ---
        if (isRadioOn)
        {
            currentBattery -= batteryDrainRate * Time.deltaTime;
            if (currentBattery <= 0)
            {
                currentBattery = 0;
                Debug.Log("แบตหมด! วิทยุบังคับปิด!");
                ForceRadioOff(); 
            }
        }
        
        // --- เช็กว่ามี "มัน" มั้ย ---
        if (monsterTransform == null)
        {
            // ถ้าไม่มี "มัน"... ก็บังคับให้เสียงซ่า/กระซิบเป็น 0
            HandleStaticSound(maxRadioRange + 1f);
            HandleWhisperSound(whisperRange + 1f);
            HandleAlertSound(proximityAlertRange + 1f);
            return;
        }

        // --- คำนวณระยะ ---
        float distance = Vector3.Distance(transform.position, monsterTransform.position);

        // --- เรียก Handle เสียง ---
        HandleStaticSound(distance);
        HandleWhisperSound(distance);
        HandleAlertSound(distance);
    }

    // --- ฟังก์ชันสำหรับให้ EquipmentManager เรียก (คืนค่า bool) ---
    public bool ToggleRadio()
    {
        if (isRadioOn)
        {
            // --- กำลังจะ "ปิด" ---
            isRadioOn = false;
            Debug.Log("วิทยุ: ปิด");
            radioOffSound.Play();
            staticSource.Stop();
            whisperSource.Stop();
            UpdateIndicatorLight(); // <<<--- อัปเดตไฟเป็นสีแดง
            return false; // <-- "ส่งคำตอบกลับ" ว่าเพิ่งปิด
        }
        else
        {
            // --- กำลังจะ "เปิด" ---
            if (currentBattery > 0)
            {
                isRadioOn = true;
                Debug.Log("วิทยุ: เปิด");
                // radioOnSound.Play();
                // staticSource.Play();
                // whisperSource.Play();
                UpdateIndicatorLight(); // <<<--- อัปเดตไฟเป็นสีเขียว
                return true; // <-- "ส่งคำตอบกลับ" ว่าเพิ่งเปิด
            }
            else
            {
                Debug.Log("วิทยุแบตหมด! เปิดไม่ติด!");
                return false; // <-- "ส่งคำตอบกลับ" ว่าเปิดไม่ติด
            }
        }
    }

    // --- ฟังก์ชันสำหรับ "บังคับปิด" (สั่งจากข้างนอก) ---
    public void ForceRadioOff()
    {
        if (isRadioOn) // <<<--- เช็กก่อนว่ามันเปิดอยู่มั้ย
        {
            isRadioOn = false;
            radioOffSound.Play(); // เล่นเสียง Beep ปิด
            staticSource.Stop();
            whisperSource.Stop();
            UpdateIndicatorLight(); // <<<--- อัปเดตไฟเป็นสีแดง
        }
    }
    
    // --- ฟังก์ชันคุมไฟสถานะ ---
    void UpdateIndicatorLight()
    {
        if (indicatorLightRenderer == null) return; 

        if (isRadioOn)
        {
            if (materialOn != null)
                indicatorLightRenderer.material = materialOn;
        }
        else
        {
            if (materialOff != null)
                indicatorLightRenderer.material = materialOff;
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
        Debug.Log($"เติมแบตวิทยุแล้ว! พลังงานปัจจุบัน: {currentBattery}");
    }


    // --- ฟังก์ชัน Handle เสียง (3 อัน) ---
    void HandleStaticSound(float distance)
    {
        if (!isRadioOn) { staticSource.volume = 0; return; }
        if (distance <= maxRadioRange) { staticSource.volume = Mathf.InverseLerp(maxRadioRange, 0f, distance); }
        else { staticSource.volume = 0; }
    }
    void HandleWhisperSound(float distance)
    {
        if (!isRadioOn) { whisperSource.volume = 0; return; }
        if (distance <= whisperRange) { whisperSource.volume = Mathf.InverseLerp(whisperRange, 0f, distance); }
        else { whisperSource.volume = 0; }
    }
    void HandleAlertSound(float distance)
    {
        if (monsterTransform == null) return; // (เพิ่มเช็กกัน Error)
        if (distance <= proximityAlertRange) { if (!isAlertPlaying) { alertSource.Play(); isAlertPlaying = true; } }
        else { if (isAlertPlaying) { alertSource.Stop(); isAlertPlaying = false; } }
    }
}