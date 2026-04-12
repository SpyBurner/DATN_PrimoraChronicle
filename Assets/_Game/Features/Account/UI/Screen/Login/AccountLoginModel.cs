internal class AccountLoginModel : IAccountLoginModel
{
    public string Email { get; set; }
    public string Password { get; set; }

    public void Initialize() { }
    public void Dispose() { }
}
