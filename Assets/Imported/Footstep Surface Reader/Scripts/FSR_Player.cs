using System;
using FSR;
using UnityEngine;
using Random = UnityEngine.Random;


namespace FSR
{
    [RequireComponent(typeof(AudioSource))]
    public class FSR_Player : MonoBehaviour
    {
        private AudioSource m_AudioSource;
        public Transform foot;
        public float raycastSize = 10;
        [SerializeField] private FSR_Data data;
        
        private const FSR_Data.FootstepAction DEFAULT_ACTION = FSR_Data.FootstepAction.Walk;

        public void Start()
        {
            m_AudioSource = GetComponent<AudioSource>();
            if (foot == null)
            {
                Debug.Log("unassigned foot ");
            }
        }

        public void PlayFootstep(float volume = 1f)
        {
            // Defaults to the Walk action when called as a simple footstep
            PlaySoundForAction(FSR_Data.FootstepAction.Walk, volume); 
        }
        
        public void PlayJumpSound()
        {
            // Jump sounds are often not volume-dependent, so we can set volume to 1f
            PlaySoundForAction(FSR_Data.FootstepAction.Jump, 1f); 
        }
        
        public void PlayLandSound(float impactVolume = 1f)
        {
            // The impact volume might be determined by fall speed
            PlaySoundForAction(FSR_Data.FootstepAction.Land, impactVolume);
        }
        
        public void PlaySoundForAction(FSR_Data.FootstepAction action, float volume = 1f)
        {
            RaycastHit hit;
            if (Physics.Raycast(foot.position, -foot.up, out hit, raycastSize))
            {
                // This section finds the name of the surface the player is standing on.
                // We'll use a single string to hold the surface name for simplicity.
                string surfaceName = "GENERIC"; 
                
                // Simplified surface detection logic (retains the try/catch structure)
                try {
                   // Try to get Simple Surface
                   FSR_SimpleSurface surface = hit.transform.GetComponent<FSR_SimpleSurface>();
                   if (surface != null) { surfaceName = surface.GetSurface(); }
                }
                catch { 
                    try {
                        // Try to get Tagged Surface
                        FSR_TagedSurface surface = hit.transform.GetComponent<FSR_TagedSurface>();
                        if (surface != null) { surfaceName = surface.GetSurface(); }
                    }
                    catch {
                        try {
                            // Try to get Terrain Surface
                            FSR_TerrainSurface surface = hit.transform.GetComponent<FSR_TerrainSurface>();
                            if (surface != null) { surfaceName = surface.GetSurface(transform.position); }
                        }
                        catch { /* Already defaulted to GENERIC */ }
                    }
                }

                // 2. Look up the Surface Data based on the name
                foreach (FSR_Data.SurfaceType surfaceData in data.surfaces)
                {
                    if (surfaceData.name.Equals(surfaceName))
                    {
                        // 3. Pass the SurfaceData AND the desired Action to the play function
                        PlaySound(surfaceData, action, volume);
                        return; // Exit once sound is found and played
                    }
                }
                
                // Fallback for GENERIC if surfaceName didn't match any specific entry
                if (surfaceName != "GENERIC")
                {
                     foreach (FSR_Data.SurfaceType surfaceData in data.surfaces)
                     {
                         if (surfaceData.name.Equals("GENERIC"))
                         {
                             PlaySound(surfaceData, action, volume);
                             return;
                         }
                     }
                }
            }
        }

        // Overload for backwards compatibility or simple walking calls
        public void PlaySoundForAction(float volume = 1f)
        {
            PlaySoundForAction(DEFAULT_ACTION, volume);
        }



        // --- MODIFIED PLAY SOUND METHOD ---
        // Now requires the desired FootstepAction to select the correct sound array.
        private void PlaySound(FSR_Data.SurfaceType surfaceType, FSR_Data.FootstepAction action, float volume)
        {
            AudioClip[] soundEffects = null;

            // Find the AudioClip array that matches the desired action
            foreach (FSR_Data.ActionSound actionSound in surfaceType.actionSounds)
            {
                if (actionSound.action == action)
                {
                    soundEffects = actionSound.soundEffects;
                    break;
                }
            }

            // If a valid sound array was found for this action/surface combination
            if (soundEffects != null && soundEffects.Length > 0)
            {
                // pick & play a random footstep sound from the array,
                // excluding sound at index 0 (as you intended for randomization)
                int n = Random.Range(0, soundEffects.Length);
                m_AudioSource.clip = soundEffects[n];
                m_AudioSource.PlayOneShot(m_AudioSource.clip, volume);
                
                // move picked sound to index 0 so it's not picked next time
                (soundEffects[0], soundEffects[n]) = (soundEffects[n], soundEffects[0]);
            }
            else
            {
                // Optional: Log a warning if a sound isn't configured for this action/surface
                Debug.LogWarning($"No sound configured for Action: {action} on Surface: {surfaceType.name}");
            }
        }
    }
}

