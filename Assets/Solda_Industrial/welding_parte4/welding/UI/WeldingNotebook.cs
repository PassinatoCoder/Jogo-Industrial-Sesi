using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Core.Settings;

namespace Core.Minigames.Welding
{
    /// <summary>
    /// O CADERNINHO: explica a fase. No começo é obrigatório (seta DIREITA avança, ESQUERDA volta).
    /// Depois da leitura, abre e fecha a qualquer hora com TAB.
    /// Pra mudar os textos: edite BuildPages() (um P(...) por página, português e inglês).
    /// </summary>
    public sealed class WeldingNotebook
    {
        private struct Page
        {
            public string Title;
            public string Body;
        }

        private readonly WeldingMinigame _c;
        private readonly RectTransform _layer;
        private GameObject _root;

        public bool IsOpen { get; private set; }

        public WeldingNotebook(WeldingMinigame c, RectTransform layer)
        {
            _c = c;
            _layer = layer;
        }

        private static Page P(string titlePt, string bodyPt, string titleEn, string bodyEn)
        {
            return new Page { Title = GameLanguage.T(titlePt, titleEn), Body = GameLanguage.T(bodyPt, bodyEn) };
        }

        private static List<Page> BuildPages(int pieces, WeldingDifficulty difficulty)
        {
            List<Page> pages = new List<Page>();

            pages.Add(P("MISSÃO",
                "Soldar " + pieces + " peças trincadas do jeito CERTO.\n\nCada etapa importa: o trabalho é avaliado pela qualidade da solda e pelo estado físico da personagem. Decisões ruins afetam integridade, stamina e vida.\n\nNormas de referência: NR-6 (EPI), NR-12 (máquinas) e NR-23 (incêndio).",
                "MISSION",
                "Weld " + pieces + " cracked parts the RIGHT way.\n\nEvery step matters: your work is evaluated by weld quality and the character's condition. Poor decisions affect integrity, stamina and health.\n\nReference standards: NR-6 (PPE), NR-12 (machinery) and NR-23 (fire)."));

            pages.Add(P("1. EPIs",
                "Pegue os EPIs que o chefe pedir: AVENTAL, MÁSCARA e LUVAS (nessa ordem).\n\nClique no item. O avental tem que ser AMARRADO: arraste as duas alças até o centro e puxe o nó pra baixo.\n\nSem EPI = acidente.",
                "1. PPE",
                "Grab the PPE the boss asks for: APRON, MASK and GLOVES (in that order).\n\nClick the item. The apron must be TIED: drag both straps to the center and pull the knot down.\n\nNo PPE = accident."));

            pages.Add(P("2. LIMPAR A ÁREA",
                "Clique no lixo e no que atrapalha pra pegar e jogar fora. Arraste as ferramentas pro lugar delas.\n\nQuando a mesa estiver limpa, aperte PRONTO.\n\nDURANTE a solda a mesa suja de novo: limpe pra não prejudicar o cordão.",
                "2. CLEAR THE AREA",
                "Click trash and anything in the way to pick it up and throw it out. Drag tools to their place.\n\nWhen the table is clean, press READY.\n\nWHILE welding the table gets dirty again: clean it so it does not hurt the bead."));

            pages.Add(P("3. BIOMBO E EXTINTOR",
                "Olhe PARA FRENTE (seta): o biombo está na parede à sua frente. Abra. Ele pode EMPERRAR: aperte ESPAÇO várias vezes.\n\nDepois olhe pra DIREITA: o EXTINTOR fica preso à parede lateral. Confira o medidor. Vazio? Troque. Emperrou? Desemperre.\n\nSó comece a soldar depois de confirmar a proteção da área.",
                "3. SCREEN AND EXTINGUISHER",
                "Look STRAIGHT AHEAD (arrow): the screen is mounted on the wall in front of you. Open it. It can JAM: press SPACE repeatedly.\n\nThen look RIGHT: the EXTINGUISHER is mounted on the side wall. Check the gauge. Empty? Swap it. Jammed? Free it.\n\nOnly begin welding after confirming that the work area is protected."));

            pages.Add(P("4. SOLDANDO",
                "Clique na TOCHA pra pegar. ESPAÇO ou botão direito: abaixar a MÁSCARA (sem ela o arco cega!).\n\nSegure o botão ESQUERDO e siga a trilha. Não pare muito: o CALOR sobe e fura a chapa.\n\nA / D: move a peça. E: solta a tocha (ela volta enrolada).\n\nPontos LARANJA = solda ruim: passe de novo.",
                "4. WELDING",
                "Click the TORCH to pick it up. SPACE or right click: lower the MASK (without it the arc blinds you!).\n\nHold LEFT click and follow the seam. Do not stall: HEAT rises and burns through.\n\nA / D: move the part. E: put the torch down (it goes back coiled).\n\nORANGE spots = bad weld: go over them again."));

            pages.Add(P("5. RESPIRAR",
                "A máscara reduz a entrada de ar. O OXIGÊNIO cai enquanto ela está abaixada. Quanto menos oxigênio, mais rápido a STAMINA despenca; pouca stamina aumenta o tremor e dificulta manter a tocha na trilha.\n\nTire a máscara (ESPAÇO) para respirar e recuperar fôlego. Não espere chegar a zero: a falta de ar também causa dano à vida.",
                "5. OXYGEN & STAMINA",
                "The mask restricts airflow. OXYGEN drops while it is down. The lower your oxygen, the faster STAMINA drains; low stamina increases shaking and makes it harder to keep the torch on the seam.\n\nLift the mask (SPACE) to breathe and recover stamina. Do not wait until it reaches zero: oxygen deprivation also damages health."));

            pages.Add(P("6. EMERGÊNCIAS",
                "Podem acontecer durante o serviço:\n\n- TOCHA ENGASGOU: aperte R várias vezes.\n- LUZ APAGOU: religue com L ou clique no interruptor. Você ainda pode soldar no escuro, mas a margem para acertar cai pela metade e há risco de ferimentos.\n- COLEGA CHEGANDO: clique nele para avisar antes de acender o arco.\n- MESA SUJA: clique para recolher o lixo.\n\nO tremor de câmera só aparece em eventos, esforço ou dano — não é um efeito constante.",
                "6. EMERGENCIES",
                "Events can happen during the job:\n\n- TORCH JAMMED: press R repeatedly.\n- LIGHT OUT: restore it with L or click the switch. You can still weld in the dark, but your accuracy window is cut in half and injuries become possible.\n- COWORKER COMING: click to warn them before striking the arc.\n- DIRTY TABLE: click to collect the trash.\n\nCamera shake only appears during events, strain or damage — it is not constant."));

            pages.Add(P("7. INTEGRIDADE E VIDA",
                "INTEGRIDADE mede a condição geral do equipamento e do trabalho (0–100%). STAMINA mede o fôlego para continuar trabalhando. VIDA mede a condição física da personagem. Integridade ou vida em zero encerram a missão; stamina baixa reduz a precisão e acelera o consumo de oxigênio.\n\nA dificuldade escolhida agora é: " + GameLanguage.T(difficulty == WeldingDifficulty.Easy ? "FÁCIL" : (difficulty == WeldingDifficulty.Hard ? "DIFÍCIL" : "MÉDIO"), difficulty.ToString().ToUpperInvariant()) + ". Ela altera quantidade de peças, oxigênio, frequência dos eventos, tolerância da solda e punições.",
                "7. INTEGRITY & HEALTH",
                "INTEGRITY measures the overall condition of the work and equipment (0–100%). STAMINA measures how long the character can keep working. HEALTH is the character's physical condition. Zero integrity or zero health ends the mission; low stamina reduces accuracy and accelerates oxygen use.\n\nCurrent difficulty: " + difficulty.ToString().ToUpperInvariant() + ". It changes the number of parts, oxygen supply, event frequency, weld tolerance and penalties."));

            pages.Add(P("8. CONTROLES E CADERNINHO",
                "TAB abre ou fecha este caderno durante a partida. Enquanto ele estiver aberto, o tempo e as ações ficam pausados. Use as setas ou os botões laterais para navegar; TAB fecha.\n\nMouse: clicar e arrastar. ESPAÇO: máscara ou esforço. Botão direito: máscara. A/D: mover a peça. E: guardar a tocha. R: destravar. L: religar a luz.",
                "8. CONTROLS & NOTEBOOK",
                "TAB opens or closes this notebook during play. While it is open, time and actions are paused. Use the arrow keys or side buttons to navigate; TAB closes it.\n\nMouse: click and drag. SPACE: mask or effort. Right mouse: mask. A/D: move the part. E: put away the torch. R: unjam it. L: restore the light."));

            return pages;
        }

        public IEnumerator Show(bool mandatory)
        {
            if (IsOpen) yield break;
            IsOpen = true;

            bool wasPaused = _c.Paused;
            bool wasLocked = _c.Clicks.Locked;
            bool wasModal = _c.Clicks.Modal;
            _c.Paused = true;
            _c.Clicks.Locked = false;
            _c.Clicks.Modal = true;

            int index = 0;

            RectTransform root = WeldUI.MakeRect("Notebook", _layer);
            WeldUI.Fill(root);
            _root = root.gameObject;

            Image dim = WeldUI.MakeImage(root, "Dim", null, new Color(0f, 0f, 0f, 0.78f));
            WeldUI.Fill(dim.rectTransform);

            RectTransform paper = WeldUI.MakeRect("Paper", root);
            WeldUI.Place(paper, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1380f, 820f));

            Image shadow = WeldUI.MakeImage(paper, "Shadow", null, new Color(0f, 0f, 0f, 0.5f));
            WeldUI.Fill(shadow.rectTransform, -16f, -16f, -16f, -16f);
            shadow.rectTransform.offsetMin = new Vector2(-8f, -22f);
            shadow.rectTransform.offsetMax = new Vector2(16f, 8f);

            Image sheet = WeldUI.MakeImage(paper, "Sheet", null, new Color(0.95f, 0.91f, 0.78f, 1f));
            WeldUI.Fill(sheet.rectTransform);

            Image margin = WeldUI.MakeImage(paper, "Margin", null, new Color(0.8f, 0.25f, 0.2f, 0.7f));
            margin.rectTransform.anchorMin = new Vector2(0f, 0f);
            margin.rectTransform.anchorMax = new Vector2(0f, 1f);
            margin.rectTransform.pivot = new Vector2(0f, 0.5f);
            margin.rectTransform.offsetMin = new Vector2(110f, 0f);
            margin.rectTransform.offsetMax = new Vector2(114f, 0f);

            Color ink = new Color(0.12f, 0.14f, 0.25f, 1f);

            TextMeshProUGUI title = WeldUI.MakeText(paper, "Title", "", 66f, new Color(0.75f, 0.2f, 0.12f, 1f), TextAlignmentOptions.TopLeft);
            WeldUI.Fill(title.rectTransform, 150f, 0f, 60f, 44f);

            TextMeshProUGUI body = WeldUI.MakeText(paper, "Body", "", 36f, ink, TextAlignmentOptions.TopLeft, true);
            WeldUI.Fill(body.rectTransform, 150f, 130f, 70f, 150f);

            TextMeshProUGUI counter = WeldUI.MakeText(paper, "Counter", "", 30f, ink, TextAlignmentOptions.BottomLeft);
            WeldUI.Fill(counter.rectTransform, 150f, 36f, 0f, 0f);

            TextMeshProUGUI footer = WeldUI.MakeText(paper, "Footer", "", 28f, ink, TextAlignmentOptions.BottomRight);
            WeldUI.Fill(footer.rectTransform, 0f, 36f, 60f, 0f);

            if (mandatory)
                yield return SelectDifficulty(paper);

            List<Page> pages = BuildPages(_c.Settings.piecesToWeld, _c.Difficulty);

            Image left = MakeButton(paper, "<", new Vector2(0f, 0.5f), new Vector2(-70f, 0f), ink);
            Image right = MakeButton(paper, ">", new Vector2(1f, 0.5f), new Vector2(70f, 0f), ink);

            int move = 0;
            WeldingInput.Hit leftHit = _c.Clicks.Register(left.rectTransform, () => move = -1, null, 300);
            WeldingInput.Hit rightHit = _c.Clicks.Register(right.rectTransform, () => move = 1, null, 300);

            bool done = false;
            int shown = -1;

            while (!done)
            {
                if (index != shown)
                {
                    shown = index;
                    title.text = pages[index].Title;
                    body.text = pages[index].Body;
                    counter.text = (index + 1) + " / " + pages.Count;

                    bool last = index == pages.Count - 1;
                    if (mandatory)
                        footer.text = last ? GameLanguage.T("SETA DIREITA: COMEÇAR  >", "RIGHT ARROW: START  >") : GameLanguage.T("SETA DIREITA: próxima  >", "RIGHT ARROW: next  >");
                    else
                        footer.text = GameLanguage.T("TAB: fechar", "TAB: close");

                    left.gameObject.SetActive(index > 0);
                    right.gameObject.SetActive(!(last && !mandatory));
                    _c.Audio.Play(_c.Sounds.uiClick);
                }

                if (Input.GetKeyDown(KeyCode.RightArrow)) move = 1;
                if (Input.GetKeyDown(KeyCode.LeftArrow)) move = -1;

                if (move != 0)
                {
                    if (move > 0 && index == pages.Count - 1)
                    {
                        if (mandatory) done = true;
                    }
                    else
                    {
                        index = Mathf.Clamp(index + move, 0, pages.Count - 1);
                    }

                    move = 0;
                }

                if (!mandatory && Input.GetKeyDown(KeyCode.Tab)) done = true;

                yield return null;
            }

            _c.Clicks.Unregister(leftHit);
            _c.Clicks.Unregister(rightHit);
            Dispose();

            _c.Paused = wasPaused;
            _c.Clicks.Modal = wasModal;
            _c.Clicks.Locked = wasLocked;
        }

        private IEnumerator SelectDifficulty(RectTransform paper)
        {
            RectTransform selection = WeldUI.MakeRect("DifficultySelection", paper);
            WeldUI.Fill(selection);

            TextMeshProUGUI title = WeldUI.MakeText(selection, "DifficultyTitle",
                GameLanguage.T("ESCOLHA A DIFICULDADE", "CHOOSE DIFFICULTY"), 54f,
                new Color(0.75f, 0.2f, 0.12f, 1f), TextAlignmentOptions.Center);
            WeldUI.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -130f), new Vector2(1100f, 80f),
                new Vector2(0.5f, 1f));

            TextMeshProUGUI subtitle = WeldUI.MakeText(selection, "DifficultySubtitle",
                GameLanguage.T("A dificuldade muda a quantidade de soldas, o oxigênio e o rigor dos eventos.",
                    "Difficulty changes the number of welds, oxygen supply and event severity."), 27f,
                new Color(0.15f, 0.16f, 0.2f, 1f), TextAlignmentOptions.Center, true);
            WeldUI.Place(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -235f), new Vector2(1050f, 70f),
                new Vector2(0.5f, 1f));

            bool selected = false;
            WeldingInput.Hit[] hits = new WeldingInput.Hit[3];
            Image[] buttons = new Image[3];
            WeldingDifficulty[] modes = { WeldingDifficulty.Easy, WeldingDifficulty.Medium, WeldingDifficulty.Hard };
            string[] names = { "FÁCIL", "MÉDIO", "DIFÍCIL" };
            string[] details =
            {
                GameLanguage.T("2 SOLDAS\nMais oxigênio e tolerância", "2 WELDS\nMore oxygen and tolerance"),
                GameLanguage.T("4 SOLDAS\nEquilíbrio entre risco e fôlego", "4 WELDS\nBalanced risk and stamina"),
                GameLanguage.T("6 SOLDAS\nMenos oxigênio e eventos severos", "6 WELDS\nLess oxygen and harsher events")
            };
            Color[] accents =
            {
                new Color(0.25f, 0.68f, 0.42f, 1f),
                new Color(0.88f, 0.58f, 0.16f, 1f),
                new Color(0.78f, 0.2f, 0.18f, 1f)
            };

            for (int i = 0; i < 3; i++)
            {
                int captured = i;
                buttons[i] = WeldUI.MakeProp(selection, "Difficulty_" + names[i], null,
                    new Vector2((i - 1) * 420f, -40f), new Vector2(370f, 230f), accents[i],
                    names[i] + "\n\n" + details[i], 27f);
                WeldUI.AddShadow(buttons[i], 0.28f);
                TextMeshProUGUI buttonText = buttons[i].GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.color = Color.white;
                    buttonText.fontStyle = FontStyles.Bold;
                }
                hits[i] = _c.Clicks.Register(buttons[i].rectTransform, () =>
                {
                    _c.SetDifficulty(modes[captured]);
                    selected = true;
                }, null, 500);
            }

            TextMeshProUGUI footer = WeldUI.MakeText(selection, "DifficultyFooter",
                GameLanguage.T("Selecione uma opção para continuar.", "Select an option to continue."), 24f,
                new Color(0.28f, 0.28f, 0.3f, 1f), TextAlignmentOptions.Center);
            WeldUI.Place(footer.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 65f), new Vector2(900f, 45f),
                new Vector2(0.5f, 0f));

            while (!selected)
            {
                WeldingInput.Hit hovered = _c.Clicks.Hovered;
                for (int i = 0; i < buttons.Length; i++)
                {
                    float targetScale = hovered == hits[i] ? 1.035f : 1f;
                    float current = buttons[i].rectTransform.localScale.x;
                    float next = Mathf.Lerp(current, targetScale, 0.22f);
                    buttons[i].rectTransform.localScale = new Vector3(next, next, 1f);
                }
                yield return null;
            }

            for (int i = 0; i < hits.Length; i++) _c.Clicks.Unregister(hits[i]);
            _c.Audio.Play(_c.Sounds.uiClick);
            Object.Destroy(selection.gameObject);
        }

        private Image MakeButton(RectTransform parent, string symbol, Vector2 anchor, Vector2 pos, Color ink)
        {
            Image img = WeldUI.MakeImage(parent, "Btn" + symbol, WeldUI.Circle, new Color(0.95f, 0.91f, 0.78f, 1f));
            WeldUI.Place(img.rectTransform, anchor, pos, new Vector2(110f, 110f));
            TextMeshProUGUI text = WeldUI.MakeText(img.transform, "Symbol", symbol, 80f, ink, TextAlignmentOptions.Center);
            WeldUI.Fill(text.rectTransform);
            return img;
        }

        /// <summary>Fecha na hora (usado quando o minigame é cancelado).</summary>
        public void Dispose()
        {
            if (_root != null) Object.Destroy(_root);
            _root = null;
            IsOpen = false;
        }
    }
}
