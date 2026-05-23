using System.Collections.Generic;
using UnityEngine;

// 1. Corrupted Crest — Apply corrupted on tile; if already corrupted, spread to adjacent
public class CorruptedCrestBehavior : SkillBehavior
{
    public override string Id => "skb_corrupted_crest";
    public override string Name => "Corrupted Crest";
    public override int DefaultCooldown => 3;
    public override bool IsOneTime => false;

    private static readonly int[,] Dirs = { { 1, 0 }, { 1, -1 }, { 0, -1 }, { -1, 0 }, { -1, 1 }, { 0, 1 } };

    public override void Execute(Unit caster, Unit target, Tile targetTile, BoardController board, EffectController effects)
    {
        if (targetTile == null) return;

        if (effects.HasEffect(targetTile.P, targetTile.Q, TileEffectType.Corrupted))
        {
            for (int i = 0; i < 6; i++)
            {
                int ap = targetTile.P + Dirs[i, 0];
                int aq = targetTile.Q + Dirs[i, 1];
                effects.ApplyEffect(ap, aq, new TileEffect(TileEffectType.Corrupted, 3, caster.OwnerPlayer, 5));
            }
        }
        else
        {
            effects.ApplyEffect(targetTile.P, targetTile.Q, new TileEffect(TileEffectType.Corrupted, 3, caster.OwnerPlayer, 5));
        }
    }

    public override float EvaluateValue(Unit caster, Unit target, Tile targetTile, BoardController board, EffectController effects)
    {
        if (targetTile != null && effects.HasEffect(targetTile.P, targetTile.Q, TileEffectType.Corrupted))
            return 30f;
        return 15f;
    }
}

// 2. Graveclaw Frenzy — 2 normal attacks on 1 enemy
public class GraveclawFrenzyBehavior : SkillBehavior
{
    public override string Id => "skb_graveclaw_frenzy";
    public override string Name => "Graveclaw Frenzy";
    public override int DefaultCooldown => 3;
    public override bool IsOneTime => false;

    public override void Execute(Unit caster, Unit target, Tile targetTile, BoardController board, EffectController effects)
    {
        if (target == null || target.IsDead) return;
        if (target.OwnerPlayer == caster.OwnerPlayer) return;
        target.TakeDamage(caster.Attack);
        if (!target.IsDead)
            target.TakeDamage(caster.Attack);
    }

    public override float EvaluateValue(Unit caster, Unit target, Tile targetTile, BoardController board, EffectController effects)
    {
        if (target == null) return 0f;
        return caster.Attack * 2f;
    }
}

// 3. Death's Toll — 2 attacks to all enemies within 2 hex range
public class DeathsTollBehavior : SkillBehavior
{
    public override string Id => "skb_deaths_toll";
    public override string Name => "Death's Toll";
    public override int DefaultCooldown => 4;
    public override bool IsOneTime => false;

    public override void Execute(Unit caster, Unit target, Tile targetTile, BoardController board, EffectController effects)
    {
        int size = board.Size;
        for (int p = -size; p <= size; p++)
        {
            int qMin = Mathf.Max(-size, -p - size);
            int qMax = Mathf.Min(size, -p + size);
            for (int q = qMin; q <= qMax; q++)
            {
                if (HexDistance(caster.P, caster.Q, p, q) > 2) continue;
                int r = -p - q;
                Tile tile = board.GetTile(p, q, r);
                if (tile == null) continue;
                var u = tile.OccupiedBy;
                if (u != null && u.OwnerPlayer != caster.OwnerPlayer && !u.IsDead)
                {
                    u.TakeDamage(caster.Attack);
                    if (!u.IsDead)
                        u.TakeDamage(caster.Attack);
                }
            }
        }
    }

    public override float EvaluateValue(Unit caster, Unit target, Tile targetTile, BoardController board, EffectController effects)
    {
        if (target == null) return 0f;
        return caster.Attack * 2f * 1.5f;
    }
}

// 4. Bloom — Heal self and adjacent allies for 10 HP
public class BloomBehavior : SkillBehavior
{
    public override string Id => "skb_bloom";
    public override string Name => "Bloom";
    public override int DefaultCooldown => 2;
    public override bool IsOneTime => false;

    private static readonly int[,] Dirs = { { 1, 0 }, { 1, -1 }, { 0, -1 }, { -1, 0 }, { -1, 1 }, { 0, 1 } };

    public override void Execute(Unit caster, Unit target, Tile targetTile, BoardController board, EffectController effects)
    {
        caster.Heal(10);
        for (int i = 0; i < 6; i++)
        {
            int ap = caster.P + Dirs[i, 0];
            int aq = caster.Q + Dirs[i, 1];
            int ar = -ap - aq;
            Tile adj = board.GetTile(ap, aq, ar);
            if (adj != null && adj.OccupiedBy != null && adj.OccupiedBy.OwnerPlayer == caster.OwnerPlayer)
                adj.OccupiedBy.Heal(10);
        }
    }

    public override float EvaluateValue(Unit caster, Unit target, Tile targetTile, BoardController board, EffectController effects)
    {
        int missing = caster.MaxHP - caster.HP;
        return Mathf.Min(missing, 10) * 1.5f;
    }
}

// 5. Root Overgrow — Apply rooted to 1 adjacent enemy for 3 turns
public class RootOvergrowBehavior : SkillBehavior
{
    public override string Id => "skb_root_overgrow";
    public override string Name => "Root Overgrow";
    public override int DefaultCooldown => 3;
    public override bool IsOneTime => false;

    public override void Execute(Unit caster, Unit target, Tile targetTile, BoardController board, EffectController effects)
    {
        if (target == null || target.IsDead) return;
        if (target.OwnerPlayer == caster.OwnerPlayer) return;
        effects.ApplyEffect(target.P, target.Q, new TileEffect(TileEffectType.Rooted, 3, caster.OwnerPlayer));
    }

    public override float EvaluateValue(Unit caster, Unit target, Tile targetTile, BoardController board, EffectController effects)
    {
        if (target == null) return 0f;
        return 20f;
    }
}

// 6. Life Sapping Thorn — 1 normal attack; if caster stands on seeded tile, heal 20
public class LifeSappingThornBehavior : SkillBehavior
{
    public override string Id => "skb_life_sapping_thorn";
    public override string Name => "Life Sapping Thorn";
    public override int DefaultCooldown => 2;
    public override bool IsOneTime => false;

    public override void Execute(Unit caster, Unit target, Tile targetTile, BoardController board, EffectController effects)
    {
        if (target == null || target.IsDead) return;
        if (target.OwnerPlayer == caster.OwnerPlayer) return;
        target.TakeDamage(caster.Attack);

        if (effects.HasEffect(caster.P, caster.Q, TileEffectType.Seeded))
            caster.Heal(20);
    }

    public override float EvaluateValue(Unit caster, Unit target, Tile targetTile, BoardController board, EffectController effects)
    {
        if (target == null) return 0f;
        float val = caster.Attack;
        if (effects.HasEffect(caster.P, caster.Q, TileEffectType.Seeded))
            val += 20f;
        return val;
    }
}

// 7. Mastery of Flame — Inflict burning on enemies; if already burning, apply melting (extra damage)
public class MasteryOfFlameBehavior : SkillBehavior
{
    public override string Id => "skb_mastery_of_flame";
    public override string Name => "Mastery of Flame";
    public override int DefaultCooldown => 4;
    public override bool IsOneTime => false;

    public override void Execute(Unit caster, Unit target, Tile targetTile, BoardController board, EffectController effects)
    {
        if (target == null || target.IsDead) return;
        if (target.OwnerPlayer == caster.OwnerPlayer) return;

        if (effects.HasEffect(target.P, target.Q, TileEffectType.Burning))
        {
            // Already burning → apply heavy damage (melting)
            target.TakeDamage(caster.Attack * 2);
            effects.ApplyEffect(target.P, target.Q, new TileEffect(TileEffectType.Burning, 99, caster.OwnerPlayer, 10));
        }
        else
        {
            effects.ApplyEffect(target.P, target.Q, new TileEffect(TileEffectType.Burning, 3, caster.OwnerPlayer, 6));
        }
    }

    public override float EvaluateValue(Unit caster, Unit target, Tile targetTile, BoardController board, EffectController effects)
    {
        if (target == null) return 0f;
        if (effects.HasEffect(target.P, target.Q, TileEffectType.Burning))
            return caster.Attack * 2f + 30f;
        return 18f;
    }
}

// 8. Severed Tail — 30 damage at long range; caster loses 10 MaxHP
public class SeveredTailBehavior : SkillBehavior
{
    public override string Id => "skb_severed_tail";
    public override string Name => "Severed Tail";
    public override int DefaultCooldown => 5;
    public override bool IsOneTime => false;

    public override void Execute(Unit caster, Unit target, Tile targetTile, BoardController board, EffectController effects)
    {
        if (target == null || target.IsDead) return;
        if (target.OwnerPlayer == caster.OwnerPlayer) return;
        target.TakeDamage(30);

        int newMax = Mathf.Max(10, caster.MaxHP - 10);
        int delta = caster.MaxHP - newMax;
        caster.MaxHP = newMax;
        caster.HP = Mathf.Min(caster.HP, caster.MaxHP);
    }

    public override float EvaluateValue(Unit caster, Unit target, Tile targetTile, BoardController board, EffectController effects)
    {
        if (target == null || target.OwnerPlayer == caster.OwnerPlayer) return 0f;
        float val = 30f;
        if (caster.MaxHP <= 20) val -= 15f; // Penalty when already low
        return val;
    }
}

// 9. Molten Dive — Jump to target tile, deal damage to all adjacent enemies
public class MoltenDiveBehavior : SkillBehavior
{
    public override string Id => "skb_molten_dive";
    public override string Name => "Molten Dive";
    public override int DefaultCooldown => 4;
    public override bool IsOneTime => false;

    private static readonly int[,] Dirs = { { 1, 0 }, { 1, -1 }, { 0, -1 }, { -1, 0 }, { -1, 1 }, { 0, 1 } };

    public override void Execute(Unit caster, Unit target, Tile targetTile, BoardController board, EffectController effects)
    {
        if (targetTile == null) return;
        if (targetTile.OccupiedBy != null && targetTile.OccupiedBy != caster) return;

        caster.PlaceOnTile(targetTile);

        int damage = 15;
        for (int i = 0; i < 6; i++)
        {
            int ap = targetTile.P + Dirs[i, 0];
            int aq = targetTile.Q + Dirs[i, 1];
            int ar = -ap - aq;
            Tile adj = board.GetTile(ap, aq, ar);
            if (adj != null && adj.OccupiedBy != null
                && adj.OccupiedBy.OwnerPlayer != caster.OwnerPlayer
                && !adj.OccupiedBy.IsDead)
            {
                adj.OccupiedBy.TakeDamage(damage);
            }
        }
    }

    public override float EvaluateValue(Unit caster, Unit target, Tile targetTile, BoardController board, EffectController effects)
    {
        if (target == null) return 10f;
        return 15f + 10f; // damage + mobility
    }
}

// 10. Curse of Ash — Rain ash cloud on target tile and 1 hex area around it
public class CurseOfAshBehavior : SkillBehavior
{
    public override string Id => "skb_curse_of_ash";
    public override string Name => "Curse of Ash";
    public override int DefaultCooldown => 4;
    public override bool IsOneTime => false;

    private static readonly int[,] Dirs = { { 1, 0 }, { 1, -1 }, { 0, -1 }, { -1, 0 }, { -1, 1 }, { 0, 1 } };

    public override void Execute(Unit caster, Unit target, Tile targetTile, BoardController board, EffectController effects)
    {
        if (targetTile == null) return;

        effects.ApplyEffect(targetTile.P, targetTile.Q, new TileEffect(TileEffectType.AshCloud, 7, caster.OwnerPlayer, 7));
        for (int i = 0; i < 6; i++)
        {
            int ap = targetTile.P + Dirs[i, 0];
            int aq = targetTile.Q + Dirs[i, 1];
            effects.ApplyEffect(ap, aq, new TileEffect(TileEffectType.AshCloud, 7, caster.OwnerPlayer, 7));
        }
    }

    public override float EvaluateValue(Unit caster, Unit target, Tile targetTile, BoardController board, EffectController effects)
    {
        if (target != null) return 49f; // 7 turns * 7 dmg
        return 25f;
    }
}
