using System;
using System.Collections;
using System.Collections.Generic;
using Core.Accessibility;
using Core.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace Core.Minigames.Welding
{
    /// <summary>
    /// MINIGAME DE SOLDA INDUSTRIAL (NR-6 EPI, NR-12 máquinas, NR-23 incêndio).
    ///
    /// COMO USAR: um objeto vazio na cena com ESTE script. Ele cria o Canvas e toda a interface sozinho.
    /// Pra começar:   FindFirstObjectByType&lt;WeldingMinigame&gt;().Begin(resultado => { ... });
    /// Pra testar:    deixe "Auto Start On Play" marcado e dê Play.
    /// Pra receber o resultado sem código: use o evento "On Finished" no Inspector.
    ///
    /// ARTES e SONS: arraste nos campos "Art" e "Sounds". Campo vazio = placeholder / silêncio.
    /// NÚMEROS (dificuldade, pontos): campo "Settings".
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WeldingMinigame : MonoBehaviour
    {
        [Header("Como iniciar")]
        [Tooltip("Marque pra começar sozinho ao dar Play (bom pra testar numa cena vazia). Desmarque no jogo final e chame Begin().")]
        [SerializeField] private bool autoStartOnPlay = true;

        [Header("Etapas (desmarque pra pular uma)")]
        [SerializeField] private bool stepNotebook = true;
        [SerializeField] private bool stepEpi = true;
        [SerializeField] private bool stepClean = true;
        [SerializeField] private bool stepSafety = true;
        [Tooltip("Coloca EPIs ERRADOS na mesa (luva de látex, óculos comum) pra confundir. Pegar um = perde pontos.")]
        [SerializeField] private bool epiDecoys = false;

        [Header("Artes e sons")]
        [SerializeField] private WeldingArt art = new WeldingArt();
        [SerializeField] private WeldingSounds sounds = new WeldingSounds();
        [Tooltip("Grupo SFX do seu AudioMixer (assim o volume das Opções vale). Pode deixar vazio.")]
        [SerializeField] private AudioMixerGroup sfxGroup;
        [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1f;
        [Tooltip("Fonte dos textos (TextMeshPro). Vazio = a fonte padrão do projeto.")]
        [SerializeField] private TMP_FontAsset font;

        [Header("Ajustes (números do jogo)")]
        [SerializeField] private WeldingSettings settings = new WeldingSettings();
        [SerializeField] private int canvasSortingOrder = 500;

        [Header("Eventos")]
        public UnityEngine.Events.UnityEvent onStarted;
        public WeldingResultEvent onFinished;

        // ---------- Acesso pras etapas ----------

        public WeldingArt Art { get { return art; } }
        public WeldingSounds Sounds { get { return sounds; } }
        public WeldingSettings Settings { get { return settings; } }
        public WeldingAudioPlayer Audio { get; private set; }
        public WeldingInput Clicks { get; private set; }
        public WeldingHud Hud { get; private set; }
        public RectTransform Stage { get { return _stage; } }
        public RectTransform Overlay { get { return _overlay; } }
        public WeldingResult Result { get; private set; }
        /// <summary>true = jogo parado (ex.: caderninho aberto). As etapas devem esperar.</summary>
        public bool Paused { get { return _paused || Time.timeScale <= 0f; } set { _paused = value; } }
        private bool _paused;
        /// <summary>Brilho da luz de foco (0 = apagada, 1 = normal). Os eventos de luz mexem aqui.</summary>
        public float LampLevel { get; set; }
        public WeldingNotebook Notebook { get; private set; }
        /// <summary>Depois de ler o caderninho no começo, o TAB passa a abri-lo.</summary>
        public bool NotebookUnlocked { get; set; }
        public bool EpiDecoys { get { return epiDecoys; } }
        public bool IsRunning { get { return _flow != null; } }
        public float Elapsed { get { return _elapsed; } }
        public WeldingDifficulty Difficulty { get; private set; } = WeldingDifficulty.Medium;
        public bool IsDefeated { get { return Result != null && (Result.integrity <= 0 || Result.health <= 0); } }

        public static readonly Color Ink = new Color(0.06f, 0.07f, 0.09f, 1f);
        public static readonly Color Paper = new Color(0.96f, 0.94f, 0.88f, 1f);
        public static readonly Color Accent = new Color(1f, 0.82f, 0.1f, 1f);
        public static readonly Color Good = new Color(0.35f, 0.85f, 0.45f, 1f);
        public static readonly Color Bad = new Color(1f, 0.32f, 0.25f, 1f);
        private static readonly Color Muted = new Color(0.72f, 0.72f, 0.72f, 1f);

        private Canvas _canvas;
        private RectTransform _root;
        private RectTransform _stage;
        private RectTransform _overlay;
        private RectTransform _handsLayer;
        private RectTransform _cardLayer;
        private Image _handsLeft;
        private Image _handsRight;
        private Image _handsLegacy;
        private Image _handActionLeft;
        private Image _handActionRight;
        private Image _backgroundImage;
        private Image _tableImage;
        private Image _lamp;
        private float _lampNoise;
        private Image _flash;
        private Image _blink;
        private Coroutine _flow;
        private Action<WeldingResult> _callback;
        private List<WeldingPhase> _phases;
        private readonly HashSet<string> _equipped = new HashSet<string>();
        private float _elapsed;
        private float _shakeTimer;
        private float _shakeDuration;
        private float _shakeAmount;
        private float _strain;
        private float _flashTimer;
        private float _flashDuration;
        private float _flashPeak;
        private bool _built;
        private float _startingIntegrity;

        // ---------- Ciclo de vida ----------

        private void Awake()
        {
            EnsureBuilt();
        }

        private void Start()
        {
            if (autoStartOnPlay) Begin();
        }

        private void OnDisable()
        {
            if (_flow != null) Cancel();
        }

        private void EnsureBuilt()
        {
            if (_built) return;
            _built = true;

            WeldUI.Font = font;

            Audio = new WeldingAudioPlayer(gameObject, sfxGroup, sfxVolume);
            Clicks = new WeldingInput();

            GameObject canvasObject = new GameObject("[WeldingMinigameCanvas]", typeof(RectTransform));
            canvasObject.transform.SetParent(transform, false);

            _canvas = canvasObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = canvasSortingOrder;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // O menu de acessibilidade mexe nos textos do jogo todo; aqui os tamanhos são fixos
            canvasObject.AddComponent<AccessibilityIgnore>();

            RectTransform canvasRect = (RectTransform)canvasObject.transform;

            Image dark = WeldUI.MakeImage(canvasRect, "Dark", null, Ink);
            WeldUI.Fill(dark.rectTransform);

            _root = WeldUI.MakeRect("Root", canvasRect);
            WeldUI.Fill(_root);
            _root.localScale = Vector3.one;

            BuildScenery();

            _stage = WeldUI.MakeRect("Stage", _root);
            WeldUI.Fill(_stage);

            // Luz do foco (lâmpada acima da mesa) e escurecido das bordas: dão profundidade ao 2D
            _lamp = WeldUI.MakeImage(_root, "Lamp", WeldUI.Glow, new Color(1f, 0.86f, 0.55f, 0.22f));
            WeldUI.Place(_lamp.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, 120f), new Vector2(1900f, 1300f), new Vector2(0.5f, 1f));
            _lamp.preserveAspect = false;

            Image ambient = WeldUI.MakeImage(_root, "Ambient", WeldUI.Vignette, new Color(0f, 0f, 0f, 0.5f));
            WeldUI.Fill(ambient.rectTransform);
            ambient.preserveAspect = false;

            _handsLayer = WeldUI.MakeRect("HandsLayer", _root);
            WeldUI.Fill(_handsLayer);
            _handsLeft = WeldUI.MakeProp(_handsLayer, "LeftHand", art.leftHandBare, Vector2.zero,
                new Vector2(560f, 420f), new Color(0.45f, 0.3f, 0.18f, 1f), GameLanguage.T("MÃO ESQUERDA", "LEFT HAND"), 24f);
            WeldUI.Place(_handsLeft.rectTransform, new Vector2(0f, 0f), new Vector2(20f, -135f),
                new Vector2(560f, 420f), new Vector2(0f, 0f));
            _handsRight = WeldUI.MakeProp(_handsLayer, "RightHand", art.rightHandBare, Vector2.zero,
                new Vector2(560f, 420f), new Color(0.45f, 0.3f, 0.18f, 1f), GameLanguage.T("MÃO DIREITA", "RIGHT HAND"), 24f);
            WeldUI.Place(_handsRight.rectTransform, new Vector2(1f, 0f), new Vector2(-20f, -135f),
                new Vector2(560f, 420f), new Vector2(1f, 0f));
            _handsLegacy = WeldUI.MakeProp(_handsLayer, "HandsLegacy", art.handsBare, Vector2.zero,
                new Vector2(1000f, 460f), new Color(0.82f, 0.62f, 0.48f, 1f), GameLanguage.T("MÃOS", "HANDS"), 30f);
            WeldUI.Place(_handsLegacy.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, -140f),
                new Vector2(1000f, 460f), new Vector2(0.5f, 0f));
            _handsLeft.gameObject.SetActive(false);
            _handsRight.gameObject.SetActive(false);
            _handsLegacy.gameObject.SetActive(false);

            _overlay = WeldUI.MakeRect("Overlay", _root);
            WeldUI.Fill(_overlay);

            Hud = new WeldingHud(_root);

            _cardLayer = WeldUI.MakeRect("Cards", _root);
            WeldUI.Fill(_cardLayer);
            Notebook = new WeldingNotebook(this, _cardLayer);

            _flash = WeldUI.MakeImage(_root, "Flash", null, new Color(1f, 1f, 1f, 0f));
            WeldUI.Fill(_flash.rectTransform);

            _blink = WeldUI.MakeImage(canvasRect, "Blink", null, new Color(0f, 0f, 0f, 0f));
            WeldUI.Fill(_blink.rectTransform);

            canvasObject.SetActive(false);
        }

        private void BuildScenery()
        {
            RectTransform scenery = WeldUI.MakeRect("Scenery", _root);
            WeldUI.Fill(scenery);

            Image back = WeldUI.MakeImage(scenery, "Background", art.background,
                art.background != null ? Color.white : new Color(0.16f, 0.18f, 0.21f, 1f));
            WeldUI.Fill(back.rectTransform);
            back.preserveAspect = false;
            _backgroundImage = back;

            Image table = WeldUI.MakeProp(scenery, "Table", art.table, Vector2.zero, new Vector2(1920f, 500f),
                new Color(0.34f, 0.25f, 0.18f, 1f), art.table == null ? GameLanguage.T("MESA", "TABLE") : null, 44f);
            WeldUI.Place(table.rectTransform, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(1920f, 500f),
                new Vector2(0.5f, 0f));
            if (art.table != null) table.preserveAspect = false;
            _tableImage = table;
        }

        // ---------- Começar e terminar ----------

        /// <summary>Começa o minigame. "callback" recebe o resultado no final (opcional; o evento On Finished também dispara).</summary>
        public void Begin(Action<WeldingResult> callback = null)
        {
            if (IsRunning) return;

            EnsureBuilt();
            settings.ApplyDifficulty(Difficulty);
            _callback = callback;
            _canvas.gameObject.SetActive(true);
            _flow = StartCoroutine(RunFlow());
        }

        /// <summary>Fecha o minigame na hora, sem resultado.</summary>
        public void Cancel()
        {
            if (_flow != null) StopCoroutine(_flow);
            _flow = null;
            Close();
        }

        private void Close()
        {
            Audio.StopAll();
            if (Notebook != null) Notebook.Dispose();
            Paused = false;
            NotebookUnlocked = false;
            Clicks.Clear();
            Clicks.Modal = false;
            Clicks.Locked = false;
            Cursor.visible = true;
            if (_root != null) _root.anchoredPosition = Vector2.zero;
            if (_canvas != null) _canvas.gameObject.SetActive(false);
        }

        public void SetDifficulty(WeldingDifficulty difficulty)
        {
            Difficulty = difficulty;
            settings.ApplyDifficulty(difficulty);
            if (Result != null) Result.difficulty = DifficultyName(difficulty);
        }

        private static string DifficultyName(WeldingDifficulty difficulty)
        {
            switch (difficulty)
            {
                case WeldingDifficulty.Easy: return GameLanguage.T("Fácil", "Easy");
                case WeldingDifficulty.Hard: return GameLanguage.T("Difícil", "Hard");
                default: return GameLanguage.T("Médio", "Medium");
            }
        }

        public void AdjustIntegrity(float delta)
        {
            if (Result == null) return;
            int before = Mathf.RoundToInt(Result.integrity);
            Result.integrity = Mathf.Clamp(Result.integrity + delta, 0f, 100f);
            if (before != Mathf.RoundToInt(Result.integrity))
                Hud.SetVitals(Mathf.RoundToInt(Result.integrity), Mathf.RoundToInt(Result.stamina), Mathf.RoundToInt(Result.health));
        }

        public void AdjustStamina(float delta)
        {
            if (Result == null) return;
            int before = Mathf.RoundToInt(Result.stamina);
            Result.stamina = Mathf.Clamp(Result.stamina + delta, 0f, 100f);
            if (before != Mathf.RoundToInt(Result.stamina))
                Hud.SetVitals(Mathf.RoundToInt(Result.integrity), Mathf.RoundToInt(Result.stamina), Mathf.RoundToInt(Result.health));
        }

        public void ApplyInjury(float damage, string portuguese, string english)
        {
            if (Result == null || damage <= 0f) return;
            int applied = Mathf.Max(1, Mathf.RoundToInt(damage));
            Result.health = Mathf.Clamp(Result.health - applied, 0f, 100f);
            Result.mistakes++;
            Result.mistakeLog.Add(GameLanguage.T(portuguese, english) + " (-" + applied + " vida)");
            AdjustIntegrity(-Mathf.Max(1f, applied * 0.25f));
            AdjustStamina(-Mathf.Max(1f, applied * 0.2f));
            Hud.Toast(GameLanguage.T(portuguese, english) + " (-" + applied + " VIDA)", Bad, 3.2f);
            Audio.Play(sounds.burnOuch != null ? sounds.burnOuch : sounds.alert);
            Shake(8f, 0.35f);
            Hud.SetVitals(Mathf.RoundToInt(Result.integrity), Mathf.RoundToInt(Result.stamina), Mathf.RoundToInt(Result.health));
        }

        public void SetOxygen(float normalized)
        {
            if (Result == null) return;
            Result.oxygen = Mathf.Clamp01(normalized) * 100f;
        }

        /// <summary>Restaura o cenário principal depois de olhar para uma parede.</summary>
        public void SetWorkstationView()
        {
            if (_backgroundImage != null)
            {
                _backgroundImage.sprite = art.background;
                _backgroundImage.color = art.background != null ? Color.white : new Color(0.16f, 0.18f, 0.21f, 1f);
            }
            if (_tableImage != null) _tableImage.gameObject.SetActive(true);
        }

        public void SetWallView(bool rightSide)
        {
            if (_backgroundImage != null)
            {
                Sprite view = rightSide ? art.rightWallBackground : art.frontWallBackground;
                _backgroundImage.sprite = view != null ? view : art.background;
                _backgroundImage.color = _backgroundImage.sprite != null ? Color.white : new Color(0.12f, 0.14f, 0.17f, 1f);
            }
            if (_tableImage != null) _tableImage.gameObject.SetActive(false);
        }

        public IEnumerator ReachWithHand(bool left, Vector2 target, float duration = 0.24f)
        {
            // Uma animação por mão: interações rápidas substituem o movimento anterior em vez de empilhar sprites.
            Image previous = left ? _handActionLeft : _handActionRight;
            if (previous != null) Destroy(previous.gameObject);

            Sprite sprite = GetHandSprite(left);
            Vector2 start = new Vector2(left ? -700f : 700f, -520f);
            Vector2 size = new Vector2(430f, 360f);
            Image hand = WeldUI.MakeProp(_stage, left ? "LeftHandAction" : "RightHandAction", sprite, start, size,
                new Color(0.48f, 0.32f, 0.2f, 1f), left ? GameLanguage.T("MÃO ESQ.", "LEFT HAND") : GameLanguage.T("MÃO DIR.", "RIGHT HAND"), 24f);
            if (left) _handActionLeft = hand; else _handActionRight = hand;
            hand.transform.SetAsLastSibling();

            float t = 0f;
            while (t < duration && hand != null)
            {
                if (Paused) { yield return null; continue; }
                t += Time.unscaledDeltaTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
                hand.rectTransform.anchoredPosition = Vector2.Lerp(start, target, k);
                yield return null;
            }
            if (hand != null) hand.rectTransform.anchoredPosition = target;
            yield return Wait(0.08f);

            t = 0f;
            while (t < duration && hand != null)
            {
                if (Paused) { yield return null; continue; }
                t += Time.unscaledDeltaTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
                hand.rectTransform.anchoredPosition = Vector2.Lerp(target, start, k);
                yield return null;
            }
            if (hand != null) Destroy(hand.gameObject);
            if (left && _handActionLeft == hand) _handActionLeft = null;
            if (!left && _handActionRight == hand) _handActionRight = null;
        }

        public void AnimateHandAction(bool left, Vector2 target)
        {
            if (_stage != null && _stage.gameObject.activeInHierarchy)
                StartCoroutine(ReachWithHand(left, target));
        }

        private Sprite GetHandSprite(bool left)
        {
            bool gloves = Has("gloves");
            if (left)
            {
                Sprite specific = gloves ? art.leftHandGloves : art.leftHandBare;
                if (specific != null) return specific;
            }
            else
            {
                Sprite specific = gloves ? art.rightHandGloves : art.rightHandBare;
                if (specific != null) return specific;
            }
            return gloves && art.handsGloves != null ? art.handsGloves : art.handsBare;
        }

        private List<WeldingPhase> BuildPhases()
        {
            List<WeldingPhase> list = new List<WeldingPhase>();

            if (stepNotebook) list.Add(new NotebookPhase(this));
            if (stepEpi) list.Add(new EpiPhase(this));
            if (stepClean) list.Add(new CleanPhase(this));
            if (stepSafety) list.Add(new SafetyPhase(this));
            list.Add(new WeldPhase(this));

            return list;
        }

        private IEnumerator RunFlow()
        {
            Result = new WeldingResult
            {
                integrity = Mathf.Clamp(settings.startingIntegrity, 0, 100),
                stamina = Mathf.Clamp(settings.startingStamina, 0, 100),
                health = Mathf.Clamp(settings.startingHealth, 0, 100),
                oxygen = 100,
                difficulty = DifficultyName(Difficulty)
            };
            _startingIntegrity = Result.integrity;
            _elapsed = 0f;
            if (_root != null) _root.anchoredPosition = Vector2.zero;
            _shakeAmount = 0f;
            _shakeDuration = 0f;
            _shakeTimer = 0f;
            _strain = 0f;
            _flashTimer = 0f;
            if (_flash != null) _flash.color = new Color(1f, 1f, 1f, 0f);
            if (_blink != null) _blink.color = new Color(0f, 0f, 0f, 0f);
            LampLevel = 1f;
            SetWorkstationView();
            Clicks.Clear();
            Clicks.Locked = false;
            Clicks.Modal = false;
            _equipped.Clear();
            Paused = false;
            NotebookUnlocked = !stepNotebook;
            _strain = 0f;
            _shakeTimer = 0f;

            Hud.ClearEquipment();
            Hud.SetVitals(Mathf.RoundToInt(Result.integrity), Mathf.RoundToInt(Result.stamina), Mathf.RoundToInt(Result.health));
            Hud.SetVisible(true);
            Hud.SetChecklistVisible(true);
            Hud.SetHint("");
            ShowHands(false);
            ClearStage();

            Audio.SetLoop("ambient", sounds.ambientLoop, 0.45f);
            if (onStarted != null) onStarted.Invoke();

            _phases = BuildPhases();
            List<string> titles = new List<string>();
            for (int i = 0; i < _phases.Count; i++) titles.Add(_phases[i].Title);
            Hud.BuildChecklist(titles);
            for (int i = 0; i < _phases.Count; i++) Hud.SetStep(i, WeldingHud.StepState.Pending);

            yield return Card(
                GameLanguage.T("SOLDA INDUSTRIAL", "INDUSTRIAL WELDING"),
                GameLanguage.T(
                    "Missão: reparar a trinca da peça.\n\nSiga o procedimento de segurança, na ordem, antes de acender o arco.\nNormas: NR-6 (EPI), NR-12 (máquinas) e NR-23 (incêndio).",
                    "Mission: repair the crack in the part.\n\nFollow the safety procedure, in order, before striking the arc.\nStandards: NR-6 (PPE), NR-12 (machinery) and NR-23 (fire)."),
                GameLanguage.T("COMEÇAR", "START"), Accent, art.bossPortrait);

            for (int i = 0; i < _phases.Count; i++)
            {
                Hud.SetStep(i, WeldingHud.StepState.Current);
                ClearStage();
                yield return _phases[i].Run();
                if (IsDefeated)
                {
                    yield return Finish(false);
                    yield break;
                }
                Hud.SetStep(i, WeldingHud.StepState.Done);
                Audio.Play(sounds.stepDone);
                yield return Wait(0.4f);
            }

            yield return Finish(true);
        }

        private IEnumerator Finish(bool victory)
        {
            NotebookUnlocked = false;
            ClearStage();
            ShowHands(false);
            Hud.SetHint("");

            Result.victory = victory && !IsDefeated;
            Result.completed = Result.victory;
            Result.totalTime = _elapsed;
            Result.integrity = Mathf.Clamp(Result.integrity, 0f, 100f);
            Result.stamina = Mathf.Clamp(Result.stamina, 0f, 100f);
            Result.health = Mathf.Clamp(Result.health, 0f, 100f);
            Result.oxygen = Mathf.Clamp(Result.oxygen, 0f, 100f);
            Result.integrityDelta = Mathf.RoundToInt(Result.integrity - _startingIntegrity);

            float score = Result.integrity * 0.32f + Result.health * 0.24f +
                          Result.stamina * 0.12f + Result.weldQuality * 100f * 0.32f;
            if (!Result.victory) Result.grade = "F";
            else if (score >= 88f) Result.grade = "A";
            else if (score >= 74f) Result.grade = "B";
            else if (score >= 58f) Result.grade = "C";
            else Result.grade = "D";

            Audio.Play(Result.victory ? sounds.resultGood : sounds.resultBad);
            yield return ResultScreen(Result.victory);

            _flow = null;
            Close();

            if (_callback != null) _callback(Result);
            if (onFinished != null) onFinished.Invoke(Result);
        }

        private IEnumerator ResultScreen(bool victory)
        {
            bool wasLocked = Clicks.Locked;
            Clicks.Locked = false;
            Hud.SetVisible(false);

            RectTransform screen = WeldUI.MakeRect(victory ? "VictoryScreen" : "DefeatScreen", _cardLayer);
            WeldUI.Fill(screen);
            Image bg = WeldUI.MakeImage(screen, "Background", null,
                victory ? new Color(0.025f, 0.09f, 0.075f, 0.98f) : new Color(0.12f, 0.025f, 0.03f, 0.98f));
            WeldUI.Fill(bg.rectTransform);

            Image glow = WeldUI.MakeImage(screen, "Glow", WeldUI.Glow,
                victory ? new Color(0.25f, 0.85f, 0.5f, 0.2f) : new Color(0.95f, 0.15f, 0.12f, 0.22f));
            WeldUI.Place(glow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(1500f, 900f));
            glow.preserveAspect = false;

            RectTransform panel = WeldUI.MakeRect("ReportPanel", screen);
            WeldUI.Place(panel, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1420f, 790f));
            Image panelBg = WeldUI.MakeImage(panel, "PanelBg", null, new Color(0.035f, 0.045f, 0.05f, 0.94f));
            WeldUI.Fill(panelBg.rectTransform);

            Color accent = victory ? new Color(0.35f, 0.9f, 0.58f, 1f) : new Color(1f, 0.3f, 0.24f, 1f);
            Image stripe = WeldUI.MakeImage(panel, "Stripe", null, accent);
            stripe.rectTransform.anchorMin = new Vector2(0f, 1f);
            stripe.rectTransform.anchorMax = new Vector2(1f, 1f);
            stripe.rectTransform.pivot = new Vector2(0.5f, 1f);
            stripe.rectTransform.offsetMin = new Vector2(0f, -12f);
            stripe.rectTransform.offsetMax = Vector2.zero;

            TextMeshProUGUI title = WeldUI.MakeText(panel, "Title",
                victory ? GameLanguage.T("TRABALHO CONCLUÍDO", "JOB COMPLETED") : GameLanguage.T("FALHA CRÍTICA", "CRITICAL FAILURE"),
                72f, accent, TextAlignmentOptions.Center);
            WeldUI.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(1240f, 100f),
                new Vector2(0.5f, 1f));

            TextMeshProUGUI subtitle = WeldUI.MakeText(panel, "Subtitle",
                victory ? GameLanguage.T("A operação foi encerrada. Confira o relatório.", "The operation is complete. Review the report.")
                        : GameLanguage.T("A personagem não conseguiu continuar o serviço.", "The character could not continue the job."),
                30f, Paper, TextAlignmentOptions.Center, true);
            WeldUI.Place(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(1180f, 58f),
                new Vector2(0.5f, 1f));

            string[] names =
            {
                GameLanguage.T("INTEGRIDADE", "INTEGRITY"),
                GameLanguage.T("STAMINA", "STAMINA"),
                GameLanguage.T("VIDA", "HEALTH"),
                GameLanguage.T("OXIGÊNIO", "OXYGEN"),
                GameLanguage.T("QUALIDADE DA SOLDA", "WELD QUALITY"),
                GameLanguage.T("DIFICULDADE", "DIFFICULTY")
            };
            string[] values =
            {
                Mathf.RoundToInt(Result.integrity) + "%", Mathf.RoundToInt(Result.stamina) + "%", Mathf.RoundToInt(Result.health) + "%", Mathf.RoundToInt(Result.oxygen) + "%",
                Mathf.RoundToInt(Result.weldQuality * 100f) + "%", Result.difficulty
            };
            for (int i = 0; i < names.Length; i++)
            {
                int col = i % 3;
                int row = i / 3;
                Vector2 pos = new Vector2((col - 1) * 390f, 100f - row * 155f);
                RectTransform stat = WeldUI.MakeRect("Stat" + i, panel);
                WeldUI.Place(stat, new Vector2(0.5f, 0.5f), pos, new Vector2(340f, 112f));
                Image statBg = WeldUI.MakeImage(stat, "Bg", null, new Color(1f, 1f, 1f, 0.055f));
                WeldUI.Fill(statBg.rectTransform);
                TextMeshProUGUI label = WeldUI.MakeText(stat, "Label", names[i], 20f, Muted, TextAlignmentOptions.Center);
                WeldUI.Place(label.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(310f, 32f),
                    new Vector2(0.5f, 1f));
                TextMeshProUGUI value = WeldUI.MakeText(stat, "Value", values[i], 38f, Paper, TextAlignmentOptions.Center);
                WeldUI.Place(value.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(310f, 48f),
                    new Vector2(0.5f, 0f));
            }

            int minutes = Mathf.FloorToInt(Result.totalTime / 60f);
            int seconds = Mathf.FloorToInt(Result.totalTime % 60f);
            TextMeshProUGUI footer = WeldUI.MakeText(panel, "Footer",
                GameLanguage.T("NOTA ", "GRADE ") + Result.grade + "     " + GameLanguage.T("TEMPO ", "TIME ") + minutes + ":" + seconds.ToString("00") +
                "     " + GameLanguage.T("ERROS ", "MISTAKES ") + Result.mistakes,
                26f, accent, TextAlignmentOptions.Center);
            WeldUI.Place(footer.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 164f), new Vector2(1180f, 42f),
                new Vector2(0.5f, 0f));

            string details = "";
            int shown = Mathf.Min(3, Result.mistakeLog.Count);
            for (int i = 0; i < shown; i++) details += (i == 0 ? "" : "  •  ") + Result.mistakeLog[i];
            if (!string.IsNullOrEmpty(details))
            {
                TextMeshProUGUI log = WeldUI.MakeText(panel, "Log", details, 18f, Muted, TextAlignmentOptions.Center, true);
                WeldUI.Place(log.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 112f), new Vector2(1200f, 42f),
                    new Vector2(0.5f, 0f));
            }

            Image button = WeldUI.MakeProp(panel, "Continue", null, Vector2.zero, new Vector2(360f, 72f), accent,
                GameLanguage.T("CONTINUAR", "CONTINUE"), 32f);
            WeldUI.Place(button.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(360f, 72f),
                new Vector2(0.5f, 0f));
            TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null) buttonText.color = Ink;

            bool clicked = false;
            WeldingInput.Hit hit = Clicks.Register(button.rectTransform, () => clicked = true, null, 1000);
            while (!clicked)
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
                    clicked = true;
                yield return null;
            }
            Clicks.Unregister(hit);
            Destroy(screen.gameObject);
            Clicks.Locked = wasLocked;
        }

        // ---------- Ferramentas pras etapas ----------

        /// <summary>Apaga tudo que a etapa anterior deixou na tela.</summary>
        public void ClearStage()
        {
            Clicks.Clear();
            for (int i = _stage.childCount - 1; i >= 0; i--) Destroy(_stage.GetChild(i).gameObject);
            for (int i = _overlay.childCount - 1; i >= 0; i--) Destroy(_overlay.GetChild(i).gameObject);
            _strain = 0f;
        }

        public IEnumerator Wait(float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                if (Paused) { yield return null; continue; }
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        /// <summary>
        /// Piscada: a tela escurece, ela "anda" (passos), e a tela volta.
        /// Em "atDark" troque o cenário (ele acontece com a tela preta).
        /// </summary>
        public IEnumerator Blink(int footsteps, Action atDark = null)
        {
            bool wasLocked = Clicks.Locked;
            Clicks.Locked = true;

            yield return Fade(0f, 1f, settings.blinkClose);
            Audio.Play(sounds.blink);
            if (atDark != null) atDark();

            if (footsteps > 0)
            {
                for (int i = 0; i < footsteps; i++)
                {
                    Audio.PlayRandom(sounds.footsteps);
                    yield return Wait(settings.footstepInterval);
                }
            }
            else
            {
                yield return Wait(settings.blinkHold);
            }

            yield return Fade(1f, 0f, settings.blinkOpen);
            Clicks.Locked = wasLocked;
        }

        private IEnumerator Fade(float from, float to, float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                if (Paused) { yield return null; continue; }
                t += Time.unscaledDeltaTime;
                float a = Mathf.Lerp(from, to, Mathf.Clamp01(t / seconds));
                _blink.color = new Color(0f, 0f, 0f, a);
                yield return null;
            }

            _blink.color = new Color(0f, 0f, 0f, to);
        }

        /// <summary>Erro de procedimento: tira pontos (0 = só registra), mostra o aviso e toca o alerta.</summary>
        public void Penalize(int points, string portuguese, string english)
        {
            string text = GameLanguage.T(portuguese, english);
            string suffix = points > 0 ? " (-" + points + " integridade)" : "";

            if (Result != null)
            {
                Result.mistakes++;
                Result.mistakeLog.Add(text + suffix);
            }
            AdjustIntegrity(-Mathf.Max(0, points));
            Hud.Toast(text + suffix, Bad);
            Audio.Play(sounds.alert);
        }

        /// <summary>Tremor da câmera (respeita "Tremor de câmera" da Acessibilidade).</summary>
        public void Shake(float amount, float seconds)
        {
            _shakeAmount = amount;
            _shakeDuration = Mathf.Max(0.05f, seconds);
            _shakeTimer = _shakeDuration;
        }

        /// <summary>Tremor contínuo de esforço (0 a 1). Mande 0 pra parar.</summary>
        public void SetStrain(float strength)
        {
            _strain = Mathf.Clamp01(strength);
        }

        /// <summary>Clarão na tela (respeita "Reduzir flashes" da Acessibilidade).</summary>
        public void Flash(Color color, float intensity, float seconds)
        {
            _flashPeak = AccessibilitySettings.FlashIntensity(Mathf.Clamp01(intensity));
            _flashDuration = Mathf.Max(0.05f, seconds);
            _flashTimer = _flashDuration;
            _flash.color = new Color(color.r, color.g, color.b, 0f);
        }

        // ---------- EPIs vestidos e mãos ----------

        public bool Has(string id)
        {
            return _equipped.Contains(id);
        }

        public void AddEquipped(string id, Sprite sprite, Color placeholder, string label)
        {
            if (!_equipped.Add(id)) return;
            Hud.AddEquipment(sprite, placeholder, label);
            RefreshHands();
        }

        public void ShowHands(bool visible)
        {
            if (!visible)
            {
                _handsLeft.gameObject.SetActive(false);
                _handsRight.gameObject.SetActive(false);
                _handsLegacy.gameObject.SetActive(false);
                return;
            }
            RefreshHands();
        }

        private void RefreshHands()
        {
            bool gloves = Has("gloves");
            Sprite left = gloves ? (art.leftHandGloves != null ? art.leftHandGloves : art.leftHandBare) : art.leftHandBare;
            Sprite right = gloves ? (art.rightHandGloves != null ? art.rightHandGloves : art.rightHandBare) : art.rightHandBare;
            bool separatedArt = left != null || right != null;

            _handsLegacy.gameObject.SetActive(!separatedArt && (art.handsBare != null || art.handsGloves != null));
            _handsLeft.gameObject.SetActive(separatedArt || (art.handsBare == null && art.handsGloves == null));
            _handsRight.gameObject.SetActive(separatedArt || (art.handsBare == null && art.handsGloves == null));

            if (separatedArt)
            {
                WeldUI.SetProp(_handsLeft, left, new Color(0.45f, 0.3f, 0.18f, 1f), GameLanguage.T("MÃO ESQUERDA", "LEFT HAND"), 24f);
                WeldUI.SetProp(_handsRight, right, new Color(0.45f, 0.3f, 0.18f, 1f), GameLanguage.T("MÃO DIREITA", "RIGHT HAND"), 24f);
            }
            else if (_handsLeft.gameObject.activeSelf)
            {
                WeldUI.SetProp(_handsLeft, null, new Color(0.45f, 0.3f, 0.18f, 1f), GameLanguage.T("MÃO ESQUERDA", "LEFT HAND"), 24f);
                WeldUI.SetProp(_handsRight, null, new Color(0.45f, 0.3f, 0.18f, 1f), GameLanguage.T("MÃO DIREITA", "RIGHT HAND"), 24f);
            }

            if (_handsLegacy.gameObject.activeSelf)
            {
                Sprite legacy = gloves && art.handsGloves != null ? art.handsGloves : art.handsBare;
                WeldUI.SetProp(_handsLegacy, legacy, new Color(0.82f, 0.62f, 0.48f, 1f),
                    GameLanguage.T("MÃOS", "HANDS"), 30f);
            }
        }

        // ---------- Cartão (janela de aviso) ----------

        public IEnumerator Card(string title, string body, string button, Color accent, Sprite portrait = null)
        {
            bool wasLocked = Clicks.Locked;
            Clicks.Locked = false;

            RectTransform root = WeldUI.MakeRect("Card", _cardLayer);
            WeldUI.Fill(root);

            Image dim = WeldUI.MakeImage(root, "Dim", null, new Color(0f, 0f, 0f, 0.7f));
            WeldUI.Fill(dim.rectTransform);

            RectTransform panel = WeldUI.MakeRect("Panel", root);
            WeldUI.Place(panel, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1240f, 700f));

            Image bg = WeldUI.MakeImage(panel, "Bg", null, Ink);
            WeldUI.Fill(bg.rectTransform);

            Image bar = WeldUI.MakeImage(panel, "Bar", null, accent);
            bar.rectTransform.anchorMin = new Vector2(0f, 1f);
            bar.rectTransform.anchorMax = new Vector2(1f, 1f);
            bar.rectTransform.pivot = new Vector2(0.5f, 1f);
            bar.rectTransform.offsetMin = new Vector2(0f, -14f);
            bar.rectTransform.offsetMax = Vector2.zero;

            float textLeft = 60f;
            if (portrait != null)
            {
                Image face = WeldUI.MakeImage(panel, "Portrait", portrait, Color.white);
                WeldUI.Place(face.rectTransform, new Vector2(0f, 0.5f), new Vector2(50f, 20f), new Vector2(300f, 420f),
                    new Vector2(0f, 0.5f));
                textLeft = 390f;
            }

            TextMeshProUGUI titleText = WeldUI.MakeText(panel, "Title", title, 64f, accent, TextAlignmentOptions.TopLeft);
            WeldUI.Fill(titleText.rectTransform, textLeft, 0f, 50f, 50f);

            TextMeshProUGUI bodyText = WeldUI.MakeText(panel, "Body", body, 34f, Paper, TextAlignmentOptions.TopLeft, true);
            WeldUI.Fill(bodyText.rectTransform, textLeft, 150f, 50f, 150f);

            Image buttonImage = WeldUI.MakeProp(panel, "Button", null, Vector2.zero, new Vector2(420f, 92f), accent, button, 38f);
            WeldUI.Place(buttonImage.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(420f, 92f),
                new Vector2(0.5f, 0f));
            TextMeshProUGUI buttonText = buttonImage.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null) buttonText.color = Ink;

            bool clicked = false;
            WeldingInput.Hit hit = Clicks.Register(buttonImage.rectTransform, () => clicked = true, null, 100);

            while (!clicked)
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
                    clicked = true;
                yield return null;
            }

            Audio.Play(sounds.uiClick);
            Clicks.Unregister(hit);
            Destroy(root.gameObject);
            Clicks.Locked = wasLocked;
        }

        // ---------- Atualização por quadro ----------

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            if (Audio != null) Audio.Tick(dt);
            if (!IsRunning) return;
            if (Time.timeScale <= 0f) return; // jogo pausado pelo menu de pausa

            if (NotebookUnlocked && !Notebook.IsOpen && Input.GetKeyDown(KeyCode.Tab))
                StartCoroutine(Notebook.Show(false));

            if (!Paused) _elapsed += dt;
            Clicks.Tick();
            Hud.Tick(dt);
            if (Paused) _root.anchoredPosition = Vector2.zero;
            else UpdateShake(dt);
            UpdateLamp(dt);
            UpdateFlash(dt);
        }

        private void UpdateLamp(float dt)
        {
            _lampNoise += dt * 9f;
            float hum = 1f + (Mathf.PerlinNoise(_lampNoise, 0.5f) - 0.5f) * 0.12f;
            Color c = _lamp.color;
            c.a = 0.22f * LampLevel * hum;
            _lamp.color = c;
        }

        private void UpdateShake(float dt)
        {
            // Sem amplitude base: parado, o enquadramento permanece perfeitamente estável.
            float amplitude = _strain * 5f;
            if (_shakeTimer > 0f)
            {
                _shakeTimer = Mathf.Max(0f, _shakeTimer - dt);
                float fade = _shakeDuration > 0f ? Mathf.Clamp01(_shakeTimer / _shakeDuration) : 0f;
                amplitude += _shakeAmount * fade;
            }

            amplitude = AccessibilitySettings.ShakeIntensity(amplitude);
            if (amplitude <= 0.01f)
            {
                _root.anchoredPosition = Vector2.zero;
                return;
            }

            float motion = Time.unscaledTime * 17f;
            float x = Mathf.PerlinNoise(motion, 0.31f) - 0.5f;
            float y = Mathf.PerlinNoise(0.73f, motion) - 0.5f;
            _root.anchoredPosition = new Vector2(x, y) * (2f * amplitude);
        }

        private void UpdateFlash(float dt)
        {
            if (_flashTimer <= 0f) return;

            _flashTimer -= dt;
            float k = Mathf.Clamp01(_flashTimer / _flashDuration);
            Color c = _flash.color;
            c.a = _flashPeak * k * k;
            _flash.color = c;
        }
    }
}
