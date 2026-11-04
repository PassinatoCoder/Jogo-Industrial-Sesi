using System;
using UnityEngine;

namespace Core.Settings
{
    public enum Language
    {
        Portuguese = 0,
        English = 1
    }

    /// <summary>
    /// Idioma do jogo. Salvo automaticamente.
    /// Qualquer script pode escutar GameLanguage.Changed para trocar seus textos.
    /// GameLanguage.T("texto em português", "text in english") devolve o texto do idioma atual.
    /// </summary>
    public static class GameLanguage
    {
        private const string PrefsKey = "opt_language";
        private static int _cached = -1;

        public static event Action Changed;

        public static Language Current
        {
            get
            {
                if (_cached < 0) _cached = Mathf.Clamp(PlayerPrefs.GetInt(PrefsKey, (int)Language.Portuguese), 0, 1);
                return (Language)_cached;
            }
            set
            {
                if (Current == value) return;

                _cached = (int)value;
                PlayerPrefs.SetInt(PrefsKey, _cached);
                PlayerPrefs.Save();
                Changed?.Invoke();
            }
        }

        public static string T(string portuguese, string english)
        {
            return Current == Language.English ? english : portuguese;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _cached = -1;
            Changed = null;
        }
    }
}
