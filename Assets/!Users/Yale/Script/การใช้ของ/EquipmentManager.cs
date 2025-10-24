using UnityEngine;

public class EquipmentManager : MonoBehaviour
{
    [Header("Scripts อุปกรณ์")]
    [Tooltip("ลาก 'Player' (ตัวพ่อ) ที่มีสคริปต์ FlashlightController มาใส่")]
    public FlashlightController flashlight; 
    
    [Tooltip("ลาก 'Character' (ตัวลูก) ที่มีสคริปต์ RadioTracker มาใส่")]
    public RadioTracker radio;

    public enum EquippedItem 
    { 
        None, 
        Flashlight, 
        Radio 
    }

    [Header("สถานะปัจจุบัน")]
    [Tooltip("เริ่มเกมมา ถืออะไรก่อน")]
    public EquippedItem currentItem = EquippedItem.Flashlight; 

    [Header("โมเดลในมือ (Optional)")]
    [Tooltip("ลากโมเดล 'ไฟฉาย' ที่อยู่ใน ItemHolder (ใต้ Camera) มาใส่")]
    public GameObject flashlightModel; 
    [Tooltip("ลากโมเดล 'วิทยุ' ที่อยู่ใน ItemHolder (ใต้ Camera) มาใส่")]
    public GameObject radioModel;      

    void Start()
    {
        // บังคับปิดทั้งคู่ตอนเริ่มเกม (แก้ปัญหาเริ่มมาวิทยุติด)
        if (radio != null) radio.ForceRadioOff();
        if (flashlight != null) flashlight.ForceFlashlightOff();
        
        // แล้วค่อยเซ็ตของชิ้นแรก
        EquipItem(currentItem);
    }

    void Update()
    {
        // --- 1. ระบบสลับของ (Mouse Scroll) ---
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll > 0f) // เลื่อนขึ้น
        {
            if (currentItem != EquippedItem.Flashlight)
            {
                EquipItem(EquippedItem.Flashlight);
            }
        }
        else if (scroll < 0f) // เลื่อนลง
        {
            if (currentItem != EquippedItem.Radio)
            {
                EquipItem(EquippedItem.Radio);
            }
        }

        // --- 2. ปุ่ม "ใช้งาน" (คลิกซ้าย) ---
        if (Input.GetButtonDown("Fire1")) 
        {
            UseEquippedItem();
        }
    }

    // --- นี่คือ "สมอง" ตอนสลับของ ---
    void EquipItem(EquippedItem newItem)
    {
        currentItem = newItem;
        Debug.Log("สลับไปถือ: " + newItem.ToString());

        if (newItem == EquippedItem.Radio)
        {
            // --- ถ้าสลับมาถือ "วิทยุ" ---
            // 1. บังคับปิดไฟฉาย (และมันจะเล่นเสียงปิดเอง)
            if (flashlight != null)
            {
                flashlight.ForceFlashlightOff(); 
            }
            
            // 2. จัดการโมเดล (ถ้ามี)
            if (radioModel != null) radioModel.SetActive(true);
            if (flashlightModel != null) flashlightModel.SetActive(false);
        }
        else if (newItem == EquippedItem.Flashlight)
        {
            // --- ถ้าสลับมาถือ "ไฟฉาย" ---
            // 1. บังคับปิดวิทยุ (และมันจะเล่นเสียงปิดเอง)
            if (radio != null)
            {
                radio.ForceRadioOff(); 
            }
            
            // 2. จัดการโมเดล (ถ้ามี)
            if (radioModel != null) radioModel.SetActive(false);
            if (flashlightModel != null) flashlightModel.SetActive(true);
        }
        // (ถ้านายมีช่อง None (ไม่ถืออะไร) ก็เพิ่ม else if ตรงนี้ได้)
    }

    // --- ฟังก์ชันตอน "คลิกซ้าย" ---
    void UseEquippedItem()
    {
        bool didItemTurnOn = false; // (ตัวแปรนี้ยังใช้ได้อยู่)

        switch (currentItem)
        {
            case EquippedItem.Flashlight:
                if (flashlight != null)
                {
                    // เรายังต้องเช็ก didItemTurnOn อยู่
                    // เผื่อในอนาคตเราอยากให้ "เปิดไฟฉาย" แล้ว "ปิดวิทยุ"
                    // (ถึงแม้ว่าตอนนี้ EquipItem จะชิงปิดไปแล้วก็ตาม)
                    didItemTurnOn = flashlight.ToggleFlashlight(); 
                    if (didItemTurnOn && radio != null)
                    {
                        radio.ForceRadioOff();
                    }
                }
                break;
                
            case EquippedItem.Radio:
                if (radio != null)
                {
                    didItemTurnOn = radio.ToggleRadio();
                    if (didItemTurnOn && flashlight != null)
                    {
                        flashlight.ForceFlashlightOff();
                    }
                }
                break;
                
            case EquippedItem.None:
                break;
        }
    }
}