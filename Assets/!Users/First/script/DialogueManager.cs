using System.Collections;
using TMPro; 
using UnityEngine;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    [Header("UI References (Simplified)")]
    // 🚨 NEW: Dialogue Text is now the only active element we control
    public TextMeshProUGUI dialogueText; 
    
    // 🚨 Removed: public GameObject interactionPrompt; 
    
    [Header("Audio & Speed Settings")]
    public AudioSource audioSource;
    public AudioClip typingSound;
    public float typingSpeed = 0.05f;
    
    private bool isTyping = false;
    private string currentSentence;
    
    public bool IsDialogueActive { get; private set; } = false;

    void Start()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // 🚨 Activate/Deactivate the DialogueText's GameObject directly
        if (dialogueText != null) dialogueText.gameObject.SetActive(false);
    }
    
    public void StartDialogue(string sentence)
    {
        if (IsDialogueActive) 
        {
            SkipAndClose(); 
            return;
        }
        
        IsDialogueActive = true;
        // 🚨 NEW: Activate the Text GameObject directly
        if (dialogueText != null) dialogueText.gameObject.SetActive(true);
        
        currentSentence = sentence;
        StopAllCoroutines(); 
        StartCoroutine(TypeSentence());
    }

    IEnumerator TypeSentence()
    {
        isTyping = true;
        dialogueText.text = ""; 
        
        // ... (Typing logic remains the same) ...
        foreach (char letter in currentSentence.ToCharArray())
        {
            dialogueText.text += letter;
            if (typingSound != null)
            {
                audioSource.PlayOneShot(typingSound);
            }
            yield return new WaitForSeconds(typingSpeed);
        }
        isTyping = false;
    }

    public void SkipAndClose()
    {
        if (isTyping)
        {
            StopAllCoroutines();
            dialogueText.text = currentSentence;
            isTyping = false;
        }
        else
        {
            EndDialogue();
        }
    }

    public void EndDialogue()
    {
        // 🚨 NEW: Deactivate the Text GameObject directly
        if (dialogueText != null) dialogueText.gameObject.SetActive(false);
        IsDialogueActive = false;
    }
    
    // Removed: ShowPrompt function since we deleted the Prompt UI.
}