using UnityEngine;
using System;

namespace Core.Dialogue
{
    public enum SpeakerType { Protagonist, NPC }

    [Serializable]
    public struct DialogueLine
    {
        public SpeakerType speaker;
        public string speakerName;
        [TextArea(3, 5)] public string text;
    }

    [CreateAssetMenu(fileName = "NewDialogue", menuName = "Cheetah/Dialogue Sequence")]
    public class DialogueSequence : ScriptableObject
    {
        public DialogueLine[] lines;
    }
}