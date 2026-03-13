using UnityEngine;
using Zenject;

public class UIPanel : MonoBehaviour, IUIPanel
{
    [Inject] protected readonly IUIManagerSubsystem _uiManagerSubsystem;

    [SerializeField] protected UIIdentifier _identifier;
    [SerializeField] protected UILayer _layer;
    [SerializeField] protected bool _isModal;

    public UIIdentifier Identifier => _identifier;
    public UILayer Layer => _layer;

    protected virtual void Awake()
    {
        _uiManagerSubsystem.RegisterPanel(this);
        if (_isModal)
        {
            gameObject.SetActive(false);
        }
    }

    protected virtual void OnDestroy()
    {
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
}