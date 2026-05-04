using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TextInputPopup : UIPanel
{
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_InputField _inputField;
    [SerializeField] private Button _confirmButton;
    [SerializeField] private Button _cancelButton;

    private Action<string> _onConfirm;
    private Action _onCancel;

    public override void Initialize()
    {
        base.Initialize();
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
        _onConfirm?.Invoke(_inputField.text);
        Close();
    }

    private void OnCancelClicked()
    {
        _onCancel?.Invoke();
        Close();
    }

    private void Close()
    {
        gameObject.SetActive(false);
    }
}
