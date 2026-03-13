using UnityEngine;
using UnityEngine.UI;

public class MainMenuPanel : UIPanel
{
    [SerializeField] private Button _loginButton;

    protected override void Awake()
    {
        base.Awake();
        _loginButton.onClick.AddListener(OnLoginButtonClicked);
    }

    private void OnLoginButtonClicked()
    {
        _uiManagerSubsystem.ShowPopup<TestPopupPanel>();
    }
}