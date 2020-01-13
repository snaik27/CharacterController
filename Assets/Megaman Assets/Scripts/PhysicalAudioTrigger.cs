using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Plays feet-related audio based on triggers
/// TODO:
/// 1. Create inverselerp for PlayOneShot() volume strength that's a function of time-since-last-played
/// </summary>
public class PhysicalAudioTrigger : MonoBehaviour
{
    [SerializeField]
    private AudioClip clip;
    private AudioSource audioSource;

    private void Start()
    {
        Physics.IgnoreLayerCollision(9, 9);
        audioSource = GetComponent<AudioSource>();
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer != 10 && other.gameObject.layer != 9)
            audioSource.PlayOneShot(clip, 0.5f);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.gameObject.layer != 10 && collision.collider.gameObject.layer != 9)
            audioSource.PlayOneShot(clip, 0.5f);
    }
}
