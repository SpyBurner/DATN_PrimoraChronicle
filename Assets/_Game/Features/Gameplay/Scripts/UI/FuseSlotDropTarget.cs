using UnityEngine;
using UnityEngine.EventSystems;

public class FuseSlotDropTarget : MonoBehaviour, IDropHandler
{
    [SerializeField] private int _slotIndex;
    [SerializeField] private FusionPanel _fusionPanel;

    private void Awake()
    {
        if (_fusionPanel == null) throw new System.Exception("[FuseSlotDropTarget._fusionPanel] Not assigned in Inspector — see wiring-F3.md F3 FuseSlotDropTarget");
    }

    public void Initialize(int slotIndex, FusionPanel fusionPanel)
    {
        _slotIndex = slotIndex;
        _fusionPanel = fusionPanel;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;
        if (_fusionPanel == null) return;

        var dragHandle = eventData.pointerDrag.GetComponent<CardDragHandle>();
        if (dragHandle == null) return;

        _fusionPanel.StageEquipSpell(_slotIndex, dragHandle.CardId);
    }
}
