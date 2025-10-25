using UnityEngine;
using System.Collections; // เผื่อต้องการใช้ Coroutine ในการเปิดประตูแบบนิ่มนวล

// ต้องมี 'using UnityEngine;' และต้อง Implements IInteractable
public class DoorController : MonoBehaviour, IInteractable
{
    // ตัวแปรที่ต้องการสำหรับการหมุนประตู
    [Header("Door Settings")]
    [Tooltip("แกนและมุมที่ประตูจะเปิด (เช่น Y = 90)")]
    public Vector3 openRotation = new Vector3(0, 90f, 0); // หมุนรอบแกน Y 90 องศา
    
    [Tooltip("ความเร็วในการเปิด/ปิดประตู")]
    public float rotationSpeed = 3f;

    // ตัวแปรส่วนตัว
    private bool isOpen = false;
    private Quaternion initialRotation; // Rotation เริ่มต้น
    private Quaternion targetRotation;  // Rotation เป้าหมาย

    void Start()
    {
        // เก็บ Rotation เริ่มต้นไว้เมื่อเกมเริ่ม
        initialRotation = transform.localRotation;
        targetRotation = initialRotation;
    }

    void Update()
    {
        // 💡 NEW: ทำให้ประตูหมุนไปยัง Rotation เป้าหมายอย่างนุ่มนวล
        transform.localRotation = Quaternion.Slerp(
            transform.localRotation, 
            targetRotation, 
            Time.deltaTime * rotationSpeed
        );
    }

    // ----------------------------------------------------
    // IInteractable Implementation
    // ----------------------------------------------------
    
    public void Interact(Gun interactor)
    {
        // 💡 ตรวจสอบว่าถูกเรียกใช้
        Debug.Log("Door Interact called by " + interactor.gameObject.name); 
        ToggleDoor(); 
    }

    public string GetInteractionText()
    {
        // ข้อความจะเปลี่ยนตามสถานะประตู
        return isOpen ? "Close Door" : "Open Door";
    }
    
    // ----------------------------------------------------
    // Logic การเปิด/ปิดประตูจริง
    // ----------------------------------------------------
    
    public void ToggleDoor()
    {
        isOpen = !isOpen;
        
        if (isOpen)
        {
            // กำหนดเป้าหมายให้เป็น Rotation เปิด
            targetRotation = initialRotation * Quaternion.Euler(openRotation);
        }
        else
        {
            // กำหนดเป้าหมายให้เป็น Rotation เริ่มต้น (ปิด)
            targetRotation = initialRotation;
        }
        
        Debug.Log("Door Toggled: " + (isOpen ? "Open" : "Closed"));
    }
}