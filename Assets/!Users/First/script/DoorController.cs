using UnityEngine;

public class DoorController : MonoBehaviour
{
    [Header("Door Settings")]
    public float openAngle = 90f;   // องศาที่ประตูจะหมุนเปิด (เช่น 90 องศา)
    public float closeAngle = 0f;  // องศาที่ประตูจะหมุนปิด
    public float rotationSpeed = 2f; // ความเร็วในการหมุนเปิด/ปิด

    private bool isOpen = false;
    private Quaternion targetRotation; // การหมุนเป้าหมายที่ต้องการไปถึง

    void Start()
    {
        // กำหนดให้ประตูอยู่ในสถานะปิดเมื่อเริ่มเกม
        targetRotation = Quaternion.Euler(0, closeAngle, 0);
    }

    void Update()
    {
        // หมุนประตูเข้าหา targetRotation อย่างต่อเนื่องและนุ่มนวล
        transform.localRotation = Quaternion.Slerp(
            transform.localRotation, 
            targetRotation, 
            Time.deltaTime * rotationSpeed
        );
    }

    // ฟังก์ชันที่สคริปต์ Gun จะเรียกใช้เมื่อผู้เล่นกด 'E'
    public void ToggleDoor()
    {
        isOpen = !isOpen; // สลับสถานะ (ถ้าเปิดอยู่ก็ปิด, ถ้าปิดอยู่ก็เปิด)

        if (isOpen)
        {
            // ถ้าเปิด: กำหนดการหมุนเป้าหมายเป็น openAngle
            targetRotation = Quaternion.Euler(0, openAngle, 0);
            Debug.Log(gameObject.name + " Opened.");
        }
        else
        {
            // ถ้าปิด: กำหนดการหมุนเป้าหมายเป็น closeAngle
            targetRotation = Quaternion.Euler(0, closeAngle, 0);
            Debug.Log(gameObject.name + " Closed.");
        }
    }
}