using UnityEngine;
using UnityEngine.UI;

public class TestPopupPanel : UIPanel
{
    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _moreButton;

    protected override void Awake()
    {
        base.Awake();
        _closeButton.onClick.AddListener(OnCloseButtonClicked);
        _moreButton.onClick.AddListener(OnMoreButtonClicked);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        _closeButton.onClick.RemoveListener(OnCloseButtonClicked);
        _moreButton.onClick.RemoveListener(OnMoreButtonClicked);
    }

    private void OnMoreButtonClicked()
    {
        _uiManagerSubsystem.ShowPopup<TestPopupPanel>();
    }

    private void OnCloseButtonClicked()
    {
        _uiManagerSubsystem.ClosePopup();
    }


}