using System.Collections;
using Core.Settings;

namespace Core.Minigames.Welding
{
    /// <summary>
    /// Uma etapa do procedimento (EPIs, limpar a área, biombo, extintor, solda...).
    /// Pra criar uma etapa nova: herde esta classe, escreva Run() como uma sequência (yield return) e
    /// adicione na lista de BuildPhases() do WeldingMinigame.
    /// </summary>
    public abstract class WeldingPhase
    {
        protected readonly WeldingMinigame Ctx;

        protected WeldingPhase(WeldingMinigame ctx)
        {
            Ctx = ctx;
        }

        /// <summary>Nome na lista do procedimento (já no idioma do jogo).</summary>
        public abstract string Title { get; }

        /// <summary>Roda a etapa. Só termina quando o jogador conclui.</summary>
        public abstract IEnumerator Run();

        protected static string T(string portuguese, string english)
        {
            return GameLanguage.T(portuguese, english);
        }
    }
}
