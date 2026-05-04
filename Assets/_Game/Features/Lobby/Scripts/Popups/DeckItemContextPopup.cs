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
    [SerializeField] private Button _closeButton;

    private Action _onEdit;
    private Action _onSelect;
    private Action _onDelete;

    public override void Initialize()
    {
        base.Initialize();
        _editButton?.onClick.AddListener(OnEditClicked);
        _selectButton?.onClick.AddListener(OnSelectClicked);
        _deleteButton?.onClick.AddListener(OnDeleteClicked);
        _closeButton?.onClick.AddListener(Close);
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
        Close();
    }

    private void OnSelectClicked()
    {
        _onSelect?.Invoke();
        Close();
    }

    private void OnDeleteClicked()
    {
        _onDelete?.Invoke();
        Close();
    }

    private void Close()
    {
        gameObject.SetActive(false);
    }
}
