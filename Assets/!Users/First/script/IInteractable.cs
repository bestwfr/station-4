using UnityEngine;

// สัญญาสำหรับวัตถุที่สามารถโต้ตอบได้
public interface IInteractable
{
    // เมธอดที่ Interactor จะเรียกเมื่อผู้เล่นกดปุ่ม E
    void Interact(Gun interactor); 
    
    // เมธอดสำหรับแสดงข้อความที่หน้าจอ (เช่น "Pickup Ammo")
    string GetInteractionText();
}