using Zenject;

public interface IUIPanel
{
    UIIdentifier Identifier { get; }
    UILayer Layer { get; }

    void Hide();
    void Show();
}