using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Minigames.Welding
{
    /// <summary>
    /// Cliques do minigame, sem depender do EventSystem. Cada peça clicável se registra aqui.
    /// Clique = apertar e soltar em cima da mesma peça. "Press" avisa no momento que aperta (usado pra arrastar).
    /// </summary>
    public sealed class WeldingInput
    {
        public sealed class Hit
        {
            public RectTransform Rect;
            public Action OnClick;
            public Action OnPress;
            public bool Enabled = true;
            public int Priority;
        }

        private readonly List<Hit> _hits = new List<Hit>();
        private Hit _pressed;

        /// <summary>Trava todo o clique (durante piscadas e animações).</summary>
        public bool Locked { get; set; }
        /// <summary>Quando ativo, somente hitboxes de prioridade modal podem receber clique.</summary>
        public bool Modal { get; set; }
        public int ModalMinPriority { get; set; } = 300;

        public Hit Register(RectTransform rect, Action onClick = null, Action onPress = null, int priority = 0)
        {
            Hit hit = new Hit { Rect = rect, OnClick = onClick, OnPress = onPress, Priority = priority };
            _hits.Add(hit);
            return hit;
        }

        public void Unregister(Hit hit)
        {
            if (hit == null) return;
            _hits.Remove(hit);
            if (_pressed == hit) _pressed = null;
        }

        public void Clear()
        {
            _hits.Clear();
            _pressed = null;
        }

        public Hit Top(Vector2 screenPoint)
        {
            Hit best = null;
            float bestArea = float.MaxValue;
            int bestSibling = int.MinValue;

            for (int i = 0; i < _hits.Count; i++)
            {
                Hit hit = _hits[i];
                if (Modal && hit.Priority < ModalMinPriority) continue;
                if (!hit.Enabled || hit.Rect == null || !hit.Rect.gameObject.activeInHierarchy) continue;
                if (!RectTransformUtility.RectangleContainsScreenPoint(hit.Rect, screenPoint, null)) continue;

                // Prioridade explícita vence; em empate, a hitbox menor e mais específica vence.
                // Isso evita que uma área grande capture cliques destinados a um controle menor.
                Vector2 size = hit.Rect.rect.size;
                Vector3 scale = hit.Rect.lossyScale;
                float area = Mathf.Abs(size.x * scale.x * size.y * scale.y);
                int sibling = hit.Rect.GetSiblingIndex();
                bool better = best == null || hit.Priority > best.Priority ||
                              (hit.Priority == best.Priority && area < bestArea) ||
                              (hit.Priority == best.Priority && Mathf.Approximately(area, bestArea) && sibling > bestSibling);
                if (!better) continue;

                best = hit;
                bestArea = area;
                bestSibling = sibling;
            }

            return best;
        }

        /// <summary>Peça embaixo do mouse agora (pra destacar). Nulo se estiver travado.</summary>
        public Hit Hovered
        {
            get { return Locked ? null : Top(Input.mousePosition); }
        }

        public void Tick()
        {
            if (Locked)
            {
                _pressed = null;
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                _pressed = Top(Input.mousePosition);
                if (_pressed != null && _pressed.OnPress != null) _pressed.OnPress();
            }

            if (Input.GetMouseButtonUp(0))
            {
                Hit released = Top(Input.mousePosition);
                Hit pressed = _pressed;
                _pressed = null;
                if (pressed != null && pressed == released && pressed.OnClick != null) pressed.OnClick();
            }
        }
    }
}
