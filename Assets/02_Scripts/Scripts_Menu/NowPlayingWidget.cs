using System.Collections;
using Core.Audio;
using TMPro;
using UnityEngine;

namespace Core.UI.Menu
{
    /// <summary>
    /// "Tocando a faixa: [nome]" com um disco girando devagar.
    /// Aparece (desliza + fade) toda vez que a música troca, fica alguns segundos e some.
    /// O disco "acelera" quando entra e "desacelera" quando sai.
    /// Coloque DENTRO do Buttons Group: assim some sozinho quando o tablet abre.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class NowPlayingWidget : MonoBehaviour
    {
        [Header("Referências")]
        [SerializeField] private TMP_Text trackNameText;
        [SerializeField] private RectTransform discIcon;
        [Tooltip("O que desliza. Vazio = este próprio objeto.")]
        [SerializeField] private RectTransform slideRoot;

        [Header("Texto")]
        [Tooltip("{0} = nome da faixa. Dá pra usar rich text, ex.: <size=70%>TOCANDO AGORA</size>\\n{0}")]
        [SerializeField] private string textFormat = "Tocando a faixa: {0}";

        [Header("Tempos")]
        [Tooltip("Espera depois da troca pra aparecer (deixa a transição de tela terminar).")]
        [SerializeField] private float showDelay = 0.6f;
        [SerializeField] private float displayDuration = 4.5f;
        [SerializeField] private float inTime = 0.5f;
        [SerializeField] private float outTime = 0.45f;

        [Header("Movimento")]
        [Tooltip("Graus por segundo. Negativo = gira pra direita.")]
        [SerializeField] private float discSpeed = -25f;
        [Tooltip("De quantos pixels o painel vem deslizando da esquerda.")]
        [SerializeField] private float slideDistance = 70f;

        private CanvasGroup _group;
        private Vector2 _basePos;
        private float _visible;
        private Coroutine _routine;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            if (slideRoot == null) slideRoot = transform as RectTransform;
            _basePos = slideRoot.anchoredPosition;
            Apply(0f);
        }

        private void Start()
        {
            // Todos os Awake já rodaram, então o Instance existe
            if (MenuAudioManager.Instance != null) MenuAudioManager.Instance.OnTrackChanged += ShowTrack;
            else Debug.LogWarning("[NowPlayingWidget] MenuAudioManager não encontrado na cena.");
        }

        private void OnDestroy()
        {
            if (MenuAudioManager.Instance != null) MenuAudioManager.Instance.OnTrackChanged -= ShowTrack;
        }

        private void OnDisable()
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = null;
            Apply(0f);
        }

        private void Update()
        {
            if (_visible <= 0.001f || discIcon == null || MenuMotion.ReduceMotion) return;
            discIcon.Rotate(0f, 0f, discSpeed * _visible * Time.unscaledDeltaTime);
        }

        private void ShowTrack(string trackName)
        {
            if (!isActiveAndEnabled) return;

            if (trackNameText != null) trackNameText.text = string.Format(textFormat, trackName);

            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(ShowRoutine());
        }

        private IEnumerator ShowRoutine()
        {
            // Se já estava na tela (troca seguida), só atualiza o texto e reinicia a contagem
            if (_visible < 0.99f)
            {
                yield return WaitRealtime(showDelay);
                yield return Animate(_visible, 1f, inTime, true);
            }

            yield return WaitRealtime(displayDuration);
            yield return Animate(1f, 0f, outTime, false);

            _routine = null;
        }

        private IEnumerator Animate(float from, float to, float duration, bool entering)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
                float x = Mathf.Clamp01(t / duration);
                float eased = entering ? MenuEasing.OutCubic(x) : 1f - x * x;
                Apply(entering ? Mathf.Lerp(from, to, eased) : Mathf.Lerp(to, from, eased));
                yield return null;
            }

            Apply(to);
        }

        private static IEnumerator WaitRealtime(float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
                yield return null;
            }
        }

        private void Apply(float visible)
        {
            _visible = visible;
            if (_group != null) _group.alpha = visible;

            float slide = MenuMotion.ReduceMotion ? 0f : -slideDistance * (1f - visible);
            if (slideRoot != null) slideRoot.anchoredPosition = _basePos + new Vector2(slide, 0f);
        }
    }
}