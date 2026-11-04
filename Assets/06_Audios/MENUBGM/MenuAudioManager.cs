using System;
using UnityEngine;
using UnityEngine.Audio;

namespace Core.Audio
{
    public class MenuAudioManager : MonoBehaviour
    {
        public static MenuAudioManager Instance { get; private set; }

        [Serializable]
        public class MusicTrack
        {
            [Tooltip("Nome que aparece em 'Tocando a faixa'. Vazio = usa o nome do arquivo.")]
            public string trackName;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume = 1f;
        }

        [Serializable]
        public class Sfx
        {
            public AudioClip clip;
            [Range(0f, 1f)] public float volume = 1f;
            [Tooltip("Variação aleatória de tom (deixa o som menos repetitivo).")]
            [Range(0f, 0.2f)] public float pitchVariation = 0.03f;
        }

        [Header("Mixer")]
        [Tooltip("Grupo do AudioMixer da música (ex.: BGM).")]
        [SerializeField] private AudioMixerGroup bgmGroup;
        [Tooltip("Grupo do AudioMixer dos efeitos (ex.: SFX).")]
        [SerializeField] private AudioMixerGroup sfxGroup;

        [Header("Música de fundo (arraste as faixas aqui)")]
        [SerializeField] private MusicTrack[] tracks;
        [SerializeField] private float startFadeIn = 2f;
        [SerializeField] private float crossfade = 3f;

        [Header("Sorteio")]
        [Range(0.01f, 1f)]
        [SerializeField] private float recentPenalty = 0.15f;
        [Range(0.05f, 1f)]
        [SerializeField] private float recoveryPerTrack = 0.4f;

        [Header("Efeitos sonoros (SFX)")]
        [SerializeField] private Sfx pressAnyKey = new Sfx();
        [SerializeField] private Sfx buttonSelect = new Sfx();
        [SerializeField] private Sfx buttonClick = new Sfx();
        [SerializeField] private int sfxVoices = 4;
        [SerializeField] private float minSelectInterval = 0.06f;

        public event Action<string> OnTrackChanged;

        public string CurrentTrackName { get; private set; } = string.Empty;
        public bool IsMusicOn => _musicOn;

        private sealed class Channel
        {
            public AudioSource Source;
            public float Level;
            public float Target;
            public float Rate = 1f;
            public float TrackVolume = 1f;
        }

        private Channel _a, _b, _current;
        private AudioSource[] _voices;
        private int _nextVoice;
        private float[] _weights;
        private bool _musicOn;
        private float _endFade;
        private float _selectMutedUntil;
        private float _lastSelectTime = -10f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            _a = new Channel { Source = CreateSource("BGM_A", bgmGroup) };
            _b = new Channel { Source = CreateSource("BGM_B", bgmGroup) };

            int count = Mathf.Max(1, sfxVoices);
            _voices = new AudioSource[count];
            for (int i = 0; i < count; i++) _voices[i] = CreateSource("SFX_" + i, sfxGroup);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private AudioSource CreateSource(string childName, AudioMixerGroup group)
        {
            GameObject go = new GameObject(childName);
            go.transform.SetParent(transform, false);

            AudioSource source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.outputAudioMixerGroup = group;
            return source;
        }

        private void Update()
        {
            if (_a == null) return;

            float dt = Time.unscaledDeltaTime;
            Tick(_a, dt);
            Tick(_b, dt);

            if (!_musicOn || _current == null) return;

            AudioSource src = _current.Source;
            if (src.clip == null) return;

            bool ended = !src.isPlaying && Application.isFocused;
            bool nearEnd = src.isPlaying && (src.clip.length - src.time) <= _endFade;
            if (ended || nearEnd) PlayNext(_endFade);
        }

        private static void Tick(Channel c, float dt)
        {
            if (c.Source == null) return;

            c.Level = Mathf.MoveTowards(c.Level, c.Target, c.Rate * dt);
            float smooth = c.Level * c.Level * (3f - 2f * c.Level);
            c.Source.volume = smooth * c.TrackVolume;

            if (c.Target <= 0f && c.Level <= 0f && c.Source.isPlaying) c.Source.Stop();
        }

        public void StartBGM()
        {
            if (_musicOn) return;

            _musicOn = true;
            PlayNext(startFadeIn);
        }

        public void StopBGM(float fadeTime = 0.8f)
        {
            _musicOn = false;
            float rate = fadeTime > 0f ? 1f / fadeTime : 1000f;

            foreach (Channel c in new[] { _a, _b })
            {
                if (c == null) continue;
                c.Target = 0f;
                c.Rate = rate;
            }
        }

        public void SkipTrack()
        {
            if (_musicOn) PlayNext(Mathf.Max(0.5f, _endFade));
        }

        private void PlayNext(float fadeTime)
        {
            int index = PickTrack();
            if (index < 0)
            {
                _musicOn = false;
                Debug.LogWarning("[MenuAudioManager] Nenhuma faixa com áudio na lista 'Tracks'.");
                return;
            }

            MusicTrack track = tracks[index];
            _endFade = Mathf.Min(crossfade, track.clip.length * 0.3f);

            Channel incoming = _current == _a ? _b : _a;
            Channel outgoing = _current;
            float rate = fadeTime > 0f ? 1f / fadeTime : 1000f;

            incoming.Source.Stop();
            incoming.Source.clip = track.clip;
            incoming.Source.time = 0f;
            incoming.Source.Play();
            incoming.Level = fadeTime > 0f ? 0f : 1f;
            incoming.Target = 1f;
            incoming.Rate = rate;
            incoming.TrackVolume = track.volume;

            if (outgoing != null)
            {
                outgoing.Target = 0f;
                outgoing.Rate = rate;
            }

            _current = incoming;

            for (int i = 0; i < _weights.Length; i++)
                _weights[i] = i == index ? recentPenalty : Mathf.Min(1f, _weights[i] + recoveryPerTrack);

            CurrentTrackName = string.IsNullOrWhiteSpace(track.trackName) ? track.clip.name : track.trackName;
            OnTrackChanged?.Invoke(CurrentTrackName);
        }

        private int PickTrack()
        {
            if (tracks == null || tracks.Length == 0) return -1;

            if (_weights == null || _weights.Length != tracks.Length)
            {
                _weights = new float[tracks.Length];
                for (int i = 0; i < _weights.Length; i++) _weights[i] = 1f;
            }

            float total = 0f;
            int lastValid = -1;
            for (int i = 0; i < tracks.Length; i++)
            {
                if (tracks[i] == null || tracks[i].clip == null) continue;
                total += _weights[i];
                lastValid = i;
            }

            if (lastValid < 0) return -1;

            float roll = UnityEngine.Random.value * total;
            for (int i = 0; i < tracks.Length; i++)
            {
                if (tracks[i] == null || tracks[i].clip == null) continue;
                roll -= _weights[i];
                if (roll <= 0f) return i;
            }

            return lastValid;
        }

        public void PlaySFX_PressAnyKey() => Play(pressAnyKey);
        public void PlaySFX_Click() => Play(buttonClick);

        public void PlaySFX_Hover()
        {
            float now = Time.unscaledTime;
            if (now < _selectMutedUntil) return;
            if (now - _lastSelectTime < minSelectInterval) return;

            _lastSelectTime = now;
            Play(buttonSelect);
        }

        public void MuteSelectFor(float seconds)
        {
            _selectMutedUntil = Mathf.Max(_selectMutedUntil, Time.unscaledTime + seconds);
        }

        private void Play(Sfx sfx)
        {
            if (sfx == null || sfx.clip == null || _voices == null) return;

            AudioSource voice = _voices[_nextVoice];
            _nextVoice = (_nextVoice + 1) % _voices.Length;

            voice.pitch = 1f + UnityEngine.Random.Range(-sfx.pitchVariation, sfx.pitchVariation);
            voice.PlayOneShot(sfx.clip, sfx.volume);
        }
    }
}