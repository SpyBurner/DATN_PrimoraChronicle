using System;
using Zenject;
using UnityEngine;
using UnityEngine.UI;

public class DeckItemContextPopup : UIPanel
{
    [SerializeField] private Button _editButton;
    [SerializeField] private Button _selectButton;
    [SerializeField] private Button _deleteButton;

    [Inject] private IPopupSubsystem _popupSubsystem;

    private Action _onEdit;
    private Action _onSelect;
    private Action _onDelete;

    protected override void OnEnable()
    {
        base.OnEnable();
        _editButton?.onClick.AddListener(OnEditClicked);
        _deleteButton?.onClick.AddListener(OnDeleteClicked);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        _editButton?.onClick.RemoveListener(OnEditClicked);
        _deleteButton?.onClick.RemoveListener(OnDeleteClicked);
    }

    public void Setup(string deckName, Action onEdit, Action onSelect, Action onDelete)
    {
        _onEdit = onEdit;
        _onSelect = onSelect;
        _onDelete = onDelete;
    }

    public enum DeckContextAction
    {
        Edit,
        Delete,
        Cancel
    }

    private void OnEditClicked()
    {
        _popupSubsystem.SetResult(DeckContextAction.Edit);
        _onEdit?.Invoke();
        OnClose();
    }

    private void OnDeleteClicked()
    {
        _popupSubsystem.SetResult(DeckContextAction.Delete);
        _onDelete?.Invoke();
        OnClose();
    }

    protected override void OnClose()
    {
        _popupSubsystem.SetResult(DeckContextAction.Cancel);
        base.OnClose();
    }
}
