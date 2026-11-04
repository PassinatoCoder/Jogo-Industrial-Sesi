using System;
using UnityEngine;

namespace Core.Settings
{
    /// <summary>
    /// Opções de acessibilidade salvas. O próprio menu de opções já usa:
    /// TextScale e HighContrast. O JOGO precisa ler:
    ///   CameraShake   (0 a 1)  -> multiplique a força de tremor de câmera por esse valor
    ///   ReduceFlashes (bool)   -> desligue ou suavize efeitos de flash/piscada
    ///
    /// "Reduzir movimento" e "Leitura fácil" continuam nos scripts MenuMotion e MenuReadability.
    /// </summary>
    public static class AccessibilitySettings
    {
        private const string Prefix = "opt_acc_";
        private static readonly float[] TextScales = { 1f, 1.2f, 1.4f };

        private static bool _loaded;
        private static int _textSizeIndex;
        private static bool _highContrast;
        private static float _cameraShake = 1f;
        private static bool _reduceFlashes;

        public static event Action Changed;

        public static int TextSizeCount => TextScales.Length;

        /// <summary>0 = Normal, 1 = Grande, 2 = Muito grande.</summary>
        public static int TextSizeIndex
        {
            get { Load(); return _textSizeIndex; }
            set
            {
                Load();
                value = Mathf.Clamp(value, 0, TextScales.Length - 1);
                if (value == _textSizeIndex) return;
                _textSizeIndex = value;
                Save();
            }
        }

        public static float TextScale => TextScales[TextSizeIndex];

        public static bool HighContrast
        {
            get { Load(); return _highContrast; }
            set
            {
                Load();
                if (value == _highContrast) return;
                _highContrast = value;
                Save();
            }
        }

        public static float CameraShake
        {
            get { Load(); return _cameraShake; }
            set
            {
                Load();
                value = Mathf.Clamp01(value);
                if (Mathf.Approximately(value, _cameraShake)) return;
                _cameraShake = value;
                Save();
            }
        }

        public static bool ReduceFlashes
        {
            get { Load(); return _reduceFlashes; }
            set
            {
                Load();
                if (value == _reduceFlashes) return;
                _reduceFlashes = value;
                Save();
            }
        }

        public static void ResetToDefaults()
        {
            Load();
            _textSizeIndex = 0;
            _highContrast = false;
            _cameraShake = 1f;
            _reduceFlashes = false;
            Save();
        }

        private static void Load()
        {
            if (_loaded) return;
            _loaded = true;

            _textSizeIndex = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "text", 0), 0, TextScales.Length - 1);
            _highContrast = PlayerPrefs.GetInt(Prefix + "contrast", 0) == 1;
            _cameraShake = Mathf.Clamp01(PlayerPrefs.GetFloat(Prefix + "shake", 1f));
            _reduceFlashes = PlayerPrefs.GetInt(Prefix + "flashes", 0) == 1;
        }

        private static void Save()
        {
            PlayerPrefs.SetInt(Prefix + "text", _textSizeIndex);
            PlayerPrefs.SetInt(Prefix + "contrast", _highContrast ? 1 : 0);
            PlayerPrefs.SetFloat(Prefix + "shake", _cameraShake);
            PlayerPrefs.SetInt(Prefix + "flashes", _reduceFlashes ? 1 : 0);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _loaded = false;
            Changed = null;
        }
    }
}
