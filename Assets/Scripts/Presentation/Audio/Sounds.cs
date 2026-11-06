using UnityEngine;

[System.Serializable]
public class Sound
{
    public string name;
    public AudioClip clip;
    public float volume;
    public float pitch;
    public bool loop;
    public bool randomPitch;
    [HideInInspector]
    public AudioSource audioSource;
}

public class SoundGroup
{
    public string name;
    public Sound[] sounds;
}