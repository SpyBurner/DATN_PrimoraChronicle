using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Slides a panel between its closed position (anchoredPosition zero) and an open
/// position defined by a child GameObject named "OpenPosition".
/// Attach to the anchor prefab (HandPanelAnchor, SkillPanelAnchor, TurnOrderPanelAnchor).
/// Wire _panel to the RectTransform that should move; wire _toggle to the Toggle that opens/closes it.
/// </summary>
public class PanelDrawer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform _panel;
    [SerializeField] private Toggle _toggle;

    [Header("Settings")]
    [SerializeField] private float _duration = 0.25f;
    [SerializeField] private Ease _ease = Ease.OutCubic;

    private Vector2 _openPosition;
    private readonly Vector2 _closedPosition = Vector2.zero;
    private bool _isOpen;
    private Tween _activeTween;

    private void Awake()
    {
        if (_panel == null) throw new System.Exception("[PanelDrawer._panel] Not assigned in Inspector — see wiring-F3.md (HandPanelAnchor / SkillPanelAnchor / TurnOrderPanelAnchor)");
        if (_toggle == null) throw new System.Exception("[PanelDrawer._toggle] Not assigned in Inspector — see wiring-F3.md (Toggle_Sidebar on anchor prefab)");

        Transform openMarker = transform.Find("OpenPosition");
        if (openMarker != null)
        {
            _openPosition = ((RectTransform)openMarker).anchoredPosition;
        }
        else
        {
            Debug.LogWarning($"[PanelDrawer] No 'OpenPosition' child on {gameObject.name}.");
        }

        if (_panel != null)
            _panel.anchoredPosition = _closedPosition;
    }

    private void OnEnable()  => _toggle?.onValueChanged.AddListener(OnToggleChanged);
    private void OnDisable() => _toggle?.onValueChanged.RemoveListener(OnToggleChanged);

    private void OnToggleChanged(bool isOn) => SetOpen(isOn);

    public void Toggle()   => SetOpen(!_isOpen);
    public void Open()     => SetOpen(true);
    public void Close()    => SetOpen(false);
    public bool IsOpen     => _isOpen;

    public void SetOpen(bool open, bool instant = false)
    {
        _isOpen = open;
        if (_panel == null) return;

        _activeTween?.Kill();
        Vector2 target = open ? _openPosition : _closedPosition;

        if (instant)
        {
            _panel.anchoredPosition = target;
            return;
        }

        _activeTween = DOTween.To(
            () => _panel.anchoredPosition,
            x => _panel.anchoredPosition = x,
            target,
            _duration
        ).SetEase(_ease).SetUpdate(true);
    }
}
