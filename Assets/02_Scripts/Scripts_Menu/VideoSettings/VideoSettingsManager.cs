using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Settings
{
    [Serializable]
    public struct VideoSettingsData : IEquatable<VideoSettingsData>
    {
        public int width;
        public int height;
        public int displayMode;
        public bool vSync;
        public int fpsLimit;
        public int qualityIndex;
        public bool showFps;

        public bool Equals(VideoSettingsData other)
        {
            return width == other.width && height == other.height &&
                   displayMode == other.displayMode && vSync == other.vSync &&
                   fpsLimit == other.fpsLimit && qualityIndex == other.qualityIndex &&
                   showFps == other.showFps;
        }
    }

    public sealed class VideoSettingsManager : MonoBehaviour
    {
        private const string PrefsKey = "video_settings_final";

        public static readonly FullScreenMode[] DisplayModes = {
            FullScreenMode.FullScreenWindow,  // 0: Sem bordas
            FullScreenMode.ExclusiveFullScreen, // 1: Exclusiva
            FullScreenMode.Windowed             // 2: Janela
        };
        public static readonly int[] FpsLimits = { 30, 60, 120, -1 }; // -1 = Ilimitado

        // [CORREÇÃO TRAVAMENTO] A tela cheia exclusiva troca o modo do monitor no driver e travou o PC em teste.
        // Deixe false: o modo 1 vira "Sem bordas" (que não troca o modo do monitor). Só ligue se testar bem.
        public const bool AllowExclusiveFullScreen = false;

        public static event Action SettingsApplied;
        public static VideoSettingsManager Instance { get; private set; }

        public VideoSettingsData Current => _current;
        public IReadOnlyList<Vector2Int> Resolutions => _resolutions;

        private readonly List<Vector2Int> _resolutions = new List<Vector2Int>();
        private VideoSettingsData _current;

        // [CORREÇÃO TRAVAMENTO] Lembra o que já foi mandado pra tela, pra não refazer a troca à toa.
        private int _appliedWidth = -1;
        private int _appliedHeight = -1;
        private FullScreenMode _appliedMode = FullScreenMode.Windowed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("[VideoSettingsManager]");
            go.AddComponent<VideoSettingsManager>();
            DontDestroyOnLoad(go);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            BuildResolutionList();
            _current = Load();
            ApplyToEngine(_current);
        }

        // Aplica TUDO de uma vez só na Unity
        public void ApplyToEngine(VideoSettingsData data)
        {
            _current = data;

            // 1. Tela (só mexe se algo mudou: repetir SetResolution à toa travava o driver)
            FullScreenMode mode = DisplayModes[Mathf.Clamp(data.displayMode, 0, DisplayModes.Length - 1)];
            if (!AllowExclusiveFullScreen && mode == FullScreenMode.ExclusiveFullScreen)
                mode = FullScreenMode.FullScreenWindow;

            if (data.width != _appliedWidth || data.height != _appliedHeight || mode != _appliedMode)
            {
                _appliedWidth = data.width;
                _appliedHeight = data.height;
                _appliedMode = mode;
                Screen.SetResolution(data.width, data.height, mode);
            }

            // 2. Qualidade (vem ANTES do VSync: cada nível de qualidade traz o seu próprio VSync e sobrescreveria o escolhido)
            int lastQuality = QualitySettings.names.Length - 1;
            if (lastQuality >= 0)
            {
                int quality = Mathf.Clamp(data.qualityIndex, 0, lastQuality);
                if (QualitySettings.GetQualityLevel() != quality) QualitySettings.SetQualityLevel(quality, true);
            }

            // 3. Desempenho (Forçando a hierarquia da Unity)
            QualitySettings.vSyncCount = data.vSync ? 1 : 0;
            Application.targetFrameRate = data.vSync ? -1 : data.fpsLimit;

            SettingsApplied?.Invoke();
        }

        public void Save()
        {
            PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(_current));
            PlayerPrefs.Save();
        }

        public VideoSettingsData GetDefaultSettings()
        {
            Resolution native = Screen.currentResolution;
            return new VideoSettingsData
            {
                width = native.width,
                height = native.height,
                displayMode = 0,
                vSync = true,
                fpsLimit = -1,
                qualityIndex = 2,
                showFps = false
            };
        }

        private VideoSettingsData Load()
        {
            string json = PlayerPrefs.GetString(PrefsKey, "");
            if (!string.IsNullOrEmpty(json))
            {
                try { return JsonUtility.FromJson<VideoSettingsData>(json); }
                catch { return GetDefaultSettings(); }
            }
            return GetDefaultSettings();
        }

        private void BuildResolutionList()
        {
            _resolutions.Clear();
            var seen = new HashSet<Vector2Int>();

            foreach (Resolution r in Screen.resolutions)
            {
                if (r.width < 800 || r.height < 600) continue;
                var size = new Vector2Int(r.width, r.height);
                if (seen.Add(size)) _resolutions.Add(size);
            }

            _resolutions.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
        }

        public int GetResolutionIndex(int w, int h)
        {
            for (int i = 0; i < _resolutions.Count; i++)
                if (_resolutions[i].x == w && _resolutions[i].y == h) return i;
            return _resolutions.Count - 1;
        }
    }
}