using UnityEngine;
using TMPro;

namespace Core.UI.Menu
{
    public class FutureFeatureMessage : MonoBehaviour
    {
        [Header("Message Settings")]
        [SerializeField] private GameObject messagePanel;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private float displayDuration = 2.5f;

        private void Start()
        {
            if (messagePanel != null) messagePanel.SetActive(false);
        }

        public void ShowMessage()
        {
            if (messagePanel == null || messageText == null) return;

            messageText.text = "VIRÁ EM VERSÕES FUTURAS!";
            messagePanel.SetActive(true);

            CancelInvoke(nameof(HideMessage));
            Invoke(nameof(HideMessage), displayDuration);
        }

        private void HideMessage()
        {
            messagePanel.SetActive(false);
        }
    }
}