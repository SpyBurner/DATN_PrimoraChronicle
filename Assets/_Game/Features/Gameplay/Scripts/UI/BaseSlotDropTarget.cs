using UnityEngine;
using UnityEngine.EventSystems;

public class BaseSlotDropTarget : MonoBehaviour, IDropHandler
{
    [SerializeField] private FusionPanel _fusionPanel;

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;
        if (_fusionPanel == null) return;

        var dragHandle = eventData.pointerDrag.GetComponent<CardDragHandle>();
        if (dragHandle == null) return;

        _fusionPanel.StageBase(dragHandle.CardId);
    }
}
