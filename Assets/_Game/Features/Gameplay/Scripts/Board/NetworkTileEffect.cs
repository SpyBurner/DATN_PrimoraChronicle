using Fusion;
using UnityEngine;

public class NetworkTileEffect : NetworkBehaviour
{
    [Networked] public int TileP { get; set; }
    [Networked] public int TileQ { get; set; }
    [Networked] public NetworkString<_16> EffectType { get; set; }
    [Networked] public int RemainingDuration { get; set; }
    [Networked] public PlayerRef OwnerPlayerRef { get; set; }

    public void ApplyEffect(int p, int q, string effectType, int duration, PlayerRef owner)
    {
        if (!Object.HasStateAuthority) return;

        TileP = p;
        TileQ = q;
        EffectType = effectType;
        RemainingDuration = duration;
        OwnerPlayerRef = owner;

        // Position on hex flat surface
        var board = FindObjectOfType<BoardManager>();
        if (board != null)
        {
            Vector3 worldPos = board.ResolveCoordinateToPosition(p, q);
            if (worldPos != Vector3.zero)
            {
                transform.position = new Vector3(worldPos.x, worldPos.y + 0.1f, worldPos.z);
            }
        }
    }

    public void TickTurn()
    {
        if (!Object.HasStateAuthority) return;

        // Lingering effects persist but do not tick down during board clear or inactive combat phases.
        if (NetworkGameplayManager.Instance != null && NetworkGameplayManager.Instance.CurrentPhase == GameplayPhase.CombatPhase)
        {
            RemainingDuration--;
            if (RemainingDuration <= 0)
            {
                Runner.Despawn(Object);
            }
        }
    }
}
