using UnityEngine;
using UnityEngine.EventSystems;

public class CardDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Canvas _rootCanvas;

    private string _cardId;
    private int _handIndex;
    private RectTransform _rectTransform;
    private CanvasGroup _canvasGroup;
    private Vector2 _originalPosition;
    private Transform _originalParent;
    private bool _isDragging;

    public string CardId => _cardId;
    public int HandIndex => _handIndex;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void Initialize(string cardId, int handIndex)
    {
        _cardId = cardId;
        _handIndex = handIndex;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _isDragging = true;
        _originalPosition = _rectTransform.anchoredPosition;
        _originalParent = transform.parent;

        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.alpha = 0.7f;

        if (_rootCanvas != null)
            transform.SetParent(_rootCanvas.transform, true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging) return;

        float scaleFactor = _rootCanvas != null ? _rootCanvas.scaleFactor : 1f;
        _rectTransform.anchoredPosition += eventData.delta / scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _isDragging = false;
        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.alpha = 1f;

        transform.SetParent(_originalParent, true);
        _rectTransform.anchoredPosition = _originalPosition;
    }
}
