using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Cossacks2Bridge.UnityAdapters.AddProfile
{
    /// <summary>
    /// Simple horizontal scrollbar controller (AddProfile portrait selector).
    /// Drives an integer value in range [0..Max].
    /// </summary>
    public sealed class HorizontalScrollbarController : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler
    {
        public int Max { get; private set; } = 1;
        public int Value { get; private set; } = 0;

        public Action<int> OnValueChanged;

        private RectTransform _track;
        private RectTransform _thumb;
        private Camera _uiCamera;

        private bool _dragging;
        private float _thumbTravel;
        private float _thumbWidth;

        public void Initialize(RectTransform track, RectTransform thumb, int max, int value)
        {
            _track = track;
            _thumb = thumb;
            Max = Mathf.Max(1, max);
            Value = Mathf.Clamp(value, 0, Max);
            _uiCamera = null;
            RecomputeMetrics();
            UpdateThumb();
        }

        public void SetMax(int max)
        {
            Max = Mathf.Max(1, max);
            Value = Mathf.Clamp(Value, 0, Max);
            RecomputeMetrics();
            UpdateThumb();
        }

        public void SetValue(int value, bool notify = true)
        {
            int v = Mathf.Clamp(value, 0, Max);
            if (v == Value) return;
            Value = v;
            UpdateThumb();
            if (notify) OnValueChanged?.Invoke(Value);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_track == null || _thumb == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_track, eventData.position, _uiCamera, out var local))
                return;

            float t = LocalToT(local.x);
            SetValue(Mathf.RoundToInt(t * Max));
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _dragging = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragging || _track == null || _thumb == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_track, eventData.position, _uiCamera, out var local))
                return;

            float t = LocalToT(local.x);
            SetValue(Mathf.RoundToInt(t * Max));
        }

        private void RecomputeMetrics()
        {
            if (_track == null || _thumb == null) return;
            _thumbWidth = _thumb.rect.width;
            _thumbTravel = Mathf.Max(1f, _track.rect.width - _thumbWidth);
        }

        private float LocalToT(float localX)
        {
            // pivot top-left => localX in [0..trackWidth]
            float x = Mathf.Clamp(localX, 0f, _track.rect.width);
            float left = Mathf.Clamp(x, 0f, _thumbTravel);
            return (Max <= 0) ? 0f : left / _thumbTravel;
        }

        private void UpdateThumb()
        {
            if (_thumb == null || _track == null) return;
            float t = (Max <= 0) ? 0f : (float)Value / Max;
            float left = t * _thumbTravel;
            _thumb.anchoredPosition = new Vector2(left, _thumb.anchoredPosition.y);
        }
    }
}
