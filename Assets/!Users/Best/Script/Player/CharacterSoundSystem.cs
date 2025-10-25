using FSR;
using UnityEngine;
using UnityEngine.Events;

public class CharacterSoundSystem : MonoBehaviour
{
    public FSR_Player audioPlayer;
    
    public void StartFootStep(float volume)
    {
        audioPlayer.PlayFootstep(volume);
        HearingManager.Instance.OnSoundEmitted(gameObject, transform.position,EHeardSoundCategory.EFootstep, volume);
    }
    
    public void PlayLandSound(float impactIntensity)
    {
        audioPlayer.PlayLandSound(impactIntensity);
        HearingManager.Instance.OnSoundEmitted(gameObject, transform.position,EHeardSoundCategory.EJump, impactIntensity * 2);
    }

    public void PlayJumpSound()
    {
        audioPlayer.PlayJumpSound();
    }
}