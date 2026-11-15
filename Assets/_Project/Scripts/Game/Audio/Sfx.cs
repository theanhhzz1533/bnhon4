using System.Collections.Generic;
using UnityEngine;

namespace Bnhon.Game.Audio
{
    /// <summary>
    /// Plays sound effects by id. For each id it uses the audio file
    /// <c>Resources/Audio/&lt;id&gt;</c> if one exists, otherwise a synthesized placeholder
    /// from <see cref="SoundSynth"/>. Sounds in the world get quieter with distance from the
    /// listener and pan left or right, which is how the player locates zombies in the fog.
    /// </summary>
    public sealed class Sfx : MonoBehaviour
    {
        private const string Folder = "Audio/";
        private const int Voices = 16;

        [SerializeField] private float masterVolume = 0.6f;
        [SerializeField] private float panWidth = 9f;

        private readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();
        private AudioSource[] _sources;
        private int _next;

        public static Sfx Instance { get; private set; }

        /// <summary>Whose ears distance and panning are measured from. Usually the player.</summary>
        public Transform Listener { get; set; }

        public static Sfx Ensure()
        {
            if (Instance != null) return Instance;
            return new GameObject("Sfx").AddComponent<Sfx>();
        }

        private void Awake()
        {
            Instance = this;
            _sources = new AudioSource[Voices];
            for (int i = 0; i < Voices; i++)
            {
                AudioSource source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                _sources[i] = source;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Plays a sound that happens at a place in the world. Silent beyond <paramref name="maxDistance"/>.</summary>
        public static void Play(string id, Vector2 position, float volume = 1f, float maxDistance = 18f)
        {
            Sfx sfx = Instance;
            if (sfx == null) return;

            float pan = 0f;
            if (sfx.Listener != null)
            {
                Vector2 offset = position - (Vector2)sfx.Listener.position;
                float distance = offset.magnitude;
                if (distance >= maxDistance) return;

                float falloff = 1f - distance / maxDistance;
                volume *= falloff * falloff;
                pan = Mathf.Clamp(offset.x / sfx.panWidth, -0.85f, 0.85f);
            }
            sfx.Emit(id, volume, pan);
        }

        /// <summary>Plays a sound at full volume in both ears: the player's own actions and interface feedback.</summary>
        public static void PlayUi(string id, float volume = 1f)
        {
            if (Instance != null) Instance.Emit(id, volume, 0f);
        }

        private void Emit(string id, float volume, float pan)
        {
            AudioClip clip = GetClip(id);
            if (clip == null || volume <= 0.001f) return;

            AudioSource source = NextVoice();
            source.clip = clip;
            source.volume = Mathf.Clamp(volume * masterVolume, 0f, 1f);
            source.panStereo = pan;
            // A little pitch variation keeps repeated sounds (steps, shots) from sounding mechanical.
            source.pitch = Random.Range(0.94f, 1.06f);
            source.Play();
        }

        private AudioSource NextVoice()
        {
            for (int i = 0; i < Voices; i++)
            {
                AudioSource candidate = _sources[(_next + i) % Voices];
                if (!candidate.isPlaying)
                {
                    _next = (_next + i + 1) % Voices;
                    return candidate;
                }
            }
            AudioSource oldest = _sources[_next];
            _next = (_next + 1) % Voices;
            return oldest;
        }

        private AudioClip GetClip(string id)
        {
            if (_clips.TryGetValue(id, out AudioClip cached) && cached != null) return cached;

            AudioClip clip = Resources.Load<AudioClip>(Folder + id);
            if (clip == null)
            {
                float[] samples = SoundSynth.Generate(id);
                if (samples != null && samples.Length > 0)
                {
                    clip = AudioClip.Create(id, samples.Length, 1, SoundSynth.SampleRate, false);
                    clip.SetData(samples, 0);
                }
            }

            _clips[id] = clip;
            return clip;
        }
    }
}
