using UnityEngine;

namespace Core.Systems
{
    public class PersistentUI : MonoBehaviour
    {
        private void Awake()
        {
            // Garante que o FPS e Canvas não sejam destruídos ao trocar de cena
            DontDestroyOnLoad(gameObject);
        }
    }
}