using System.Collections;

namespace Core.Minigames.Welding
{
    /// <summary>ETAPA 0 - O caderninho ensina a fase. Depois dela, o TAB abre o caderninho a qualquer hora.</summary>
    public sealed class NotebookPhase : WeldingPhase
    {
        public NotebookPhase(WeldingMinigame ctx) : base(ctx)
        {
        }

        public override string Title
        {
            get { return T("Ler o caderninho", "Read the notebook"); }
        }

        public override IEnumerator Run()
        {
            yield return Ctx.Notebook.Show(true);
            Ctx.NotebookUnlocked = true;
            Ctx.Hud.Toast(T("Dica: TAB abre o caderninho de novo.", "Tip: TAB opens the notebook again."), WeldingMinigame.Accent, 3f);
        }
    }
}
