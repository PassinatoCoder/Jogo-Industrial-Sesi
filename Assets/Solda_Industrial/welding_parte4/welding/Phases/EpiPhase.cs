using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Minigames.Welding
{
    /// <summary>
    /// ETAPA 1 - EPIs (NR-6). O chefe pede um item por vez. O jogador clica no item certo:
    /// só o braço aparece em cima do item, ela pisca, e o EPI está vestido.
    /// O AVENTAL tem um mini-jogo de amarrar (arrastar as duas alças ao centro e puxar o nó pra baixo).
    /// </summary>
    public sealed class EpiPhase : WeldingPhase
    {
        private sealed class Item
        {
            public string Id;
            public string Name;
            public bool Real;
            public Sprite Sprite;
            public Color Color;
            public string Label;
            public Image Image;
            public WeldingInput.Hit Hit;
        }

        private readonly List<Item> _items = new List<Item>();
        private Item _clicked;

        public EpiPhase(WeldingMinigame ctx) : base(ctx)
        {
        }

        public override string Title
        {
            get { return T("Vestir os EPIs", "Put on PPE"); }
        }

        public override IEnumerator Run()
        {
            _items.Clear();
            _clicked = null;

            BuildItems();

            // Ordem fixa: avental, máscara, luvas (as mãos voltam cobertas no fim)
            List<string> order = new List<string> { "apron", "mask", "gloves" };
            Ctx.ShowHands(true);

            for (int step = 0; step < order.Count && !Ctx.IsDefeated; step++)
            {
                Item wanted = Find(order[step]);
                Ctx.Hud.SetHint(T("CHEFE: pegue ", "BOSS: grab ") + wanted.Name.ToUpperInvariant() + "  (" + (step + 1) + "/3)");

                bool done = false;
                while (!done && !Ctx.IsDefeated)
                {
                    if (Ctx.Paused) { yield return null; continue; }
                    HighlightHovered();
                    yield return null;

                    if (_clicked == null) continue;

                    Item picked = _clicked;
                    _clicked = null;

                    if (picked == wanted)
                    {
                        done = true;
                    }
                    else if (!picked.Real)
                    {
                        Ctx.Penalize(Ctx.Settings.penaltyWrongEpi,
                            "EPI errado: " + picked.Name + " não protege na solda.",
                            "Wrong PPE: " + picked.Name + " does not protect while welding.");
                        Ctx.Shake(10f, 0.25f);
                    }
                    else
                    {
                        Ctx.Hud.Toast(T("Ainda não: siga o pedido do chefe.", "Not yet: follow the boss's order."), WeldingMinigame.Accent);
                        Ctx.Audio.Play(Ctx.Sounds.drop);
                    }
                }

                if (Ctx.IsDefeated) break;
                yield return Equip(wanted);
            }

            if (Ctx.IsDefeated) yield break;

            Ctx.Hud.SetHint(T("Tudo certo! EPIs vestidos.", "All set! PPE on."));
            yield return Ctx.Wait(0.8f);
            yield return Ctx.Blink(Ctx.Settings.footstepsPerWalk, () => Ctx.ShowHands(false));
        }

        // ---------- Montagem da mesa ----------

        private void BuildItems()
        {
            WeldingArt art = Ctx.Art;

            _items.Add(new Item
            {
                Id = "mask", Name = T("a máscara de solda", "the welding mask"), Real = true, Sprite = art.epiMask,
                Color = new Color(0.62f, 0.65f, 0.7f, 1f), Label = T("MÁSCARA\nDE SOLDA", "WELDING\nMASK")
            });
            _items.Add(new Item
            {
                Id = "gloves", Name = T("as luvas de raspa", "the leather gloves"), Real = true, Sprite = art.epiGloves,
                Color = new Color(0.45f, 0.3f, 0.18f, 1f), Label = T("LUVAS\nDE RASPA", "LEATHER\nGLOVES")
            });
            _items.Add(new Item
            {
                Id = "apron", Name = T("o avental de raspa", "the leather apron"), Real = true, Sprite = art.epiApron,
                Color = new Color(0.28f, 0.28f, 0.3f, 1f), Label = T("AVENTAL\nDE RASPA", "LEATHER\nAPRON")
            });

            if (Ctx.EpiDecoys)
            {
                _items.Add(new Item
                {
                    Id = "decoyGloves", Name = T("a luva de látex", "the latex glove"), Real = false, Sprite = art.epiDecoyGloves,
                    Color = new Color(0.9f, 0.9f, 0.95f, 1f), Label = T("LUVA\nDE LÁTEX", "LATEX\nGLOVE")
                });
                _items.Add(new Item
                {
                    Id = "decoyGlasses", Name = T("o óculos comum", "the plain glasses"), Real = false, Sprite = art.epiDecoyGlasses,
                    Color = new Color(0.7f, 0.85f, 0.9f, 1f), Label = T("ÓCULOS\nCOMUM", "PLAIN\nGLASSES")
                });
            }

            // Embaralha as posições na mesa
            List<Item> shuffled = new List<Item>(_items);
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                Item tmp = shuffled[i];
                shuffled[i] = shuffled[j];
                shuffled[j] = tmp;
            }

            int n = shuffled.Count;
            float gap = Mathf.Min(420f, 1500f / n);

            for (int i = 0; i < n; i++)
            {
                Item item = shuffled[i];
                Vector2 pos = new Vector2((i - (n - 1) * 0.5f) * gap, -230f);

                item.Image = WeldUI.MakeProp(Ctx.Stage, "Epi_" + item.Id, item.Sprite, pos, new Vector2(260f, 260f),
                    item.Color, item.Label, 30f);
                WeldUI.AddShadow(item.Image, 0.4f);
                Item captured = item;
                item.Hit = Ctx.Clicks.Register(item.Image.rectTransform, () => _clicked = captured, null, 10);
            }
        }

        private Item Find(string id)
        {
            for (int i = 0; i < _items.Count; i++)
                if (_items[i].Id == id) return _items[i];
            return null;
        }

        private void HighlightHovered()
        {
            WeldingInput.Hit hovered = Ctx.Clicks.Hovered;

            for (int i = 0; i < _items.Count; i++)
            {
                Item item = _items[i];
                if (item.Image == null || !item.Image.gameObject.activeSelf) continue;
                float target = hovered == item.Hit ? 1.08f : 1f;
                float s = Mathf.Lerp(item.Image.rectTransform.localScale.x, target, 0.3f);
                item.Image.rectTransform.localScale = new Vector3(s, s, 1f);
            }
        }

        // ---------- Pegar e vestir ----------

        private IEnumerator Equip(Item item)
        {
            Ctx.Clicks.Locked = true;
            Ctx.Audio.Play(Ctx.Sounds.pickup);

            // Só o braço aparece em cima do item
            Vector2 itemPos = item.Image.rectTransform.anchoredPosition;
            Sprite armSprite = Ctx.Art.rightHandBare != null ? Ctx.Art.rightHandBare : Ctx.Art.handsBare;
            Image arm = WeldUI.MakeProp(Ctx.Stage, "Arm", armSprite, Vector2.zero, new Vector2(240f, 760f),
                new Color(0.82f, 0.62f, 0.48f, 1f), T("BRAÇO", "ARM"), 34f);
            WeldUI.Place(arm.rectTransform, new Vector2(0.5f, 0.5f), itemPos, new Vector2(240f, 760f), new Vector2(0.5f, 1f));

            Vector2 from = itemPos + new Vector2(0f, -820f);
            Vector2 to = itemPos + new Vector2(0f, 40f);
            float t = 0f;
            while (t < 0.32f && !Ctx.IsDefeated)
            {
                if (Ctx.Paused) { yield return null; continue; }
                t += Time.unscaledDeltaTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.32f));
                arm.rectTransform.anchoredPosition = Vector2.Lerp(from, to, k);
                yield return null;
            }

            if (Ctx.IsDefeated) yield break;
            yield return Ctx.Wait(0.18f);
            item.Image.gameObject.SetActive(false);
            Ctx.Clicks.Unregister(item.Hit);

            if (item.Id == "apron")
            {
                Object.Destroy(arm.gameObject);
                yield return Tie();
            }

            // Pisca: ao escurecer, o EPI já está vestido
            yield return Ctx.Blink(0, () =>
            {
                if (arm != null) Object.Destroy(arm.gameObject);
                PlayEquipSound(item.Id);
                Ctx.AddEquipped(item.Id, item.Sprite, item.Color, item.Label);
            });

            Ctx.Clicks.Locked = false;
        }

        private void PlayEquipSound(string id)
        {
            switch (id)
            {
                case "mask": Ctx.Audio.Play(Ctx.Sounds.equipMask); break;
                case "gloves": Ctx.Audio.Play(Ctx.Sounds.equipGloves); break;
                default: Ctx.Audio.Play(Ctx.Sounds.equipApron); break;
            }
        }

        // ---------- Mini-jogo de amarrar o avental ----------

        private IEnumerator Tie()
        {
            Ctx.Clicks.Locked = false;
            Ctx.Hud.SetHint(T("Amarre o avental: arraste as alças até o centro e depois PUXE O NÓ PRA BAIXO.",
                "Tie the apron: drag both straps to the center, then PULL THE KNOT DOWN."));

            Image dim = WeldUI.MakeImage(Ctx.Overlay, "TieDim", null, new Color(0f, 0f, 0f, 0.6f));
            WeldUI.Fill(dim.rectTransform);

            Image body = WeldUI.MakeProp(Ctx.Overlay, "TieApron", Ctx.Art.epiApron, new Vector2(0f, 20f), new Vector2(440f, 580f),
                new Color(0.28f, 0.28f, 0.3f, 1f), T("AVENTAL", "APRON"), 44f);

            Vector2 center = new Vector2(0f, 60f);
            Image target = WeldUI.MakeImage(Ctx.Overlay, "TieTarget", WeldUI.Circle, new Color(1f, 0.82f, 0.1f, 0.35f));
            WeldUI.Place(target.rectTransform, new Vector2(0.5f, 0.5f), center, new Vector2(170f, 170f));

            Vector2 leftHome = new Vector2(-470f, 160f);
            Vector2 rightHome = new Vector2(470f, 160f);

            Image left = MakeHandle("StrapL", leftHome, T("ALÇA", "STRAP"));
            Image right = MakeHandle("StrapR", rightHome, T("ALÇA", "STRAP"));

            yield return DragTo(left.rectTransform, center + new Vector2(-18f, 0f), 85f, leftHome);
            Ctx.Hud.Toast(T("1/3", "1/3"), WeldingMinigame.Good, 0.8f);
            yield return DragTo(right.rectTransform, center + new Vector2(18f, 0f), 85f, rightHome);
            Ctx.Hud.Toast(T("2/3", "2/3"), WeldingMinigame.Good, 0.8f);

            Image knot = MakeHandle("Knot", center, T("NÓ", "KNOT"));
            target.rectTransform.anchoredPosition = center + new Vector2(0f, -230f);
            yield return DragTo(knot.rectTransform, center + new Vector2(0f, -230f), 85f, center);

            Ctx.Hud.Toast(T("Avental bem amarrado!", "Apron tied!"), WeldingMinigame.Good, 1.2f);
            yield return Ctx.Wait(0.35f);

            Object.Destroy(dim.gameObject);
            Object.Destroy(body.gameObject);
            Object.Destroy(target.gameObject);
            Object.Destroy(left.gameObject);
            Object.Destroy(right.gameObject);
            Object.Destroy(knot.gameObject);
            Ctx.Clicks.Locked = true;
        }

        private Image MakeHandle(string name, Vector2 pos, string label)
        {
            Image img = WeldUI.MakeImage(Ctx.Overlay, name, WeldUI.Circle, WeldingMinigame.Accent);
            WeldUI.Place(img.rectTransform, new Vector2(0.5f, 0.5f), pos, new Vector2(130f, 130f));
            TextMeshProUGUI text = WeldUI.MakeText(img.transform, "Label", label, 26f, WeldingMinigame.Ink, TextAlignmentOptions.Center);
            WeldUI.Fill(text.rectTransform);
            return img;
        }

        private IEnumerator DragTo(RectTransform handle, Vector2 target, float radius, Vector2 home)
        {
            bool pressed = false;
            WeldingInput.Hit hit = Ctx.Clicks.Register(handle, null, () => pressed = true, 60);

            while (true)
            {
                if (Ctx.Paused) { yield return null; continue; }
                yield return null;
                if (Ctx.Paused || !pressed) continue;
                pressed = false;

                Ctx.Audio.Play(Ctx.Sounds.pickup);

                while (Input.GetMouseButton(0))
                {
                    if (Ctx.Paused) { yield return null; continue; }
                    Vector2 local;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(Ctx.Overlay, Input.mousePosition, null, out local);
                    handle.anchoredPosition = local;
                    yield return null;
                }

                if (Vector2.Distance(handle.anchoredPosition, target) <= radius)
                {
                    handle.anchoredPosition = target;
                    Ctx.Audio.Play(Ctx.Sounds.strapTie);
                    break;
                }

                handle.anchoredPosition = home;
                Ctx.Audio.Play(Ctx.Sounds.drop);
            }

            Ctx.Clicks.Unregister(hit);
        }
    }
}
