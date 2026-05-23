using System.Collections.Generic;
using UnityEngine;

public class EffectController
{
    private Dictionary<string, List<TileEffect>> _tileEffects = new();
    private BoardController _board;

    public EffectController(BoardController board)
    {
        _board = board;
    }

    private string TileKey(int p, int q) => $"{p}-{q}";

    public void ApplyEffect(int p, int q, TileEffect effect)
    {
        string key = TileKey(p, q);
        if (!_tileEffects.ContainsKey(key))
            _tileEffects[key] = new List<TileEffect>();

        // Don't stack same type from same owner
        _tileEffects[key].RemoveAll(e => e.Type == effect.Type && e.OwnerPlayer == effect.OwnerPlayer);
        _tileEffects[key].Add(effect);
    }

    public List<TileEffect> GetEffectsAt(int p, int q)
    {
        string key = TileKey(p, q);
        if (_tileEffects.TryGetValue(key, out var effects))
            return effects;
        return new List<TileEffect>();
    }

    public bool HasEffect(int p, int q, TileEffectType type)
    {
        string key = TileKey(p, q);
        if (!_tileEffects.TryGetValue(key, out var effects)) return false;
        foreach (var e in effects)
        {
            if (e.Type == type && !e.IsExpired) return true;
        }
        return false;
    }

    public void TickAllEffects()
    {
        var keysToRemove = new List<string>();
        foreach (var kvp in _tileEffects)
        {
            kvp.Value.RemoveAll(e => e.IsExpired);
            foreach (var effect in kvp.Value)
                effect.Tick();
            kvp.Value.RemoveAll(e => e.IsExpired);

            if (kvp.Value.Count == 0)
                keysToRemove.Add(kvp.Key);
        }
        foreach (var key in keysToRemove)
            _tileEffects.Remove(key);
    }

    public void ApplyTileEffectsToUnit(Unit unit)
    {
        if (unit == null || unit.IsDead) return;

        var effects = GetEffectsAt(unit.P, unit.Q);
        foreach (var effect in effects)
        {
            if (effect.IsExpired) continue;
            if (effect.OwnerPlayer == unit.OwnerPlayer) continue;

            switch (effect.Type)
            {
                case TileEffectType.Burning:
                case TileEffectType.Poison:
                case TileEffectType.Corrupted:
                case TileEffectType.AshCloud:
                    unit.TakeDamage(effect.DamagePerTurn);
                    break;
                case TileEffectType.Frost:
                case TileEffectType.Rooted:
                case TileEffectType.Entangled:
                    break;
            }
        }

        // Healing/Seeded effects apply to owner's units
        foreach (var effect in effects)
        {
            if (effect.IsExpired) continue;
            if (effect.OwnerPlayer != unit.OwnerPlayer) continue;

            if (effect.Type == TileEffectType.Healing || effect.Type == TileEffectType.Seeded)
                unit.Heal(effect.DamagePerTurn);
        }
    }

    public bool IsRooted(int p, int q, int unitOwner)
    {
        var effects = GetEffectsAt(p, q);
        foreach (var e in effects)
        {
            if (e.Type == TileEffectType.Rooted && e.OwnerPlayer != unitOwner && !e.IsExpired)
                return true;
        }
        return false;
    }

    public bool IsFrosted(int p, int q, int unitOwner)
    {
        var effects = GetEffectsAt(p, q);
        foreach (var e in effects)
        {
            if (e.Type == TileEffectType.Frost && e.OwnerPlayer != unitOwner && !e.IsExpired)
                return true;
        }
        return false;
    }

    public void Clear()
    {
        _tileEffects.Clear();
    }
}
