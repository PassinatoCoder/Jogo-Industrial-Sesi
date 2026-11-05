using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Core.Minigames.Welding
{
    /// <summary>
    /// Toca os sons do minigame. Clipe vazio = não toca nada (nunca dá erro).
    /// Passa pelo grupo "SFX" do seu AudioMixer (se você arrastar no campo), então o volume das Opções vale.
    /// </summary>
    public sealed class WeldingAudioPlayer
    {
        private sealed class Loop
        {
            public AudioSource Source;
            public float Target;
            public float Current;
        }

        private const int PoolSize = 8;

        private readonly AudioSource[] _pool = new AudioSource[PoolSize];
        private readonly Dictionary<string, Loop> _loops = new Dictionary<string, Loop>();
        private readonly GameObject _host;
        private readonly AudioMixerGroup _group;
        private int _next;

        public float Master { get; set; }

        public WeldingAudioPlayer(GameObject host, AudioMixerGroup group, float master)
        {
            _host = host;
            _group = group;
            Master = master;

            for (int i = 0; i < PoolSize; i++) _pool[i] = CreateSource();
        }

        private AudioSource CreateSource()
        {
            AudioSource source = _host.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.outputAudioMixerGroup = _group;
            return source;
        }

        public void Play(AudioClip clip, float volume = 1f, float pitchVariance = 0.05f)
        {
            if (clip == null) return;

            AudioSource source = _pool[_next];
            _next = (_next + 1) % PoolSize;

            source.pitch = 1f + Random.Range(-pitchVariance, pitchVariance);
            source.volume = Mathf.Clamp01(volume * Master);
            source.clip = clip;
            source.Play();
        }

        public void PlayRandom(AudioClip[] clips, float volume = 1f)
        {
            if (clips == null || clips.Length == 0) return;
            Play(clips[Random.Range(0, clips.Length)], volume);
        }

        /// <summary>Liga/desliga um som em repetição com fade. Volume 0 = vai sumindo e para.</summary>
        public void SetLoop(string key, AudioClip clip, float targetVolume)
        {
            if (clip == null) return;

            Loop loop;
            if (!_loops.TryGetValue(key, out loop))
            {
                loop = new Loop { Source = CreateSource() };
                loop.Source.loop = true;
                loop.Source.clip = clip;
                loop.Source.volume = 0f;
                _loops[key] = loop;
            }

            loop.Target = Mathf.Clamp01(targetVolume);
        }

        public void StopLoop(string key)
        {
            Loop loop;
            if (_loops.TryGetValue(key, out loop)) loop.Target = 0f;
        }

        public void StopAll()
        {
            foreach (KeyValuePair<string, Loop> pair in _loops)
            {
                pair.Value.Target = 0f;
                pair.Value.Current = 0f;
                pair.Value.Source.volume = 0f;
                pair.Value.Source.Stop();
            }

            for (int i = 0; i < PoolSize; i++) _pool[i].Stop();
        }

        public void Tick(float dt)
        {
            foreach (KeyValuePair<string, Loop> pair in _loops)
            {
                Loop loop = pair.Value;
                loop.Current = Mathf.MoveTowards(loop.Current, loop.Target, dt * 2.5f);
                loop.Source.volume = loop.Current * Master;

                if (loop.Current > 0.001f && !loop.Source.isPlaying) loop.Source.Play();
                else if (loop.Current <= 0.001f && loop.Target <= 0.001f && loop.Source.isPlaying) loop.Source.Pause();
            }
        }
    }
}
