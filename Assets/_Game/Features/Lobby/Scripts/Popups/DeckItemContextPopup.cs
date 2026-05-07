using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeckItemContextPopup : UIPanel
{
    [SerializeField] private TMP_Text _deckNameText;
    [SerializeField] private Button _editButton;
    [SerializeField] private Button _selectButton;
    [SerializeField] private Button _deleteButton;

    private Action _onEdit;
    private Action _onSelect;
    private Action _onDelete;

    protected override void OnEnable()
    {
        base.OnEnable();
        _editButton?.onClick.AddListener(OnEditClicked);
        _selectButton?.onClick.AddListener(OnSelectClicked);
        _deleteButton?.onClick.AddListener(OnDeleteClicked);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        _editButton?.onClick.RemoveListener(OnEditClicked);
        _selectButton?.onClick.RemoveListener(OnSelectClicked);
        _deleteButton?.onClick.RemoveListener(OnDeleteClicked);
    }

    public void Setup(string deckName, Action onEdit, Action onSelect, Action onDelete)
    {
        if (_deckNameText != null) _deckNameText.text = deckName;
        
        _onEdit = onEdit;
        _onSelect = onSelect;
        _onDelete = onDelete;
    }

    private void OnEditClicked()
    {
        _onEdit?.Invoke();
        OnClose();
    }

    private void OnSelectClicked()
    {
        _onSelect?.Invoke();
        OnClose();
    }

    private void OnDeleteClicked()
    {
        _onDelete?.Invoke();
        OnClose();
    }
}
