using System.Collections.Generic;
using Core.Settings;
using Core.UI.Menu;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core.Accessibility
{
    /// <summary>
    /// Faz a acessibilidade valer no JOGO TODO. Cria-se sozinho quando o jogo abre (não precisa pôr na cena).
    ///
    /// Em todo texto TextMeshPro (menus, HUD, diálogos, mundo):
    ///   - tamanho do texto, espaçamento (letras / palavras / linhas) e negrito;
    ///   - fonte para dislexia, quando a "Leitura fácil" está ligada.
    /// E manda o filtro de daltonismo pro shader (ColorBlindFilter).
    ///
    /// Textos que NÃO devem mudar: ponha AccessibilityIgnore no objeto pai (ou use ReadableFontSwitcher).
    /// Fonte para dislexia: ponha o arquivo .ttf em  Assets/Resources/Accessibility/DyslexiaFont
    /// (ou um Font Asset do TextMeshPro com o nome DyslexiaFontAsset).
    /// </summary>
    public sealed class GameAccessibility : MonoBehaviour
    {
        private const float ScanInterval = 1f;
        private const string FontAssetPath = "Accessibility/DyslexiaFontAsset";
        private const string FontPath = "Accessibility/DyslexiaFont";

        public static GameAccessibility Instance { get; private set; }

        // ---------- Estado dos textos ----------

        private sealed class TextState
        {
            public TMP_FontAsset font;
            public bool autoSize;
            public float size;
            public float sizeMin;
            public float sizeMax;
            public float charSpacing;
            public float wordSpacing;
            public float lineSpacing;
            public FontStyles style;

            public static TextState Capture(TMP_Text t)
            {
                return new TextState
                {
                    font = t.font,
                    autoSize = t.enableAutoSizing,
                    size = t.fontSize,
                    sizeMin = t.fontSizeMin,
                    sizeMax = t.fontSizeMax,
                    charSpacing = t.characterSpacing,
                    wordSpacing = t.wordSpacing,
                    lineSpacing = t.lineSpacing,
                    style = t.fontStyle
                };
            }
        }

        private sealed class Entry
        {
            public TextState original; // como o texto era antes da acessibilidade
            public TextState applied;  // como ficou depois (pra perceber se o jogo mexeu depois)
        }

        private struct Wanted
        {
            public float scale;
            public bool dyslexia;
            public float charSpacing;
            public float wordSpacing;
            public float lineSpacing;
            public bool bold;

            public bool IsDefault =>
                Mathf.Approximately(scale, 1f) && !dyslexia && !bold &&
                Mathf.Approximately(charSpacing, 0f) && Mathf.Approximately(wordSpacing, 0f) &&
                Mathf.Approximately(lineSpacing, 0f);
        }

        private readonly Dictionary<TMP_Text, Entry> _entries = new Dictionary<TMP_Text, Entry>();
        private readonly HashSet<TMP_Text> _ignored = new HashSet<TMP_Text>();
        private readonly List<TMP_Text> _dead = new List<TMP_Text>();

        private TMP_FontAsset _dyslexiaFont;
        private bool _fontSearched;
        private bool _dirty = true;
        private float _scanTimer;

        // ---------- Ciclo de vida ----------

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;

            GameObject go = new GameObject("[GameAccessibility]");
            go.AddComponent<GameAccessibility>();
            DontDestroyOnLoad(go);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            ColorBlindFilter.Apply();
        }

        private void OnEnable()
        {
            AccessibilitySettings.Changed += OnSettingsChanged;
            MenuReadability.Changed += OnSettingsChanged;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            AccessibilitySettings.Changed -= OnSettingsChanged;
            MenuReadability.Changed -= OnSettingsChanged;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSettingsChanged()
        {
            ColorBlindFilter.Apply();
            _dirty = true;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ColorBlindFilter.Apply();
            _scanTimer = 0f; // procura os textos novos já no próximo quadro
        }

        private void Update()
        {
            _scanTimer -= Time.unscaledDeltaTime;
            if (!_dirty && _scanTimer > 0f) return;

            bool reapplyAll = _dirty;
            _dirty = false;
            _scanTimer = ScanInterval;

            Refresh(reapplyAll);
        }

        /// <summary>Reaplica tudo agora (use se você criar um monte de textos de uma vez).</summary>
        public void ForceRefresh()
        {
            _dirty = true;
        }

        // ---------- Aplicação nos textos ----------

        private static Wanted ReadSettings()
        {
            bool easyReading = MenuReadability.ReadableTitles;
            int spacing = AccessibilitySettings.SpacingFor(easyReading);

            return new Wanted
            {
                scale = AccessibilitySettings.TextScale,
                dyslexia = easyReading,
                charSpacing = AccessibilitySettings.CharacterSpacing(spacing),
                wordSpacing = AccessibilitySettings.WordSpacing(spacing),
                lineSpacing = AccessibilitySettings.LineSpacing(spacing),
                bold = AccessibilitySettings.BoldText
            };
        }

        private void Refresh(bool reapplyAll)
        {
            PruneDead();
            Wanted wanted = ReadSettings();

            if (wanted.IsDefault)
            {
                // Tudo no padrão: devolve o que foi mexido e deixa de gastar processamento
                if (_entries.Count > 0)
                {
                    foreach (KeyValuePair<TMP_Text, Entry> pair in _entries)
                        if (pair.Key != null) Restore(pair.Key, pair.Value);

                    _entries.Clear();
                }

                return;
            }

            TMP_Text[] all = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                TMP_Text text = all[i];
                if (text == null || ShouldSkip(text)) continue;

                Entry entry;
                if (!_entries.TryGetValue(text, out entry))
                {
                    entry = new Entry { original = TextState.Capture(text) };
                    _entries[text] = entry;
                    Apply(text, entry, wanted);
                }
                else if (reapplyAll)
                {
                    Apply(text, entry, wanted);
                }
            }
        }

        private bool ShouldSkip(TMP_Text text)
        {
            if (_ignored.Contains(text)) return true;

            // ReadableFontSwitcher é procurado pelo NOME: assim o código compila mesmo sem esse script no projeto
            bool skip = text.GetComponentInParent<AccessibilityIgnore>(true) != null ||
                        text.GetComponent("ReadableFontSwitcher") != null;
            if (skip) _ignored.Add(text);
            return skip;
        }

        private void PruneDead()
        {
            _dead.Clear();
            foreach (TMP_Text key in _entries.Keys)
                if (key == null) _dead.Add(key);
            foreach (TMP_Text key in _dead) _entries.Remove(key);

            _dead.Clear();
            foreach (TMP_Text key in _ignored)
                if (key == null) _dead.Add(key);
            foreach (TMP_Text key in _dead) _ignored.Remove(key);
        }

        private void Apply(TMP_Text text, Entry entry, Wanted wanted)
        {
            SyncGameChanges(text, entry);
            TextState o = entry.original;

            // Tamanho: texto com Auto Size muda o tamanho máximo; os outros mudam o tamanho direto
            if (o.autoSize) text.fontSizeMax = o.sizeMax * wanted.scale;
            else text.fontSize = o.size * wanted.scale;

            // Fonte
            TMP_FontAsset dyslexia = wanted.dyslexia ? GetDyslexiaFont() : null;
            TMP_FontAsset target = dyslexia != null ? dyslexia : o.font;
            if (dyslexia != null) AddFallback(dyslexia, o.font);
            if (target != null && text.font != target) text.font = target;

            // Espaçamento e negrito
            text.characterSpacing = o.charSpacing + wanted.charSpacing;
            text.wordSpacing = o.wordSpacing + wanted.wordSpacing;
            text.lineSpacing = o.lineSpacing + wanted.lineSpacing;
            text.fontStyle = wanted.bold ? (o.style | FontStyles.Bold) : o.style;

            entry.applied = TextState.Capture(text);
        }

        private void Restore(TMP_Text text, Entry entry)
        {
            SyncGameChanges(text, entry);
            TextState o = entry.original;

            if (o.autoSize) text.fontSizeMax = o.sizeMax;
            else text.fontSize = o.size;

            if (o.font != null && text.font != o.font) text.font = o.font;

            text.characterSpacing = o.charSpacing;
            text.wordSpacing = o.wordSpacing;
            text.lineSpacing = o.lineSpacing;
            text.fontStyle = o.style;
        }

        /// <summary>Se o seu código mexeu no texto DEPOIS de nós (ex.: trocou o tamanho), o novo valor vira o "original".</summary>
        private static void SyncGameChanges(TMP_Text text, Entry entry)
        {
            if (entry.applied == null) return;

            TextState now = TextState.Capture(text);
            TextState o = entry.original;
            TextState a = entry.applied;

            o.autoSize = now.autoSize;

            if (now.autoSize)
            {
                if (!Mathf.Approximately(now.sizeMax, a.sizeMax)) o.sizeMax = now.sizeMax;
                if (!Mathf.Approximately(now.sizeMin, a.sizeMin)) o.sizeMin = now.sizeMin;
            }
            else if (!Mathf.Approximately(now.size, a.size))
            {
                o.size = now.size;
            }

            if (now.font != a.font) o.font = now.font;
            if (!Mathf.Approximately(now.charSpacing, a.charSpacing)) o.charSpacing = now.charSpacing;
            if (!Mathf.Approximately(now.wordSpacing, a.wordSpacing)) o.wordSpacing = now.wordSpacing;
            if (!Mathf.Approximately(now.lineSpacing, a.lineSpacing)) o.lineSpacing = now.lineSpacing;
            if (now.style != a.style) o.style = now.style;
        }

        // ---------- Fonte para dislexia ----------

        private TMP_FontAsset GetDyslexiaFont()
        {
            if (_fontSearched) return _dyslexiaFont;
            _fontSearched = true;

            _dyslexiaFont = Resources.Load<TMP_FontAsset>(FontAssetPath);

            if (_dyslexiaFont == null)
            {
                Font font = Resources.Load<Font>(FontPath);
                if (font != null) _dyslexiaFont = TMP_FontAsset.CreateFontAsset(font);
            }

            if (_dyslexiaFont == null)
            {
                Debug.LogWarning("[Acessibilidade] Fonte para dislexia não encontrada. " +
                                 "Coloque o arquivo da fonte em Assets/Resources/Accessibility/DyslexiaFont " +
                                 "(a leitura fácil continua aplicando o espaçamento).");
            }

            return _dyslexiaFont;
        }

        /// <summary>A fonte original vira "reserva" da fonte de dislexia (pra símbolos e letras que ela não tem).</summary>
        private static void AddFallback(TMP_FontAsset dyslexia, TMP_FontAsset original)
        {
            if (dyslexia == null || original == null || dyslexia == original) return;

            if (dyslexia.fallbackFontAssetTable == null)
                dyslexia.fallbackFontAssetTable = new List<TMP_FontAsset>();

            if (!dyslexia.fallbackFontAssetTable.Contains(original))
                dyslexia.fallbackFontAssetTable.Add(original);
        }
    }
}