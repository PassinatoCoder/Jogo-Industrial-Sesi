using System;
using UnityEngine;
using UnityEngine.Audio;

namespace Core.Audio
{
    /// <summary>
    /// Áudio do menu principal.
    ///
    /// MÚSICA: sorteia faixas da lista, sempre tem algo tocando (a próxima entra em crossfade antes da atual acabar).
    ///         A faixa que acabou de tocar fica com chance MUITO menor de repetir (mas nunca zero),
    ///         e as outras vão "recuperando" a chance a cada sorteio.
    /// SFX:    pressionar tecla, selecionar botão (mouse/setas/controle) e clicar. Usa várias "vozes"
    ///         pra um som não cortar o outro.
    ///
    /// Os AudioSources são criados sozinhos. Só ligue os Mixer Groups (opcional) e arraste os áudios.
    /// </summary>
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

        [Header("Mixer (opcional)")]
        [Tooltip("Grupo do AudioMixer da música (ex.: BGM). Vazio = sem mixer.")]
        [SerializeField] private AudioMixerGroup bgmGroup;
        [Tooltip("Grupo do AudioMixer dos efeitos (ex.: SFX). Vazio = sem mixer.")]
        [SerializeField] private AudioMixerGroup sfxGroup;

        [Header("Música de fundo (arraste as faixas aqui)")]
        [SerializeField] private MusicTrack[] tracks;
        [Tooltip("Segundos de fade-in quando a primeira música começa.")]
        [SerializeField] private float startFadeIn = 2f;
        [Tooltip("Segundos de crossfade entre uma faixa e a próxima.")]
        [SerializeField] private float crossfade = 3f;

        [Header("Sorteio")]
        [Tooltip("Chance da faixa que ACABOU de tocar (1 = normal, 0.15 = 15% da chance das outras).")]
        [Range(0.01f, 1f)]
        [SerializeField] private float recentPenalty = 0.15f;
        [Tooltip("Quanto a chance das outras faixas se recupera a cada sorteio.")]
        [Range(0.05f, 1f)]
        [SerializeField] private float recoveryPerTrack = 0.4f;

        [Header("Efeitos sonoros (SFX)")]
        [SerializeField] private Sfx pressAnyKey = new Sfx();
        [SerializeField] private Sfx buttonSelect = new Sfx();
        [SerializeField] private Sfx buttonClick = new Sfx();
        [Tooltip("Quantos sons podem tocar ao mesmo tempo.")]
        [SerializeField] private int sfxVoices = 4;
        [Tooltip("Tempo mínimo entre dois sons de seleção (evita 'metralhadora').")]
        [SerializeField] private float minSelectInterval = 0.06f;

        /// <summary>Disparado quando uma faixa nova começa. Entrega o nome dela.</summary>
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

        // ---------- Ciclo de vida ----------

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

            // Garante que SEMPRE tem música: entra a próxima antes da atual acabar
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

        // ---------- Música ----------

        /// <summary>Começa a música (chame quando o menu aparece). Não faz nada se já estiver tocando.</summary>
        public void StartBGM()
        {
            if (_musicOn) return;

            _musicOn = true;
            PlayNext(startFadeIn);
        }

        /// <summary>Para a música com fade-out.</summary>
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

        /// <summary>Pula pra próxima faixa (crossfade).</summary>
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

            // Atualiza as chances: a que tocou cai, as outras se recuperam
            for (int i = 0; i < _weights.Length; i++)
                _weights[i] = i == index ? recentPenalty : Mathf.Min(1f, _weights[i] + recoveryPerTrack);

            CurrentTrackName = string.IsNullOrWhiteSpace(track.trackName) ? track.clip.name : track.trackName;
            OnTrackChanged?.Invoke(CurrentTrackName);
        }

        /// <summary>Sorteio ponderado: quanto maior o peso, maior a chance.</summary>
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

        // ---------- SFX ----------

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

        /// <summary>
        /// Silencia o som de seleção por um instante.
        /// Usado quando o PRÓPRIO menu seleciona um botão sozinho (ex.: ao abrir), pra não tocar som sem o jogador mexer.
        /// </summary>
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