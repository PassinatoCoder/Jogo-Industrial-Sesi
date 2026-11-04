using Core.Settings;

namespace Core.UI.Options
{
    /// <summary>
    /// Controles: remapear teclas. Enter troca a tecla da linha selecionada, Esc cancela.
    /// Tecla já usada por outra ação? As duas trocam de lugar (nunca fica ação sem tecla).
    /// Pra valer no jogo, o jogador deve ler as teclas por GameKeyBindings.
    /// </summary>
    public sealed class ControlsSection : OptionsSection
    {
        protected override void Build()
        {
            Title = T("Controles", "Controls");
            Subtitle = T("Escolha as teclas do jogo.", "Choose the game keys.");

            string hint = T("Enter para trocar a tecla. Esc cancela. Se a tecla já estiver em uso, as duas trocam de lugar.",
                            "Press Enter to change the key. Esc cancels. If the key is already in use, the two swap places.");

            AddHeader(T("Movimento", "Movement"));
            AddBind(GameAction.MoveUp, hint);
            AddBind(GameAction.MoveDown, hint);
            AddBind(GameAction.MoveLeft, hint);
            AddBind(GameAction.MoveRight, hint);

            AddHeader(T("Ações", "Actions"));
            AddBind(GameAction.Jump, hint);
            AddBind(GameAction.Crouch, hint);
            AddBind(GameAction.Interact, hint);
            AddBind(GameAction.Use, hint);
            AddBind(GameAction.Inspect, hint);
            AddBind(GameAction.Drop, hint);
            AddBind(GameAction.Inventory, hint);

            AddHeader(T("Cinto de ferramentas", "Tool belt"));
            AddBind(GameAction.Belt1, hint);
            AddBind(GameAction.Belt2, hint);
            AddBind(GameAction.Belt3, hint);
            AddBind(GameAction.Belt4, hint);
            AddBind(GameAction.Belt5, hint);

            AddHeader(T("Geral", "General"));
            AddInfo(T("Esc pausa e volta (fixo). O controle (gamepad) usa os botões padrão.",
                      "Esc pauses and goes back (fixed). The gamepad uses the default buttons."), 70f);

            AddButton(T("Restaurar teclas padrão", "Reset keys to default"),
                T("Volta todas as teclas para o padrão do jogo.", "Sets every key back to the game default."),
                () =>
                {
                    GameKeyBindings.ResetAll();
                    RefreshRows();
                });
        }

        private void AddBind(GameAction action, string description)
        {
            Add(new KeybindRow(Menu, action, description));
        }
    }
}
