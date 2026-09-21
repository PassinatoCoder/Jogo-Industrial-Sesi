using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(BoxCollider2D))]
public class GatilhoArea : MonoBehaviour
{
    [Header("Configurações")]
    [SerializeField] private string tagAlvo = "Player";
    [SerializeField] private bool dispararApenasUmaVez = true;

    [Header("Eventos")]
    public UnityEvent aoEntrarNaArea;

    private bool jaDisparou = false;

    private void Awake()
    {
        // Garante que o colisor seja um sensor invisível
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (jaDisparou && dispararApenasUmaVez) return;

        if (collision.CompareTag(tagAlvo))
        {
            jaDisparou = true;
            aoEntrarNaArea?.Invoke();
        }
    }
}