using System.Collections;
using UnityEngine;

public class SaidaMascote : MonoBehaviour
{
    [Header("Caminhada")]
    [SerializeField] private Transform pontoDestino;
    [SerializeField] private float velocidade = 5f;
    [SerializeField] private bool inverterLadoPadrao = false; // Marque no Inspector se ele virar pro lado errado

    [Header("Transição de Câmeras")]
    [SerializeField] private GameObject cameraEstabelecimento;
    [SerializeField] private GameObject cameraPlayer;

    [Header("UI do Tutorial")]
    [SerializeField] private GameObject popupWASD;

    public void IrEmbora()
    {
        StartCoroutine(RotinaVazar());
    }

    private IEnumerator RotinaVazar()
    {
        // 1. Vira o mascote imediatamente para a direção do destino
        float direcao = Mathf.Sign(pontoDestino.position.x - transform.position.x);
        float fator = inverterLadoPadrao ? -1f : 1f;
        transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x) * direcao * fator, transform.localScale.y, transform.localScale.z);

        // 2. O Mascote anda até o ponto de destino
        while (Vector2.Distance(transform.position, pontoDestino.position) > 0.1f)
        {
            transform.position = Vector2.MoveTowards(transform.position, pontoDestino.position, velocidade * Time.deltaTime);
            yield return null; // Pausa a execução aqui até o próximo frame
        }

        // 3. Troca as câmeras (Corte seco da sala pro Player)
        if (cameraEstabelecimento != null) cameraEstabelecimento.SetActive(false);
        if (cameraPlayer != null) cameraPlayer.SetActive(true);

        // 4. Libera o jogo pra Protagonista e liga o Pop-up animado
        GerenciadorTutorial.Instancia.DesbloquearMovimento();
        if (popupWASD != null) popupWASD.SetActive(true);

        // 5. Mascote é desativado da memória ativa
        gameObject.SetActive(false);
    }
}