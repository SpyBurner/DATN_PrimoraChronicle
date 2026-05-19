using UnityEngine;

public class GameplayHUDController : MonoBehaviour
{
    [SerializeField] private GameplayPlayerProfileUI _localProfile;
    [SerializeField] private GameplayPlayerProfileUI _opponentProfile;

    private bool _initialized;

    private void Awake()
    {
        if (_localProfile == null)
        {
            var go = GameObject.Find("Profile_Player");
            if (go != null) _localProfile = go.GetComponent<GameplayPlayerProfileUI>();
        }
        if (_opponentProfile == null)
        {
            var go = GameObject.Find("Profile_Enemy1");
            if (go != null) _opponentProfile = go.GetComponent<GameplayPlayerProfileUI>();
        }
    }

    private void Update()
    {
        if (_initialized || NetworkGameplayManager.Instance == null) return;
        if (NetworkGameplayManager.Instance.PlayerCount < 2) return;

        var runner = NetworkGameplayManager.Instance.Runner;
        if (runner == null) return;

        NetworkPlayerState localState = null;
        NetworkPlayerState opponentState = null;

        for (int i = 0; i < NetworkGameplayManager.Instance.PlayerStates.Length; i++)
        {
            var id = NetworkGameplayManager.Instance.PlayerStates.Get(i);
            if (!id.IsValid) continue;

            if (!runner.TryFindObject(id, out var obj)) continue;
            var ps = obj.GetComponent<NetworkPlayerState>();
            if (ps == null) continue;

            if (ps.Player == runner.LocalPlayer) localState = ps;
            else opponentState = ps;
        }

        if (localState == null || opponentState == null) return;

        _localProfile?.Initialize(localState);
        _opponentProfile?.Initialize(opponentState);
        _initialized = true;
    }
}
