using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class AudioManager : MonoBehaviour
{
    public SoundGroup[] soundGroups;
    public Sound[] sounds;

    void Awake()
    {
        foreach (var sound in sounds)
        {
            sound.audioSource = gameObject.AddComponent<AudioSource>();

            sound.audioSource.clip = sound.clip;
            sound.audioSource.volume = sound.volume;
            sound.audioSource.pitch = sound.pitch;
            sound.audioSource.loop = sound.loop;
        }
    }

    void Start()
    {

    }

    public void RegisterController(Controller controller)
    {
       
    }

    public void DeregisterController(Controller controller)
    {

    }

    private void Play(string name)
    {
        var matchingSounds = sounds.Where(sound => sound.name == name).ToArray();

        if (matchingSounds.Length == 0)
        {
            
            return;
        }

        // Select a random sound from the matching sounds
        Sound soundToPlay = matchingSounds[Random.Range(0, matchingSounds.Length)];

        if (soundToPlay.randomPitch)
        {
            // Randomize the pitch within a specified range
            float randomPitch = Random.Range(0.95f, 1.1f); // Adjust the range as needed
            soundToPlay.audioSource.pitch = randomPitch;
        }

        soundToPlay.audioSource.Play();
    }

    private void OnEnemyHit(Entity entity)
    {
        Play("Hit");
    }
}