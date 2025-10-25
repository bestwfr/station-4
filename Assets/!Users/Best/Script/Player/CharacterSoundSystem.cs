using FSR;
using UnityEngine;
using UnityEngine.Events;

public class CharacterSoundSystem : MonoBehaviour
{
    public FSR_Player audioPlayer;
    
    public void StartFootStep(float volume)
    {
        audioPlayer.PlayFootstep(volume);
    }
    
    public void PlayLandSound(float impactIntensity)
    {
        audioPlayer.PlayLandSound(impactIntensity);
    }

    public void PlayJumpSound()
    {
        audioPlayer.PlayJumpSound();
    }
}