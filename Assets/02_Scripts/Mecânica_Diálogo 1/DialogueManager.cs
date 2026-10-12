using UnityEngine;
using System;
using Core.Player;

namespace Core.Dialogue
{
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        [Header("Dependencies")]
        [SerializeField] private PlayerController playerController;
        [SerializeField] private float lineAdvanceDelay = 0.2f;

        public event Action<DialogueLine> OnLinePlayed;
        public event Action OnDialogueEnded;

        private DialogueSequence _currentSequence;
        private int _currentIndex;
        private bool _isActive;
        private float _lastLineTime;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void StartDialogue(DialogueSequence sequence)
        {
            if (_isActive) return;

            _currentSequence = sequence;
            _currentIndex = 0;
            _isActive = true;
            _lastLineTime = Time.time;

            LockPlayer(true);
            PlayCurrentLine();
        }

        private void Update()
        {
            if (!_isActive) return;

            if (Input.GetKeyDown(KeyCode.E) && Time.time >= _lastLineTime + lineAdvanceDelay)
            {
                NextLine();
            }
        }

        private void NextLine()
        {
            _currentIndex++;
            if (_currentIndex < _currentSequence.lines.Length)
            {
                PlayCurrentLine();
            }
            else
            {
                EndDialogue();
            }
        }

        private void PlayCurrentLine()
        {
            _lastLineTime = Time.time;
            DialogueLine line = _currentSequence.lines[_currentIndex];
            OnLinePlayed?.Invoke(line);

            string uiState = line.speaker == SpeakerType.Protagonist
                ? "[ESQUERDA EM FOCO | Direita opacidade reduzida]"
                : "[DIREITA EM FOCO | Esquerda opacidade reduzida]";

            Debug.Log($"DIÁLOGO {uiState} -> {line.speakerName}: {line.text}");
        }

        private void EndDialogue()
        {
            _isActive = false;
            _currentSequence = null;
            OnDialogueEnded?.Invoke();

            LockPlayer(false);
            Debug.Log("DIÁLOGO: Encerrado.");
        }

        private void LockPlayer(bool state)
        {
            if (playerController == null) return;

            playerController.isMovementUnlocked = !state;
            playerController.isJumpUnlocked = !state;
            playerController.isCrouchUnlocked = !state;
            playerController.isSprintUnlocked = !state;

            if (state) playerController.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        }
    }
}