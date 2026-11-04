using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Minigames.Welding
{
    public class PhaseEPI : MonoBehaviour
    {
        [Header("Controle Central")]
        [SerializeField] private WeldingMinigameController controller;

        [Header("Configuração de UI / Sprites 2D")]
        [Tooltip("Sprite do braço da personagem alcançando algo")]
        [SerializeField] private GameObject armSpriteOverlay;
        [Tooltip("Imagem da tela inteira que pisca rápido (diferente da piscada de mudança de fase)")]
        [SerializeField] private Image fastBlinkImage;
        [Tooltip("Texto na tela: Pressione [1] Luvas, [2] Máscara, [3] Avental")]
        [SerializeField] private GameObject uiPromptsText;

        [Header("Sprites dos Itens (Na Mesa/Cabide)")]
        [SerializeField] private GameObject spriteGlovesOnTable;
        [SerializeField] private GameObject spriteMaskOnTable;
        [SerializeField] private GameObject spriteApronOnTable;

        [Header("Minigame do Avental")]
        [SerializeField] private Slider tieApronProgressBar;
        [SerializeField] private float timeToTieApron = 2.5f;

        // Estado atual
        private bool _hasGloves = false;
        private bool _hasMask = false;
        private bool _hasApron = false;

        private bool _isActionLocked = false;
        private float _apronHoldTimer = 0f;
        private bool _isTyingApron = false;

        private void OnEnable()
        {
            // Quando a fase 1 começa, liga os textos e os itens visíveis
            if (uiPromptsText != null) uiPromptsText.SetActive(true);
            if (armSpriteOverlay != null) armSpriteOverlay.SetActive(false);
            if (tieApronProgressBar != null) tieApronProgressBar.gameObject.SetActive(false);

            Debug.Log("[Fase EPI] Etapa iniciada. Escolha as peças (1, 2 ou 3).");
        }

        private void Update()
        {
            if (_isActionLocked) return;

            // Se o jogador segurar espaço e já ativou a amarração do avental
            if (_isTyingApron)
            {
                ProcessApronMinigame();
                return;
            }

            // Seleção rápida de itens
            if (Input.GetKeyDown(KeyCode.Alpha1) && !_hasGloves)
            {
                StartCoroutine(EquipItemRoutine(spriteGlovesOnTable, "Luvas", false));
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2) && !_hasMask)
            {
                StartCoroutine(EquipItemRoutine(spriteMaskOnTable, "Máscara", false));
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3) && !_hasApron)
            {
                // O avental engatilha o minigame de segurar o botão
                StartCoroutine(EquipItemRoutine(spriteApronOnTable, "Avental", true));
            }
        }

        private IEnumerator EquipItemRoutine(GameObject itemSpriteOnTable, string itemName, bool requiresTieMinigame)
        {
            _isActionLocked = true;
            Debug.Log($"[Fase EPI] Pegando {itemName}...");

            // 1. Mostra o braço indo pegar
            if (armSpriteOverlay != null) armSpriteOverlay.SetActive(true);
            yield return new WaitForSeconds(0.4f); // Pequena pausa para a animação do braço ler na mente do jogador

            // Se for o avental, muda para o modo de amarrar e pausa a rotina principal
            if (requiresTieMinigame)
            {
                Debug.Log($"[Fase EPI] Segure [ESPAÇO] para amarrar o {itemName} pesado.");
                _isTyingApron = true;
                if (tieApronProgressBar != null)
                {
                    tieApronProgressBar.gameObject.SetActive(true);
                    tieApronProgressBar.value = 0;
                }
                yield break; // Sai da rotina. O método Update() vai cuidar do minigame de segurar o botão
            }

            // Se não for o avental, pisca a tela rápido e equipa direto
            yield return StartCoroutine(FastBlinkAndEquip(itemSpriteOnTable, itemName));
        }

        private IEnumerator FastBlinkAndEquip(GameObject itemSpriteOnTable, string itemName)
        {
            // Pisca rápido (fecha)
            if (fastBlinkImage != null) fastBlinkImage.color = new Color(0, 0, 0, 1f);

            // SOME com o item da mesa e some com o braço
            if (itemSpriteOnTable != null) itemSpriteOnTable.SetActive(false);
            if (armSpriteOverlay != null) armSpriteOverlay.SetActive(false);

            yield return new WaitForSeconds(0.15f); // Fica preto por quase nada

            // Abre o olho
            if (fastBlinkImage != null) fastBlinkImage.color = new Color(0, 0, 0, 0f);

            // Marca como vestido
            if (itemName == "Luvas") _hasGloves = true;
            else if (itemName == "Máscara") _hasMask = true;
            else if (itemName == "Avental") _hasApron = true;

            _isActionLocked = false;
            CheckPhaseCompletion();
        }

        private void ProcessApronMinigame()
        {
            if (Input.GetKey(KeyCode.Space))
            {
                _apronHoldTimer += Time.deltaTime;
                if (tieApronProgressBar != null) tieApronProgressBar.value = _apronHoldTimer / timeToTieApron;

                if (_apronHoldTimer >= timeToTieApron)
                {
                    // Conseguiu amarrar!
                    _isTyingApron = false;
                    _apronHoldTimer = 0f;
                    if (tieApronProgressBar != null) tieApronProgressBar.gameObject.SetActive(false);

                    // Finaliza com a piscada rápida padrão
                    StartCoroutine(FastBlinkAndEquip(spriteApronOnTable, "Avental"));
                }
            }
            else if (Input.GetKeyUp(KeyCode.Space))
            {
                // Deixou cair
                Debug.LogWarning("[Fase EPI] Você soltou o avental pesado! Amarre de novo.");
                _apronHoldTimer = 0f;
                if (tieApronProgressBar != null) tieApronProgressBar.value = 0;
            }
        }

        private void CheckPhaseCompletion()
        {
            if (_hasGloves && _hasMask && _hasApron)
            {
                Debug.Log("[Fase EPI] Todos os EPIs vestidos. Iniciando piscada longa (transição de fase)...");
                if (uiPromptsText != null) uiPromptsText.SetActive(false);

                // Pede para o controlador mestre piscar a tela e ir para a Fase 2 (Limpeza)
                controller.AdvanceToPhase(WeldingMinigameController.WeldingPhase.Cleaning);
            }
        }
    }
}