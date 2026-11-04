using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Settings
{
    /// <summary>Opções de jogabilidade salvas. O jogo lê GameplaySettings.ShowHints antes de mostrar dicas na tela.</summary>
    public static class GameplaySettings
    {
        private const string HintsKey = "opt_show_hints";
        private static int _hints = -1;

        public static event Action Changed;

        public static bool ShowHints
        {
            get
            {
                if (_hints < 0) _hints = PlayerPrefs.GetInt(HintsKey, 1);
                return _hints == 1;
            }
            set
            {
                if (ShowHints == value) return;

                _hints = value ? 1 : 0;
                PlayerPrefs.SetInt(HintsKey, _hints);
                PlayerPrefs.Save();
                Changed?.Invoke();
            }
        }

        public static void ResetToDefaults() => ShowHints = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _hints = -1;
            Changed = null;
        }
    }

    /// <summary>
    /// Quais partes do tutorial o jogador já aprendeu (pra poder revê-las em Opções > Jogabilidade).
    /// No seu tutorial, quando ensinar algo: TutorialProgress.Unlock("belt");
    /// </summary>
    public static class TutorialProgress
    {
        private const string PrefsKey = "tutorial_unlocked";
        private static HashSet<string> _unlocked;

        public static event Action Changed;

        public static bool IsUnlocked(string id)
        {
            if (string.IsNullOrEmpty(id)) return true;
            Load();
            return _unlocked.Contains(id);
        }

        public static void Unlock(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            Load();
            if (!_unlocked.Add(id)) return;

            Save();
            Changed?.Invoke();
        }

        public static void ResetAll()
        {
            Load();
            _unlocked.Clear();
            Save();
            Changed?.Invoke();
        }

        private static void Load()
        {
            if (_unlocked != null) return;

            _unlocked = new HashSet<string>();
            string csv = PlayerPrefs.GetString(PrefsKey, "");
            if (string.IsNullOrEmpty(csv)) return;

            foreach (string id in csv.Split(','))
                if (!string.IsNullOrWhiteSpace(id)) _unlocked.Add(id.Trim());
        }

        private static void Save()
        {
            PlayerPrefs.SetString(PrefsKey, string.Join(",", _unlocked));
            PlayerPrefs.Save();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _unlocked = null;
            Changed = null;
        }
    }
}
