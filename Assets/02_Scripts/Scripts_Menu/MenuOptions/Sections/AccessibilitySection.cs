using Core.Settings;
using Core.UI.Menu;

namespace Core.UI.Options
{
    /// <summary>
    /// Acessibilidade. Reduzir movimento, leitura fácil, tamanho do texto e alto contraste
    /// já valem pro próprio menu na hora. Tremor de câmera e flashes ficam salvos em
    /// AccessibilitySettings pro jogo ler.
    /// </summary>
    public sealed class AccessibilitySection : OptionsSection
    {
        protected override void Build()
        {
            Title = T("Acessibilidade", "Accessibility");
            Subtitle = T("Deixe o jogo confortável pra você.", "Make the game comfortable for you.");

            AddHeader(T("Leitura e visão", "Reading and vision"));

            AddToggle(T("Leitura fácil", "Easy reading"),
                T("Troca os títulos por texto simples, sem maiúsculas, e usa a fonte de leitura (boa para dislexia).",
                  "Replaces titles with plain text, no all-caps, and uses the reading font (good for dyslexia)."),
                () => MenuReadability.ReadableTitles,
                v => MenuReadability.ReadableTitles = v);

            AddChoice(T("Tamanho do texto", "Text size"),
                T("Aumenta os textos do menu.", "Makes menu text bigger."),
                new[] { T("Normal", "Normal"), T("Grande", "Large"), T("Muito grande", "Extra large") },
                () => AccessibilitySettings.TextSizeIndex,
                i => AccessibilitySettings.TextSizeIndex = i);

            AddToggle(T("Alto contraste", "High contrast"),
                T("Cores puras (preto e branco) no menu pra ler melhor.", "Pure black and white colors in the menu for better legibility."),
                () => AccessibilitySettings.HighContrast,
                v => AccessibilitySettings.HighContrast = v);

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
                T("Suaviza efeitos de piscada e clarão durante o jogo.", "Softens blinking and flash effects during the game."),
                () => AccessibilitySettings.ReduceFlashes,
                v => AccessibilitySettings.ReduceFlashes = v);

            AddButton(T("Restaurar padrão de acessibilidade", "Reset accessibility to default"),
                T("Desliga todas as opções desta aba.", "Turns off every option in this tab."),
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
