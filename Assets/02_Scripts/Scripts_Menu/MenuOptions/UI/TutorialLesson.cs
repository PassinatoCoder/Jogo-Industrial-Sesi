using System;
using System.Collections.Generic;
using Core.Settings;
using UnityEngine;

namespace Core.UI.Options
{
    /// <summary>
    /// Uma lição do tutorial que o jogador pode rever em Opções > Jogabilidade.
    /// No texto, marcadores como {Interact} ou {Jump} viram o nome da tecla atual do jogador.
    /// (Nomes válidos: os do enum GameAction.)
    /// </summary>
    [Serializable]
    public class TutorialLesson
    {
        [Tooltip("Identificador único (ex.: 'belt'). Use o mesmo em TutorialProgress.Unlock(\"belt\").")]
        public string id;
        public string titlePt;
        public string titleEn;
        [TextArea(3, 10)] public string bodyPt;
        [TextArea(3, 10)] public string bodyEn;
        [Tooltip("Imagem opcional da lição.")]
        public Sprite image;

        public string Title => GameLanguage.T(titlePt, titleEn);
        public string Body => GameKeyBindings.ReplaceTokens(GameLanguage.T(bodyPt, bodyEn));

        /// <summary>Lições de exemplo (textos provisórios). Usadas quando a lista do Inspector está vazia.</summary>
        public static List<TutorialLesson> CreateDefaults()
        {
            return new List<TutorialLesson>
            {
                new TutorialLesson
                {
                    id = "movement",
                    titlePt = "Movimento",
                    titleEn = "Movement",
                    bodyPt = "Use {MoveUp}, {MoveLeft}, {MoveDown} e {MoveRight} para andar.\n\n{Jump} salta obstáculos baixos.\n{Crouch} agacha para passar sob barras e vãos.",
                    bodyEn = "Use {MoveUp}, {MoveLeft}, {MoveDown} and {MoveRight} to walk.\n\n{Jump} jumps over low obstacles.\n{Crouch} crouches to pass under bars and gaps."
                },
                new TutorialLesson
                {
                    id = "interact",
                    titlePt = "Pegar e usar objetos",
                    titleEn = "Picking up and using objects",
                    bodyPt = "Chegue perto de um objeto e aperte {Interact} para pegar.\n{Use} usa o item que está na mão.\n{Inspect} inspeciona: objetos desconhecidos ficam mais claros depois de ler o manual.\n{Drop} guarda ou solta o item.",
                    bodyEn = "Get close to an object and press {Interact} to pick it up.\n{Use} uses the item in your hand.\n{Inspect} inspects: unknown objects become clearer after reading the manual.\n{Drop} stores or drops the item."
                },
                new TutorialLesson
                {
                    id = "belt",
                    titlePt = "Cinto de ferramentas",
                    titleEn = "Tool belt",
                    bodyPt = "O cinto guarda suas ferramentas de acesso rápido.\nUse {Belt1} a {Belt5} para escolher o espaço.",
                    bodyEn = "The belt holds your quick-access tools.\nUse {Belt1} to {Belt5} to pick a slot."
                },
                new TutorialLesson
                {
                    id = "backpack",
                    titlePt = "Mochila",
                    titleEn = "Backpack",
                    bodyPt = "Ao equipar a mochila, o inventário é liberado.\nAperte {Inventory} para abrir e organizar seus itens.",
                    bodyEn = "Equipping the backpack unlocks the inventory.\nPress {Inventory} to open it and organize your items."
                },
                new TutorialLesson
                {
                    id = "safety",
                    titlePt = "EPI e Integridade",
                    titleEn = "PPE and Integrity",
                    bodyPt = "Usar o EPI correto e seguir as boas práticas aumenta sua Integridade.\nLargar itens em qualquer lugar ou ignorar regras diminui.\nSe a Integridade chegar a zero, você é demitida.",
                    bodyEn = "Wearing the right PPE and following good practices raises your Integrity.\nLeaving items lying around or ignoring rules lowers it.\nIf Integrity reaches zero, you are fired."
                }
            };
        }
    }
}
