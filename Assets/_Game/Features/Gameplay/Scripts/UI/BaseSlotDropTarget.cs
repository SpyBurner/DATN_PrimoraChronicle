using UnityEngine;
using UnityEngine.EventSystems;

public class BaseSlotDropTarget : MonoBehaviour, IDropHandler
{
    [SerializeField] private FusionPanel _fusionPanel;

    private void Awake()
    {
        if (_fusionPanel == null) throw new System.Exception("[BaseSlotDropTarget._fusionPanel] Not assigned in Inspector — see wiring-F3.md F3 BaseSlotDropTarget");
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;
        if (_fusionPanel == null) return;

        var dragHandle = eventData.pointerDrag.GetComponent<CardDragHandle>();
        if (dragHandle == null) return;

        _fusionPanel.StageBase(dragHandle.CardId);
    }
}
