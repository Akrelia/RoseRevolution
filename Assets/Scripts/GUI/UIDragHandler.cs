using UnityEngine;
using UnityEngine.EventSystems;

public class UIDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    public RectTransform windowToMove;

    private Vector2 offset;

    public void OnBeginDrag(PointerEventData eventData)
    {
        RectTransform parent = windowToMove.parent as RectTransform;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
        {
            offset = (Vector2)windowToMove.localPosition - localPoint;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        RectTransform parent = windowToMove.parent as RectTransform;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
        {
            windowToMove.localPosition = localPoint + offset;
        }
    }
}