using UnityEngine;

namespace Core.UI.Menu
{
    /// <summary>
    /// Mola simples (valor + velocidade) usada pelas animações do menu estilizado.
    /// Sub-passos internos deixam a mola estável mesmo com FPS baixo.
    /// </summary>
    public struct MenuSpring
    {
        public float Value;
        public float Velocity;

        public MenuSpring(float value)
        {
            Value = value;
            Velocity = 0f;
        }

        /// <summary>Coloca a mola direto no valor, sem animação.</summary>
        public void Snap(float value)
        {
            Value = value;
            Velocity = 0f;
        }

        public void Step(float target, float stiffness, float damping, float dt)
        {
            const float maxSubStep = 0.008f;
            int steps = Mathf.Max(1, Mathf.CeilToInt(dt / maxSubStep));
            float h = dt / steps;

            for (int i = 0; i < steps; i++)
            {
                float force = (target - Value) * stiffness - Velocity * damping;
                Velocity += force * h;
                Value += Velocity * h;
            }
        }
    }
}