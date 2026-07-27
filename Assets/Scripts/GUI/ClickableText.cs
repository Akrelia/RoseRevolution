using TMPro;
using UnityEngine.EventSystems;
using UnityEngine;
using System;

public class ClickableText : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public Action Clicked;

    public TextMeshProUGUI text;

    private void Awake()
    {
        text = GetComponent<TextMeshProUGUI>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Clicked?.Invoke();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        text.fontStyle |= FontStyles.Underline;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        text.fontStyle &= ~FontStyles.Underline;
    }
}