using System;
using System.Threading.Tasks;
using UnityEngine.Events;
using UnityObservables;
using Zenject;

public class AccountLoginSubsystem : IAccountLoginSubsystem
{
    [Inject] private readonly IAccountLoginController _controller;
    [Inject] private readonly IAccountLoginModel _model;

    public event UnityAction<string> ErrorMessageChanged;
    public event UnityAction<bool> IsSubmittingChanged;

    public void Initialize()
    {
        if (_model?.ErrorMessage != null)
            _model.ErrorMessage.OnChanged += HandleErrorMessageChanged;

        if (_model?.IsSubmitting != null)
            _model.IsSubmitting.OnChanged += HandleIsSubmittingChanged;
    }

    public void Dispose()
    {
        if (_model?.ErrorMessage != null)
            _model.ErrorMessage.OnChanged -= HandleErrorMessageChanged;

        if (_model?.IsSubmitting != null)
            _model.IsSubmitting.OnChanged -= HandleIsSubmittingChanged;
    }

    public void SetEmail(string email) => _controller.SetEmail(email);
    public void SetPassword(string password) => _controller.SetPassword(password);
    public Task Login() => _controller.Login();

    private void HandleErrorMessageChanged()
    {
        try
        {
            ErrorMessageChanged?.Invoke(_model.ErrorMessage.Value);
        }
        catch (Exception)
        {
            // swallow
        }
    }

    private void HandleIsSubmittingChanged()
    {
        try
        {
            IsSubmittingChanged?.Invoke(_model.IsSubmitting.Value);
        }
        catch (Exception)
        {
            // swallow
        }
    }
}
