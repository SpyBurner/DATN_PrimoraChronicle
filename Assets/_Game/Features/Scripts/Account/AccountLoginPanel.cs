using UnityEngine;
using UnityEngine.UI;

public class AccountLoginPanel : UIPanel
{
    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _registerButton;

    public override void Show()
    {
        base.Show();
        _closeButton.onClick.AddListener(() => OnClose());
    }
    private void OnClose()
    {
        _uiManagerSubsystem.CloseView(this);
    }
}
