using Core.Settings;

namespace Core.UI.Options
{
    /// <summary>
    /// Idioma. Trocar o idioma refaz o próprio menu na hora e dispara GameLanguage.Changed
    /// (os outros textos do jogo podem escutar esse evento e usar GameLanguage.T("pt", "en")).
    /// </summary>
    public sealed class LanguageSection : OptionsSection
    {
        protected override void Build()
        {
            Title = T("Idioma", "Language");
            Subtitle = T("Escolha o idioma do jogo.", "Choose the game language.");

            AddHeader(T("Texto", "Text"));

            AddChoice(T("Idioma do jogo", "Game language"),
                T("Muda os textos do menu e do jogo.", "Changes the menu and game texts."),
                new[] { "Português (Brasil)", "English" },
                () => (int)GameLanguage.Current,
                i => GameLanguage.Current = (Language)i);

            AddInfo(T("O idioma fica salvo e vale na próxima vez que você abrir o jogo.",
                      "The language is saved and applies the next time you open the game."), 70f);
        }
    }
}
