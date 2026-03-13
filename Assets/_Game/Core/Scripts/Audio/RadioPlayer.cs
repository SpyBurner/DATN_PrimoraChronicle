using UnityEngine;

public class RadioPlayer : MonoBehaviour
{
    [SerializeField] private VoidEventChannelSO startSong;
    [SerializeField] private VoidEventChannelSO stopSong;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioGroupSO _audios;
    private bool _isPaused = false;

    private void OnEnable()
    {
        stopSong.OnEventRaised += StopSong;
        startSong.OnEventRaised += StartSong;
    }

    private void OnDisable()
    {
        stopSong.OnEventRaised -= StopSong;
        startSong.OnEventRaised -= StartSong;
    }

    private void Start()
    {
        PlayNextSong();
        _isPaused = false;
    }

    private void StopSong()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            _isPaused = true;
            audioSource.Pause();
        }
    }

    void Update()
    {
        if (!audioSource.isPlaying && !_isPaused)
        {
            PlayNextSong();
        }
    }

    private void PlayNextSong()
    {
        audioSource.clip = _audios.GetClip();
        audioSource.volume = _audios.Volume;
        audioSource.Play();
    }

    private void StartSong()
    {
        if (audioSource != null && !audioSource.isPlaying)
        {
            _isPaused = false;
            audioSource.UnPause();
        }
    }
}
