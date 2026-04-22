public interface IAccountRegisterModel : IModel
{
    string Email { get; set; }
    string Password { get; set; }
    string ConfirmPassword { get; set; }
}
