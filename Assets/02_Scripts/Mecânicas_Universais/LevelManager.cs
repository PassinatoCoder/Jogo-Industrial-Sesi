
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core.Systems
{
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void LoadFábricaMundoSenai()
        {
            SceneManager.LoadScene("Fabrica Mundo Senai");
        }

        public void LoadPrensa_Minigame()
        {
            SceneManager.LoadScene("Prensa Minigame");
        }

        public void LoadSorteio()
        {
            SceneManager.LoadScene("Sorteio");
        }

        public void LoadPrensa_Parte2()
        {
            SceneManager.LoadScene("Minigame Prensa Parte 2");
        }

        public void LoadControle_Qualidade()
        {
            SceneManager.LoadScene("Minigame Controle de Qualidade");
        }

        public void LoadSolda_Industrial()
        {
            SceneManager.LoadScene("Solda_Industrial");
        }

        public void QuitGame()
        {
            Application.Quit();
        }
    }
}
