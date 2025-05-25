using UnityEngine;
using UnityEngine.EventSystems;

public class UIDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    public RectTransform windowToMove;

    private Vector2 offset;

    public void OnBeginDrag(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            windowToMove, eventData.position, eventData.pressEventCamera, out offset);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            windowToMove.parent as RectTransform, eventData.position, eventData.pressEventCamera, out localPoint))
        {
            windowToMove.localPosition = localPoint - offset;
        }
    }
}
