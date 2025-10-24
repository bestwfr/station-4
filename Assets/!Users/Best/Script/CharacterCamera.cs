using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KinematicCharacterController
{
    public class CharacterCamera : MonoBehaviour
    {
        [Header("Dependencies")]
        // Drag your MainCharacterController here in the Inspector
        public MainCharacterController CharacterController;

        [Header("Framing")] public Camera Camera;
        public Vector2 FollowPointFraming = new Vector2(0f, 0f);
        public float FollowingSharpness = 10000f;
        
        [Header("Crouching")] 
        public float CrouchVerticalOffset = -0.7f; // How much lower the camera should be
        public float CrouchTransitionSpeed = 12f;  // Speed of the smooth transition

        [Header("Distance")] public float DefaultDistance = 6f;
        public float MinDistance = 0f;
        public float MaxDistance = 10f;
        public float DistanceMovementSpeed = 5f;
        public float DistanceMovementSharpness = 10f;

        [Header("Rotation")] public bool InvertX = false;
        public bool InvertY = false;
        [Range(-90f, 90f)] public float DefaultVerticalAngle = 20f;
        [Range(-90f, 90f)] public float MinVerticalAngle = -90f;
        [Range(-90f, 90f)] public float MaxVerticalAngle = 90f;
        public float RotationSpeed = 1f;
        public float RotationSharpness = 10000f;
        public bool RotateWithPhysicsMover = false;

        [Header("Obstruction")] public float ObstructionCheckRadius = 0.2f;
        public LayerMask ObstructionLayers = -1;
        public float ObstructionSharpness = 10000f;
        public List<Collider> IgnoredColliders = new List<Collider>();

        [Header("Head Bobbing (Walk/Base)")] public bool enableBobbing = true;
        public float bobFrequency = 1.8f;
        public float bobAmplitude = 0.05f;
        public float bobSpeedReference = 6f;
        public float lateralMultiplier = 0.6f;
        public float swaySmooth = 8f;

        [Header("Tired Bobbing")] 
        public float TiredBobFrequency = 0.8f;
        public float TiredBobAmplitude = 0.1f;
        public float TiredExitSmoothing = 2.0f;
        
        private float _currentLateral;
        private float _bobbingTimer;
        private float _currentBobFrequency;
        private float _currentBobAmplitude;

        public Transform Transform { get; private set; }
        public Transform FollowTransform { get; private set; }

        public Vector3 PlanarDirection { get; set; }
        public float TargetDistance { get; set; }

        private bool _distanceIsObstructed;
        private float _currentDistance;
        private float _targetVerticalAngle;
        private RaycastHit _obstructionHit;
        private int _obstructionCount;
        private RaycastHit[] _obstructions = new RaycastHit[MaxObstructions];
        private float _obstructionTime;
        private Vector3 _currentFollowPosition;

        private Vector3 _lastFollowPos;
        private float _smoothedSpeed;
        
        private float _targetVerticalOffset = 0f;

        private const int MaxObstructions = 32;

        void OnValidate()
        {
            DefaultDistance = Mathf.Clamp(DefaultDistance, MinDistance, MaxDistance);
            DefaultVerticalAngle = Mathf.Clamp(DefaultVerticalAngle, MinVerticalAngle, MaxVerticalAngle);
        }

        void Awake()
        {
            Transform = this.transform;

            _currentDistance = DefaultDistance;
            TargetDistance = _currentDistance;

            _targetVerticalAngle = 0f;

            PlanarDirection = Vector3.forward;

            // Initialize bobbing parameters
            _currentBobFrequency = bobFrequency;
            _currentBobAmplitude = bobAmplitude;
        }
        
        public void SetCrouchOffset(bool isCrouching)
        {
            if (isCrouching)
            {
                // Set the target offset to the crouching value
                _targetVerticalOffset = CrouchVerticalOffset;
            }
            else
            {
                // Set the target offset back to zero (default)
                _targetVerticalOffset = 0f;
            }
        }

        // Set the transform that the camera will orbit around
        public void SetFollowTransform(Transform t)
        {
            FollowTransform = t;
            PlanarDirection = FollowTransform.forward;
            _currentFollowPosition = FollowTransform.position;
        }

        public void UpdateWithInput(float deltaTime, float zoomInput, Vector3 rotationInput)
        {
            if (FollowTransform)
            {
                if (InvertX)
                {
                    rotationInput.x *= -1f;
                }

                if (InvertY)
                {
                    rotationInput.y *= -1f;
                }

                // Process rotation input
                Quaternion rotationFromInput = Quaternion.Euler(FollowTransform.up * (rotationInput.x * RotationSpeed));
                PlanarDirection = rotationFromInput * PlanarDirection;
                PlanarDirection = Vector3.Cross(FollowTransform.up, Vector3.Cross(PlanarDirection, FollowTransform.up));
                Quaternion planarRot = Quaternion.LookRotation(PlanarDirection, FollowTransform.up);

                _targetVerticalAngle -= (rotationInput.y * RotationSpeed);
                _targetVerticalAngle = Mathf.Clamp(_targetVerticalAngle, MinVerticalAngle, MaxVerticalAngle);
                Quaternion verticalRot = Quaternion.Euler(_targetVerticalAngle, 0, 0);
                Quaternion targetRotation = Quaternion.Slerp(Transform.rotation, planarRot * verticalRot,
                    1f - Mathf.Exp(-RotationSharpness * deltaTime));

                // Apply rotation
                Transform.rotation = targetRotation;

                // Process distance input
                if (_distanceIsObstructed && Mathf.Abs(zoomInput) > 0f)
                {
                    TargetDistance = _currentDistance;
                }

                TargetDistance += zoomInput * DistanceMovementSpeed;
                TargetDistance = Mathf.Clamp(TargetDistance, MinDistance, MaxDistance);

                // Find the smoothed follow position
                _currentFollowPosition = Vector3.Lerp(_currentFollowPosition, FollowTransform.position,
                    1f - Mathf.Exp(-FollowingSharpness * deltaTime));

                // Handle obstructions
                {
                    RaycastHit closestHit = new RaycastHit();
                    closestHit.distance = Mathf.Infinity;
                    _obstructionCount = Physics.SphereCastNonAlloc(_currentFollowPosition, ObstructionCheckRadius,
                        -Transform.forward, _obstructions, TargetDistance, ObstructionLayers,
                        QueryTriggerInteraction.Ignore);
                    for (int i = 0; i < _obstructionCount; i++)
                    {
                        bool isIgnored = false;
                        for (int j = 0; j < IgnoredColliders.Count; j++)
                        {
                            if (IgnoredColliders[j] == _obstructions[i].collider)
                            {
                                isIgnored = true;
                                break;
                            }
                        }

                        for (int j = 0; j < IgnoredColliders.Count; j++)
                        {
                            if (IgnoredColliders[j] == _obstructions[i].collider)
                            {
                                isIgnored = true;
                                break;
                            }
                        }

                        if (!isIgnored && _obstructions[i].distance < closestHit.distance &&
                            _obstructions[i].distance > 0)
                        {
                            closestHit = _obstructions[i];
                        }
                    }

                    // If obstructions detecter
                    if (closestHit.distance < Mathf.Infinity)
                    {
                        _distanceIsObstructed = true;
                        _currentDistance = Mathf.Lerp(_currentDistance, closestHit.distance,
                            1 - Mathf.Exp(-ObstructionSharpness * deltaTime));
                    }
                    // If no obstruction
                    else
                    {
                        _distanceIsObstructed = false;
                        _currentDistance = Mathf.Lerp(_currentDistance, TargetDistance,
                            1 - Mathf.Exp(-DistanceMovementSharpness * deltaTime));
                    }
                }

                // --- 1. Apply Smooth Vertical Offset (Crouching) ---
                // Smoothly move the current framing Y value towards the target offset
                FollowPointFraming.y = Mathf.Lerp(
                    FollowPointFraming.y, 
                    _targetVerticalOffset, 
                    1f - Mathf.Exp(-CrouchTransitionSpeed * deltaTime)
                );

                // Define the **Actual Pivot Point** where the camera should orbit.
                // This combines the character's smoothed position with the vertical framing/crouch offset.
                Vector3 orbitPivotPoint = _currentFollowPosition;
                orbitPivotPoint += FollowTransform.up * FollowPointFraming.y; 
                orbitPivotPoint += FollowTransform.right * FollowPointFraming.x; 
                
                // Find the smoothed camera orbit position.
                // This now pulls the camera back from the correctly offset pivot point.
                Vector3 targetPosition =
                    orbitPivotPoint - ((targetRotation * Vector3.forward) * _currentDistance);

                // Apply position
                Transform.position = targetPosition;

                // --- ENHANCED SMOOTH HEAD BOB & SWAY (No Jiggle) ---
                if (enableBobbing && FollowTransform != null && CharacterController != null)
                {
                    // 1️⃣ Calculate speed and smooth it
                    float speed = ((FollowTransform.position - _lastFollowPos) / deltaTime).magnitude;
                    _lastFollowPos = FollowTransform.position;
                    _smoothedSpeed = Mathf.Lerp(_smoothedSpeed, speed, deltaTime * 8f);

                    // 2️⃣ Determine target bob parameters
                    float targetFrequency = bobFrequency;
                    float targetAmplitude = bobAmplitude;
                    float transitionSmoothing = 8f;

                    if (_smoothedSpeed > 0.1f) // moving
                    {
                        if (CharacterController.IsTired)
                        {
                            // Tired walking: higher bob frequency + higher amplitude for heavy steps
                            targetFrequency = TiredBobFrequency; // fast shallow bob
                            targetAmplitude = TiredBobAmplitude * 1.5f; // boost amplitude for walking
                            transitionSmoothing = 4f; // smoother transition to tired state
                        }
                        else if (CharacterController.IsSprinting)
                        {
                            targetFrequency = bobFrequency * 1.5f;
                            targetAmplitude = bobAmplitude * 1.5f;
                        }

                        _currentBobFrequency = Mathf.Lerp(_currentBobFrequency, targetFrequency,
                            deltaTime * transitionSmoothing);
                        _currentBobAmplitude = Mathf.Lerp(_currentBobAmplitude, targetAmplitude,
                            deltaTime * transitionSmoothing);

                        // Timer advances faster while moving
                        _bobbingTimer += deltaTime * _currentBobFrequency *
                                         Mathf.Clamp01(_smoothedSpeed / bobSpeedReference);
                    }
                    else // standing still
                    {
                        // Slow decay to subtle standing bob
                        float stillAmplitude = 0.008f; // subtle breathing
                        float stillFrequency = 0.7f; // slow breathing pace
                        float stopDecaySpeed = 1.5f; // slower fade to reduce snappy feel

                        _currentBobAmplitude =
                            Mathf.Lerp(_currentBobAmplitude, stillAmplitude, deltaTime * stopDecaySpeed);
                        _currentBobFrequency =
                            Mathf.Lerp(_currentBobFrequency, stillFrequency, deltaTime * stopDecaySpeed);

                        // Slowly advance timer to keep smooth movement
                        _bobbingTimer += deltaTime * _currentBobFrequency * 0.5f;
                    }

                    // 3️⃣ Apply offsets
                    float verticalOffset = Mathf.Sin(_bobbingTimer * Mathf.PI * 2f) * _currentBobAmplitude;
                    float swayPhase = _bobbingTimer * Mathf.PI * 2f * 0.5f + Mathf.PI / 2f;
                    float targetLateral = Mathf.Sin(swayPhase) * (_currentBobAmplitude * lateralMultiplier);
                    _currentLateral = Mathf.Lerp(_currentLateral, targetLateral, deltaTime * swaySmooth);

                    Transform.position += Transform.up * verticalOffset;
                    Transform.position += Transform.right * _currentLateral;
                }
            }
        }
    }
}