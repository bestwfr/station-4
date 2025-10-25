using UnityEngine;

namespace FSR
{
    [CreateAssetMenu]
    public class FSR_Data : ScriptableObject
    {
        public SurfaceType[] surfaces;
        
        public enum FootstepAction
        {
            Walk,
            Jump,
            Land,
            CrouchWalk,
        }
        
        [System.Serializable]
        public class SurfaceType
        {
            public string name;
            public ActionSound[] actionSounds;
        }
        
        [System.Serializable]
        public class ActionSound
        {
            // The specific action this sound group is for (e.g., Land)
            public FootstepAction action;
            
            // The array of sound effects for this specific action on this specific surface.
            public AudioClip[] soundEffects;
        }
    }
}
