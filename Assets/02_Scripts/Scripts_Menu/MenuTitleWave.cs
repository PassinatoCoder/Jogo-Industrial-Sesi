using UnityEngine;
using TMPro;

namespace Core.UI.Menu
{
    [RequireComponent(typeof(TMP_Text))]
    public class MenuTitleWave : MonoBehaviour
    {
        [Header("Onda")]
        [SerializeField] private float amplitude = 7f;
        [SerializeField] private float frequency = 2.4f;
        [Tooltip("Defasagem entre letras (maior = onda mais 'espalhada')")]
        [SerializeField] private float phasePerCharacter = 0.35f;
        [SerializeField] private float tiltDegrees = 4f;

        private TMP_Text _text;
        private bool _needsRestore;

        private void Awake() => _text = GetComponent<TMP_Text>();

        private void OnDisable() => _needsRestore = true;

        private void LateUpdate()
        {
            if (MenuMotion.ReduceMotion)
            {
                if (_needsRestore) { _text.ForceMeshUpdate(); _needsRestore = false; }
                return;
            }

            _text.ForceMeshUpdate();
            TMP_TextInfo info = _text.textInfo;
            float time = Time.unscaledTime * frequency;

            for (int i = 0; i < info.characterCount; i++)
            {
                TMP_CharacterInfo c = info.characterInfo[i];
                if (!c.isVisible) continue;

                Vector3[] verts = info.meshInfo[c.materialReferenceIndex].vertices;
                int v = c.vertexIndex;

                float phase = time + i * phasePerCharacter;
                float wave = Mathf.Sin(phase);
                Vector3 lift = new Vector3(0f, wave * amplitude, 0f);
                Quaternion tilt = Quaternion.Euler(0f, 0f, wave * tiltDegrees);
                Vector3 center = (verts[v] + verts[v + 2]) * 0.5f;

                for (int j = 0; j < 4; j++)
                    verts[v + j] = center + tilt * (verts[v + j] - center) + lift;
            }

            for (int i = 0; i < info.meshInfo.Length; i++)
            {
                info.meshInfo[i].mesh.vertices = info.meshInfo[i].vertices;
                _text.UpdateGeometry(info.meshInfo[i].mesh, i);
            }

            _needsRestore = true;
        }
    }
}