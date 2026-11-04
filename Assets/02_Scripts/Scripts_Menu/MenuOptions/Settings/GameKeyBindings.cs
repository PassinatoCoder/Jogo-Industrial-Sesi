using System;
using System.Text;
using UnityEngine;

namespace Core.Settings
{
    /// <summary>
    /// Ações do jogo que o jogador pode remapear.
    /// IMPORTANTE: a ordem aqui TEM que bater com a lista "Defaults" do GameKeyBindings.
    /// Quer mudar nomes/teclas padrão? Edite os dois lugares (enum + Defaults + GetLabel).
    /// </summary>
    public enum GameAction
    {
        MoveUp,
        MoveDown,
        MoveLeft,
        MoveRight,
        Jump,
        Crouch,
        Interact,
        Use,
        Inspect,
        Drop,
        Inventory,
        Belt1,
        Belt2,
        Belt3,
        Belt4,
        Belt5
    }

    /// <summary>
    /// Teclas do jogo (Input antigo / KeyCode). O menu de Opções > Controles mexe aqui.
    /// Nos seus scripts de jogador, troque:
    ///   Input.GetKey(KeyCode.E)    ->  GameKeyBindings.GetKey(GameAction.Interact)
    ///   Input.GetKeyDown(KeyCode.E) -> GameKeyBindings.GetKeyDown(GameAction.Interact)
    /// </summary>
    public static class GameKeyBindings
    {
        private const string Prefix = "bind_";

        private static readonly KeyCode[] Defaults =
        {
            KeyCode.W, KeyCode.S, KeyCode.A, KeyCode.D,
            KeyCode.Space, KeyCode.LeftControl,
            KeyCode.E, KeyCode.F, KeyCode.Y, KeyCode.G, KeyCode.I,
            KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5
        };

        private static KeyCode[] _keys;

        public static event Action Changed;

        public static int Count => Defaults.Length;

        // ---------- Uso no jogo ----------

        public static KeyCode Get(GameAction action)
        {
            EnsureLoaded();
            return _keys[(int)action];
        }

        public static bool GetKey(GameAction action) => Input.GetKey(Get(action));
        public static bool GetKeyDown(GameAction action) => Input.GetKeyDown(Get(action));
        public static bool GetKeyUp(GameAction action) => Input.GetKeyUp(Get(action));

        public static KeyCode GetDefault(GameAction action) => Defaults[(int)action];

        // ---------- Alteração ----------

        /// <summary>
        /// Troca a tecla de uma ação. Se outra ação já usa a tecla, as duas trocam de lugar.
        /// Devolve false se a tecla não pode ser usada (Esc é reservada).
        /// </summary>
        public static bool Set(GameAction action, KeyCode key)
        {
            EnsureLoaded();
            if (key == KeyCode.None || key == KeyCode.Escape) return false;

            int index = (int)action;
            KeyCode old = _keys[index];
            if (old == key) return true;

            for (int i = 0; i < _keys.Length; i++)
            {
                if (i != index && _keys[i] == key)
                {
                    _keys[i] = old; // quem tinha a tecla fica com a antiga
                    Save(i);
                }
            }

            _keys[index] = key;
            Save(index);
            PlayerPrefs.Save();
            Changed?.Invoke();
            return true;
        }

        public static void ResetAll()
        {
            EnsureLoaded();
            for (int i = 0; i < _keys.Length; i++)
            {
                _keys[i] = Defaults[i];
                Save(i);
            }

            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        // ---------- Textos ----------

        public static string GetLabel(GameAction action)
        {
            switch (action)
            {
                case GameAction.MoveUp:    return GameLanguage.T("Andar para cima", "Move up");
                case GameAction.MoveDown:  return GameLanguage.T("Andar para baixo", "Move down");
                case GameAction.MoveLeft:  return GameLanguage.T("Andar para a esquerda", "Move left");
                case GameAction.MoveRight: return GameLanguage.T("Andar para a direita", "Move right");
                case GameAction.Jump:      return GameLanguage.T("Pular", "Jump");
                case GameAction.Crouch:    return GameLanguage.T("Agachar", "Crouch");
                case GameAction.Interact:  return GameLanguage.T("Pegar / Interagir", "Pick up / Interact");
                case GameAction.Use:       return GameLanguage.T("Usar item", "Use item");
                case GameAction.Inspect:   return GameLanguage.T("Inspecionar", "Inspect");
                case GameAction.Drop:      return GameLanguage.T("Guardar / Soltar", "Store / Drop");
                case GameAction.Inventory: return GameLanguage.T("Mochila", "Backpack");
                case GameAction.Belt1:     return GameLanguage.T("Cinto: espaço 1", "Belt: slot 1");
                case GameAction.Belt2:     return GameLanguage.T("Cinto: espaço 2", "Belt: slot 2");
                case GameAction.Belt3:     return GameLanguage.T("Cinto: espaço 3", "Belt: slot 3");
                case GameAction.Belt4:     return GameLanguage.T("Cinto: espaço 4", "Belt: slot 4");
                case GameAction.Belt5:     return GameLanguage.T("Cinto: espaço 5", "Belt: slot 5");
                default:                   return action.ToString();
            }
        }

        /// <summary>Nome bonito da tecla (ex.: LeftControl vira "Ctrl Esq.").</summary>
        public static string DisplayName(KeyCode key)
        {
            switch (key)
            {
                case KeyCode.None:         return "-";
                case KeyCode.Space:        return GameLanguage.T("Espaço", "Space");
                case KeyCode.Return:
                case KeyCode.KeypadEnter:  return "Enter";
                case KeyCode.Backspace:    return GameLanguage.T("Apagar", "Backspace");
                case KeyCode.Tab:          return "Tab";
                case KeyCode.LeftControl:  return GameLanguage.T("Ctrl Esq.", "Left Ctrl");
                case KeyCode.RightControl: return GameLanguage.T("Ctrl Dir.", "Right Ctrl");
                case KeyCode.LeftShift:    return GameLanguage.T("Shift Esq.", "Left Shift");
                case KeyCode.RightShift:   return GameLanguage.T("Shift Dir.", "Right Shift");
                case KeyCode.LeftAlt:      return GameLanguage.T("Alt Esq.", "Left Alt");
                case KeyCode.RightAlt:     return GameLanguage.T("Alt Dir.", "Right Alt");
                case KeyCode.UpArrow:      return GameLanguage.T("Seta cima", "Up arrow");
                case KeyCode.DownArrow:    return GameLanguage.T("Seta baixo", "Down arrow");
                case KeyCode.LeftArrow:    return GameLanguage.T("Seta esq.", "Left arrow");
                case KeyCode.RightArrow:   return GameLanguage.T("Seta dir.", "Right arrow");
            }

            string name = key.ToString();
            if (name.StartsWith("Alpha")) return name.Substring(5);
            if (name.StartsWith("Keypad")) return "Num " + name.Substring(6);
            return name;
        }

        /// <summary>
        /// Troca marcadores como {Interact} ou {Jump} pelo nome da tecla atual.
        /// Útil nos textos do tutorial: "Aperte {Interact} para pegar".
        /// </summary>
        public static string ReplaceTokens(string text)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf('{') < 0) return text;

            StringBuilder sb = new StringBuilder(text);
            foreach (GameAction action in Enum.GetValues(typeof(GameAction)))
                sb.Replace("{" + action + "}", DisplayName(Get(action)));

            return sb.ToString();
        }

        // ---------- Internos ----------

        private static void EnsureLoaded()
        {
            if (_keys != null) return;

            _keys = new KeyCode[Defaults.Length];
            for (int i = 0; i < _keys.Length; i++)
            {
                int stored = PlayerPrefs.GetInt(Prefix + (GameAction)i, (int)Defaults[i]);
                _keys[i] = Enum.IsDefined(typeof(KeyCode), stored) ? (KeyCode)stored : Defaults[i];
            }
        }

        private static void Save(int index)
        {
            PlayerPrefs.SetInt(Prefix + (GameAction)index, (int)_keys[index]);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _keys = null;
            Changed = null;
        }
    }
}
