using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AudioClipLibrary", menuName = "Audio/Clip Library")]
public class AudioClipLibrary : ScriptableObject
{
    [System.Serializable]
    public struct NamedClip
    {
        public string key;
        public AudioClip clip;
    }
    public void SetClips(List<NamedClip> clips)
    {
        clipLibrary.AddRange(clips);
    }
    [SerializeField] private List<NamedClip> clipLibrary = new List<NamedClip>();
    public IReadOnlyList<NamedClip> Clips => clipLibrary;
}