using Cossacks2Bridge.Core;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static Cossacks2Bridge.UnityAdapters.Renderers.BaseUiRenderer;

/// <summary>
/// Контроллер списка подключений (ListDesk)
/// Без линий, без подложек, только текст (как в оригинале Cossacks 2)
/// </summary>
public sealed class ListDeskController : MonoBehaviour
{
    private UiListDesk _listDesk;
    private RectTransform _content;
    private RenderOptions _opt;

    private readonly List<GameObject> _items = new();

    // ФИКСИРОВАННАЯ ВЫСОТА СТРОКИ (оригинал ~20–22px)
    private const float ROW_HEIGHT = 22f;

    public void Initialize(UiListDesk listDesk, RectTransform content, RenderOptions opt)
    {
        _listDesk = listDesk;
        _content = content;
        _opt = opt;
    }

    public void RefreshItems()
    {
        // очистка старых элементов
        foreach (var go in _items)
        {
            if (go != null)
                Destroy(go);
        }
        _items.Clear();

        if (_listDesk == null || _content == null || _listDesk.Items == null)
            return;

        for (int i = 0; i < _listDesk.Items.Count; i++)
        {
            var item = CreateItemElement(i, _listDesk.Items[i]);
            item.transform.SetParent(_content, false);
            _items.Add(item);
        }
    }

    private GameObject CreateItemElement(int index, string text)
    {
        // Корневой объект строки
        var itemGO = new GameObject($"Item_{index}", typeof(RectTransform));
        var rt = itemGO.GetComponent<RectTransform>();

        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0, 1);
        rt.sizeDelta = new Vector2(0, ROW_HEIGHT);

        // ПРОЗРАЧНЫЙ Image — ТОЛЬКО ДЛЯ RAYCAST (НИЧЕГО НЕ РИСУЕТ)
        var raycastImg = itemGO.AddComponent<Image>();
        raycastImg.color = new Color(0, 0, 0, 0);
        raycastImg.raycastTarget = true;

        // ТЕКСТ
        var textGO = new GameObject("Label", typeof(RectTransform));
        textGO.transform.SetParent(itemGO.transform, false);

        var textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0, 0);
        textRT.anchorMax = new Vector2(1, 1);
        textRT.offsetMin = new Vector2(8, 0);
        textRT.offsetMax = new Vector2(-8, 0);

        var label = textGO.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.color = Color.black;
        label.raycastTarget = false;

        // HOVER — ТОЛЬКО ЦВЕТ ТЕКСТА (БЕЗ ПОДЛОЖЕК)
        var hover = itemGO.AddComponent<ListDeskItemHover>();
        hover.Label = label;
        hover.NormalColor = Color.black;
        hover.HoverColor = Color.red;

        return itemGO;
    }
}

/// <summary>
/// Hover-эффект строки списка (меняет только цвет текста)
/// </summary>
public sealed class ListDeskItemHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public TextMeshProUGUI Label;
    public Color NormalColor = Color.black;
    public Color HoverColor = Color.red;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (Label != null)
            Label.color = HoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (Label != null)
            Label.color = NormalColor;
    }
}
