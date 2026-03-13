using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioPlayer : MonoBehaviour
{
    private AudioSource _audioSource;

    [Header("Listen on channel:")]
    [SerializeField] private AudioEventChannelSO _playSfxChannel;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        _playSfxChannel.OnEventRaised += PlayAudio;
    }


    private void PlayAudio(AudioGroupSO audioGroupSO)
    {
        _audioSource.PlayOneShot(audioGroupSO.GetClip(), audioGroupSO.Volume);
    }

    private void OnDisable()
    {
        _playSfxChannel.OnEventRaised -= PlayAudio;
    }
}