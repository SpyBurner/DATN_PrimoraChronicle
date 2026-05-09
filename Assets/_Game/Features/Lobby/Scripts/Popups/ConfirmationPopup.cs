using System;
using TMPro;
using Zenject;
using UnityEngine;
using UnityEngine.UI;

public class ConfirmationPopup : UIPanel
{
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _messageText;
    [SerializeField] private Button _confirmButton;
    [SerializeField] private Button _cancelButton;

    [Inject] private IPopupSubsystem _popupSubsystem;

    private Action _onConfirm;
    private Action _onCancel;

    protected override void OnEnable()
    {
        base.OnEnable();
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
        _popupSubsystem.SetResult(true);
        _onConfirm?.Invoke();
        Close();
    }

    private void OnCancelClicked()
    {
        _popupSubsystem.SetResult(false); // Or Cancel()
        _onCancel?.Invoke();
        Close();
    }

    private void Close()
    {
        // Typically call UIManager to hide, or self close
        gameObject.SetActive(false);
    }
}
