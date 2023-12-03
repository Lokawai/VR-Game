using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    AudioSource audioSource;
    [SerializeField]
    private AudioClip lobbyMusic;
    [SerializeField]
    private AudioClip bgMusic;
    public float fadeDuration = 1.0f;

    private float initialVolume;
    private float fadeTimer;

    // Start is called before the first frame update
    void Start()
    {
        audioSource = GetComponent<AudioSource>();

    }
    public void ModifyVolume(float value)
    {
        audioSource.volume = value;
    }
    public void PlayBgMusic()
    {
        if (audioSource.clip == lobbyMusic)
        {
           StartCoroutine(ResetAndPlay(bgMusic));
        }
        else
        {
            audioSource.clip = bgMusic;
            audioSource.Play();
        }
    }
    public void PlayLobbyMusic()
    {
        if (audioSource.clip == bgMusic)
        {
           StartCoroutine(ResetAndPlay(lobbyMusic));
        } else { 
            audioSource.clip = lobbyMusic;
            audioSource.Play();
        }
    }
    public IEnumerator ResetAndPlay(AudioClip target)
    {
        StopMusic();
        yield return new WaitForSeconds(fadeDuration);
        audioSource.clip = target;
        audioSource.Play();
    }

    public void StopMusic()
    {
        // Start the fade-out process
        fadeTimer = fadeDuration;
        initialVolume = audioSource.volume;
    }

    private void Update()
    {
        // Gradually decrease volume during fade-out
        if (fadeTimer > 0)
        {
            fadeTimer -= Time.deltaTime;

            if (fadeTimer <= 0)
            {
                // Stop the sound when fade-out is complete
                audioSource.volume = initialVolume;
                audioSource.Stop();
            }
            else
            {
                // Calculate the new volume based on the remaining fade duration
                float t = fadeTimer / fadeDuration;
                audioSource.volume = initialVolume * t;
            }
        }
    }
}
