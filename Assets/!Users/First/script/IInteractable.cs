using UnityEngine;

// **********************************************
// สร้างเป็นไฟล์ชื่อ IInteractable.cs
// **********************************************
public interface IInteractable
{
    // เมธอดที่ Gun.cs จะเรียกเมื่อผู้เล่นกด 'E'
    void Interact(GameObject interactor); 

    // เมธอดสำหรับดึงข้อความที่จะแสดงใน InteractionUI
    string GetInteractionText(); 
}