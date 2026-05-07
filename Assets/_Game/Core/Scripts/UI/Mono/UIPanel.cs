using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class UIPanel : MonoBehaviour, IUIPanel
{
    [Inject] protected readonly IUIManagerSubsystem _uiManagerSubsystem;

    [Header("Meta data")]
    [SerializeField] protected UIIdentifier _identifier;
    [SerializeField] protected UILayer _layer;

    [SerializeField] private Button _closeButton;

    private bool _isRegistered;

    public UIIdentifier Identifier => _identifier;
    public UILayer Layer => _layer;

    protected virtual void Awake()
    {
        if (_isRegistered) return;
        _isRegistered = true;
        _uiManagerSubsystem.RegisterPanel(this);
    }

    protected virtual void OnEnable()
    {
        _closeButton?.onClick.AddListener(OnClose);
    }

    protected virtual void OnDisable()
    {
        _closeButton?.onClick.RemoveListener(OnClose);
    }

    protected virtual void OnDestroy()
    {
        _isRegistered = false;
        _uiManagerSubsystem.UnregisterPanel(this);
    }

    public virtual void Show()
    {
        gameObject.SetActive(true);
    }

    public virtual void Hide()
    {
        gameObject.SetActive(false);
    }

    protected virtual void OnClose()
    {
        _uiManagerSubsystem.CloseView(this);
    }
}