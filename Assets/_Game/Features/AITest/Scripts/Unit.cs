using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct HexOffset
{
    public int P;
    public int Q;

    public int R => -P - Q;

    public HexOffset(int p, int q)
    {
        P = p;
        Q = q;
    }
}

[Serializable]
public class HexPattern
{
    public List<HexOffset> Offsets = new();

    public List<Vector3Int> GetWorldCoords(int originP, int originQ)
    {
        var result = new List<Vector3Int>();
        foreach (var offset in Offsets)
        {
            int p = originP + offset.P;
            int q = originQ + offset.Q;
            int r = -p - q;
            result.Add(new Vector3Int(p, q, r));
        }
        return result;
    }
}

[Serializable]
public class Skill
{
    public string Name;
    public HexPattern ChoosePattern = new();
    public HexPattern ApplyPattern = new();
    public int Cooldown;
    public int CurrentCooldown;
    public bool OneTime;
    public bool Used;

    [NonSerialized] public SkillBehavior Behavior;

    public bool IsReady => CurrentCooldown <= 0 && !(OneTime && Used);

    public void TickCooldown()
    {
        if (CurrentCooldown > 0)
            CurrentCooldown--;
    }

    public void Use()
    {
        CurrentCooldown = Cooldown;
        if (OneTime)
            Used = true;
    }

    public List<Vector3Int> GetValidTargets(int unitP, int unitQ)
    {
        return ChoosePattern.GetWorldCoords(unitP, unitQ);
    }

    public List<Vector3Int> GetAffectedTiles(int targetP, int targetQ)
    {
        return ApplyPattern.GetWorldCoords(targetP, targetQ);
    }
}

public class Unit : MonoBehaviour
{
    public int OwnerPlayer;
    public int HP;
    public int MaxHP;
    public int Attack;
    public int Speed;
    public int DeathAnchor;
    public bool IsDead { get; private set; }

    [SerializeField]
    private HexPattern _movePattern = new()
    {
        Offsets = new()
        {
            new HexOffset(1, 0), new HexOffset(-1, 0),
            new HexOffset(0, 1), new HexOffset(0, -1),
            new HexOffset(1, -1), new HexOffset(-1, 1)
        }
    };
    [SerializeField]
    private HexPattern _attackPattern = new()
    {
        Offsets = new()
        {
            new HexOffset(1, 0), new HexOffset(-1, 0),
            new HexOffset(0, 1), new HexOffset(0, -1),
            new HexOffset(1, -1), new HexOffset(-1, 1)
        }
    };
    [SerializeField] private Skill[] _skills = new Skill[2];

    private int _p, _q, _r;
    private BoardController _board;

    public Tile CurrentTile { get; private set; }
    public int P => _p;
    public int Q => _q;
    public int R => _r;

    public HexPattern MovePattern => _movePattern;
    public HexPattern AttackPattern => _attackPattern;
    public Skill[] Skills => _skills;

    public void Init(BoardController board, int p, int q)
    {
        _board = board;
        SetCoordinate(p, q);
    }

    public void SetCoordinate(int p, int q)
    {
        _p = p;
        _q = q;
        _r = -p - q;

        if (CurrentTile != null)
            CurrentTile.OccupiedBy = null;

        Tile tile = _board.GetTile(_p, _q, _r);
        if (tile != null)
        {
            CurrentTile = tile;
            tile.OccupiedBy = this;
            RectTransform unitRect = GetComponent<RectTransform>();
            RectTransform tileRect = tile.GetComponent<RectTransform>();
            unitRect.anchoredPosition = tileRect.anchoredPosition;
        }
    }

    public void PlaceOnTile(Tile tile)
    {
        SetCoordinate(tile.P, tile.Q);
    }

    public void TakeDamage(int damage)
    {
        HP -= damage;
        Debug.Log($"Unit (Player {OwnerPlayer}) takes {damage} damage, HP: {HP}/{MaxHP}");
        if (HP <= 0)
        {
            HP = 0;
            Die();
        }
    }

    public void Heal(int amount)
    {
        HP = Mathf.Min(HP + amount, MaxHP);
    }

    public void Die()
    {
        IsDead = true;
        if (CurrentTile != null)
            CurrentTile.OccupiedBy = null;
        CurrentTile = null;
        Debug.Log($"Unit (Player {OwnerPlayer}) died!");
        gameObject.SetActive(false);
    }

    public List<Vector3Int> GetMoveTiles()
    {
        if (CurrentTile == null) return new List<Vector3Int>();
        return _movePattern.GetWorldCoords(CurrentTile.P, CurrentTile.Q);
    }

    public List<Vector3Int> GetAttackTiles()
    {
        if (CurrentTile == null) return new List<Vector3Int>();
        return _attackPattern.GetWorldCoords(CurrentTile.P, CurrentTile.Q);
    }

    public List<Vector3Int> GetSkillTargets(int skillIndex)
    {
        if (skillIndex < 0 || skillIndex >= _skills.Length) return new List<Vector3Int>();
        if (CurrentTile == null) return new List<Vector3Int>();
        var skill = _skills[skillIndex];
        if (skill == null || !skill.IsReady) return new List<Vector3Int>();
        return skill.GetValidTargets(CurrentTile.P, CurrentTile.Q);
    }

    public List<Vector3Int> GetSkillApplyArea(int skillIndex, int targetP, int targetQ)
    {
        if (skillIndex < 0 || skillIndex >= _skills.Length) return new List<Vector3Int>();
        var skill = _skills[skillIndex];
        if (skill == null) return new List<Vector3Int>();
        return skill.GetAffectedTiles(targetP, targetQ);
    }

    public void UseSkill(int skillIndex)
    {
        if (skillIndex < 0 || skillIndex >= _skills.Length) return;
        _skills[skillIndex]?.Use();
    }

    public void TickCooldowns()
    {
        foreach (var skill in _skills)
            skill?.TickCooldown();
    }
}
