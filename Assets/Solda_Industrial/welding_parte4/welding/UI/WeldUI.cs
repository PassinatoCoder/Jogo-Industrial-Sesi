using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Minigames.Welding
{
    /// <summary>Ferramentas pra criar a interface do minigame por código (sem prefab, sem Canvas na mão).</summary>
    public static class WeldUI
    {
        private static Sprite _circle;
        private static Sprite _glow;
        private static Sprite _vignette;

        /// <summary>Fonte do TextMeshPro usada nos textos. Vazio = a fonte padrão do projeto.</summary>
        public static TMP_FontAsset Font;

        // ---------- Retângulos ----------

        public static RectTransform MakeRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static void Fill(RectTransform rt, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        public static void Place(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
        {
            Place(rt, anchor, position, size, new Vector2(0.5f, 0.5f));
        }

        public static void Place(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size, Vector2 pivot)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
        }

        // ---------- Imagens e peças ----------

        public static Image MakeImage(Transform parent, string name, Sprite sprite, Color color)
        {
            RectTransform rt = MakeRect(name, parent);
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            img.preserveAspect = sprite != null;
            return img;
        }

        /// <summary>
        /// Cria uma peça do cenário. Com sprite: mostra a arte. Sem sprite: retângulo colorido com o nome
        /// (assim o minigame já roda antes da arte chegar).
        /// </summary>
        public static Image MakeProp(Transform parent, string name, Sprite sprite, Vector2 position, Vector2 size,
            Color placeholder, string label, float labelSize = 34f)
        {
            Image img = MakeImage(parent, name, sprite, sprite != null ? Color.white : placeholder);
            Place(img.rectTransform, new Vector2(0.5f, 0.5f), position, size);
            SetPlaceholderLabel(img, sprite == null ? label : null, labelSize);
            return img;
        }

        /// <summary>Troca a arte de uma peça (ex.: extintor cheio -> vazio), mantendo o retângulo se não houver sprite.</summary>
        public static void SetProp(Image img, Sprite sprite, Color placeholder, string label, float labelSize = 34f)
        {
            img.sprite = sprite;
            img.color = sprite != null ? Color.white : placeholder;
            img.preserveAspect = sprite != null;
            SetPlaceholderLabel(img, sprite == null ? label : null, labelSize);
        }

        private static void SetPlaceholderLabel(Image img, string label, float size)
        {
            Transform existing = img.transform.Find("Label");

            if (string.IsNullOrEmpty(label))
            {
                if (existing != null) existing.gameObject.SetActive(false);
                return;
            }

            TextMeshProUGUI tmp;
            if (existing == null)
            {
                tmp = MakeText(img.transform, "Label", label, size, new Color(1f, 1f, 1f, 0.92f),
                    TextAlignmentOptions.Center, true);
                Fill(tmp.rectTransform, 10f, 10f, 10f, 10f);
            }
            else
            {
                existing.gameObject.SetActive(true);
                tmp = existing.GetComponent<TextMeshProUGUI>();
                tmp.text = label;
                tmp.fontSize = size;
            }
        }

        /// <summary>Sombra suave embaixo da peça (elipse escura). Fica atrás da peça e não se mexe junto.</summary>
        public static Image AddShadow(Image prop, float strength = 0.4f)
        {
            if (prop == null) return null;

            RectTransform rt = prop.rectTransform;
            Transform parent = rt.parent;

            Image shadow = MakeImage(parent, "Shadow", Circle, new Color(0f, 0f, 0f, strength));
            shadow.preserveAspect = false;
            Place(shadow.rectTransform, new Vector2(0.5f, 0.5f),
                rt.anchoredPosition + new Vector2(14f, -rt.sizeDelta.y * 0.5f + 6f),
                new Vector2(rt.sizeDelta.x * 1.05f, Mathf.Max(24f, rt.sizeDelta.y * 0.18f)));
            shadow.transform.SetSiblingIndex(rt.GetSiblingIndex());
            return shadow;
        }

        // ---------- Texto ----------

        public static TextMeshProUGUI MakeText(Transform parent, string name, string text, float size, Color color,
            TextAlignmentOptions alignment, bool wrap = false)
        {
            RectTransform rt = MakeRect(name, parent);
            TextMeshProUGUI tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (Font != null) tmp.font = Font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Overflow;
            return tmp;
        }

        // ---------- Sprites gerados (círculo, brilho, vinheta) ----------

        public static Sprite Circle
        {
            get
            {
                if (_circle == null) _circle = BuildRadial(64, r => Mathf.Clamp01((1f - r) * 32f));
                return _circle;
            }
        }

        public static Sprite Glow
        {
            get
            {
                if (_glow == null)
                {
                    _glow = BuildRadial(128, r =>
                    {
                        float v = Mathf.Clamp01(1f - r);
                        return v * v;
                    });
                }

                return _glow;
            }
        }

        /// <summary>Escuro nas bordas, transparente no meio (visor da máscara).</summary>
        public static Sprite Vignette
        {
            get
            {
                if (_vignette == null)
                    _vignette = BuildRadial(256, r => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((r - 0.35f) / 0.8f)));
                return _vignette;
            }
        }

        private static Sprite BuildRadial(int size, System.Func<float, float> alphaByRadius)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            Color32[] pixels = new Color32[size * size];
            float half = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f - half) / half;
                    float dy = (y + 0.5f - half) / half;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(alphaByRadius(r)) * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, a);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false);
            return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
