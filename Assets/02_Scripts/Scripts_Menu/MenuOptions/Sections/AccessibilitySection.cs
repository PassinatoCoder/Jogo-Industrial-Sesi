using Core.Settings;
using Core.UI.Menu;
using UnityEngine;

namespace Core.UI.Options
{
    /// <summary>
    /// Acessibilidade. Tudo aqui vale no jogo inteiro, não só no menu:
    ///   texto (tamanho, espaçamento, negrito, leitura fácil/dislexia) -> GameAccessibility
    ///   cores (daltonismo, alto contraste) ......................... -> ColorBlindFilter + shader
    /// Tremor de câmera e flashes: o seu código de jogo usa AccessibilitySettings.ShakeIntensity / FlashIntensity.
    /// </summary>
    public sealed class AccessibilitySection : OptionsSection
    {
        protected override void Build()
        {
            Title = T("Acessibilidade", "Accessibility");
            Subtitle = T("Deixe o jogo confortável pra você.", "Make the game comfortable for you.");

            // ---------- Texto ----------

            AddHeader(T("Texto", "Text"));

            AddToggle(T("Leitura fácil (dislexia)", "Easy reading (dyslexia)"),
                T("Usa a fonte própria para dislexia, mais espaço entre letras e sem títulos em maiúsculas, no jogo todo.",
                  "Uses a dyslexia-friendly font, wider letter spacing and no all-caps titles across the whole game."),
                () => MenuReadability.ReadableTitles,
                v => MenuReadability.ReadableTitles = v);

            AddChoice(T("Tamanho do texto", "Text size"),
                T("Aumenta os textos do jogo e do menu. O menu para de crescer em \"Muito grande\".",
                  "Makes game and menu text bigger. The menu stops growing at \"Very large\"."),
                new[]
                {
                    T("Normal", "Normal"),
                    T("Grande", "Large"),
                    T("Muito grande", "Very large"),
                    T("Enorme", "Huge")
                },
                () => AccessibilitySettings.TextSizeIndex,
                i => AccessibilitySettings.TextSizeIndex = i);

            AddChoice(T("Espaçamento do texto", "Text spacing"),
                T("Afasta letras, palavras e linhas. Ajuda a não embaralhar as palavras.",
                  "Spreads out letters, words and lines. Helps keep words from blurring together."),
                new[] { T("Normal", "Normal"), T("Médio", "Medium"), T("Grande", "Large") },
                () => AccessibilitySettings.TextSpacingIndex,
                i => AccessibilitySettings.TextSpacingIndex = i);

            AddToggle(T("Texto em negrito", "Bold text"),
                T("Deixa as letras mais grossas e fáceis de ver.", "Makes letters thicker and easier to see."),
                () => AccessibilitySettings.BoldText,
                v => AccessibilitySettings.BoldText = v);

            // ---------- Cores ----------

            AddHeader(T("Cores e imagem", "Colors and image"));

            AddChoice(T("Filtro para daltonismo", "Color blindness filter"),
                T("Ajusta as cores do jogo para ficarem mais fáceis de diferenciar. Escolha o tipo que combina com você.",
                  "Adjusts the game's colors so they are easier to tell apart. Pick the type that matches you."),
                new[]
                {
                    T("Desligado", "Off"),
                    T("Protanopia (sem vermelho)", "Protanopia (no red)"),
                    T("Protanomalia (pouco vermelho)", "Protanomaly (weak red)"),
                    T("Deuteranopia (sem verde)", "Deuteranopia (no green)"),
                    T("Deuteranomalia (pouco verde)", "Deuteranomaly (weak green)"),
                    T("Tritanopia (sem azul)", "Tritanopia (no blue)"),
                    T("Tritanomalia (pouco azul)", "Tritanomaly (weak blue)"),
                    T("Acromatopsia (sem cores)", "Achromatopsia (no color)")
                },
                () => (int)AccessibilitySettings.ColorBlind,
                i =>
                {
                    AccessibilitySettings.ColorBlind = (ColorBlindMode)i;
                    RefreshRows();
                });

            SliderRow strength = AddSlider(T("Intensidade do filtro", "Filter strength"),
                T("Quanto o filtro mexe nas cores. 100% é o recomendado.", "How much the filter changes the colors. 100% is recommended."),
                () => AccessibilitySettings.ColorBlindStrength,
                v => AccessibilitySettings.ColorBlindStrength = v,
                0.1f);
            strength.EnabledWhen = () => AccessibilitySettings.ColorBlind != ColorBlindMode.Off;

            AddToggle(T("Alto contraste", "High contrast"),
                T("Menu em preto e branco puro e imagem do jogo com mais contraste.",
                  "Pure black and white menu and a higher-contrast game image."),
                () => AccessibilitySettings.HighContrast,
                v => AccessibilitySettings.HighContrast = v);

            if (Application.isEditor || Debug.isDebugBuild)
            {
                ToggleRow preview = AddToggle(T("Teste: ver como daltônico", "Test: see as colorblind"),
                    T("SÓ PARA DESENVOLVEDOR: mostra como a pessoa enxerga em vez de corrigir. Não é salvo.",
                      "DEVELOPER ONLY: shows how the person sees instead of correcting. Not saved."),
                    () => AccessibilitySettings.ColorBlindPreview,
                    v => AccessibilitySettings.ColorBlindPreview = v);
                preview.EnabledWhen = () => AccessibilitySettings.ColorBlind != ColorBlindMode.Off;
            }

            // ---------- Movimento e efeitos ----------

            AddHeader(T("Movimento e efeitos", "Motion and effects"));

            AddToggle(T("Reduzir movimento", "Reduce motion"),
                T("Desliga deslizes, balanços e transições do menu. Tudo aparece direto.",
                  "Turns off slides, sways and menu transitions. Everything just appears."),
                () => MenuMotion.ReduceMotion,
                v => MenuMotion.ReduceMotion = v);

            AddSlider(T("Tremor de câmera", "Camera shake"),
                T("Força do tremor da câmera durante o jogo. 0% desliga.", "Strength of in-game camera shake. 0% turns it off."),
                () => AccessibilitySettings.CameraShake,
                v => AccessibilitySettings.CameraShake = v,
                0.1f);

            AddToggle(T("Reduzir flashes", "Reduce flashes"),
                T("Suaviza clarões e piscadas durante o jogo.", "Softens flashes and blinking during the game."),
                () => AccessibilitySettings.ReduceFlashes,
                v => AccessibilitySettings.ReduceFlashes = v);

            AddButton(T("Restaurar padrão de acessibilidade", "Reset accessibility to default"),
                T("Volta todas as opções desta aba ao normal.", "Returns every option in this tab to normal."),
                () =>
                {
                    AccessibilitySettings.ResetToDefaults();
                    MenuMotion.ReduceMotion = false;
                    MenuReadability.ReadableTitles = false;
                    RefreshRows();
                });
        }
    }
}
