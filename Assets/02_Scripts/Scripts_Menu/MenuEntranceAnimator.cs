using System;
using System.Collections;
using UnityEngine;

namespace Core.UI.Menu
{
    public class MenuEntranceAnimator : MonoBehaviour
    {
        [Serializable]
        private class Item
        {
            public RectTransform target;
            [Tooltip("De onde o elemento vem, relativo à posição final (ex: -500,0 = vem da esquerda)")]
            public Vector2 startOffset = new Vector2(-500f, 0f);
            [Tooltip("Atraso em segundos (use 0, 0.1, 0.2... pra fazer cascata)")]
            public float delay;
            public EaseType ease = EaseType.OutBack;

            [NonSerialized] public CanvasGroup group;
            [NonSerialized] public Vector2 basePosition;
        }

        [SerializeField] private Item[] items;
        [SerializeField] private float duration = 0.7f;
        [SerializeField] private float initialDelay = 0.15f;
        [SerializeField] private bool playOnEnable = true;

        private Coroutine _routine;
        private bool _captured;

        private void Awake()
        {
            foreach (Item item in items)
            {
                if (item.target == null) continue;
                item.group = item.target.GetComponent<CanvasGroup>();
                if (item.group == null) item.group = item.target.gameObject.AddComponent<CanvasGroup>();
            }
        }

        private void OnEnable()
        {
            if (playOnEnable) Play();
        }

        private void OnDisable()
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = null;
            ApplyAll(float.MaxValue); // deixa tudo no estado final
        }

        public void Play()
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(PlayRoutine());
        }

        private IEnumerator PlayRoutine()
        {
            Canvas.ForceUpdateCanvases();
            CaptureBasePositions();

            if (MenuMotion.ReduceMotion)
            {
                ApplyAll(float.MaxValue);
                _routine = null;
                yield break;
            }

            float total = initialDelay + duration + MaxDelay();
            float elapsed = 0f;

            while (elapsed < total)
            {
                elapsed += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
                ApplyAll(elapsed);
                yield return null;
            }

            ApplyAll(total);
            _routine = null;
        }

        private void CaptureBasePositions()
        {
            if (_captured) return;
            foreach (Item item in items)
            {
                if (item.target != null) item.basePosition = item.target.anchoredPosition;
            }
            _captured = true;
        }

        private float MaxDelay()
        {
            float max = 0f;
            foreach (Item item in items) max = Mathf.Max(max, item.delay);
            return max;
        }

        private void ApplyAll(float elapsed)
        {
            if (!_captured) return;

            foreach (Item item in items)
            {
                if (item.target == null || item.group == null) continue;

                float x = Mathf.Clamp01((elapsed - initialDelay - item.delay) / duration);
                float eased = MenuEasing.Evaluate(item.ease, x);

                item.target.anchoredPosition = item.basePosition + item.startOffset * (1f - eased);
                item.group.alpha = MenuEasing.OutCubic(Mathf.Clamp01(x * 3f));
            }
        }
    }
}