using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Core.Settings;

namespace Core.Minigames.Welding
{
    /// <summary>
    /// ETAPA FINAL - A SOLDA (várias peças).
    ///  - Clique na tocha (enrolada na mesa) pra pegar. E solta de novo (ela volta enrolada).
    ///  - ESPAÇO ou botão direito: abaixar / levantar a máscara. Sem máscara, acender o arco cega (perde pontos).
    ///  - Botão esquerdo (segurado): acende o arco. Siga a trilha com o mouse, sem parar demais (esquenta e fura).
    ///  - A / D (ou setas): move a peça na bancada (a mão esquerda segura a peça).
    ///  - Máscara gasta o ar. Pouco ar = mão treme + coração acelera. Tire a máscara pra respirar.
    ///  - Pedaços laranja = solda ruim: passe de novo (retrabalho).
    /// </summary>
    public sealed class WeldPhase : WeldingPhase
    {
        private enum BinState { Open, Good, Defect }

        private sealed class Bin
        {
            public Vector2 Pos;
            public BinState State;
            public float Progress;
            public float DistSum;
            public float Time;
            public float Age = 999f;
            public Image Cool;
            public Image Hot;
            public Image Guide;
        }

        private const int SparkCount = 32;

        private readonly List<Bin> _bins = new List<Bin>();
        private readonly Vector2[] _poly = new Vector2[16];
        private int _polyCount;

        private RectTransform _pieceRoot;
        private RectTransform _pieceBeads;
        private Image _pieceImage;
        private Image _leftHand;
        private Image _torchHeld;
        private Image _torchTable;
        private WeldingInput.Hit _torchHit;
        private Image _visor;
        private Image _tint;
        private Image _glow;
        private Image _arcLight;
        private Image _pulse;
        private RectTransform _oxygenFill;
        private RectTransform _heatFill;
        private Image _oxygenFillImage;
        private TextMeshProUGUI _progressText;

        private readonly Image[] _sparkImg = new Image[SparkCount];
        private readonly Vector2[] _sparkVel = new Vector2[SparkCount];
        private readonly float[] _sparkLife = new float[SparkCount];
        private int _sparkNext;

        private WeldEvents _events;

        private bool _torchIn;
        private bool _maskOn;
        private bool _arcOn;
        private float _visorT;
        private float _oxygen = 1f;
        private float _heat;
        private float _gaspTimer;
        private float _stun;
        private float _sinceProgress;
        private float _pieceX;
        private float _sparkSoundTimer;
        private float _beatTimer;
        private float _tremorSeed;
        private float _darkExposure;
        private bool _reworkMode;
        private string _lastHint = "";

        public WeldPhase(WeldingMinigame ctx) : base(ctx)
        {
        }

        public override string Title
        {
            get { return T("Soldar as peças", "Weld the parts"); }
        }

        public override IEnumerator Run()
        {
            WeldingSettings s = Ctx.Settings;

            CheckProcedure();

            Ctx.Hud.SetChecklistVisible(false);
            Ctx.ShowHands(false);
            BuildStaticUi();
            _events = new WeldEvents(Ctx, Ctx.Overlay);

            float qualitySum = 0f;
            int completedPieces = 0;
            int total = Mathf.Max(1, s.piecesToWeld);

            try
            {
                for (int i = 0; i < total; i++)
                {
                    if (Ctx.IsDefeated) break;

                    BuildPiece(i, total);
                    yield return SlideIn();
                    if (Ctx.IsDefeated) break;

                    yield return WeldOne(i, total);
                    if (Ctx.IsDefeated) break;

                    qualitySum += _lastQuality;
                    completedPieces++;
                    yield return SlideOut();
                }
            }
            finally
            {
                Cursor.visible = true;
                Ctx.Audio.StopLoop("arc");
                Ctx.Audio.StopLoop("breath");
                Ctx.Audio.StopLoop("gasp");
                Ctx.Audio.StopLoop("heart");
                Ctx.SetStrain(0f);
                Ctx.Hud.SetChecklistVisible(true);
            }

            Ctx.Result.weldQuality = completedPieces > 0 ? qualitySum / completedPieces : 0f;
            if (!Ctx.IsDefeated && completedPieces == total)
            {
                Ctx.Hud.SetHint(T("Todas as peças soldadas!", "All parts welded!"));
                yield return Ctx.Wait(0.8f);
            }
        }

        // ---------- Conferência do procedimento ----------

        private void CheckProcedure()
        {
            // Só pune aqui se uma etapa anterior foi desligada no Inspector ou pulada
            if (!Ctx.Has("mask"))
                Ctx.Penalize(Ctx.Settings.penaltyIgnoredHazard, "Começou a soldar sem a máscara.", "Started welding without the mask.");
            if (!Ctx.Has("gloves"))
                Ctx.Penalize(Ctx.Settings.penaltyIgnoredHazard, "Começou a soldar sem as luvas.", "Started welding without gloves.");
            if (!Ctx.Has("apron"))
                Ctx.Penalize(Ctx.Settings.penaltyIgnoredHazard, "Começou a soldar sem o avental.", "Started welding without the apron.");
        }

        // ---------- Interface fixa da solda ----------

        private void BuildStaticUi()
        {
            RectTransform layer = Ctx.Overlay;
            WeldingSettings s = Ctx.Settings;

            // Pulso vermelho (batimento) e escuro da máscara
            _pulse = WeldUI.MakeImage(layer, "Pulse", WeldUI.Vignette, new Color(0.8f, 0.05f, 0.05f, 0f));
            WeldUI.Fill(_pulse.rectTransform);
            _pulse.preserveAspect = false;

            _tint = WeldUI.MakeImage(layer, "VisorTint", null, new Color(0f, 0f, 0f, 0f));
            WeldUI.Fill(_tint.rectTransform);

            Sprite visorSprite = Ctx.Art.maskVisor != null ? Ctx.Art.maskVisor : WeldUI.Vignette;
            _visor = WeldUI.MakeImage(layer, "Visor", visorSprite, new Color(0.01f, 0.02f, 0.02f, 1f));
            WeldUI.Fill(_visor.rectTransform);
            _visor.preserveAspect = false;
            _visor.gameObject.SetActive(false);

            // Luz do arco iluminando a mesa toda (laranja) + brilho na ponta + faíscas
            _arcLight = WeldUI.MakeImage(layer, "ArcLight", WeldUI.Glow, new Color(1f, 0.7f, 0.35f, 0f));
            WeldUI.Place(_arcLight.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1500f, 1100f));
            _arcLight.preserveAspect = false;

            _glow = WeldUI.MakeImage(layer, "ArcGlow", WeldUI.Glow, new Color(1f, 0.9f, 0.65f, 0f));
            WeldUI.Place(_glow.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(320f, 320f));

            for (int i = 0; i < SparkCount; i++)
            {
                Image spark = WeldUI.MakeImage(layer, "Spark", WeldUI.Circle, new Color(1f, 0.8f, 0.3f, 0f));
                WeldUI.Place(spark.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(9f, 9f));
                spark.gameObject.SetActive(false);
                _sparkImg[i] = spark;
            }

            // Barras (oxigênio e calor)
            _oxygenFill = BuildBar("Oxygen", -250f, T("OXIGÊNIO", "OXYGEN"), new Color(0.35f, 0.8f, 1f, 1f), out _oxygenFillImage);
            Image dummy;
            _heatFill = BuildBar("Heat", 250f, T("CALOR", "HEAT"), new Color(1f, 0.55f, 0.1f, 1f), out dummy);

            _progressText = WeldUI.MakeText(layer, "Progress", "", 36f, WeldingMinigame.Paper, TextAlignmentOptions.Center);
            WeldUI.Place(_progressText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -225f), new Vector2(700f, 54f),
                new Vector2(0.5f, 1f));

            // Tocha na mão
            Sprite torch = Ctx.Art.handTorch;
            _torchHeld = WeldUI.MakeProp(layer, "TorchHeld", torch, Vector2.zero, s.torchSize, new Color(0.2f, 0.22f, 0.26f, 1f),
                T("TOCHA\nMIG/MAG", "MIG/MAG\nTORCH"), 34f);
            _torchHeld.rectTransform.pivot = s.torchTipPivot;
            _torchHeld.gameObject.SetActive(false);

            // Tocha enrolada na mesa (clique pra pegar)
            _torchTable = WeldUI.MakeProp(Ctx.Stage, "TorchTable", Ctx.Art.torchOnTable, new Vector2(620f, -300f), new Vector2(340f, 170f),
                new Color(0.25f, 0.27f, 0.3f, 1f), T("TOCHA\n(clique)", "TORCH\n(click)"), 30f);
            WeldUI.AddShadow(_torchTable, 0.4f);
        }

        private RectTransform BuildBar(string name, float x, string label, Color color, out Image fillImage)
        {
            RectTransform box = WeldUI.MakeRect(name, Ctx.Overlay);
            WeldUI.Place(box, new Vector2(0.5f, 0f), new Vector2(x, 135f), new Vector2(440f, 34f), new Vector2(0.5f, 0f));

            Image bg = WeldUI.MakeImage(box, "Bg", null, new Color(0.04f, 0.05f, 0.07f, 0.85f));
            WeldUI.Fill(bg.rectTransform);

            fillImage = WeldUI.MakeImage(box, "Fill", null, color);
            RectTransform fill = fillImage.rectTransform;
            fill.anchorMin = new Vector2(0f, 0f);
            fill.anchorMax = new Vector2(1f, 1f);
            fill.offsetMin = new Vector2(3f, 3f);
            fill.offsetMax = new Vector2(-3f, -3f);

            TextMeshProUGUI text = WeldUI.MakeText(box, "Label", label, 22f, Color.white, TextAlignmentOptions.Center);
            WeldUI.Fill(text.rectTransform);

            return fill;
        }

        private static void SetBar(RectTransform fill, float value)
        {
            fill.anchorMax = new Vector2(Mathf.Clamp01(value), 1f);
            fill.offsetMax = new Vector2(-3f, -3f);
        }

        // ---------- Peça ----------

        private float _lastQuality;

        private void BuildPiece(int index, int total)
        {
            WeldingSettings s = Ctx.Settings;

            _bins.Clear();
            _pieceX = 0f;
            _reworkMode = false;
            _heat = 0f;
            _sinceProgress = 0f;

            if (_pieceRoot != null) Object.Destroy(_pieceRoot.gameObject);

            _pieceRoot = WeldUI.MakeRect("PieceRoot", Ctx.Stage);
            WeldUI.Place(_pieceRoot, new Vector2(0.5f, 0.5f), s.piecePosition, s.pieceSize);

            Sprite sprite = Ctx.Art.pieceClean != null ? Ctx.Art.pieceClean : Ctx.Art.pieceDirty;
            _pieceImage = WeldUI.MakeProp(_pieceRoot, "Piece", sprite, Vector2.zero, s.pieceSize, new Color(0.46f, 0.48f, 0.52f, 1f),
                T("PEÇA ", "PART ") + (index + 1) + "/" + total, 52f);

            // Mão esquerda segurando a peça
            Sprite leftGlove = Ctx.Art.leftHandGloves != null ? Ctx.Art.leftHandGloves : Ctx.Art.handsGloves;
            _leftHand = WeldUI.MakeProp(_pieceRoot, "LeftHand", leftGlove, new Vector2(-s.pieceSize.x * 0.5f - 20f, -s.pieceSize.y * 0.3f),
                new Vector2(260f, 240f), new Color(0.45f, 0.3f, 0.18f, 1f), T("MÃO\nESQ.", "LEFT\nHAND"), 28f);

            Image pieceShadow = WeldUI.AddShadow(_pieceImage, 0.45f);
            if (pieceShadow != null) pieceShadow.transform.SetAsFirstSibling();

            _pieceBeads = WeldUI.MakeRect("Beads", _pieceRoot);
            WeldUI.Fill(_pieceBeads);

            BuildSeam();
            _pieceRoot.SetSiblingIndex(0);
        }

        private void BuildSeam()
        {
            WeldingSettings s = Ctx.Settings;
            Vector2[] src = s.seamPoints;
            int n = Mathf.Min(src.Length, _poly.Length);
            _polyCount = n;

            // Cada peça tem a trilha levemente diferente
            for (int i = 0; i < n; i++)
            {
                float jitter = (i == 0 || i == n - 1) ? 0f : Random.Range(-0.07f, 0.07f);
                Vector2 p = src[i];
                _poly[i] = new Vector2((p.x - 0.5f) * s.pieceSize.x, (p.y + jitter - 0.5f) * s.pieceSize.y);
            }

            float length = 0f;
            for (int i = 1; i < n; i++) length += Vector2.Distance(_poly[i - 1], _poly[i]);

            int count = Mathf.Max(4, Mathf.CeilToInt(length / Mathf.Max(4f, s.binLength)));

            for (int b = 0; b < count; b++)
            {
                float d = (b + 0.5f) / count * length;
                Bin bin = new Bin { Pos = PointAt(d), State = BinState.Open };

                float size = s.binLength * 1.5f;

                if (s.showSeamGuide)
                {
                    bin.Guide = WeldUI.MakeImage(_pieceBeads, "Guide", WeldUI.Circle, new Color(0.05f, 0.05f, 0.06f, 0.75f));
                    WeldUI.Place(bin.Guide.rectTransform, new Vector2(0.5f, 0.5f), bin.Pos, new Vector2(10f, 10f));
                }

                bin.Cool = WeldUI.MakeImage(_pieceBeads, "Cool", Ctx.Art.weldBeadCool != null ? Ctx.Art.weldBeadCool : WeldUI.Circle,
                    Ctx.Art.weldBeadCool != null ? Color.white : new Color(0.24f, 0.24f, 0.27f, 1f));
                WeldUI.Place(bin.Cool.rectTransform, new Vector2(0.5f, 0.5f), bin.Pos, new Vector2(size, size));
                bin.Cool.gameObject.SetActive(false);

                bin.Hot = WeldUI.MakeImage(_pieceBeads, "Hot", Ctx.Art.weldBeadHot != null ? Ctx.Art.weldBeadHot : WeldUI.Circle,
                    Ctx.Art.weldBeadHot != null ? Color.white : new Color(1f, 0.25f, 0.08f, 1f));
                WeldUI.Place(bin.Hot.rectTransform, new Vector2(0.5f, 0.5f), bin.Pos, new Vector2(size, size));
                bin.Hot.gameObject.SetActive(false);

                _bins.Add(bin);
            }
        }

        private Vector2 PointAt(float distance)
        {
            for (int i = 1; i < _polyCount; i++)
            {
                float seg = Vector2.Distance(_poly[i - 1], _poly[i]);
                if (distance <= seg || i == _polyCount - 1)
                    return Vector2.Lerp(_poly[i - 1], _poly[i], seg > 0.001f ? Mathf.Clamp01(distance / seg) : 0f);
                distance -= seg;
            }

            return _poly[0];
        }

        private float DistanceToSeam(Vector2 p)
        {
            float best = float.MaxValue;

            for (int i = 1; i < _polyCount; i++)
            {
                Vector2 a = _poly[i - 1];
                Vector2 b = _poly[i];
                Vector2 ab = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(0.0001f, ab.sqrMagnitude));
                float d = Vector2.Distance(p, a + ab * t);
                if (d < best) best = d;
            }

            return best;
        }

        private IEnumerator SlideIn()
        {
            Vector2 target = Ctx.Settings.piecePosition;
            Vector2 start = target + new Vector2(-1500f, 0f);
            Ctx.Audio.Play(Ctx.Sounds.pickup);

            float t = 0f;
            while (t < 0.5f)
            {
                if (Ctx.Paused) { yield return null; continue; }
                t += Time.unscaledDeltaTime;
                _pieceRoot.anchoredPosition = Vector2.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t / 0.5f));
                yield return null;
            }

            _pieceRoot.anchoredPosition = target;
        }

        private IEnumerator SlideOut()
        {
            Vector2 start = _pieceRoot.anchoredPosition;
            Vector2 end = start + new Vector2(0f, -900f);
            Ctx.Audio.Play(Ctx.Sounds.pickup);

            float t = 0f;
            while (t < 0.5f)
            {
                if (Ctx.Paused) { yield return null; continue; }
                t += Time.unscaledDeltaTime;
                _pieceRoot.anchoredPosition = Vector2.Lerp(start, end, Mathf.SmoothStep(0f, 1f, t / 0.5f));
                yield return null;
            }

            Ctx.Audio.Play(Ctx.Sounds.drop);
        }

        // ---------- Solda de uma peça ----------

        private IEnumerator WeldOne(int index, int total)
        {
            WeldingSettings s = Ctx.Settings;

            _torchIn = false;
            _darkExposure = 0f;
            _torchHeld.gameObject.SetActive(false);
            _torchTable.gameObject.SetActive(true);
            _torchHit = Ctx.Clicks.Register(_torchTable.rectTransform, PickTorch, null, 30);

            int goodBins = 0;
            bool firstPassDone = false;
            int defectsAtEnd = 0;

            while (true)
            {
                if (Ctx.IsDefeated) break;
                if (Ctx.Paused)
                {
                    _arcOn = false;
                    Ctx.Audio.SetLoop("arc", Ctx.Sounds.arcLoop, 0f);
                    yield return null;
                    continue;
                }

                float dt = Time.unscaledDeltaTime;

                HandleInput(dt);
                UpdateBody(dt);

                Vector2 screen = (Vector2)Input.mousePosition + Tremor();
                Vector2 overlayLocal;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(Ctx.Overlay, screen, null, out overlayLocal);
                Vector2 pieceLocal;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_pieceRoot, screen, null, out pieceLocal);

                UpdateArc();
                _events.Tick(dt, _arcOn);
                UpdateDarkExposure(dt);
                if (Ctx.IsDefeated) break;

                float oxygenTolerance = (_oxygen <= 0f && _maskOn ? 0.7f : 1f);
                float staminaTolerance = Mathf.Lerp(0.55f, 1f, Ctx.Result.stamina / 100f);
                float lightTolerance = _events.LightsOut ? 0.5f : 1f;
                float tolMul = oxygenTolerance * staminaTolerance * lightTolerance * _events.ToleranceMultiplier;
                float goodTol = s.seamGoodTolerance * tolMul;
                float maxTol = s.seamMaxTolerance * tolMul;

                bool progressed = false;
                float seamDist = DistanceToSeam(pieceLocal);

                if (_arcOn)
                {
                    progressed = _reworkMode
                        ? ProgressRework(dt, pieceLocal, maxTol)
                        : ProgressFirstPass(dt, pieceLocal, seamDist, goodTol, maxTol);
                }

                UpdateHeat(dt, progressed, seamDist, maxTol, pieceLocal);
                UpdateBeads(dt);
                UpdateVisuals(dt, overlayLocal, progressed);
                UpdateAudio(dt);
                UpdateHint();

                // Fim da primeira passada / retrabalho
                int open = 0;
                int defects = 0;
                goodBins = 0;
                for (int i = 0; i < _bins.Count; i++)
                {
                    if (_bins[i].State == BinState.Open) open++;
                    else if (_bins[i].State == BinState.Defect) defects++;
                    else goodBins++;
                }

                if (!firstPassDone && open == 0)
                {
                    firstPassDone = true;
                    _lastQuality = _bins.Count > 0 ? (float)goodBins / _bins.Count : 1f;
                    defectsAtEnd = defects;

                    if (defects > 0)
                    {
                        _reworkMode = true;
                        Ctx.Audio.Play(Ctx.Sounds.alert);
                        Ctx.Hud.Toast(T("Solda falhou em " + defects + " pontos (laranja). Refaça: retrabalho.",
                            "Weld failed in " + defects + " spots (orange). Redo them: rework."), WeldingMinigame.Accent, 3.5f);
                    }
                }

                _progressText.text = BuildProgressText(open, defects, index, total);

                if (firstPassDone && defects == 0) break;
                yield return null;
            }

            Ctx.Result.reworkCount += defectsAtEnd;
            if (!Ctx.IsDefeated && firstPassDone)
                _lastQuality = _bins.Count > 0 ? (float)goodBins / _bins.Count : 1f;
            if (!Ctx.IsDefeated && firstPassDone && defectsAtEnd == 0)
                Ctx.AdjustIntegrity(s.integrityRewardGoodWeld);

            // Terminou a peça: ela solta a tocha (volta enrolada) e tira a máscara
            _arcOn = false;
            if (_torchIn) DropTorch();
            if (_maskOn) SetMask(false);
            Ctx.Clicks.Unregister(_torchHit);

            if (!Ctx.IsDefeated)
            {
                Ctx.Audio.Play(Ctx.Sounds.stepDone);
                Ctx.Hud.Toast(T("Peça " + (index + 1) + " pronta!", "Part " + (index + 1) + " done!"), WeldingMinigame.Good);
                yield return Ctx.Wait(0.9f);
            }
        }

        private string BuildProgressText(int open, int defects, int index, int total)
        {
            string head = T("PEÇA ", "PART ") + (index + 1) + "/" + total + "   ";
            if (_reworkMode) return head + T("RETRABALHO: ", "REWORK: ") + defects + T(" pontos", " spots");
            int done = _bins.Count - open;
            return head + T("SOLDA ", "WELD ") + Mathf.RoundToInt(100f * done / Mathf.Max(1, _bins.Count)) + "%";
        }

        // ---------- Entrada ----------

        private void HandleInput(float dt)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(1))
            {
                if (Ctx.Has("mask")) SetMask(!_maskOn);
                else Ctx.Hud.Toast(T("Você não está com a máscara!", "You are not wearing the mask!"), WeldingMinigame.Bad);
            }

            if (Input.GetKeyDown(KeyCode.E) && _torchIn) DropTorch();

            float move = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) move -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) move += 1f;

            if (Mathf.Abs(move) > 0.01f)
            {
                // A mão esquerda acompanha a primeira ação de reposicionar a peça,
                // sem disparar uma animação a cada quadro enquanto a tecla fica pressionada.
                if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D) ||
                    Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow))
                    Ctx.AnimateHandAction(true, _pieceRoot.anchoredPosition);

                _pieceX = Mathf.Clamp(_pieceX + move * 300f * dt, -220f, 220f);
                _pieceRoot.anchoredPosition = Ctx.Settings.piecePosition + new Vector2(_pieceX, 0f);
            }
        }

        private void PickTorch()
        {
            if (_torchIn) return;

            Ctx.AnimateHandAction(false, _torchTable.rectTransform.anchoredPosition);
            _torchIn = true;
            _torchTable.gameObject.SetActive(false);
            _torchHit.Enabled = false;
            _torchHeld.gameObject.SetActive(true);
            Ctx.Audio.Play(Ctx.Sounds.pickup);
            Cursor.visible = false;
        }

        private void DropTorch()
        {
            Ctx.AnimateHandAction(false, _torchTable.rectTransform.anchoredPosition);
            _torchIn = false;
            _arcOn = false;
            _torchHeld.gameObject.SetActive(false);
            _torchTable.gameObject.SetActive(true);
            _torchHit.Enabled = true;
            Ctx.Audio.Play(Ctx.Sounds.drop);
            Cursor.visible = true;
        }

        private void SetMask(bool on)
        {
            Ctx.AnimateHandAction(false, new Vector2(0f, 250f));
            _maskOn = on;
            Ctx.Audio.Play(on ? Ctx.Sounds.maskDown : Ctx.Sounds.maskUp);
            if (!on) _gaspTimer = 0f;
        }

        // ---------- Arco ----------

        private void UpdateArc()
        {
            bool overUi = Ctx.Clicks.Hovered != null;

            if (_stun > 0f) _stun -= Time.unscaledDeltaTime;

            if (_torchIn && Input.GetMouseButtonDown(0) && !overUi && !_maskOn && !_events.BlocksArc && _stun <= 0f)
                ArcFlash();

            _arcOn = _torchIn && _maskOn && _stun <= 0f && !_events.BlocksArc && !overUi && Input.GetMouseButton(0);
        }

        private void ArcFlash()
        {
            Ctx.Penalize(Ctx.Settings.penaltyArcFlash, "Acendeu o arco sem a máscara: clarão nos olhos.",
                "Struck the arc without the mask: flash in the eyes.");
            Ctx.ApplyInjury(Ctx.Settings.arcFlashDamage,
                "Faíscas atingiram o rosto. A personagem sofreu queimaduras.",
                "Sparks hit the face. The character suffered burns.");
            Ctx.Audio.Play(Ctx.Sounds.flashBang);
            Ctx.Flash(new Color(1f, 0.82f, 0.62f, 1f), 0.72f, 0.8f);
            EmitSparks(new Vector2(0f, 315f), 0.18f);
            Ctx.Shake(14f, 0.4f);
            _stun = Ctx.Settings.arcFlashStun;
        }

        // ---------- Progresso da solda ----------

        private bool ProgressFirstPass(float dt, Vector2 p, float seamDist, float goodTol, float maxTol)
        {
            Bin bin = null;
            for (int i = 0; i < _bins.Count; i++)
            {
                if (_bins[i].State == BinState.Open)
                {
                    bin = _bins[i];
                    break;
                }
            }

            if (bin == null) return false;
            if (Vector2.Distance(p, bin.Pos) > maxTol) return false;

            bin.Progress += dt;
            bin.DistSum += seamDist * dt;
            bin.Time += dt;

            if (bin.Progress >= Ctx.Settings.binTime)
            {
                float avg = bin.Time > 0f ? bin.DistSum / bin.Time : 999f;
                bin.State = avg <= goodTol ? BinState.Good : BinState.Defect;
                bin.Age = 0f;
                ShowBead(bin);
                _events.OnBead();
            }

            return true;
        }

        private bool ProgressRework(float dt, Vector2 p, float maxTol)
        {
            Bin best = null;
            float bestDist = maxTol;

            for (int i = 0; i < _bins.Count; i++)
            {
                if (_bins[i].State != BinState.Defect) continue;
                float d = Vector2.Distance(p, _bins[i].Pos);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = _bins[i];
                }
            }

            if (best == null) return false;

            best.Progress += dt;
            if (best.Progress >= Ctx.Settings.binTime * 1.3f)
            {
                best.State = BinState.Good;
                best.Age = 0f;
                ShowBead(best);
            }

            return true;
        }

        private void ShowBead(Bin bin)
        {
            bin.Cool.gameObject.SetActive(true);
            bin.Hot.gameObject.SetActive(true);
            if (bin.Guide != null) bin.Guide.gameObject.SetActive(false);
        }

        private void UpdateHeat(float dt, bool progressed, float seamDist, float maxTol, Vector2 pieceLocal)
        {
            WeldingSettings s = Ctx.Settings;

            if (progressed) _sinceProgress = 0f;
            else _sinceProgress += dt;

            bool overheating = _arcOn && _sinceProgress > s.stallTime && seamDist <= maxTol * 1.5f;
            _heat = Mathf.Clamp01(_heat + (overheating ? s.heatRise : -s.heatFall) * dt);

            if (_heat >= 1f)
            {
                // Furou a chapa: marca o ponto como defeito
                Bin target = null;
                float bestDist = float.MaxValue;
                for (int i = 0; i < _bins.Count; i++)
                {
                    float d = Vector2.Distance(pieceLocal, _bins[i].Pos);
                    if (d < bestDist && (_bins[i].State == BinState.Good || _bins[i].State == BinState.Open))
                    {
                        bestDist = d;
                        target = _bins[i];
                    }
                }

                if (target != null)
                {
                    target.State = BinState.Defect;
                    target.Progress = 0f;
                    target.Age = 0f;
                    ShowBead(target);
                }

                _heat = 0.35f;
                Ctx.Hud.Toast(T("FUROU A CHAPA! Não pare no mesmo lugar.", "BURNED THROUGH! Do not stall in one place."), WeldingMinigame.Bad);
                Ctx.Audio.Play(Ctx.Sounds.alert);
                Ctx.Shake(12f, 0.35f);
            }
        }

        private void UpdateBeads(float dt)
        {
            float cool = Mathf.Max(0.1f, Ctx.Settings.coolSeconds);
            bool hasCoolArt = Ctx.Art.weldBeadCool != null;
            float pulse = 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 8f);

            for (int i = 0; i < _bins.Count; i++)
            {
                Bin bin = _bins[i];
                if (bin.State == BinState.Open) continue;

                bin.Age += dt;
                float hot = Mathf.Clamp01(1f - bin.Age / cool);
                bin.Hot.color = new Color(bin.Hot.color.r, bin.Hot.color.g, bin.Hot.color.b, hot);

                if (bin.State == BinState.Defect)
                {
                    Color defect = new Color(1f, 0.55f, 0.1f, 1f) * (_reworkMode ? pulse : 0.85f);
                    defect.a = 1f;
                    bin.Cool.color = defect;
                }
                else
                {
                    bin.Cool.color = hasCoolArt ? Color.white : new Color(0.24f, 0.24f, 0.27f, 1f);
                }
            }
        }

        // ---------- Corpo: oxigênio, tremor, coração ----------

        private void UpdateBody(float dt)
        {
            WeldingSettings s = Ctx.Settings;
            float oxygenLack = 1f - _oxygen;
            float stamina01 = Mathf.Clamp01(Ctx.Result.stamina / 100f);

            if (_maskOn)
            {
                // Stamina baixa piora a eficiência respiratória: o oxigênio termina mais rápido.
                float breathingPenalty = 1f + (1f - stamina01) * 0.65f;
                float drain = dt / Mathf.Max(1f, s.oxygenSeconds) * (_arcOn ? s.oxygenArcMultiplier : 1f) * breathingPenalty;
                _oxygen = Mathf.Max(0f, _oxygen - drain);

                float workDrain = s.staminaWorkDrain * (_arcOn ? 1f : 0.32f);
                workDrain += oxygenLack * 0.75f;
                Ctx.AdjustStamina(-workDrain * dt);

                if (_oxygen <= 0f)
                {
                    _gaspTimer += dt;
                    if (_gaspTimer >= s.gaspGraceSeconds)
                    {
                        SetMask(false);
                        Ctx.Penalize(s.penaltyBreathless, "Ficou sem ar e precisou tirar a máscara.", "Ran out of air and had to take the mask off.");
                        Ctx.ApplyInjury(s.breathlessDamage, "A falta de oxigênio causou mal-estar.", "Oxygen deprivation caused physical distress.");
                        Ctx.Shake(10f, 0.4f);
                    }
                }
            }
            else
            {
                _oxygen = Mathf.Min(1f, _oxygen + dt / Mathf.Max(0.5f, s.oxygenRefillSeconds));
                _gaspTimer = 0f;
                Ctx.AdjustStamina(s.staminaRecovery * dt);
            }

            Ctx.SetOxygen(_oxygen);

            // Batimentos aceleram conforme a queda do oxigênio.
            float lack = Mathf.Clamp01((0.6f - _oxygen) / 0.6f);
            if (lack > 0.01f)
            {
                float bpm = Mathf.Lerp(80f, 175f, lack);
                _beatTimer += dt * bpm / 60f;
                if (_beatTimer >= 1f)
                {
                    _beatTimer -= 1f;
                    Ctx.Shake(2f + 4f * lack, 0.14f);
                }
            }
        }

        private void UpdateDarkExposure(float dt)
        {
            if (!_arcOn || _events == null || !_events.LightsOut)
            {
                _darkExposure = 0f;
                return;
            }

            _darkExposure += dt;
            float interval = Mathf.Max(0.5f, Ctx.Settings.darkExposureInterval);
            if (_darkExposure < interval) return;
            _darkExposure -= interval;

            if (Random.value < Ctx.Settings.darkInjuryChance)
            {
                Ctx.ApplyInjury(Ctx.Settings.darkWeldingDamage,
                    "Soldou no escuro e se feriu com respingos e faíscas.",
                    "Welding in the dark caused an injury from spatter and sparks.");
                EmitSparks(new Vector2(0f, 300f), 0.16f);
                Ctx.Flash(new Color(1f, 0.48f, 0.16f, 1f), 0.22f, 0.24f);
            }
        }

        private Vector2 Tremor()
        {
            float oxygenLack = Mathf.Clamp01((0.6f - _oxygen) / 0.6f);
            float staminaLack = 1f - Mathf.Clamp01(Ctx.Result.stamina / 100f);
            float lack = Mathf.Max(oxygenLack, staminaLack * 0.65f);
            float amount = lack * lack * Ctx.Settings.tremorMax;
            if (amount < 0.01f) return Vector2.zero;

            _tremorSeed += Time.unscaledDeltaTime * 18f;
            return new Vector2(Mathf.PerlinNoise(_tremorSeed, 0.3f) - 0.5f, Mathf.PerlinNoise(0.7f, _tremorSeed) - 0.5f) * 2f * amount;
        }

        // ---------- Visual ----------

        private void UpdateVisuals(float dt, Vector2 overlayLocal, bool progressed)
        {
            // Visor desce/sobe
            _visorT = Mathf.MoveTowards(_visorT, _maskOn ? 1f : 0f, dt / 0.18f);
            _visor.gameObject.SetActive(_visorT > 0.001f);
            _visor.rectTransform.offsetMin = new Vector2(0f, 0f);
            _visor.rectTransform.anchoredPosition = new Vector2(0f, (1f - _visorT) * 600f);
            _tint.color = new Color(0f, 0f, 0f, 0.55f * _visorT);

            // Tocha segue o mouse (a ponta fica no cursor)
            if (_torchIn)
            {
                _torchHeld.rectTransform.anchoredPosition = overlayLocal;
            }

            // Brilho do arco
            if (_arcOn)
            {
                float flick = 0.8f + Random.value * 0.2f;
                _arcLight.rectTransform.anchoredPosition = overlayLocal;
                _arcLight.color = new Color(1f, 0.72f, 0.38f, AccessibilitySettings.FlashIntensity(0.2f * flick));
                _glow.rectTransform.anchoredPosition = overlayLocal;
                _glow.color = new Color(1f, 0.92f, 0.7f, AccessibilitySettings.FlashIntensity(flick));
                EmitSparks(overlayLocal, dt);
            }
            else
            {
                _glow.color = new Color(1f, 0.92f, 0.7f, 0f);
                _arcLight.color = new Color(1f, 0.72f, 0.38f, 0f);
            }

            UpdateSparks(dt);

            // Barras
            SetBar(_oxygenFill, _oxygen);
            _oxygenFillImage.color = _oxygen > 0.3f ? new Color(0.35f, 0.8f, 1f, 1f) : new Color(1f, 0.3f, 0.25f, 1f);
            SetBar(_heatFill, _heat);

            // Pulso vermelho do coração
            float lack = Mathf.Clamp01((0.6f - _oxygen) / 0.6f);
            float pulse = lack * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(_beatTimer * Mathf.PI)), 4f) * 0.7f;
            _pulse.color = new Color(0.8f, 0.05f, 0.05f, AccessibilitySettings.FlashIntensity(pulse));

            // Efeito de esforço (sem ar) na câmera: leve tremor constante
            if (_events.BlocksArc == false) Ctx.SetStrain(lack * 0.35f);
        }

        private void EmitSparks(Vector2 origin, float dt)
        {
            float rate = AccessibilitySettings.ReduceFlashes ? 28f : 90f;
            int count = Mathf.RoundToInt(rate * dt + Random.value);

            for (int i = 0; i < count; i++)
            {
                int k = _sparkNext;
                _sparkNext = (_sparkNext + 1) % SparkCount;

                Image img = _sparkImg[k];
                img.gameObject.SetActive(true);
                img.rectTransform.anchoredPosition = origin;
                _sparkVel[k] = new Vector2(Random.Range(-420f, 420f), Random.Range(80f, 520f));
                _sparkLife[k] = Random.Range(0.25f, 0.6f);
                img.color = new Color(1f, Random.Range(0.6f, 0.9f), 0.25f, 1f);
            }
        }

        private void UpdateSparks(float dt)
        {
            for (int i = 0; i < SparkCount; i++)
            {
                if (_sparkLife[i] <= 0f) continue;

                _sparkLife[i] -= dt;
                if (_sparkLife[i] <= 0f)
                {
                    _sparkImg[i].gameObject.SetActive(false);
                    continue;
                }

                _sparkVel[i].y -= 1100f * dt;
                _sparkImg[i].rectTransform.anchoredPosition += _sparkVel[i] * dt;
                Color c = _sparkImg[i].color;
                c.a = Mathf.Clamp01(_sparkLife[i] * 3f);
                _sparkImg[i].color = c;
            }
        }

        // ---------- Áudio ----------

        private void UpdateAudio(float dt)
        {
            WeldingSounds snd = Ctx.Sounds;

            Ctx.Audio.SetLoop("arc", snd.arcLoop, _arcOn ? 0.85f : 0f);

            if (_arcOn)
            {
                _sparkSoundTimer -= dt;
                if (_sparkSoundTimer <= 0f)
                {
                    _sparkSoundTimer = Random.Range(0.08f, 0.2f);
                    Ctx.Audio.PlayRandom(snd.sparks, 0.6f);
                }
            }

            float lack = Mathf.Clamp01((0.6f - _oxygen) / 0.6f);
            float breath = (!_maskOn && _oxygen < 0.999f) ? 0.8f : (_maskOn && _oxygen < 0.3f ? 0.35f : 0f);
            Ctx.Audio.SetLoop("breath", snd.breathingLoop, breath);
            Ctx.Audio.SetLoop("gasp", snd.gaspLoop, (_maskOn && _oxygen <= 0f) ? 1f : 0f);
            Ctx.Audio.SetLoop("heart", snd.heartbeatLoop, lack * 0.9f);
        }

        // ---------- Dica ----------

        private void UpdateHint()
        {
            string hint;

            if (_events.BlocksArc) return; // o evento escreve a própria dica

            if (!_torchIn)
                hint = T("Clique na TOCHA (na mesa) pra pegar. A/D move a peça.", "Click the TORCH (on the table) to pick it up. A/D moves the part.");
            else if (!_maskOn)
                hint = T("ESPAÇO / botão direito: abaixar a MÁSCARA. Depois segure o botão esquerdo pra soldar.",
                    "SPACE / right click: lower the MASK. Then hold left click to weld.");
            else if (_oxygen <= 0f)
                hint = T("SEM AR! Tire a máscara (ESPAÇO) e respire!", "OUT OF AIR! Lift the mask (SPACE) and breathe!");
            else if (_reworkMode)
                hint = T("Passe de novo nos pontos LARANJA. E = soltar a tocha.", "Go over the ORANGE spots again. E = put the torch down.");
            else
                hint = T("Segure o botão esquerdo e siga a trilha. ESPAÇO: respirar. E: soltar a tocha. A/D: mover a peça.",
                    "Hold left click and follow the seam. SPACE: breathe. E: put torch down. A/D: move part.");

            if (hint != _lastHint)
            {
                _lastHint = hint;
                Ctx.Hud.SetHint(hint);
            }
        }
    }
}
