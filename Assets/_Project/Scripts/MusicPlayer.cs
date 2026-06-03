using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MusicPlayer : MonoBehaviour
{
    private static MusicPlayer instance;

    private AudioSource audioSource;

    public static void Play(AudioClip clip, float volume)
    {
        if (clip == null)
        {
            return;
        }

        MusicPlayer player = GetOrCreateInstance();
        player.PlayInternal(clip, volume);
    }

    private static MusicPlayer GetOrCreateInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        GameObject musicPlayerObject = new GameObject("MusicPlayer");
        instance = musicPlayerObject.AddComponent<MusicPlayer>();
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = true;

        if (audioSource.clip != null && !audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    private void PlayInternal(AudioClip clip, float volume)
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = volume;

        if (audioSource.clip != clip)
        {
            audioSource.clip = clip;
            audioSource.Play();
            return;
        }

        if (!audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }
}
