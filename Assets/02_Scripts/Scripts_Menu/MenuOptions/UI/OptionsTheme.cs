using System;
using TMPro;
using UnityEngine;

namespace Core.UI.Options
{
    /// <summary>
    /// Aparência do menu de opções. Tudo editável no Inspector do OptionsMenu.
    /// Tudo é desenhado por código (retângulos inclinados), então NÃO precisa de arte.
    /// Se quiser, arraste suas fontes. Vazio = fonte padrão do TextMeshPro.
    /// </summary>
    [Serializable]
    public class OptionsTheme
    {
        [Header("Fontes (TextMeshPro Font Asset)")]
        [Tooltip("Fonte dos títulos e abas (a bonitona/grossa).")]
        public TMP_FontAsset displayFont;
        [Tooltip("Fonte dos textos menores.")]
        public TMP_FontAsset bodyFont;
        [Tooltip("Fonte do 'modo leitura fácil' (ex.: dislexia). Vazio = usa a fonte de texto.")]
        public TMP_FontAsset readableFont;

        [Header("Cores")]
        public Color ink = new Color(0.055f, 0.063f, 0.082f, 0.94f);
        public Color inkSoft = new Color(0.115f, 0.13f, 0.155f, 0.9f);
        public Color paper = new Color(0.96f, 0.955f, 0.93f, 1f);
        public Color muted = new Color(0.62f, 0.66f, 0.72f, 1f);

        [Header("Cor de cada seção (na ordem das abas)")]
        public Color[] sectionAccents =
        {
            new Color(0.84f, 0.09f, 0.16f, 1f), // Vídeo       - vermelho
            new Color(0.12f, 0.44f, 0.66f, 1f), // Áudio       - azul
            new Color(0.95f, 0.72f, 0.02f, 1f), // Acessib.    - amarelo
            new Color(0.12f, 0.64f, 0.54f, 1f), // Controles   - verde-água
            new Color(0.91f, 0.41f, 0.10f, 1f), // Jogabilidade- laranja
            new Color(0.54f, 0.36f, 0.84f, 1f)  // Idioma      - roxo
        };

        [Header("Medidas (pixels, base 1920x1080)")]
        public float tabWidth = 470f;
        public float tabHeight = 82f;
        public float tabSpacing = 10f;
        [Tooltip("Quanto a aba selecionada entorta (graus). Pequeno de propósito: nunca encosta na vizinha.")]
        public float tabTilt = 2.2f;
        public float rowHeight = 68f;
        public float rowSpacing = 8f;
        [Tooltip("Inclinação dos retângulos (graus). Dá o visual 'de corrida'.")]
        public float skew = 10f;

        public Color AccentFor(int index)
        {
            if (sectionAccents == null || sectionAccents.Length == 0) return new Color(0.84f, 0.09f, 0.16f, 1f);
            return sectionAccents[Mathf.Abs(index) % sectionAccents.Length];
        }
    }
}
