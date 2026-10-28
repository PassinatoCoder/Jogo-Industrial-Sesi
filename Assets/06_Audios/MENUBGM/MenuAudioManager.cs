using System;
using UnityEngine;

namespace Core.Audio
{
    public class MenuAudioManager : MonoBehaviour
    {
        public static MenuAudioManager Instance { get; private set; }

        [Serializable]
        public struct MusicTrack
        {
            public string trackName;
            public AudioClip clip;
        }

        [Header("Música de Fundo (BGM)")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private MusicTrack[] tracks;

        [Header("Efeitos Sonoros (SFX)")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioClip sfxPressAnyKey;
        [SerializeField] private AudioClip sfxButtonHover;
        [SerializeField] private AudioClip sfxButtonClick;

        /// <summary>Evento disparado quando a faixa de música muda. Entrega o nome da música.</summary>
        public event Action<string> OnTrackChanged;

        private int _lastTrackIndex = -1;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // Opcional: sobrevive caso mude de cena
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            // Checa se a música acabou para tocar a próxima da fila automaticamente
            if (bgmSource != null && !bgmSource.isPlaying && bgmSource.clip != null)
            {
                PlayRandomTrack();
            }
        }

        public void StartBGM()
        {
            if (bgmSource != null && !bgmSource.isPlaying) PlayRandomTrack();
        }

        private void PlayRandomTrack()
        {
            if (tracks == null || tracks.Length == 0) return;

            int nextIndex;
            if (tracks.Length == 1)
            {
                nextIndex = 0;
            }
            else
            {
                // Tenta sortear uma música diferente da última. 
                // Dá até 3 tentativas, o que reduz drasticamente a chance de repetição sem proibir 100%.
                nextIndex = UnityEngine.Random.Range(0, tracks.Length);
                int attempts = 0;
                while (nextIndex == _lastTrackIndex && attempts < 3)
                {
                    nextIndex = UnityEngine.Random.Range(0, tracks.Length);
                    attempts++;
                }
            }

            _lastTrackIndex = nextIndex;
            bgmSource.clip = tracks[nextIndex].clip;
            bgmSource.Play();

            // Avisa o widget de UI que a música trocou
            OnTrackChanged?.Invoke(tracks[nextIndex].trackName);
        }

        // --- CHAMADAS DE SFX ---
        public void PlaySFX_PressAnyKey() { if (sfxPressAnyKey != null) sfxSource.PlayOneShot(sfxPressAnyKey); }
        public void PlaySFX_Hover() { if (sfxButtonHover != null) sfxSource.PlayOneShot(sfxButtonHover); }
        public void PlaySFX_Click() { if (sfxButtonClick != null) sfxSource.PlayOneShot(sfxButtonClick); }
    }
}