using UnityEngine;
using UnityEngine.Audio;

namespace Core.Settings
{
    public enum AudioChannel
    {
        Master = 0,
        Music = 1,
        Effects = 2
    }

    public static class GameAudioSettings
    {
        private static readonly string[] Keys =
        {
            "Config_VolumeMaster",
            "Config_VolumeBGM",
            "Config_VolumeSFX"
        };

        // Nomes exatos apontando para os Exposed Parameters do seu AudioMixer
        public static string MasterParameter = "MasterVol";
        public static string MusicParameter = "BGMVol";
        public static string EffectsParameter = "SFXVol";

        public static float Get(AudioChannel channel)
        {
            return Mathf.Clamp01(PlayerPrefs.GetFloat(Keys[(int)channel], 1f));
        }

        public static void Set(AudioMixer mixer, AudioChannel channel, float value01)
        {
            value01 = Mathf.Clamp01(value01);
            PlayerPrefs.SetFloat(Keys[(int)channel], value01);
            PlayerPrefs.Save();
            Push(mixer, channel, value01);
        }

        /// <summary>Manda todos os volumes salvos pro mixer. Chame isso no Awake/Start do seu gerenciador de menus.</summary>
        public static void ApplyAll(AudioMixer mixer)
        {
            if (mixer == null)
            {
                Debug.LogWarning("[GameAudioSettings] AudioMixer nulo ao tentar aplicar os volumes!");
                return;
            }

            for (int i = 0; i < Keys.Length; i++)
            {
                Push(mixer, (AudioChannel)i, Get((AudioChannel)i));
            }
        }

        public static void ResetAll(AudioMixer mixer)
        {
            for (int i = 0; i < Keys.Length; i++)
                Set(mixer, (AudioChannel)i, 1f);
        }

        private static void Push(AudioMixer mixer, AudioChannel channel, float value01)
        {
            if (mixer == null) return;

            string paramName = ParameterOf(channel);
            float decibels = value01 > 0.001f ? Mathf.Log10(value01) * 20f : -80f;

            bool success = mixer.SetFloat(paramName, decibels);
            if (!success)
            {
                Debug.LogError($"[GameAudioSettings] Falha ao injetar o volume no parâmetro '{paramName}' do AudioMixer. Verifique se ele está exposto corretamente!");
            }
        }

        private static string ParameterOf(AudioChannel channel)
        {
            switch (channel)
            {
                case AudioChannel.Music: return MusicParameter;
                case AudioChannel.Effects: return EffectsParameter;
                default: return MasterParameter;
            }
        }
    }
}