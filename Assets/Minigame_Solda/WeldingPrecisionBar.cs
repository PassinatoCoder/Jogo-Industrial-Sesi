using UnityEngine;
using UnityEngine.UI;
using Core.Systems;

namespace Core.Minigames.Welding
{
    public class WeldingPrecisionBar : MonoBehaviour
    {
        [Header("Referências UI")]
        [Tooltip("O CanvasGroup principal da interface de solda")]
        [SerializeField] private CanvasGroup uiGroup;
        [Tooltip("O cursor/ponto de calor que o jogador controla")]
        [SerializeField] private RectTransform heatCursor;
        [Tooltip("A área verde ideal (onde o cursor deve ficar)")]
        [SerializeField] private RectTransform targetZone;
        [Tooltip("A barra de progresso geral do minigame (0 a 1)")]
        [SerializeField] private Slider progressSlider;

        [Header("Configurações do Minigame")]
        [SerializeField] private float heatGravity = 200f;     // Força que puxa o cursor para baixo
        [SerializeField] private float heatThrust = 400f;      // Força ao apertar o botão
        [SerializeField] private float cursorMaxSpeed = 500f;  // Velocidade máxima do cursor
        [SerializeField] private float progressSpeed = 0.15f;  // Quão rápido a barra principal enche
        [SerializeField] private float decaySpeed = 0.25f;     // Quão rápido a barra principal esvazia se errar

        [Header("Recompensas/Penalidades")]
        [SerializeField] private int perfectWeldReward = 30;
        [SerializeField] private int badWeldPenalty = 15;

        private float _cursorPosition; // Posição vertical atual
        private float _cursorVelocity; // Velocidade vertical
        private float _minPosition;
        private float _maxPosition;

        private float _currentProgress;
        private bool _isWeldingActive;

        private WeldingMinigameController _mainController;

        private void Awake()
        {
            if (uiGroup != null)
            {
                uiGroup.alpha = 0f;
                uiGroup.interactable = false;
                uiGroup.blocksRaycasts = false;
            }
        }

        private void Start()
        {
            _mainController = FindFirstObjectByType<WeldingMinigameController>();

            // Define os limites baseados na altura do fundo da UI (assumindo que o pai do heatCursor é o fundo)
            RectTransform bgRect = heatCursor.parent.GetComponent<RectTransform>();
            _minPosition = -(bgRect.rect.height / 2f) + (heatCursor.rect.height / 2f);
            _maxPosition = (bgRect.rect.height / 2f) - (heatCursor.rect.height / 2f);
        }

        private void Update()
        {
            if (!_isWeldingActive) return;

            UpdateCursorPhysics();
            CheckZoneAndProgress();
        }

        /// <summary>
        /// Inicia o minigame da barra de precisão (chamado pelo WeldingMinigameController)
        /// </summary>
        public void StartPrecisionMinigame()
        {
            _isWeldingActive = true;
            _cursorPosition = _minPosition;
            _cursorVelocity = 0f;
            _currentProgress = 0f;

            if (progressSlider != null) progressSlider.value = 0f;

            if (uiGroup != null)
            {
                uiGroup.alpha = 1f;
                uiGroup.interactable = true;
                uiGroup.blocksRaycasts = true;
            }

            Debug.Log("Minigame de Solda Iniciado! Segure ESPAÇO ou Clique Esquerdo para manter o calor na zona verde.");
        }

        private void UpdateCursorPhysics()
        {
            // Aplica impulso se o jogador estiver segurando o botão
            if (Input.GetKey(KeyCode.Space) || Input.GetMouseButton(0))
            {
                _cursorVelocity += heatThrust * Time.deltaTime;
            }
            else
            {
                _cursorVelocity -= heatGravity * Time.deltaTime;
            }

            // Limita a velocidade para não ficar incontrolável
            _cursorVelocity = Mathf.Clamp(_cursorVelocity, -cursorMaxSpeed, cursorMaxSpeed);

            // Atualiza a posição
            _cursorPosition += _cursorVelocity * Time.deltaTime;

            // Restringe aos limites superior e inferior (faz bater e parar)
            if (_cursorPosition >= _maxPosition)
            {
                _cursorPosition = _maxPosition;
                _cursorVelocity = 0f;
            }
            else if (_cursorPosition <= _minPosition)
            {
                _cursorPosition = _minPosition;
                _cursorVelocity = 0f;
            }

            // Move o objeto na UI
            heatCursor.anchoredPosition = new Vector2(heatCursor.anchoredPosition.x, _cursorPosition);
        }

        private void CheckZoneAndProgress()
        {
            // Verifica se o cursor está dentro da zona alvo
            float targetMin = targetZone.anchoredPosition.y - (targetZone.rect.height / 2f);
            float targetMax = targetZone.anchoredPosition.y + (targetZone.rect.height / 2f);

            bool isInsideZone = _cursorPosition >= targetMin && _cursorPosition <= targetMax;

            if (isInsideZone)
            {
                _currentProgress += progressSpeed * Time.deltaTime;
                // Efeito visual de acertando poderia vir aqui (mudar cor da barra)
            }
            else
            {
                _currentProgress -= decaySpeed * Time.deltaTime;
                // Efeito visual de errando (faíscas falhando)
            }

            _currentProgress = Mathf.Clamp01(_currentProgress);

            if (progressSlider != null)
                progressSlider.value = _currentProgress;

            // Condição de Vitória
            if (_currentProgress >= 1f)
            {
                FinishPrecisionMinigame(true);
            }
            // Condição de Derrota (se cair de novo pra zero depois de já ter começado a pontuar muito)
            // Aqui optamos por não dar Game Over imediato, mas obrigar o jogador a continuar lutando, 
            // a não ser que ele zere totalmente após já ter progredido (opcional).
        }

        private void FinishPrecisionMinigame(bool success)
        {
            _isWeldingActive = false;

            if (uiGroup != null)
            {
                uiGroup.alpha = 0f;
                uiGroup.interactable = false;
                uiGroup.blocksRaycasts = false;
            }

            if (success)
            {
                Debug.Log("Solda perfeita! Barra completada.");
                if (PlayerIntegrity.Instance != null)
                    PlayerIntegrity.Instance.AddIntegrity(perfectWeldReward);

                if (_mainController != null)
                    _mainController.ConcludeWelding();
            }
            else
            {
                Debug.Log("Solda mal feita. Retrabalho necessário.");
                if (PlayerIntegrity.Instance != null)
                    PlayerIntegrity.Instance.LoseIntegrity(badWeldPenalty);

                // Reinicia a barra de precisão para tentar de novo
                StartPrecisionMinigame();
            }
        }
    }
}