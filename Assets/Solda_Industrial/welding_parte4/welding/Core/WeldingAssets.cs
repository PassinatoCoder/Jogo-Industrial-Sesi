using System;
using UnityEngine;

namespace Core.Minigames.Welding
{
    /// <summary>
    /// TODAS as artes do minigame. Arraste cada sprite no campo com o nome certo.
    /// Campo vazio = aparece um retângulo colorido com o nome da peça (dá pra jogar já).
    /// Dica: pivot do sprite no centro; PNG com fundo transparente.
    /// </summary>
    [Serializable]
    public sealed class WeldingArt
    {
        [Header("Cenário (primeira pessoa)")]
        [Tooltip("Parede/oficina ao fundo. Tamanho ideal: 1920x1080.")]
        public Sprite background;
        [Tooltip("A mesa em perspectiva de quem olha de pé. Ocupa a parte de baixo da tela.")]
        public Sprite table;
        [Tooltip("Vista frontal da parede, usada quando a personagem olha para o biombo.")]
        public Sprite frontWallBackground;
        [Tooltip("Vista da parede lateral direita, usada quando a personagem olha para o extintor.")]
        public Sprite rightWallBackground;
        [Tooltip("Sombra/escuro das bordas da máscara de solda (visor). Opcional: sem ela o jogo gera uma.")]
        public Sprite maskVisor;

        [Header("Mãos do jogador (slots separados)")]
        [Tooltip("Mão esquerda sem luva. Use um recorte individual, com fundo transparente.")]
        public Sprite leftHandBare;
        [Tooltip("Mão direita sem luva. Use um recorte individual, com fundo transparente.")]
        public Sprite rightHandBare;
        [Tooltip("Mão esquerda com luva de raspa.")]
        public Sprite leftHandGloves;
        [Tooltip("Mão direita com luva de raspa.")]
        public Sprite rightHandGloves;
        [Header("Compatibilidade com artes antigas")]
        [Tooltip("Arte antiga com as duas mãos juntas. Usada somente como fallback se os slots separados estiverem vazios.")]
        public Sprite handsBare;
        [Tooltip("Arte antiga com as duas mãos enluvadas. Usada somente como fallback se os slots separados estiverem vazios.")]
        public Sprite handsGloves;
        [Tooltip("Mão direita com a tocha MIG/MAG. A PONTA da tocha deve ficar no canto indicado em Ajustes > Solda > Torch Tip Pivot.")]
        public Sprite handTorch;

        [Header("EPIs")]
        public Sprite epiMask;
        public Sprite epiGloves;
        public Sprite epiApron;
        [Tooltip("Opcional: EPIs ERRADOS pra confundir (ex.: luva de látex, óculos comum).")]
        public Sprite epiDecoyGloves;
        public Sprite epiDecoyGlasses;

        [Header("Peça de trabalho")]
        public Sprite pieceDirty;
        public Sprite pieceClean;
        [Tooltip("Cordão de solda recém-feito (vermelho incandescente).")]
        public Sprite weldBeadHot;
        [Tooltip("Cordão de solda frio (cinza escuro fosco).")]
        public Sprite weldBeadCool;
        public Sprite torchOnTable;

        [Header("Segurança")]
        public Sprite screenClosed;
        public Sprite screenOpen;
        public Sprite extinguisherFull;
        public Sprite extinguisherEmpty;

        [Header("Bagunça da mesa e eventos")]
        public Sprite trashBin;
        public Sprite oilPuddle;
        public Sprite rustyScrap;
        public Sprite sprayCan;
        [Tooltip("Colega que entra na área de solda sem EPI.")]
        public Sprite npcIntruder;
        [Tooltip("Retrato do chefe, pros alertas.")]
        public Sprite bossPortrait;
    }

    /// <summary>
    /// TODOS os sons do minigame. Campo vazio = silêncio nesse ponto (não dá erro).
    /// Os que dizem "(loop)" tocam em repetição: use áudio que emenda sem corte.
    /// </summary>
    [Serializable]
    public sealed class WeldingSounds
    {
        [Header("Transição")]
        [Tooltip("Vários passos diferentes; o jogo sorteia.")]
        public AudioClip[] footsteps;
        [Tooltip("Piscada.")]
        public AudioClip blink;

        [Header("Interface e procedimento")]
        public AudioClip uiClick;
        public AudioClip stepDone;
        public AudioClip alert;
        public AudioClip pickup;
        public AudioClip drop;
        public AudioClip trashThrow;
        public AudioClip resultGood;
        public AudioClip resultBad;

        [Header("EPIs")]
        public AudioClip equipMask;
        public AudioClip equipGloves;
        public AudioClip equipApron;
        public AudioClip strapTie;
        public AudioClip maskUp;
        public AudioClip maskDown;

        [Header("Biombo")]
        public AudioClip screenStuck;
        [Tooltip("(loop curto) esforço enquanto ela força.")]
        public AudioClip screenStrain;
        public AudioClip screenOpen;

        [Header("Extintor")]
        public AudioClip extinguisherCheck;
        public AudioClip extinguisherSwap;
        public AudioClip extinguisherSpray;
        public AudioClip extinguisherStuck;

        [Header("Eventos")]
        public AudioClip oilSplat;
        public AudioClip intruderVoice;
        public AudioClip burnOuch;

        [Header("Solda")]
        [Tooltip("(loop) som do arco enquanto solda.")]
        public AudioClip arcLoop;
        [Tooltip("Faíscas; o jogo sorteia.")]
        public AudioClip[] sparks;
        public AudioClip flashBang;
        [Tooltip("(loop) respiração normal com a máscara.")]
        public AudioClip breathingLoop;
        [Tooltip("(loop) Batimento do coração, toca quando falta ar.")]
        public AudioClip heartbeatLoop;
        public AudioClip torchJam;
        public AudioClip lightFlicker;
        public AudioClip lightsOut;
        public AudioClip lightsOn;
        [Tooltip("(loop) falta de ar.")]
        public AudioClip gaspLoop;

        [Header("Ambiente (opcional)")]
        [Tooltip("(loop) barulho de oficina ao fundo.")]
        public AudioClip ambientLoop;
    }
}
