using System;
using System.Collections.Generic;
using Core.Accessibility;
using Core.Settings;
using Core.UI.Options;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Core.UI.Pause
{
    /// <summary>
    /// MENU DE PAUSA estilo Persona. Um objeto vazio na cena com ESTE script e pronto: ele monta tudo sozinho.
    /// ESC abre/fecha. W/S ou setas escolhem, ENTER/ESPAÇO confirma, mouse também funciona.
    /// Botões: Continuar, Opções (abre o seu OptionsMenu), Menu Principal, Sair do Jogo.
    /// Para o jogo com Time.timeScale = 0 (o minigame de solda e o resto respeitam isso).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PersonaPauseMenu : MonoBehaviour
    {
        private enum State { Closed, Main, Confirm, Options }

        private sealed class Row
        {
            public RectTransform Rt;
            public Image Bg;
            public Image Stripe;
            public TextMeshProUGUI Text;
            public Action Action;
            public float Offset;
            public float Scale = 1f;
        }

        [Header("Geral")]
        [SerializeField] private KeyCode pauseKey = KeyCode.Escape;
        [Tooltip("Desmarque pra impedir a pausa (ex.: durante uma cutscene). Também dá pra mudar por código: pauseMenu.CanPause = false.")]
        [SerializeField] private bool canPause = true;
        [Tooltip("Pausa também o som do jogo (música e efeitos). Os sons do menu continuam tocando.")]
        [SerializeField] private bool pauseGameAudio = true;
        [Tooltip("Nome EXATO da cena do menu principal (precisa estar no Build Settings).")]
        [SerializeField] private string mainMenuScene = "MainMenu";
        [SerializeField] private int sortingOrder = 900;
        [SerializeField] private TMP_FontAsset font;

        [Header("Cores (uma por botão, estilo Persona)")]
        [SerializeField]
        private Color[] accents =
        {
            new Color(0.92f, 0.08f, 0.16f), new Color(1f, 0.84f, 0.08f),
            new Color(0.08f, 0.8f, 0.95f), new Color(0.95f, 0.2f, 0.62f)
        };

        [Header("Sons (opcionais)")]
        [SerializeField] private AudioClip openSound;
        [SerializeField] private AudioClip closeSound;
        [SerializeField] private AudioClip moveSound;
        [SerializeField] private AudioClip confirmSound;
        [SerializeField] private UnityEngine.Audio.AudioMixerGroup sfxGroup;
        [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1f;

        [Header("Eventos")]
        public UnityEvent onPaused;
        public UnityEvent onResumed;

        public static bool IsPaused { get; private set; }
        public bool CanPause { get { return canPause; } set { canPause = value; } }

        private Canvas _canvas;
        private CanvasGroup _group;
        private RectTransform _panel;
        private Image _dim;
        private Image _shard;
        private TextMeshProUGUI _title;
        private TextMeshProUGUI _titleShadow;
        private TextMeshProUGUI _hint;
        private RectTransform _list;
        private readonly List<Row> _rows = new List<Row>();
        private AudioSource _audio;
        private OptionsMenu _options;

        private State _state = State.Closed;
        private int _selected;
        private float _intro;
        private float _closing = -1f;
        private float _prevTimeScale = 1f;
        private bool _prevCursorVisible;
        private CursorLockMode _prevLock;
        private Vector3 _lastMouse;
        private Color _accent;

        private void Awake()
        {
            Build();
            _canvas.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            if (IsPaused) ForceResume();
        }

        // ---------- Montagem ----------

        private void Build()
        {
            GameObject canvasObject = new GameObject("[PersonaPauseMenuCanvas]", typeof(RectTransform));
            canvasObject.transform.SetParent(transform, false);

            _canvas = canvasObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = sortingOrder;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<AccessibilityIgnore>();
            _group = canvasObject.AddComponent<CanvasGroup>();

            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.ignoreListenerPause = true;
            _audio.outputAudioMixerGroup = sfxGroup;

            RectTransform root = (RectTransform)canvasObject.transform;

            _dim = MakeImage(root, "Dim", new Color(0f, 0f, 0f, 0f));
            Fill(_dim.rectTransform);

            _panel = MakeRect("Panel", root);
            Fill(_panel);

            // Blocos inclinados atrás (preto + cor)
            Image black = MakeImage(_panel, "ShardBlack", new Color(0.02f, 0.02f, 0.03f, 0.96f));
            Place(black.rectTransform, new Vector2(0f, 0.5f), new Vector2(-260f, 0f), new Vector2(1500f, 2400f), new Vector2(0f, 0.5f));
            black.rectTransform.localEulerAngles = new Vector3(0f, 0f, -12f);

            _shard = MakeImage(_panel, "ShardAccent", Color.red);
            Place(_shard.rectTransform, new Vector2(0f, 0.5f), new Vector2(-330f, 0f), new Vector2(1500f, 2400f), new Vector2(0f, 0.5f));
            _shard.rectTransform.localEulerAngles = new Vector3(0f, 0f, -12f);
            _shard.rectTransform.SetSiblingIndex(black.rectTransform.GetSiblingIndex());

            Image white = MakeImage(_panel, "Slash", new Color(1f, 1f, 1f, 0.9f));
            Place(white.rectTransform, new Vector2(0f, 0.5f), new Vector2(1180f, 0f), new Vector2(14f, 2600f), new Vector2(0.5f, 0.5f));
            white.rectTransform.localEulerAngles = new Vector3(0f, 0f, -12f);

            // Título gigante (com sombra colorida)
            _titleShadow = MakeText(_panel, "TitleShadow", "", 210f, Color.red, TextAlignmentOptions.Left);
            Place(_titleShadow.rectTransform, new Vector2(0f, 0.5f), new Vector2(290f, 330f), new Vector2(1000f, 260f), new Vector2(0f, 0.5f));
            _titleShadow.rectTransform.localEulerAngles = new Vector3(0f, 0f, 5f);

            _title = MakeText(_panel, "Title", "", 210f, Color.white, TextAlignmentOptions.Left);
            Place(_title.rectTransform, new Vector2(0f, 0.5f), new Vector2(278f, 342f), new Vector2(1000f, 260f), new Vector2(0f, 0.5f));
            _title.rectTransform.localEulerAngles = new Vector3(0f, 0f, 5f);

            _list = MakeRect("List", _panel);
            Fill(_list);

            _hint = MakeText(_panel, "Hint", "", 30f, new Color(1f, 1f, 1f, 0.85f), TextAlignmentOptions.BottomLeft);
            _hint.fontStyle = FontStyles.Normal;
            Place(_hint.rectTransform, new Vector2(0f, 0f), new Vector2(90f, 50f), new Vector2(1300f, 60f), new Vector2(0f, 0f));
        }

        // ---------- Abrir e fechar ----------

        public void Pause()
        {
            if (IsPaused || !canPause) return;

            IsPaused = true;
            _prevTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            _prevCursorVisible = Cursor.visible;
            _prevLock = Cursor.lockState;

            Time.timeScale = 0f;
            if (pauseGameAudio) AudioListener.pause = true;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            _canvas.gameObject.SetActive(true);
            _group.alpha = 1f;
            _closing = -1f;
            _intro = 0f;
            _lastMouse = Input.mousePosition;
            _accent = accents.Length > 0 ? accents[0] : Color.red;
            ShowMain();
            Play(openSound);
            if (onPaused != null) onPaused.Invoke();
        }

        public void Resume()
        {
            if (!IsPaused || _closing >= 0f) return;
            Play(closeSound);
            _closing = 0f;
        }

        private void ForceResume()
        {
            IsPaused = false;
            _state = State.Closed;
            Time.timeScale = _prevTimeScale;
            AudioListener.pause = false;
            Cursor.visible = _prevCursorVisible;
            Cursor.lockState = _prevLock;
            if (_canvas != null) _canvas.gameObject.SetActive(false);
            if (onResumed != null) onResumed.Invoke();
        }

        // ---------- Telas (principal / confirmação) ----------

        private void ShowMain()
        {
            _state = State.Main;
            SetTitle(GameLanguage.T("PAUSA", "PAUSE"));
            SetItems(new[]
            {
                new KeyValuePair<string, Action>(GameLanguage.T("CONTINUAR", "RESUME"), Resume),
                new KeyValuePair<string, Action>(GameLanguage.T("OPÇÕES", "OPTIONS"), OpenOptions),
                new KeyValuePair<string, Action>(GameLanguage.T("MENU PRINCIPAL", "MAIN MENU"),
                    () => ShowConfirm(GameLanguage.T("VOLTAR AO MENU?", "BACK TO MENU?"), GoToMainMenu)),
                new KeyValuePair<string, Action>(GameLanguage.T("SAIR DO JOGO", "QUIT GAME"),
                    () => ShowConfirm(GameLanguage.T("SAIR MESMO?", "QUIT?"), QuitGame))
            });
            _hint.text = GameLanguage.T("W/S ou SETAS: escolher    ENTER: confirmar    ESC: continuar",
                "W/S or ARROWS: choose    ENTER: confirm    ESC: resume");
        }

        private void ShowConfirm(string question, Action yes)
        {
            _state = State.Confirm;
            SetTitle(question);
            SetItems(new[]
            {
                new KeyValuePair<string, Action>(GameLanguage.T("NÃO", "NO"), ShowMain),
                new KeyValuePair<string, Action>(GameLanguage.T("SIM", "YES"), yes)
            });
            _hint.text = GameLanguage.T("Progresso não salvo será perdido.    ESC: voltar", "Unsaved progress will be lost.    ESC: back");
        }

        private void SetTitle(string text)
        {
            _title.text = text;
            _titleShadow.text = text;
            _title.fontSize = text.Length > 8 ? 120f : 210f;
            _titleShadow.fontSize = _title.fontSize;
        }

        private void SetItems(KeyValuePair<string, Action>[] items)
        {
            for (int i = 0; i < _rows.Count; i++) Destroy(_rows[i].Rt.gameObject);
            _rows.Clear();
            _selected = 0;

            for (int i = 0; i < items.Length; i++)
            {
                Row row = new Row { Action = items[i].Value };

                row.Rt = MakeRect("Row" + i, _list);
                Place(row.Rt, new Vector2(0f, 0.5f), new Vector2(260f, 120f - i * 132f), new Vector2(820f, 112f), new Vector2(0f, 0.5f));
                row.Rt.localEulerAngles = new Vector3(0f, 0f, -3f);

                row.Bg = MakeImage(row.Rt, "Bg", new Color(0.04f, 0.04f, 0.05f, 0.92f));
                Fill(row.Bg.rectTransform);

                row.Stripe = MakeImage(row.Rt, "Stripe", Color.white);
                Place(row.Stripe.rectTransform, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(18f, 112f), new Vector2(0f, 0.5f));

                row.Text = MakeText(row.Rt, "Text", items[i].Key, 74f, Color.white, TextAlignmentOptions.Left);
                Fill(row.Text.rectTransform, 56f, 0f, 20f, 0f);

                _rows.Add(row);
            }
        }

        // ---------- Ações ----------

        // Estado que o OptionsMenu tinha antes (pra devolver igual ao fechar)
        private CanvasGroup _optGroup;
        private float _optAlpha;
        private bool _optInteractable;
        private bool _optBlocks;
        private readonly List<GameObject> _activatedParents = new List<GameObject>();

        private void OpenOptions()
        {
            if (_options == null) _options = FindFirstObjectByType<OptionsMenu>(FindObjectsInactive.Include);

            if (_options == null)
            {
                _hint.text = GameLanguage.T("Menu de opções não encontrado na cena.", "Options menu not found in the scene.");
                return;
            }

            _state = State.Options;
            EnsureOptionsCanvas();

            // O OptionsMenu nasce com alpha 0 (quem mostra é o menu principal). Aqui a gente mostra.
            _optGroup = _options.GetComponent<CanvasGroup>();
            if (_optGroup != null)
            {
                _optAlpha = _optGroup.alpha;
                _optInteractable = _optGroup.interactable;
                _optBlocks = _optGroup.blocksRaycasts;
                _optGroup.alpha = 1f;
                _optGroup.interactable = true;
                _optGroup.blocksRaycasts = true;
            }

            // Liga os "pais" desligados (Canvas etc.) pra ele poder aparecer
            _activatedParents.Clear();
            for (Transform t = _options.transform.parent; t != null; t = t.parent)
            {
                if (t.gameObject.activeSelf) continue;
                t.gameObject.SetActive(true);
                _activatedParents.Add(t.gameObject);
            }

            // Esconde o menu de pausa inteiro (senão ele fica por cima e só escurece a tela)
            _canvas.gameObject.SetActive(false);

            _options.CloseRequested -= OnOptionsClosed;
            _options.CloseRequested += OnOptionsClosed;
            _options.Open();
        }

        // Se o OptionsMenu está solto na cena (sem Canvas), cria um Canvas pra ele e coloca dentro.
        private void EnsureOptionsCanvas()
        {
            if (_options.GetComponentInParent<Canvas>(true) != null) return;

            GameObject host = new GameObject("[PauseOptionsCanvas]", typeof(RectTransform));
            host.transform.SetParent(transform, false);

            Canvas canvas = host.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder + 50;

            CanvasScaler scaler = host.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            host.AddComponent<GraphicRaycaster>();

            RectTransform rt = (RectTransform)_options.transform;
            rt.SetParent(host.transform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        private void OnOptionsClosed()
        {
            _options.CloseRequested -= OnOptionsClosed;
            _options.Close();

            if (_optGroup != null)
            {
                _optGroup.alpha = _optAlpha;
                _optGroup.interactable = _optInteractable;
                _optGroup.blocksRaycasts = _optBlocks;
            }

            _options.gameObject.SetActive(false);
            for (int i = 0; i < _activatedParents.Count; i++)
                if (_activatedParents[i] != null) _activatedParents[i].SetActive(false);
            _activatedParents.Clear();

            _canvas.gameObject.SetActive(true);
            _group.alpha = 1f;
            _lastMouse = Input.mousePosition;
            ShowMain();
            _intro = 0f;
        }

        private void GoToMainMenu()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            IsPaused = false;
            _state = State.Closed;
            Cursor.visible = true;
            SceneManager.LoadScene(mainMenuScene);
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ---------- Atualização ----------

        private void Update()
        {
            if (!IsPaused)
            {
                if (canPause && Input.GetKeyDown(pauseKey)) Pause();
                return;
            }

            float dt = Time.unscaledDeltaTime;

            if (_closing >= 0f)
            {
                _closing += dt / 0.18f;
                _group.alpha = Mathf.Clamp01(1f - _closing);
                if (_closing >= 1f) ForceResume();
                return;
            }

            if (_state == State.Options) return;

            _intro = Mathf.Min(1f, _intro + dt / 0.35f);
            HandleInput();
            Animate(dt);
        }

        private void HandleInput()
        {
            if (Input.GetKeyDown(pauseKey))
            {
                if (_state == State.Confirm) { Play(closeSound); ShowMain(); }
                else Resume();
                return;
            }

            int move = 0;
            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) move = 1;
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) move = -1;
            if (move != 0) Select((_selected + move + _rows.Count) % _rows.Count);

            // Mouse: passar por cima seleciona, clicar confirma
            Vector3 mouse = Input.mousePosition;
            bool moved = (mouse - _lastMouse).sqrMagnitude > 1f;
            _lastMouse = mouse;
            int hover = -1;
            for (int i = 0; i < _rows.Count; i++)
                if (RectTransformUtility.RectangleContainsScreenPoint(_rows[i].Rt, mouse, null)) hover = i;

            if (moved && hover >= 0 && hover != _selected) Select(hover);

            bool click = Input.GetMouseButtonDown(0) && hover >= 0;
            if (click) _selected = hover;

            if (click || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
            {
                Play(confirmSound);
                _rows[_selected].Action();
            }
        }

        private void Select(int index)
        {
            if (index == _selected) return;
            _selected = index;
            Play(moveSound);
        }

        private void Animate(float dt)
        {
            float ease = 1f - Mathf.Pow(1f - _intro, 3f);
            _panel.anchoredPosition = new Vector2(Mathf.Lerp(-1300f, 0f, ease), 0f);
            _dim.color = new Color(0f, 0f, 0f, 0.72f * ease);

            Color target = accents.Length > 0 ? accents[_selected % accents.Length] : Color.red;
            _accent = Color.Lerp(_accent, target, 1f - Mathf.Exp(-14f * dt));
            _shard.color = _accent;
            _titleShadow.color = _accent;

            for (int i = 0; i < _rows.Count; i++)
            {
                Row row = _rows[i];
                bool on = i == _selected;
                Color rowAccent = accents.Length > 0 ? accents[i % accents.Length] : Color.red;

                row.Offset = Mathf.Lerp(row.Offset, on ? 70f : 0f, 1f - Mathf.Exp(-16f * dt));
                row.Scale = Mathf.Lerp(row.Scale, on ? 1.07f : 1f, 1f - Mathf.Exp(-16f * dt));

                Vector2 pos = row.Rt.anchoredPosition;
                pos.x = 260f + row.Offset;
                row.Rt.anchoredPosition = pos;
                row.Rt.localScale = new Vector3(row.Scale, row.Scale, 1f);

                Color bg = on ? rowAccent : new Color(0.04f, 0.04f, 0.05f, 0.92f);
                row.Bg.color = Color.Lerp(row.Bg.color, bg, 1f - Mathf.Exp(-18f * dt));
                row.Stripe.color = rowAccent;

                float lum = rowAccent.r * 0.3f + rowAccent.g * 0.59f + rowAccent.b * 0.11f;
                row.Text.color = on ? (lum > 0.55f ? Color.black : Color.white) : Color.white;
            }
        }

        // ---------- Ferramentas ----------

        private void Play(AudioClip clip)
        {
            if (clip != null) _audio.PlayOneShot(clip, sfxVolume);
        }

        private static RectTransform MakeRect(string name, Transform parent)
        {
            RectTransform rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            return rt;
        }

        private static Image MakeImage(Transform parent, string name, Color color)
        {
            Image img = MakeRect(name, parent).gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private TextMeshProUGUI MakeText(Transform parent, string name, string text, float size, Color color, TextAlignmentOptions align)
        {
            TextMeshProUGUI tmp = MakeRect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = align;
            tmp.fontStyle = FontStyles.Bold | FontStyles.Italic;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Overflow;
            return tmp;
        }

        private static void Fill(RectTransform rt, float l = 0f, float b = 0f, float r = 0f, float t = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(l, b);
            rt.offsetMax = new Vector2(-r, -t);
        }

        private static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size, Vector2 pivot)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }
    }
}