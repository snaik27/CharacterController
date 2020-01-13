using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [SerializeField]
    public List<AudioClip> musicList;
    [SerializeField]
    private List<AudioClip> SFX;

    private AudioSource audioSource;

}
