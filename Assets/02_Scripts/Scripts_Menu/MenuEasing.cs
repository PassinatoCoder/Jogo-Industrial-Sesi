using UnityEngine;

namespace Core.UI.Menu
{
    public enum EaseType { OutCubic, OutBack, OutElastic, OutBounce }

    /// <summary>Curvas de easing usadas pelas animações de menu.</summary>
    public static class MenuEasing
    {
        private const float C1 = 1.70158f;
        private const float C3 = C1 + 1f;
        private const float C4 = (2f * Mathf.PI) / 3f;

        public static float Evaluate(EaseType type, float x)
        {
            x = Mathf.Clamp01(x);
            switch (type)
            {
                case EaseType.OutBack: return OutBack(x);
                case EaseType.OutElastic: return OutElastic(x);
                case EaseType.OutBounce: return OutBounce(x);
                default: return OutCubic(x);
            }
        }

        public static float OutCubic(float x)
        {
            float inv = 1f - x;
            return 1f - inv * inv * inv;
        }

        public static float OutBack(float x)
        {
            float p = x - 1f;
            return 1f + C3 * p * p * p + C1 * p * p;
        }

        public static float OutElastic(float x)
        {
            if (x <= 0f) return 0f;
            if (x >= 1f) return 1f;
            return Mathf.Pow(2f, -10f * x) * Mathf.Sin((x * 10f - 0.75f) * C4) + 1f;
        }

        public static float OutBounce(float x)
        {
            const float n1 = 7.5625f;
            const float d1 = 2.75f;

            if (x < 1f / d1) return n1 * x * x;
            if (x < 2f / d1) { x -= 1.5f / d1; return n1 * x * x + 0.75f; }
            if (x < 2.5f / d1) { x -= 2.25f / d1; return n1 * x * x + 0.9375f; }
            x -= 2.625f / d1;
            return n1 * x * x + 0.984375f;
        }
    }
}