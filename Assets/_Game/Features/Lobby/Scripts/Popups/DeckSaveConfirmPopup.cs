using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeckSaveConfirmPopup : UIPanel
{
    [SerializeField] private TMP_InputField _deckNameInput;
    [SerializeField] private Button _confirmButton;
    [SerializeField] private Button _cancelButton;

    private Action<string> _onConfirm;
    private Action _onCancel;

    protected override void OnEnable()
    {
        base.OnEnable();
        _confirmButton?.onClick.AddListener(OnConfirmClicked);
        _cancelButton?.onClick.AddListener(OnCancelClicked);
    }

    protected override void OnDisable()
    {
        _confirmButton?.onClick.RemoveListener(OnConfirmClicked);
        _cancelButton?.onClick.RemoveListener(OnCancelClicked);
        base.OnDisable();
    }

    public void Setup(string deckName, Action<string> onConfirm, Action onCancel = null)
    {
        if (_deckNameInput != null)
        {
            _deckNameInput.text = string.IsNullOrWhiteSpace(deckName) ? "Untitled Deck" : deckName;
            _deckNameInput.ActivateInputField();
            _deckNameInput.Select();
        }

        _onConfirm = onConfirm;
        _onCancel = onCancel;
    }

    private void OnConfirmClicked()
    {
        _onConfirm?.Invoke(_deckNameInput != null ? _deckNameInput.text : string.Empty);
        OnClose();
    }

    private void OnCancelClicked()
    {
        _onCancel?.Invoke();
        OnClose();
    }
}
