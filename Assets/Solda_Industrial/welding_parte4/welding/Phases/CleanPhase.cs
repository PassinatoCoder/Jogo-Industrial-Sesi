using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Minigames.Welding
{
    /// <summary>
    /// ETAPA 2 - Limpar a área (NR-12/NR-23). O lixo muda a cada partida (mais ou menos).
    ///  - Clique no lixo / coisa que atrapalha: ela pega com a mão e é jogado fora.
    ///  - Arraste as ferramentas pra "bancada de ferramentas" (organizar).
    ///  - Aperte PRONTO. Se ainda sobrou bagunça, perde pontos (começou sem checar a área).
    /// </summary>
    public sealed class CleanPhase : WeldingPhase
    {
        private sealed class Junk
        {
            public Image Image;
            public WeldingInput.Hit Hit;
            public bool IsTool;
            public bool Done;
            public Vector2 Home;
        }

        private readonly List<Junk> _items = new List<Junk>();
        private Junk _grab;
        private bool _readyPressed;
        private RectTransform _zone;

        public CleanPhase(WeldingMinigame ctx) : base(ctx)
        {
        }

        public override string Title
        {
            get { return T("Limpar a área", "Clear the area"); }
        }

        public override IEnumerator Run()
        {
            _items.Clear();
            _grab = null;
            _readyPressed = false;

            BuildScene();
            Ctx.Hud.SetHint(T("Jogue fora o que atrapalha (clique) e arrume as ferramentas (arraste). Depois: PRONTO.",
                "Throw out what is in the way (click) and tidy the tools (drag). Then: READY."));

            while (!_readyPressed && !Ctx.IsDefeated)
            {
                if (Ctx.Paused) { yield return null; continue; }
                UpdateDrag();
                yield return null;
            }

            if (Ctx.IsDefeated) yield break;

            int left = Remaining();
            if (left > 0)
            {
                Ctx.Penalize(Ctx.Settings.penaltyRushedArea,
                    "Disse que a área estava pronta, mas ainda havia " + left + " coisas atrapalhando.",
                    "Said the area was ready, but " + left + " things were still in the way.");
                Ctx.Shake(10f, 0.3f);
                yield return Ctx.Wait(1.2f);
            }
            else
            {
                Ctx.Hud.Toast(T("Área limpa e propícia ao trabalho!", "Area clear and ready for work!"), WeldingMinigame.Good, 2f);
                Ctx.Audio.Play(Ctx.Sounds.stepDone);
                Ctx.AdjustIntegrity(Ctx.Settings.integrityRewardCleanArea);
                yield return Ctx.Wait(0.8f);
            }
        }

        private int Remaining()
        {
            int n = 0;
            for (int i = 0; i < _items.Count; i++)
                if (!_items[i].Done) n++;
            return n;
        }

        // ---------- Montagem ----------

        private void BuildScene()
        {
            // Zona das ferramentas
            Image zone = WeldUI.MakeImage(Ctx.Stage, "ToolZone", null, new Color(0.2f, 0.6f, 0.3f, 0.35f));
            WeldUI.Place(zone.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(600f, -60f), new Vector2(520f, 210f));
            _zone = zone.rectTransform;
            TextMeshProUGUI zoneLabel = WeldUI.MakeText(zone.transform, "Label", T("BANCADA DE FERRAMENTAS", "TOOL BENCH"), 26f,
                WeldingMinigame.Paper, TextAlignmentOptions.Top);
            WeldUI.Fill(zoneLabel.rectTransform, 0f, 0f, 0f, 8f);

            // Posições espalhadas na mesa
            List<Vector2> slots = new List<Vector2>();
            for (int x = 0; x < 4; x++)
                for (int y = 0; y < 2; y++)
                    slots.Add(new Vector2(-780f + x * 260f + Random.Range(-30f, 30f), -170f - y * 150f + Random.Range(-20f, 20f)));

            for (int i = slots.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                Vector2 tmp = slots[i];
                slots[i] = slots[j];
                slots[j] = tmp;
            }

            int junkCount = Random.Range(3, 6);
            int slotIndex = 0;

            for (int i = 0; i < junkCount; i++)
            {
                int type = Random.Range(0, 3);
                Sprite sprite = type == 0 ? Ctx.Art.rustyScrap : (type == 1 ? Ctx.Art.sprayCan : Ctx.Art.oilPuddle);
                string label = type == 0 ? T("FERRO\nENFERRUJADO", "RUSTY\nSCRAP") : (type == 1 ? T("LATA DE\nSPRAY", "SPRAY\nCAN") : T("POÇA\nDE ÓLEO", "OIL\nPUDDLE"));
                Color color = type == 0 ? new Color(0.55f, 0.3f, 0.15f, 1f) : (type == 1 ? new Color(0.8f, 0.2f, 0.15f, 1f) : new Color(0.08f, 0.08f, 0.1f, 1f));
                AddItem(sprite, color, label, slots[slotIndex++], false);
            }

            string[] tools = { T("CHAVE", "WRENCH"), T("ESQUADRO", "SQUARE"), T("PAQUÍMETRO", "CALIPER") };
            for (int i = 0; i < 3 && slotIndex < slots.Count; i++)
                AddItem(null, new Color(0.35f, 0.4f, 0.55f, 1f), tools[i], slots[slotIndex++], true);

            // Botão PRONTO
            Image button = WeldUI.MakeProp(Ctx.Stage, "Ready", null, new Vector2(640f, 280f), new Vector2(380f, 110f),
                WeldingMinigame.Good, T("PRONTO", "READY"), 44f);
            TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null) text.color = WeldingMinigame.Ink;
            Ctx.Clicks.Register(button.rectTransform, () => _readyPressed = true, null, 180);
        }

        private void AddItem(Sprite sprite, Color color, string label, Vector2 pos, bool tool)
        {
            Junk item = new Junk { IsTool = tool, Home = pos };
            item.Image = WeldUI.MakeProp(Ctx.Stage, tool ? "Tool" : "Junk", sprite, pos, tool ? new Vector2(190f, 90f) : new Vector2(190f, 150f),
                color, label, 24f);

            Junk captured = item;
            if (tool) item.Hit = Ctx.Clicks.Register(item.Image.rectTransform, null, () => _grab = captured, 20);
            else item.Hit = Ctx.Clicks.Register(item.Image.rectTransform, () => Discard(captured), null, 10);

            _items.Add(item);
        }

        // ---------- Jogar fora e arrastar ----------

        private void Discard(Junk item)
        {
            if (item.Done) return;
            item.Done = true;
            Ctx.Clicks.Unregister(item.Hit);
            Ctx.Audio.Play(Ctx.Sounds.pickup);
            Ctx.Audio.Play(Ctx.Sounds.trashThrow);
            Vector2 target = item.Image != null ? item.Image.rectTransform.anchoredPosition : Vector2.zero;
            Ctx.StartCoroutine(Ctx.ReachWithHand(false, target, 0.18f));
            Ctx.StartCoroutine(PopAway(item.Image));
        }

        private IEnumerator PopAway(Image image)
        {
            if (image == null) yield break;

            RectTransform rt = image.rectTransform;
            Vector2 start = rt.anchoredPosition;
            float t = 0f;
            while (t < 0.22f && image != null)
            {
                if (Ctx.Paused) { yield return null; continue; }
                t += Time.unscaledDeltaTime;
                float k = t / 0.22f;
                rt.anchoredPosition = start + new Vector2(0f, 140f * k);
                rt.localScale = Vector3.one * (1f - k);
                yield return null;
            }

            if (image != null) Object.Destroy(image.gameObject);
        }

        private void UpdateDrag()
        {
            if (_grab == null) return;

            Junk item = _grab;
            _grab = null;
            if (item.Done) return;

            Ctx.Audio.Play(Ctx.Sounds.pickup);
            Ctx.AnimateHandAction(false, item.Image != null ? item.Image.rectTransform.anchoredPosition : item.Home);
            Ctx.StartCoroutine(Drag(item));
        }

        private IEnumerator Drag(Junk item)
        {
            RectTransform rt = item.Image.rectTransform;
            rt.SetAsLastSibling();

            while (Input.GetMouseButton(0) && item.Image != null && !Ctx.IsDefeated)
            {
                if (Ctx.Paused) { yield return null; continue; }
                Vector2 local;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(Ctx.Stage, Input.mousePosition, null, out local);
                rt.anchoredPosition = local;
                yield return null;
            }

            if (Ctx.IsDefeated || item.Image == null) yield break;

            if (RectTransformUtility.RectangleContainsScreenPoint(_zone, Input.mousePosition, null))
            {
                item.Done = true;
                Ctx.Clicks.Unregister(item.Hit);
                Ctx.Audio.Play(Ctx.Sounds.drop);

                int placed = 0;
                for (int i = 0; i < _items.Count; i++)
                    if (_items[i].IsTool && _items[i].Done) placed++;

                rt.anchoredPosition = _zone.anchoredPosition + new Vector2(-170f + (placed - 1) * 170f, -20f);
                Ctx.AnimateHandAction(false, rt.anchoredPosition);
            }
            else
            {
                Ctx.Audio.Play(Ctx.Sounds.drop);
            }
        }
    }
}
