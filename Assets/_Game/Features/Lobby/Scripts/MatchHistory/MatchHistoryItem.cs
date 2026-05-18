using TMPro;
using UnityEngine;

public class MatchHistoryItem : MonoBehaviour
{
    [SerializeField] private TMP_Text _dateText;
    [SerializeField] private TMP_Text _resultText;
    [SerializeField] private TMP_Text _goldText;
    [SerializeField] private TMP_Text _xpText;

    public void Setup(MatchHistoryData data)
    {
        if (data == null) return;

        SetText(_dateText, data.endDateTime);
        SetText(_resultText, data.isWinner ? "Win" : "Lose");
        SetText(_goldText, $"+{data.goldReceived}");
        SetText(_xpText, $"+{data.xpReceived}");
    }

    private static void SetText(TMP_Text label, string value)
    {
        if (label == null) return;
        label.text = value;
    }
}
