using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Minigames.Welding
{
    /// <summary>
    /// ETAPA 3 - Biombo e extintor (NR-12 / NR-23). Setas estilo FNAF: ela "olha" pra um lado, a tela pisca.
    ///  CIMA: o biombo está fechado. Abre (pode EMPERRAR: aperte ESPAÇO várias vezes, a câmera treme).
    ///  DIREITA: o extintor. Confira o medidor. Vazio = troque. Depois teste o gatilho (pode emperrar).
    /// </summary>
    public sealed class SafetyPhase : WeldingPhase
    {
        private bool _clicked;

        public SafetyPhase(WeldingMinigame ctx) : base(ctx)
        {
        }

        public override string Title
        {
            get { return T("Biombo e extintor", "Screen and extinguisher"); }
        }

        public override IEnumerator Run()
        {
            Ctx.SetWorkstationView();
            Ctx.Hud.SetHint(T("Olhe PARA FRENTE para inspecionar o biombo da área de solda.",
                "Look STRAIGHT AHEAD to inspect the welding-area screen."));
            yield return WaitArrow(90f, new Vector2(0f, 360f));
            yield return Ctx.Blink(0, () =>
            {
                Ctx.ClearStage();
                Ctx.SetWallView(false);
            });
            yield return ScreenView();

            Ctx.Hud.SetHint(T("Agora olhe para a DIREITA: o extintor fica preso à parede lateral.",
                "Now look RIGHT: the extinguisher is mounted on the side wall."));
            yield return WaitArrow(0f, new Vector2(860f, 0f));
            yield return Ctx.Blink(Ctx.Settings.footstepsPerWalk, () =>
            {
                Ctx.ClearStage();
                Ctx.SetWallView(true);
            });
            yield return ExtinguisherView();

            Ctx.Hud.SetHint(T("Inspeção concluída. Volte para a bancada.", "Inspection complete. Return to the workbench."));
            yield return WaitArrow(180f, new Vector2(-860f, 0f));
            yield return Ctx.Blink(Ctx.Settings.footstepsPerWalk, () =>
            {
                Ctx.ClearStage();
                Ctx.SetWorkstationView();
            });
            Ctx.Hud.SetHint(T("Área protegida. Você pode iniciar o trabalho.", "Area secured. You may begin work."));
            yield return Ctx.Wait(0.5f);
        }

        // ---------- Setas ----------

        private IEnumerator WaitArrow(float angle, Vector2 position)
        {
            Image arrow = WeldUI.MakeImage(Ctx.Overlay, "Arrow", WeldUI.Circle, new Color(1f, 0.82f, 0.1f, 1f));
            WeldUI.Place(arrow.rectTransform, new Vector2(0.5f, 0.5f), position, new Vector2(170f, 170f));

            TextMeshProUGUI symbol = WeldUI.MakeText(arrow.transform, "Symbol", ">", 150f, WeldingMinigame.Ink, TextAlignmentOptions.Center);
            WeldUI.Fill(symbol.rectTransform);
            symbol.rectTransform.localEulerAngles = new Vector3(0f, 0f, angle);

            _clicked = false;
            WeldingInput.Hit hit = Ctx.Clicks.Register(arrow.rectTransform, () => _clicked = true, null, 220);

            float t = 0f;
            while (!_clicked)
            {
                if (Ctx.Paused) { yield return null; continue; }
                t += Time.unscaledDeltaTime;
                float pulse = 0.65f + 0.35f * Mathf.Sin(t * 5f);
                arrow.color = new Color(1f, 0.82f, 0.1f, pulse);
                arrow.rectTransform.localScale = Vector3.one * (1f + 0.05f * Mathf.Sin(t * 5f));
                yield return null;
            }

            Ctx.Audio.Play(Ctx.Sounds.uiClick);
            Ctx.Clicks.Unregister(hit);
            UnityEngine.Object.Destroy(arrow.gameObject);
        }

        // ---------- Biombo ----------

        private IEnumerator ScreenView()
        {
            Ctx.Hud.SetHint(T("O biombo está FECHADO. Clique nele pra abrir.", "The screen is CLOSED. Click it to open."));

            BuildWallDecoration(false);
            Image screen = WeldUI.MakeProp(Ctx.Stage, "Screen", Ctx.Art.screenClosed, new Vector2(0f, 30f), new Vector2(980f, 700f),
                new Color(0.4f, 0.42f, 0.46f, 1f), T("BIOMBO\nFECHADO", "SCREEN\nCLOSED"), 56f);

            _clicked = false;
            WeldingInput.Hit hit = Ctx.Clicks.Register(screen.rectTransform, () => _clicked = true, null, 90);
            while (!_clicked)
            {
                if (Ctx.Paused) { yield return null; continue; }
                yield return null;
            }
            Ctx.Clicks.Unregister(hit);

            if (UnityEngine.Random.value < Ctx.Settings.screenJamChance)
            {
                Ctx.Audio.Play(Ctx.Sounds.screenStuck);
                Ctx.Shake(14f, 0.4f);
                Ctx.Hud.Toast(T("O BIOMBO EMPERROU!", "THE SCREEN IS JAMMED!"), WeldingMinigame.Bad, 3f);
                yield return Mash(T("FORÇA! ESPAÇO!", "PULL! SPACE!"));
            }

            Ctx.Audio.Play(Ctx.Sounds.screenOpen);
            WeldUI.SetProp(screen, Ctx.Art.screenOpen, new Color(0.25f, 0.55f, 0.3f, 1f), T("BIOMBO\nABERTO", "SCREEN\nOPEN"), 56f);
            Ctx.Hud.Toast(T("Biombo aberto.", "Screen open."), WeldingMinigame.Good);
            yield return Ctx.Wait(0.9f);
        }

        // ---------- Extintor ----------

        private IEnumerator ExtinguisherView()
        {
            bool empty = UnityEngine.Random.value < Ctx.Settings.extinguisherEmptyChance;

            BuildWallDecoration(true);
            Image ext = WeldUI.MakeProp(Ctx.Stage, "Extinguisher", empty ? Ctx.Art.extinguisherEmpty : Ctx.Art.extinguisherFull,
                new Vector2(530f, 10f), new Vector2(340f, 620f), new Color(0.8f, 0.12f, 0.1f, 1f),
                T("EXTINTOR", "EXTINGUISHER"), 40f);

            WeldUI.AddShadow(ext, 0.4f);
            Ctx.Hud.SetHint(T("Confira o MEDIDOR do extintor (clique nele).", "Check the extinguisher GAUGE (click it)."));

            Image gauge = WeldUI.MakeImage(Ctx.Overlay, "Gauge", WeldUI.Circle, new Color(0.95f, 0.95f, 0.9f, 1f));
            WeldUI.Place(gauge.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(530f, 150f), new Vector2(130f, 130f));
            TextMeshProUGUI gaugeText = WeldUI.MakeText(gauge.transform, "Label", T("MEDIDOR", "GAUGE"), 22f, WeldingMinigame.Ink, TextAlignmentOptions.Center);
            WeldUI.Fill(gaugeText.rectTransform);

            _clicked = false;
            WeldingInput.Hit gaugeHit = Ctx.Clicks.Register(gauge.rectTransform, () => _clicked = true, null, 180);
            while (!_clicked)
            {
                if (Ctx.Paused) { yield return null; continue; }
                yield return null;
            }
            Ctx.Clicks.Unregister(gaugeHit);
            Ctx.Audio.Play(Ctx.Sounds.extinguisherCheck);

            if (empty)
            {
                gauge.color = WeldingMinigame.Bad;
                Ctx.Hud.Toast(T("MEDIDOR NO VERMELHO: extintor VAZIO!", "GAUGE IN THE RED: extinguisher EMPTY!"), WeldingMinigame.Bad, 3f);
                Ctx.Shake(8f, 0.3f);
                Ctx.Hud.SetHint(T("Clique em TROCAR pra buscar outro extintor.", "Click SWAP to fetch another extinguisher."));

                Image swap = MakeButton("Swap", new Vector2(-300f, -240f), T("TROCAR", "SWAP"), WeldingMinigame.Accent);
                _clicked = false;
                WeldingInput.Hit swapHit = Ctx.Clicks.Register(swap.rectTransform, () => _clicked = true, null, 220);
                while (!_clicked)
                {
                    if (Ctx.Paused) { yield return null; continue; }
                    yield return null;
                }
                Ctx.Clicks.Unregister(swapHit);
                UnityEngine.Object.Destroy(swap.gameObject);

                Ctx.Audio.Play(Ctx.Sounds.extinguisherSwap);
                yield return Ctx.Blink(Ctx.Settings.footstepsPerWalk, () =>
                {
                    WeldUI.SetProp(ext, Ctx.Art.extinguisherFull, new Color(0.8f, 0.12f, 0.1f, 1f), T("EXTINTOR\n(NOVO)", "EXTINGUISHER\n(NEW)"), 40f);
                    gauge.color = WeldingMinigame.Good;
                });
                Ctx.Hud.Toast(T("Extintor trocado: carga cheia.", "Extinguisher swapped: fully charged."), WeldingMinigame.Good);
            }
            else
            {
                gauge.color = WeldingMinigame.Good;
                Ctx.Hud.Toast(T("Medidor no verde: carga OK.", "Gauge in the green: charge OK."), WeldingMinigame.Good);
            }

            // Teste do gatilho: pode emperrar
            Ctx.Hud.SetHint(T("Teste o gatilho: clique em PUXAR O PINO.", "Test the trigger: click PULL THE PIN."));
            Image pin = MakeButton("Pin", new Vector2(-300f, -240f), T("PUXAR\nO PINO", "PULL\nTHE PIN"), WeldingMinigame.Accent);
            _clicked = false;
            WeldingInput.Hit pinHit = Ctx.Clicks.Register(pin.rectTransform, () => _clicked = true, null, 220);
            while (!_clicked)
            {
                if (Ctx.Paused) { yield return null; continue; }
                yield return null;
            }
            Ctx.Clicks.Unregister(pinHit);
            UnityEngine.Object.Destroy(pin.gameObject);

            if (UnityEngine.Random.value < Ctx.Settings.extinguisherJamChance)
            {
                Ctx.Audio.Play(Ctx.Sounds.extinguisherStuck);
                Ctx.Shake(16f, 0.5f);
                Ctx.Hud.Toast(T("O GATILHO EMPERROU! Desemperre!", "THE TRIGGER JAMMED! Free it!"), WeldingMinigame.Bad, 3f);
                yield return Mash(T("DESEMPERRE! ESPAÇO!", "FREE IT! SPACE!"));
            }

            Ctx.Audio.Play(Ctx.Sounds.extinguisherSpray);
            Ctx.Hud.Toast(T("Extintor funcionando. Área protegida.", "Extinguisher working. Area protected."), WeldingMinigame.Good);
            yield return Ctx.Wait(1f);
        }

        private void BuildWallDecoration(bool rightSide)
        {
            // A mesa some nesta vista. O painel e os detalhes dão escala de parede, não de bancada.
            Image wall = WeldUI.MakeImage(Ctx.Stage, "WallSurface", null,
                new Color(0.16f, 0.19f, 0.22f, 0.96f));
            WeldUI.Place(wall.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 30f),
                new Vector2(1680f, 900f));
            wall.transform.SetAsFirstSibling();

            Image rail = WeldUI.MakeImage(Ctx.Stage, "WallRail", null, new Color(0.38f, 0.42f, 0.45f, 1f));
            WeldUI.Place(rail.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -320f),
                new Vector2(1680f, 22f));

            if (rightSide)
            {
                Image sign = WeldUI.MakeProp(Ctx.Stage, "ExtinguisherSign", null, new Vector2(530f, 420f),
                    new Vector2(440f, 100f), new Color(0.68f, 0.12f, 0.1f, 1f),
                    T("EQUIPAMENTO DE INCÊNDIO", "FIRE EQUIPMENT"), 25f);
            }
            else
            {
                TextMeshProUGUI label = WeldUI.MakeText(Ctx.Stage, "WallLabel",
                    T("PROTEÇÃO DA ÁREA DE SOLDA", "WELDING AREA PROTECTION"), 30f,
                    new Color(0.86f, 0.88f, 0.9f, 0.9f), TextAlignmentOptions.Center);
                WeldUI.Place(label.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -40f),
                    new Vector2(1000f, 50f), new Vector2(0.5f, 1f));
            }
        }

        private Image MakeButton(string name, Vector2 pos, string label, Color color)
        {
            Image button = WeldUI.MakeProp(Ctx.Overlay, name, null, pos, new Vector2(300f, 130f), color, label, 38f);
            TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null) text.color = WeldingMinigame.Ink;
            return button;
        }

        // ---------- Minigame de força (câmera treme) ----------

        private IEnumerator Mash(string label)
        {
            WeldingSettings s = Ctx.Settings;
            float value = 0f;

            RectTransform box = WeldUI.MakeRect("Mash", Ctx.Overlay);
            WeldUI.Place(box, new Vector2(0.5f, 0.5f), new Vector2(0f, 250f), new Vector2(900f, 200f));

            TextMeshProUGUI text = WeldUI.MakeText(box, "Text", label, 80f, WeldingMinigame.Paper, TextAlignmentOptions.Top);
            WeldUI.Fill(text.rectTransform);

            Image bg = WeldUI.MakeImage(box, "BarBg", null, new Color(0.04f, 0.05f, 0.07f, 0.9f));
            WeldUI.Place(bg.rectTransform, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(800f, 44f), new Vector2(0.5f, 0f));
            Image fill = WeldUI.MakeImage(bg.transform, "Fill", null, WeldingMinigame.Accent);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(0f, 1f);
            fill.rectTransform.offsetMin = new Vector2(4f, 4f);
            fill.rectTransform.offsetMax = new Vector2(-4f, -4f);

            float goal = Mathf.Max(3f, s.mashGoal);
            float t = 0f;

            while (value < goal)
            {
                if (Ctx.Paused) { yield return null; continue; }
                float dt = Time.unscaledDeltaTime;
                t += dt;

                if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
                {
                    value += 1f;
                    Ctx.Audio.Play(Ctx.Sounds.screenStrain);
                    Ctx.Shake(9f, 0.12f);
                }

                value = Mathf.Max(0f, value - s.mashDecay * dt);
                Ctx.SetStrain(0.4f + 0.6f * Mathf.Clamp01(value / goal));

                fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(value / goal), 1f);
                float a = 0.6f + 0.4f * Mathf.Sin(t * 12f);
                text.color = new Color(1f, 1f, 1f, WeldingMinigame_Alpha(a));
                yield return null;
            }

            Ctx.SetStrain(0f);
            UnityEngine.Object.Destroy(box.gameObject);
        }

        private static float WeldingMinigame_Alpha(float a)
        {
            return Core.Settings.AccessibilitySettings.ReduceFlashes ? Mathf.Lerp(0.85f, 1f, a) : a;
        }
    }
}
