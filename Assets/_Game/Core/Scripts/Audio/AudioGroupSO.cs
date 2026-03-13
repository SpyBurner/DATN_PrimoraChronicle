using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AudioGroupSO", menuName = "Audio/AudioGroupSO")]
public class AudioGroupSO : ScriptableObject
{
    [SerializeField] List<AudioClip> _audioClips;
    [SerializeField] private AudioType _audioType;

    [Range(0f, 1f)] public float Volume = 1;

    private int idx = -1; // Used for both SEQUENTIAL and RANDOM_NO_REPEAT

    public AudioClip GetClip()
    {
        if (_audioClips == null || _audioClips.Count == 0) return null;

        return _audioType switch
        {
            AudioType.SINGLE => _audioClips[0],
            AudioType.RAMDOM => _audioClips[Random.Range(0, _audioClips.Count)],
            AudioType.SEQUENTIAL => _audioClips[(idx >= (_audioClips.Count - 1)) ? idx = 0 : ++idx],
            AudioType.RANDOM_NO_REPEAT => GetRandomNoRepeatClip(),
            _ => null,
        };
    }

    private AudioClip GetRandomNoRepeatClip()
    {
        if (_audioClips.Count == 1) return _audioClips[0]; // Only one clip, no choice but to repeat

        int newIdx;
        do
        {
            newIdx = Random.Range(0, _audioClips.Count);
        } while (newIdx == idx && _audioClips.Count > 1); // Keep trying until a different index is found

        idx = newIdx; // Update idx to the new index
        return _audioClips[newIdx];
    }
}

public enum AudioType
{
    SINGLE,
    RAMDOM,
    SEQUENTIAL,
    RANDOM_NO_REPEAT
}