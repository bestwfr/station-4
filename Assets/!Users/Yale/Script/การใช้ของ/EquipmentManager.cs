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
        // บังคับปิดทั้งคู่ตอนเริ่มเกม
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
            else
            {
                EquipItem(EquippedItem.Radio);
            }
        }
        else if (scroll < 0f) // เลื่อนลง
        {
            if (currentItem != EquippedItem.Radio)
            {
                EquipItem(EquippedItem.Radio);
            }
            else
            {
                EquipItem(EquippedItem.Flashlight);
            }
        }

        // --- 2. ปุ่ม "ใช้งาน" (<<<<<--- แก้ตรงนี้!!! ---) ---
        if (Input.GetKeyDown(KeyCode.V)) 
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
            // 1. บังคับปิดไฟฉาย
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
            // 1. บังคับปิดวิทยุ
            if (radio != null)
            {
                radio.ForceRadioOff(); 
            }
            
            // 2. จัดการโมเดล (ถ้ามี)
            if (radioModel != null) radioModel.SetActive(false);
            if (flashlightModel != null) flashlightModel.SetActive(true);
        }
    }

    // --- ฟังก์ชันตอน "คลิกซ้าย" (ตอนนี้คือปุ่ม V) ---
    void UseEquippedItem()
    {
        bool didItemTurnOn = false; 

        switch (currentItem)
        {
            case EquippedItem.Flashlight:
                if (flashlight != null)
                {
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