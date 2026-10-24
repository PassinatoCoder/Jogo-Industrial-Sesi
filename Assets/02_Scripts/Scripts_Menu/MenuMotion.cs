using UnityEngine;

namespace Core.UI.Menu
{
    /// <summary>
    /// Configuração global de movimento. O painel de Opções só precisa fazer:
    /// MenuMotion.ReduceMotion = toggle.isOn;
    /// </summary>
    public static class MenuMotion
    {
        private const string PrefsKey = "opt_reduce_motion";
        private static int _cached = -1;

        public static bool ReduceMotion
        {
            get
            {
                if (_cached < 0) _cached = PlayerPrefs.GetInt(PrefsKey, 0);
                return _cached == 1;
            }
            set
            {
                _cached = value ? 1 : 0;
                PlayerPrefs.SetInt(PrefsKey, _cached);
                PlayerPrefs.Save();
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache() => _cached = -1;
    }
}