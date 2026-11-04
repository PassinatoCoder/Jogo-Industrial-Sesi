using System;
using System.Collections.Generic;
using Core.Settings;
using Core.UI.Menu;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Core.UI.Options
{
    /// <summary>
    /// MENU DE OPÇÕES (substitui o tablet).
    ///
    /// Esquerda: abas empilhadas (Vídeo, Áudio, Acessibilidade, Controles, Jogabilidade, Idioma).
    /// Direita: o painel da aba selecionada. Mover entre abas troca o painel NA HORA.
    ///
    /// Teclado / controle:
    ///   W/S ou setas ........ move entre abas (ou entre opções, dentro do painel)
    ///   Q/E (LB/RB) ......... troca de aba de qualquer lugar
    ///   D / seta direita / Enter ... entra no painel
    ///   A/D ou setas ........ ajusta a opção selecionada
    ///   Enter ............... confirma
    ///   Esc (B) ............. volta (do painel pras abas; das abas pro menu principal)
    /// Mouse: só CLIQUE (de propósito não existe "mouse em cima"). Roda do mouse rola o painel.
    ///
    /// Toda a interface é criada por código. Só coloque este script num objeto vazio (veja o GUIA).
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    [DisallowMultipleComponent]
    public sealed class OptionsMenu : MonoBehaviour
    {
        // Tempo (s) até a faixa de transição terminar de revelar a tela
        private const float OpenDelay = 0.3f;

        private enum Focus
        {
            Tabs,
            Panel
        }

        private sealed class Repeater
        {
            private bool _held;
            private float _next;

            public bool Pulse(bool down, float now)
            {
                if (!down)
                {
                    _held = false;
                    return false;
                }

                if (!_held)
                {
                    _held = true;
                    _next = now + 0.38f;
                    return true;
                }

                if (now >= _next)
                {
                    _next = now + 0.075f;
                    return true;
                }

                return false;
            }

            public void Reset() => _held = false;
        }

        [Header("Áudio (arraste o AudioMixer do jogo)")]
        [SerializeField] private AudioMixer mixer;
        [SerializeField] private string masterParameter = "MasterVol";
        [SerializeField] private string musicParameter = "BGMVol";
        [SerializeField] private string effectsParameter = "SFXVol";

        [Header("Aparência")]
        [SerializeField] private OptionsTheme theme = new OptionsTheme();

        [Header("Fundo por seção (opcional, na ordem das abas)")]
        [Tooltip("Vídeo, Áudio, Acessibilidade, Controles, Jogabilidade, Idioma. Deixe vazio pra não ter fundo.")]
        [SerializeField] private Sprite[] sectionBackgrounds;

        [Header("Tutorial (aba Jogabilidade)")]
        [Tooltip("Vazio = usa lições de exemplo.")]
        [SerializeField] private List<TutorialLesson> lessons = new List<TutorialLesson>();
        [Tooltip("Ligado: só mostra lições que o jogador já aprendeu (TutorialProgress.Unlock).")]
        [SerializeField] private bool lockLessonsUntilTaught;

        /// <summary>Disparado quando o jogador pede pra sair (Esc nas abas ou botão Voltar).</summary>
        public event Action CloseRequested;

        private CanvasGroup _group;
        private OptionsChrome _chrome;
        private OptionsBackground _background;
        private OptionsModal _modal;
        private CanvasGroup _backGroup;
        private List<OptionsSection> _sections = new List<OptionsSection>();
        private readonly List<OptionsTab> _tabs = new List<OptionsTab>();
        private List<TutorialLesson> _defaultLessons;

        private readonly Repeater _up = new Repeater();
        private readonly Repeater _down = new Repeater();
        private readonly Repeater _left = new Repeater();
        private readonly Repeater _right = new Repeater();

        private Focus _focus = Focus.Tabs;
        private int _tab;
        private int _shownIndex = -1;
        private bool _built;
        private bool _open;
        private bool _rebuildPending;
        private bool _refreshPending;
        private bool _usingPad;
        private bool _axisFailed;
        private int _styleSignature;
        private float _ignoreInputUntil;
        private float _intro;
        private Color _accent;
        private Color _accentTarget;

        // ---------- Acesso pelas outras classes ----------

        public OptionsTheme Theme => theme;
        public OptionsStyle Style { get; private set; }
        public Color Accent => _accent;
        public float RowHeight => theme.rowHeight;
        public AudioMixer Mixer => mixer;
        public OptionsModal Modal => _modal;
        public bool IsOpen => _open;
        public bool LockLessons => lockLessonsUntilTaught;

        public IReadOnlyList<TutorialLesson> Lessons
        {
            get
            {
                if (lessons != null && lessons.Count > 0) return lessons;
                if (_defaultLessons == null) _defaultLessons = TutorialLesson.CreateDefaults();
                return _defaultLessons;
            }
        }

        public float ViewportHeight
        {
            get { return _chrome != null && _chrome.ViewportRoot != null ? _chrome.ViewportRoot.rect.height : 0f; }
        }

        private OptionsSection CurrentSection => _tab >= 0 && _tab < _sections.Count ? _sections[_tab] : null;

        // ---------- Ciclo de vida ----------

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();

            RectTransform rt = (RectTransform)transform;
            OptionsUI.Fill(rt);

            ApplyMixerSettings();
            if (!_built) BuildAll();
        }

        private void OnEnable()
        {
            GameLanguage.Changed += OnStyleSettingChanged;
            MenuReadability.Changed += OnStyleSettingChanged;
            AccessibilitySettings.Changed += OnStyleSettingChanged;
            GameKeyBindings.Changed += OnValuesChanged;
            TutorialProgress.Changed += OnValuesChanged;
        }

        private void OnDisable()
        {
            GameLanguage.Changed -= OnStyleSettingChanged;
            MenuReadability.Changed -= OnStyleSettingChanged;
            AccessibilitySettings.Changed -= OnStyleSettingChanged;
            GameKeyBindings.Changed -= OnValuesChanged;
            TutorialProgress.Changed -= OnValuesChanged;
        }

        private void OnStyleSettingChanged()
        {
            if (ComputeSignature() != _styleSignature) _rebuildPending = true;
            else _refreshPending = true;
        }

        private void OnValuesChanged() => _refreshPending = true;

        private int ComputeSignature()
        {
            int sig = (int)GameLanguage.Current;
            sig = sig * 4 + (MenuReadability.ReadableTitles ? 1 : 0);
            sig = sig * 4 + AccessibilitySettings.TextSizeIndex;
            sig = sig * 4 + (AccessibilitySettings.HighContrast ? 1 : 0);
            return sig;
        }

        private void ApplyMixerSettings()
        {
            GameAudioSettings.MasterParameter = masterParameter;
            GameAudioSettings.MusicParameter = musicParameter;
            GameAudioSettings.EffectsParameter = effectsParameter;
            GameAudioSettings.ApplyAll(mixer);
        }

        // ---------- Abrir / fechar ----------

        /// <summary>Mostra o menu (chame com a tela coberta pela transição).</summary>
        public void Open()
        {
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            if (!_built) BuildAll();

            ApplyMixerSettings();
            EventSystem es = EventSystem.current;
            if (es != null) es.SetSelectedGameObject(null);

            _open = true;
            _focus = Focus.Tabs;
            _tab = 0;
            _shownIndex = -1;
            // Espera a faixa de transição revelar a tela antes de começar a animação de entrada
            _intro = -OpenDelay / 0.6f;
            _ignoreInputUntil = Time.unscaledTime + 0.25f;
            _accent = _accentTarget = theme.AccentFor(0);
            ResetRepeaters();

            foreach (OptionsSection section in _sections)
            {
                section.CancelInputs();
                section.Show(false, true);
            }

            for (int i = 0; i < _tabs.Count; i++)
            {
                _tabs[i].SetState(i == _tab, true);
                _tabs[i].SetShown(true, OpenDelay + 0.06f + i * 0.055f);
            }

            _chrome.SetIntro(0f);
            SelectTab(_tab, false, OpenDelay);
        }

        /// <summary>Esconde o menu e cancela qualquer coisa pendente.</summary>
        public void Close()
        {
            _open = false;
            if (_modal != null) _modal.Dismiss(true);
            foreach (OptionsSection section in _sections) section.CancelInputs();
        }

        /// <summary>Pede pra sair. Quem controla a tela (MainMenuController) escuta CloseRequested.</summary>
        public void RequestClose()
        {
            if (!_open || (_modal != null && _modal.Active)) return;

            PlayConfirm();
            if (CloseRequested != null) CloseRequested.Invoke();
        }

        // ---------- Montagem ----------

        private void BuildAll()
        {
            Style = OptionsStyle.Create(theme);
            _styleSignature = ComputeSignature();
            _accent = _accentTarget = theme.AccentFor(_tab);

            _chrome = new OptionsChrome(this);
            _chrome.Build(transform);

            _background = new OptionsBackground(_chrome.BackgroundRoot);

            _sections = new List<OptionsSection>
            {
                new VideoSection(),
                new AudioSection(),
                new AccessibilitySection(),
                new ControlsSection(),
                new GameplaySection(),
                new LanguageSection()
            };

            for (int i = 0; i < _sections.Count; i++)
            {
                _sections[i].Accent = theme.AccentFor(i);
                _sections[i].Initialize(this, _chrome.ViewportRoot, _chrome.ExtrasRoot);
            }

            _tabs.Clear();
            for (int i = 0; i < _sections.Count; i++)
                _tabs.Add(new OptionsTab(this, _chrome.TabsRoot, i, _sections[i].Title));

            BuildBackButton(_chrome.TabsRoot);

            _modal = new OptionsModal(this);
            _modal.Build(_chrome.OverlayRoot);

            _built = true;
        }

        private void BuildBackButton(Transform parent)
        {
            float y = -(_sections.Count * (theme.tabHeight + theme.tabSpacing)) - 22f;

            RectTransform rt = OptionsUI.Rect("BackButton", parent);
            OptionsUI.Place(rt, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, y),
                new Vector2(theme.tabWidth * 0.62f, 58f));
            _backGroup = rt.gameObject.AddComponent<CanvasGroup>();
            _backGroup.alpha = 0f;

            Image bg = OptionsUI.Image(rt, "Bg", Style.InkSoft, true);
            OptionsUI.Fill(bg.rectTransform);
            OptionsUI.Skew(bg, theme.skew);
            OptionsPointerRelay relay = bg.gameObject.AddComponent<OptionsPointerRelay>();
            relay.Click = _ => RequestClose();

            TMP_Text label = OptionsUI.Text(rt, "Label", "<   " + Style.Caps(GameLanguage.T("Voltar", "Back")),
                Style.Display, Style.Size(28), Style.Muted, TextAlignmentOptions.MidlineLeft);
            OptionsUI.Fill(label.rectTransform, 38f, 0f, 10f, 0f);
        }

        private void Rebuild()
        {
            int tab = _tab;
            Focus focus = _focus;
            int[] focusIndexes = new int[_sections.Count];
            for (int i = 0; i < _sections.Count; i++) focusIndexes[i] = _sections[i].FocusIndex;

            if (_modal != null) _modal.Dismiss(true);

            List<GameObject> old = new List<GameObject>();
            foreach (Transform child in transform) old.Add(child.gameObject);
            foreach (GameObject go in old)
            {
                go.SetActive(false);
                Destroy(go);
            }

            _tab = tab;
            BuildAll();

            for (int i = 0; i < _sections.Count && i < focusIndexes.Length; i++)
                if (focusIndexes[i] >= 0) _sections[i].SetFocusIndex(focusIndexes[i]);

            _focus = focus;
            _shownIndex = -1;
            _intro = 1f;
            _chrome.SetIntro(1f);
            SelectTab(_tab, true);
            foreach (OptionsTab t in _tabs) t.Snap();
        }

        // ---------- Atualização ----------

        private void Update()
        {
            if (!_built) return;

            if (_rebuildPending)
            {
                _rebuildPending = false;
                _refreshPending = false;
                Rebuild();
            }

            if (_refreshPending)
            {
                _refreshPending = false;
                if (CurrentSection != null) CurrentSection.RefreshRows();
            }

            if (!_open) return;

            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.033f);

            _intro = MenuMotion.ReduceMotion ? 1f : Mathf.Min(1f, _intro + dt / 0.6f);
            _chrome.SetIntro(_intro);
            _backGroup.alpha = MenuEasing.OutCubic(Mathf.Clamp01((_intro - 0.3f) / 0.7f));

            HandleInput();

            _accent = MenuMotion.ReduceMotion
                ? _accentTarget
                : Color.Lerp(_accent, _accentTarget, 1f - Mathf.Exp(-10f * dt));

            foreach (OptionsTab tab in _tabs) tab.Tick(dt);
            foreach (OptionsSection section in _sections) section.Tick(dt);
            _chrome.Tick(dt);
            _background.Tick(dt);
            _modal.Tick(dt);

            UpdateHints();
        }

        // ---------- Entrada (teclado / controle / roda do mouse) ----------

        private bool InputAllowed()
        {
            if (_modal.Active) return false;
            if (_group != null && !_group.interactable) return false;
            return Time.unscaledTime >= _ignoreInputUntil;
        }

        private void HandleInput()
        {
            if (!InputAllowed())
            {
                ResetRepeaters();
                return;
            }

            OptionsSection section = CurrentSection;
            if (section == null) return;

            DetectDevice();

            // Linha esperando uma tecla (remapear) lê o teclado sozinha
            OptionRow row = section.FocusedRow;
            if (_focus == Focus.Panel && row != null && row.ConsumesInput)
            {
                row.UpdateInput();
                ResetRepeaters();
                return;
            }

            float now = Time.unscaledTime;
            float vertical = ReadAxis("Vertical");
            float horizontal = ReadAxis("Horizontal");

            bool upPulse = _up.Pulse(Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) || vertical > 0.6f, now);
            bool downPulse = _down.Pulse(Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) || vertical < -0.6f, now);
            bool leftPulse = _left.Pulse(Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) || horizontal < -0.6f, now);
            bool rightPulse = _right.Pulse(Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) || horizontal > 0.6f, now);

            bool submit = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) ||
                          Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.JoystickButton0);
            bool cancel = Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace) ||
                          Input.GetKeyDown(KeyCode.JoystickButton1);

            // Q / E (LB / RB): troca de aba de qualquer lugar
            if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.JoystickButton4)) MoveTab(-1);
            else if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.JoystickButton5)) MoveTab(1);
            else if (_focus == Focus.Tabs)
            {
                if (upPulse) MoveTab(-1);
                else if (downPulse) MoveTab(1);
                else if ((rightPulse || submit) && section.HasFocusable) EnterPanel();
                else if (cancel) RequestClose();
            }
            else
            {
                section = CurrentSection;
                row = section.FocusedRow;

                if (upPulse) MoveRow(-1);
                else if (downPulse) MoveRow(1);
                else if (leftPulse)
                {
                    if (row != null && row.HandlesHorizontal)
                    {
                        if (row.IsEnabled) row.Adjust(-1);
                    }
                    else LeaveToTabs();
                }
                else if (rightPulse)
                {
                    if (row != null && row.HandlesHorizontal && row.IsEnabled) row.Adjust(1);
                }
                else if (submit)
                {
                    if (row != null && row.IsEnabled) row.Activate();
                }
                else if (cancel) LeaveToTabs();
            }

            float wheel = Input.mouseScrollDelta.y;
            if (Mathf.Abs(wheel) > 0.01f && CurrentSection != null) CurrentSection.ScrollBy(-wheel * 90f);
        }

        private float ReadAxis(string axis)
        {
            if (_axisFailed) return 0f;

            try
            {
                return Input.GetAxisRaw(axis);
            }
            catch (Exception)
            {
                // Eixo não existe no Input Manager do projeto: usa só o teclado
                _axisFailed = true;
                return 0f;
            }
        }

        private void DetectDevice()
        {
            for (int k = (int)KeyCode.JoystickButton0; k <= (int)KeyCode.JoystickButton19; k++)
            {
                if (Input.GetKeyDown((KeyCode)k))
                {
                    _usingPad = true;
                    return;
                }
            }

            if (Input.anyKeyDown) _usingPad = false;
        }

        private void ResetRepeaters()
        {
            _up.Reset();
            _down.Reset();
            _left.Reset();
            _right.Reset();
        }

        // ---------- Navegação ----------

        private void MoveTab(int direction)
        {
            int count = _sections.Count;
            if (count == 0) return;

            SelectTab((_tab + direction + count) % count, false);
            PlayTick();
        }

        private void SelectTab(int index, bool instant, float extraDelay = 0f)
        {
            index = Mathf.Clamp(index, 0, _sections.Count - 1);
            OptionsSection section = _sections[index];
            bool animate = !instant && extraDelay <= 0f;

            if (_shownIndex >= 0 && _shownIndex != index)
            {
                _sections[_shownIndex].CancelInputs();
                _sections[_shownIndex].Show(false, instant);
            }

            if (_shownIndex != index) section.Show(true, instant, extraDelay);

            _shownIndex = index;
            _tab = index;
            _accentTarget = section.Accent;

            if (_focus == Focus.Panel && !section.HasFocusable) _focus = Focus.Tabs;

            _chrome.SetSection(index, _sections.Count, section.Title, section.Accent, animate);
            if (sectionBackgrounds != null && index < sectionBackgrounds.Length)
                _background.Show(sectionBackgrounds[index], !animate);

            section.ApplyFocus(_focus == Focus.Panel);
            UpdateTabsState();
            UpdateDescription(animate);
        }

        private void EnterPanel()
        {
            OptionsSection section = CurrentSection;
            if (section == null || !section.HasFocusable) return;

            _focus = Focus.Panel;
            if (section.FocusIndex < 0) section.SetFocusIndex(section.FirstFocusable());
            section.ApplyFocus(true);
            UpdateTabsState();
            UpdateDescription(true);
            PlayTick();
        }

        private void LeaveToTabs()
        {
            OptionsSection section = CurrentSection;
            _focus = Focus.Tabs;

            if (section != null)
            {
                section.CancelInputs();
                section.ApplyFocus(false);
            }

            UpdateTabsState();
            UpdateDescription(true);
            PlayTick();
        }

        private void MoveRow(int direction)
        {
            OptionsSection section = CurrentSection;
            if (section == null || !section.MoveFocus(direction)) return;

            section.ApplyFocus(true);
            UpdateDescription(true);
            PlayTick();
        }

        private void FocusRowInternal(int index)
        {
            OptionsSection section = CurrentSection;
            if (section == null) return;

            _focus = Focus.Panel;
            section.SetFocusIndex(index);
            section.ApplyFocus(true);
            UpdateTabsState();
            UpdateDescription(true);
        }

        private void UpdateTabsState()
        {
            for (int i = 0; i < _tabs.Count; i++)
                _tabs[i].SetState(i == _tab, _focus == Focus.Tabs);
        }

        private void UpdateDescription(bool animate)
        {
            OptionsSection section = CurrentSection;
            if (section == null) return;

            string text = section.Subtitle;
            if (_focus == Focus.Panel)
            {
                OptionRow row = section.FocusedRow;
                if (row != null && !string.IsNullOrEmpty(row.Description)) text = row.Description;
            }

            _chrome.SetDescription(text, animate);
        }

        // ---------- Chamadas vindas do mouse ----------

        public void TabClicked(int index)
        {
            if (!_open || _modal.Active) return;

            OptionsSection section = CurrentSection;
            if (_focus == Focus.Panel && section != null)
            {
                section.CancelInputs();
                section.ApplyFocus(false);
            }

            _focus = Focus.Tabs;
            if (index != _tab) SelectTab(index, false);
            else
            {
                UpdateTabsState();
                UpdateDescription(true);
            }

            PlayConfirm();
        }

        public void RowClicked(OptionRow row)
        {
            if (!_open || _modal.Active || CurrentSection == null) return;

            int index = CurrentSection.IndexOf(row);
            if (index < 0) return;

            FocusRowInternal(index);
            if (row.ActivatesOnClick && row.IsEnabled) row.Activate();
        }

        public void RowFocusFromMouse(OptionRow row)
        {
            if (!_open || _modal.Active || CurrentSection == null) return;

            int index = CurrentSection.IndexOf(row);
            if (index >= 0) FocusRowInternal(index);
        }

        public void RowAdjustClicked(OptionRow row, int direction)
        {
            if (!_open || _modal.Active || CurrentSection == null) return;

            int index = CurrentSection.IndexOf(row);
            if (index < 0) return;

            FocusRowInternal(index);
            if (row.IsEnabled) row.Adjust(direction);
        }

        public void RefreshCurrentSection()
        {
            if (CurrentSection != null) CurrentSection.RefreshRows();
        }

        // ---------- Dicas de tecla ----------

        private void UpdateHints()
        {
            OptionsSection section = CurrentSection;
            OptionRow row = section != null ? section.FocusedRow : null;
            string text;

            if (_modal.Active)
            {
                text = _usingPad
                    ? GameLanguage.T("[Direcional] Escolher   [A] Confirmar\n[B] Reverter", "[D-Pad] Choose   [A] Confirm\n[B] Revert")
                    : GameLanguage.T("[A/D] Escolher   [Enter] Confirmar\n[Esc] Reverter", "[A/D] Choose   [Enter] Confirm\n[Esc] Revert");
            }
            else if (_focus == Focus.Panel && row != null && row.ConsumesInput)
            {
                text = GameLanguage.T("Aperte a nova tecla\n[Esc] Cancelar", "Press the new key\n[Esc] Cancel");
            }
            else if (_focus == Focus.Tabs)
            {
                text = _usingPad
                    ? GameLanguage.T("[Direcional] Seção   [A] Entrar\n[LB/RB] Trocar   [B] Voltar", "[D-Pad] Section   [A] Enter\n[LB/RB] Switch   [B] Back")
                    : GameLanguage.T("[W/S] Seção   [Enter] Entrar\n[Q/E] Trocar   [Esc] Voltar", "[W/S] Section   [Enter] Enter\n[Q/E] Switch   [Esc] Back");
            }
            else
            {
                text = _usingPad
                    ? GameLanguage.T("[Direcional] Navegar   [A] Confirmar\n[LB/RB] Seção   [B] Seções", "[D-Pad] Navigate   [A] Confirm\n[LB/RB] Section   [B] Sections")
                    : GameLanguage.T("[W/S] Opção   [A/D] Ajustar   [Enter] Confirmar\n[Q/E] Seção   [Esc] Seções", "[W/S] Option   [A/D] Adjust   [Enter] Confirm\n[Q/E] Section   [Esc] Sections");
            }

            _chrome.SetHints(text);
        }

        // ---------- Sons ----------

        public void PlayTick()
        {
            if (Core.Audio.MenuAudioManager.Instance != null) Core.Audio.MenuAudioManager.Instance.PlaySFX_Hover();
        }

        public void PlayConfirm()
        {
            if (Core.Audio.MenuAudioManager.Instance != null) Core.Audio.MenuAudioManager.Instance.PlaySFX_Click();
        }
    }
}
