using System;

internal class AccountRegisterModel : IAccountRegisterModel
{
    public string Email { get; set; }
    public string Password { get; set; }
    public string ConfirmPassword { get; set; }

    public void Initialize() { }
    public void Dispose() { }
}
