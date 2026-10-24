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

        public void LoadMenu_Principal() => SceneManager.LoadScene(0);
        public void LoadTutorial() => SceneManager.LoadScene(1);
        public void LoadPrensa_Minigame() => SceneManager.LoadScene(2);
        public void QuitGame() => Application.Quit();
    }
}