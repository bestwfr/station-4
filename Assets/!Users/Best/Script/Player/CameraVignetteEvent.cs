using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

namespace KinematicCharacterController
{
    public class CameraVignetteEvent : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MainCharacterController characterController;
        [SerializeField] private Volume volume;
        private Vignette vignette;
        
        [Header("Settings")]
        [SerializeField] private float defaultVignette;
        [SerializeField] private float tiredVignette;
        [SerializeField] private float transitionSpeed = 0.2f;
        
        private float _currentVignette;
        private float _targetVignette;

        private void Awake()
        {
            if(volume.profile.TryGet(out vignette))
            {
                _currentVignette = defaultVignette;
            }
        }

        private void Update()
        {
            if (characterController.IsTired)
            {
                _targetVignette = tiredVignette;
            }
            else
            {
                _targetVignette = defaultVignette;
            }
            
            _currentVignette = Mathf.Lerp(_currentVignette, _targetVignette, Time.deltaTime * transitionSpeed);
            
            vignette.intensity.value = _currentVignette;
        }
    }
}