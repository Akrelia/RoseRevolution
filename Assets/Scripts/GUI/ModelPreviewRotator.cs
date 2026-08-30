using UnityEngine;
using UnityEngine.EventSystems;

public class ModelPreviewRotator : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    public ModelPreview preview;
    public float rotationSpeed = 0.5f;

    private Vector2 lastPosition;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            lastPosition = eventData.position;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || preview.CurrentModel == null)
        {
            return;
        }

        Vector2 delta = eventData.position - lastPosition;
        lastPosition = eventData.position;

        preview.ChangeRotation(-delta.x * rotationSpeed);
    }
}