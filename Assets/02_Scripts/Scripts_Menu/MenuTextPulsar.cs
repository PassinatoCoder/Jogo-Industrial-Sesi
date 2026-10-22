using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

namespace Core.UI.Menu
{
    [RequireComponent(typeof(TMP_Text))]
    public class MenuTextPulsar : MonoBehaviour
    {
        [Header("Batimento (Estilo Orgânico / RDR2)")]
        [Tooltip("Duração de um ciclo completo de batimento + descanso.")]
        [SerializeField] private float beatInterval = 1.4f;
        [SerializeField, Range(0f, 0.4f)] private float beatScale = 0.1f;

        [Header("Respiração (Transparência Cinematográfica)")]
        [SerializeField] private float breathSpeed = 1.5f;
        [SerializeField, Range(0f, 1f)] private float minAlpha = 0.5f;

        [Header("Brilho Dinâmico nas Letras")]
        [SerializeField] private Color highlightColor = new Color(1f, 0.85f, 0.25f);
        [Tooltip("Velocidade do feixe de luz passando pelas letras.")]
        [SerializeField] private float shimmerSpeed = 7f;
        [Tooltip("Largura da onda de brilho.")]
        [SerializeField] private float shimmerWidth = 3f;
        [Tooltip("Pausa orgânica entre uma passada e outra (segundos).")]
        [SerializeField] private float shimmerPause = 1f;
        [Tooltip("Elevação sutil (hop) das letras ao passar o brilho.")]
        [SerializeField] private float hopHeight = 6f;

        [Header("Saída Cinematográfica (Ao pressionar tecla)")]
        [SerializeField] private float dismissDuration = 0.35f;
        [SerializeField] private float dismissScale = 1.5f;
        [SerializeField] private UnityEvent onDismissed;

        private TMP_Text _text;
        private Vector3 _baseScale;
        private Color _baseColor;

        private bool _dismissing;
        private float _dismissFade = 1f;
        private float _dismissScaleCurrent = 1f;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
            _baseScale = transform.localScale;
            _baseColor = _text.color;
        }

        private void OnEnable()
        {
            _dismissing = false;
            _dismissFade = 1f;
            _dismissScaleCurrent = 1f;
        }

        private void OnDisable()
        {
            transform.localScale = _baseScale;
            if (_text != null) _text.ForceMeshUpdate();
        }

        /// <summary>Chame quando o jogador pressionar a tecla. Dispara onDismissed no fim com transição suave.</summary>
        public void Dismiss()
        {
            if (_dismissing) return;
            _dismissing = true;
            StartCoroutine(DismissRoutine());
        }

        private IEnumerator DismissRoutine()
        {
            float elapsed = 0f;
            while (elapsed < dismissDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float x = Mathf.Clamp01(elapsed / dismissDuration);
                _dismissFade = 1f - MenuEasing.OutCubic(x);
                _dismissScaleCurrent = Mathf.LerpUnclamped(1f, dismissScale, MenuEasing.OutBack(x));
                yield return null;
            }

            _dismissFade = 0f;
            onDismissed?.Invoke();
        }

        private void LateUpdate()
        {
            bool reduce = MenuMotion.ReduceMotion;
            float time = Time.unscaledTime;

            // Escala: Batimento orgânico + estouro de saída suave
            float beat = reduce ? 0f : EvaluateCinematicBeat(time);
            transform.localScale = _baseScale * ((1f + beat * beatScale) * _dismissScaleCurrent);

            // Transparência: Respiração lenta e fluida
            float breath = 0.5f + 0.5f * Mathf.Sin(time * breathSpeed);
            float alpha = Mathf.Lerp(minAlpha, 1f, breath) * _dismissFade * _baseColor.a;

            _text.ForceMeshUpdate();
            TMP_TextInfo info = _text.textInfo;
            int count = info.characterCount;
            if (count == 0) return;

            // Posição do brilho com espaçamento cinematográfico
            float sweepTime = (count + shimmerWidth * 2f) / shimmerSpeed;
            float cycle = sweepTime + shimmerPause;
            float shimmerPos = (time % cycle) * shimmerSpeed - shimmerWidth;

            for (int i = 0; i < count; i++)
            {
                TMP_CharacterInfo c = info.characterInfo[i];
                if (!c.isVisible) continue;

                float weight = 0f;
                if (!reduce)
                {
                    float dist = Mathf.Abs(i - shimmerPos);
                    weight = Mathf.Clamp01(1f - dist / shimmerWidth);
                    // Curva cúbica suave (Smoothstep refinado para brilho natural)
                    weight = weight * weight * (3f - 2f * weight);
                }

                int mat = c.materialReferenceIndex;
                int v = c.vertexIndex;
                Vector3[] verts = info.meshInfo[mat].vertices;
                Color32[] colors = info.meshInfo[mat].colors32;

                Color tint = Color.Lerp(_baseColor, highlightColor, weight);
                tint.a = alpha;
                Color32 tint32 = tint;
                Vector3 hop = new Vector3(0f, weight * hopHeight, 0f);

                for (int j = 0; j < 4; j++)
                {
                    verts[v + j] += hop;
                    colors[v + j] = tint32;
                }
            }

            for (int i = 0; i < info.meshInfo.Length; i++)
            {
                info.meshInfo[i].mesh.vertices = info.meshInfo[i].vertices;
                info.meshInfo[i].mesh.colors32 = info.meshInfo[i].colors32;
                _text.UpdateGeometry(info.meshInfo[i].mesh, i);
            }
        }

        // Batimento cardíaco refinado com padrão orgânico ("tum-tum" cinematográfico)
        private float EvaluateCinematicBeat(float time)
        {
            float p = (time % beatInterval) / beatInterval;
            float first = CinematicBump(p, 0.06f, 0.065f);
            float second = 0.7f * CinematicBump(p, 0.23f, 0.07f);
            return Mathf.Max(first, second);
        }

        private static float CinematicBump(float p, float center, float width)
        {
            float d = (p - center) / width;
            if (Mathf.Abs(d) >= 1f) return 0f;
            float v = 1f - d * d;
            // Suavização extra para eliminar picos secos
            return v * v * (3f - 2f * v);
        }
    }
}