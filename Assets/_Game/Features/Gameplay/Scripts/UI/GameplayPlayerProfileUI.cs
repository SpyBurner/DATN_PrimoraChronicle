using TMPro;
using UnityEngine;

public class GameplayPlayerProfileUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _hpText;

    private NetworkPlayerState _state;

    private void Awake()
    {
        foreach (var tmp in GetComponentsInChildren<TMP_Text>(true))
        {
            if (_hpText == null && tmp.name == "HPValueText") _hpText = tmp;
            if (_nameText == null && tmp.name == "NameText") _nameText = tmp;
        }
    }

    public void Initialize(NetworkPlayerState state)
    {
        _state = state;
        Refresh();
    }

    private void Update()
    {
        if (_state == null) return;
        Refresh();
    }

    private void Refresh()
    {
        if (_hpText != null) _hpText.text = _state.HP.ToString();
        if (_nameText != null) _nameText.text = _state.PlayerName.ToString();
    }
}
