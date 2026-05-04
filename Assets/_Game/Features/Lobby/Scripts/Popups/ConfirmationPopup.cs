using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConfirmationPopup : UIPanel
{
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _messageText;
    [SerializeField] private Button _confirmButton;
    [SerializeField] private Button _cancelButton;

    private Action _onConfirm;
    private Action _onCancel;

    public override void Initialize()
    {
        base.Initialize();
        _confirmButton?.onClick.AddListener(OnConfirmClicked);
        _cancelButton?.onClick.AddListener(OnCancelClicked);
    }

    public void Setup(string title, string message, Action onConfirm, Action onCancel = null)
    {
        if (_titleText != null) _titleText.text = title;
        if (_messageText != null) _messageText.text = message;
        
        _onConfirm = onConfirm;
        _onCancel = onCancel;
    }

    private void OnConfirmClicked()
    {
        _onConfirm?.Invoke();
        Close();
    }

    private void OnCancelClicked()
    {
        _onCancel?.Invoke();
        Close();
    }

    private void Close()
    {
        // Typically call UIManager to hide, or self close
        gameObject.SetActive(false);
    }
}
