using System;
using UnityEngine;

namespace Core.UI.Menu
{
    /// <summary>
    /// Configuração global do "modo leitura fácil" (ex.: dislexia).
    /// Ligado: os títulos em IMAGEM do menu são trocados por texto legível (TextMeshPro),
    /// e os textos com ReadableFontSwitcher passam a usar a fonte de leitura.
    ///
    /// O painel de Acessibilidade só precisa fazer:
    ///   MenuReadability.ReadableTitles = toggle.isOn;
    /// </summary>
    public static class MenuReadability
    {
        private const string PrefsKey = "opt_readable_titles";
        private static int _cached = -1;

        /// <summary>Disparado quando o modo muda.</summary>
        public static event Action Changed;

        public static bool ReadableTitles
        {
            get
            {
                if (_cached < 0) _cached = PlayerPrefs.GetInt(PrefsKey, 0);
                return _cached == 1;
            }
            set
            {
                if (ReadableTitles == value) return;

                _cached = value ? 1 : 0;
                PlayerPrefs.SetInt(PrefsKey, _cached);
                PlayerPrefs.Save();
                Changed?.Invoke();
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _cached = -1;
            Changed = null;
        }
    }
}