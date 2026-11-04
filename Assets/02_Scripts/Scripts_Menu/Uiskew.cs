using UnityEngine;
using UnityEngine.UI;

namespace Core.UI.Menu
{
    /// <summary>
    /// Inclina (corta na diagonal) uma Image: transforma retângulos em faixas "de corrida", sem precisar de arte.
    /// Funciona em Image (não em texto). O corte é feito em torno do pivô do objeto.
    /// </summary>
    [AddComponentMenu("UI/Effects/Skew (Menu)")]
    public class UISkew : BaseMeshEffect
    {
        [Tooltip("Positivo = o topo vai pra direita (itálico). Negativo = pra esquerda.")]
        [SerializeField, Range(-45f, 45f)] private float skewDegrees = 14f;

        public float SkewDegrees
        {
            get => skewDegrees;
            set
            {
                skewDegrees = value;
                if (graphic != null) graphic.SetVerticesDirty();
            }
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount == 0) return;

            float factor = Mathf.Tan(skewDegrees * Mathf.Deg2Rad);
            UIVertex vertex = new UIVertex();

            for (int i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref vertex, i);
                vertex.position.x += vertex.position.y * factor;
                vh.SetUIVertex(vertex, i);
            }
        }
    }
}