using UnityEngine;

public class PlaySoundOnTrigger : MonoBehaviour
{
    // [1] ลาก "ไฟล์เสียง" (AudioClip) ที่คุณต้องการมาใส่ช่องนี้
    [SerializeField] private AudioClip soundToPlay;

    // [2] ตั้งค่าความดัง (ปรับได้ 0.0 ถึง 1.0)
    [SerializeField] [Range(0f, 1f)] private float volume = 1.0f;

    // [3] (ติ๊กอันนี้) ถ้าอยากให้เสียงนี้เล่นแค่ "ครั้งเดียว"
    [SerializeField] private bool playOnce = true;

    private bool hasTriggered = false; // ตัวแปรจำว่าเล่นไปรึยัง

    // ฟังก์ชันนี้จะทำงาน... เมื่อมีอะไร "เดิน" เข้ามาในกล่อง
    void OnTriggerEnter(Collider other)
    {
        // เช็กว่า:
        // 1. คนที่เดินเข้ามาคือ "Player" รึเปล่า?
        // 2. เราอยากให้เล่นแค่ครั้งเดียว... และมันยังไม่เคยเล่น... (หรือ เราไม่ได้ติ๊กว่าจะเล่นครั้งเดียว)
        if (other.CompareTag("Player") && (!playOnce || !hasTriggered))
        {
            // ถ้าติ๊ก "เล่นครั้งเดียว" -> ตั้งค่าว่า "เล่นไปแล้วนะ!"
            if (playOnce)
            {
                hasTriggered = true;
            }

            // เช็กว่าคุณลืมลากไฟล์เสียงใส่ช่องรึเปล่า
            if (soundToPlay != null)
            {
                // เล่นเสียงแบบ 3D (เสียงดังจากจุดนี้)
                AudioSource.PlayClipAtPoint(soundToPlay, transform.position, volume);
            }
            else
            {
                // แจ้งเตือน... ถ้าลืมลากเสียงมาใส่
                Debug.LogWarning("ลืมลากไฟล์เสียงใส่ Trigger '" + gameObject.name + "'");
            }
        }
    }
}