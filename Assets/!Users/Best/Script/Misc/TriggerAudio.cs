using System;
using UnityEngine;

namespace _Users.Best.Script.Misc
{
    [RequireComponent(typeof(AudioSource))]
    public class TriggerAudio : MonoBehaviour
    {
        [SerializeField] private AudioClip clip;
        private AudioSource source;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
        }

        private void OnTriggerEnter(Collider other)
        {
            source.PlayOneShot(clip);
        }
    }
}