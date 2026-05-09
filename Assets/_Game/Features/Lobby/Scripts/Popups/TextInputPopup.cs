using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class TextInputPopup : UIPanel
{
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_InputField _inputField;
    [SerializeField] private Button _confirmButton;
    [SerializeField] private Button _cancelButton;

    [Inject] private IPopupSubsystem _popupSubsystem;

    private Action<string> _onConfirm;
    private Action _onCancel;

    protected override void OnEnable()
    {
        base.OnEnable();
        _confirmButton?.onClick.AddListener(OnConfirmClicked);
        _cancelButton?.onClick.AddListener(OnCancelClicked);
    }

    public void Setup(string title, string defaultText, Action<string> onConfirm, Action onCancel = null)
    {
        if (_titleText != null) _titleText.text = title;
        if (_inputField != null) _inputField.text = defaultText;
        
        _onConfirm = onConfirm;
        _onCancel = onCancel;
    }

    private void OnConfirmClicked()
    {
        _popupSubsystem.SetResult(_inputField.text);
        _onConfirm?.Invoke(_inputField.text);
        Close();
    }

    private void OnCancelClicked()
    {
        _popupSubsystem.Cancel();
        _onCancel?.Invoke();
        Close();
    }

    private void Close()
    {
        gameObject.SetActive(false);
    }
}
